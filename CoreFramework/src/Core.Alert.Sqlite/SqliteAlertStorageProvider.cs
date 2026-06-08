using System.Text.Json;
using System.Threading;
using Core.Alert;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Core.Alert.Sqlite
{
    /// <summary>
    /// SQLite 告警状态存储实现。
    /// 适用于单机部署且希望跨进程重启保留告警状态的场景。
    /// </summary>
    public sealed class SqliteAlertStorageProvider : IAlertStorageProvider
    {
        private const string TableName = "alert_escalation_state";

        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = false,
        };

        private readonly IOptionsMonitor<AlertSqliteStorageOptions> _options;
        private readonly SemaphoreSlim _initGate = new(1, 1);
        private volatile bool _initialized;

        public SqliteAlertStorageProvider(IOptionsMonitor<AlertSqliteStorageOptions> options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<AlertSessionState> GetAsync(string sessionKey)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var connection = await OpenAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT state_json FROM " + TableName + " WHERE session_key = $key LIMIT 1;";
            command.Parameters.AddWithValue("$key", sessionKey);

            var raw = await command.ExecuteScalarAsync().ConfigureAwait(false);
            if (raw == null || raw is DBNull)
            {
                return null;
            }

            return JsonSerializer.Deserialize<AlertSessionState>((string)raw, JsonSerializerOptions);
        }

        public async Task SetAsync(string sessionKey, AlertSessionState state)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var json = JsonSerializer.Serialize(state, JsonSerializerOptions);
            var nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await using var connection = await OpenAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO " + TableName + @" (session_key, state_json, updated_at)
VALUES ($key, $json, $updatedAt)
ON CONFLICT(session_key) DO UPDATE SET
    state_json = excluded.state_json,
    updated_at = excluded.updated_at;";
            command.Parameters.AddWithValue("$key", sessionKey);
            command.Parameters.AddWithValue("$json", json);
            command.Parameters.AddWithValue("$updatedAt", nowEpoch);

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        public async Task RemoveAsync(string sessionKey)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            await using var connection = await OpenAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM " + TableName + " WHERE session_key = $key;";
            command.Parameters.AddWithValue("$key", sessionKey);

            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task<SqliteConnection> OpenAsync()
        {
            var connectionString = _options.CurrentValue.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Alert sqlite storage requires a non-empty connection string.");
            }

            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync().ConfigureAwait(false);

            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA busy_timeout = 5000;";
            await pragma.ExecuteNonQueryAsync().ConfigureAwait(false);

            return connection;
        }

        /// <summary>
        /// 首次使用时建表并启用 WAL。
        /// </summary>
        private async Task EnsureInitializedAsync()
        {
            if (_initialized)
            {
                return;
            }

            await _initGate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                await using var connection = await OpenAsync().ConfigureAwait(false);

                await using (var ddl = connection.CreateCommand())
                {
                    ddl.CommandText = @"
CREATE TABLE IF NOT EXISTS " + TableName + @" (
    session_key TEXT PRIMARY KEY,
    state_json  TEXT NOT NULL,
    updated_at  INTEGER NOT NULL
);";
                    await ddl.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                await using (var wal = connection.CreateCommand())
                {
                    wal.CommandText = "PRAGMA journal_mode = WAL;";
                    await wal.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                _initialized = true;
            }
            finally
            {
                _initGate.Release();
            }
        }
    }
}
