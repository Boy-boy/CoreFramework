using System;
using Core.Configuration.Storage;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.SqlServer
{
    public class SqlServerConfigurationStorage : ConfigurationStorageBase
    {
        private readonly SqlServerConfigurationSource _source;

        public SqlServerConfigurationStorage(SqlServerConfigurationSource source)
        {
            _source = source;
        }

        public override async Task<int> AddAsync(ConfigurationMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return 0;
            object[] sqlParams =
            {
                new SqlParameter("@Id", message.Id),
                new SqlParameter("@Key", message.Key),
                new SqlParameter("@Value", message.Value),
                new SqlParameter("@Description", message.Description),
                new SqlParameter("@CreateTime", message.CreateTime),
                new SqlParameter("@UpdateTime", message.UpdateTime),
                new SqlParameter("@UtcTime", message.UtcTime),
                new SqlParameter("@IsDeleted", message.IsDeleted)
            };

            var sql = $@"INSERT INTO {GetTableName()} ([Id],[Key],[Value],[Description],[CreateTime],[UpdateTime],[UtcTime],[IsDeleted]) 
VALUES (@Id,@Key,@Value,@Description,@CreateTime,@UpdateTime,@UtcTime,@IsDeleted);";

            using var connection = new SqlConnection(_source.DbConnectionStr);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);

            if (executeRows <= 0) return executeRows;
            var events = new List<Event>
            {
                new Event(EventType.Add, message.Key, message.Value)
            };
            InvokeEvent(events);
            return await Task.FromResult(executeRows);
        }

        public override async Task<int> UpdateAsync(ConfigurationMessage message, CancellationToken cancellationToken = default)
        {
            object[] sqlParams =
            {
                new SqlParameter("@Id", message.Id),
                new SqlParameter("@Key", message.Key),
                new SqlParameter("@Value", message.Value),
                new SqlParameter("@Description", message.Description),
                new SqlParameter("@UpdateTime", message.UpdateTime)
            };
            var sql = $@"UPDATE {GetTableName()} 
SET [Key]=@Key,[Value]=@Value,[Description]=@Description,[UpdateTime]=@UpdateTime
WHERE [Id]=@Id";

            using var connection = new SqlConnection(_source.DbConnectionStr);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);

            if (executeRows <= 0) return executeRows;
            var events = new List<Event>
            {
                new Event(EventType.Update, message.Key, message.Value)
            };
            InvokeEvent(events);
            return await Task.FromResult(executeRows);
        }

        public override async Task<int> DeletedAsync(string id, CancellationToken cancellationToken = default)
        {
            var messages = await GetAsync(id, null, cancellationToken);
            object[] sqlParams =
            {
                new SqlParameter("@Id", id),
                new SqlParameter("@IsDeleted", true),
                new SqlParameter("@UpdateTime", DateTime.Now)
            };
            var sql = $@"UPDATE {GetTableName()} SET [IsDeleted]=@IsDeleted,[UpdateTime]=@UpdateTime WHERE [Id]=@Id";

            using var connection = new SqlConnection(_source.DbConnectionStr);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);

            if (executeRows <= 0) return executeRows;

            var message = messages.FirstOrDefault(m => m.Id == id);
            if (message == null) return executeRows;
            var events = new List<Event>
            {
                new Event(EventType.Deleted, message.Key, message.Value)
            };
            InvokeEvent(events);
            return await Task.FromResult(executeRows);
        }

        public override async Task<List<ConfigurationMessage>> GetAsync(string id, string key, CancellationToken cancellationToken = default)
        {
            var result = new List<ConfigurationMessage>();
            if (cancellationToken.IsCancellationRequested) return result;
            var sqlParams = new List<object>()
            {
                new SqlParameter("@IsDeleted", false)
            };
            var sqlWhere = new StringBuilder("WHERE [IsDeleted]=@IsDeleted ");
            if (!string.IsNullOrEmpty(id))
            {
                sqlWhere = sqlWhere.Append("AND [Id]=@Id ");
                sqlParams.Add(new SqlParameter("@Id", id));
            }
            if (!string.IsNullOrEmpty(key))
            {
                sqlWhere = sqlWhere.Append("AND [Key] LIKE @Key ");
                sqlParams.Add(new SqlParameter("@Key", key));
            }

            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere}";
            using var connection = new SqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result.Add(new ConfigurationMessage
                {
                    Id = reader["Id"].ToString(),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString()),
                    IsDeleted = Convert.ToBoolean(reader["IsDeleted"].ToString())
                });
            }
            return await Task.FromResult(result);
        }

        public override async Task<List<ConfigurationMessage>> GetAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<ConfigurationMessage>();
            if (cancellationToken.IsCancellationRequested) return result;
            object[] sqlParams =
            {
                new SqlParameter("@IsDeleted", false)
            };
            var sql = $@"SELECT * FROM {GetTableName()} WHERE [IsDeleted]=@IsDeleted";
            using var connection = new SqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams);
            while (reader.Read())
            {
                result.Add(new ConfigurationMessage
                {
                    Id = reader["Id"].ToString(),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString()),
                    IsDeleted = Convert.ToBoolean(reader["IsDeleted"].ToString())
                });
            }
            return await Task.FromResult(result);
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var sql = $@"
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = '{_source.DbSchema}')
BEGIN
	EXEC('CREATE SCHEMA [{_source.DbSchema}]')
END;

IF OBJECT_ID(N'{GetTableName()}',N'U') IS NULL
BEGIN
CREATE TABLE {GetTableName()}(
	[Id] [varchar](200) NOT NULL PRIMARY KEY,
    [Key] [varchar](200) NOT NULL UNIQUE,
	[Value] [text] NOT NULL,
	[Description] [text] NULL,
	[CreateTime] [datetime2](6) NOT NULL,
    [UpdateTime] [datetime2](6) NOT NULL,
	[UtcTime] [datetime2](6) NOT NULL,
    [IsDeleted] [bit] NOT NULL
	)
END;";
            using (var connection = new SqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql);

            await Task.CompletedTask;
        }

        public virtual string GetTableName()
        {
            return $"{_source.DbSchema}.{_source.TableName}";
        }
    }
}
