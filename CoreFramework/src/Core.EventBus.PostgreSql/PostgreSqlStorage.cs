using Core.EventBus.Storage;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace Core.EventBus.PostgreSql
{
    public class PostgreSqlStorage : IStorage
    {
        private readonly IOptions<EventBusPostgreSqlOptions> _options;
        private readonly ILogger<PostgreSqlStorage> _logger;

        public PostgreSqlStorage(
            IOptions<EventBusPostgreSqlOptions> options,
            ILogger<PostgreSqlStorage> logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// 初始化消息表
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var sql = $@"
CREATE SCHEMA IF NOT EXISTS {_options.Value.DbSchema};

CREATE TABLE IF NOT EXISTS {GetTableName()} (
  Id VARCHAR(200) NOT NULL,
  Version INT NOT NULL,
  MessageType TEXT NOT NULL,
  MessageData TEXT NOT NULL,
  CreateTime timestamp(6) NOT NULL,
  UtcTime timestamp(6) NOT NULL,
  PRIMARY KEY (Id)
);";

            using (var connection = new NpgsqlConnection(_options.Value.DbConnectionStr))
                connection.ExecuteNonQuery(sql);

            _logger.LogInformation($"initial message table successfully. table name is [{GetTableName()}]");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 新增消息记录
        /// </summary>
        /// <param name="message"></param>
        /// <param name="dbTransaction"></param>
        public void StoreMessage(MediumMessage message, object dbTransaction = null)
        {
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", message.Id),
                new NpgsqlParameter("@Version", message.Version),
                new NpgsqlParameter("@MessageType", message.MessageType),
                new NpgsqlParameter("@MessageData", message.MessageData),
                new NpgsqlParameter("@CreateTime", message.CreateTime),
                new NpgsqlParameter("@UtcTime", message.UtcTime)
            };

            var sql = $@"INSERT INTO {GetTableName()} (Id,Version,MessageType,MessageData,CreateTime,UtcTime) 
VALUES (@id,@Version,@MessageType,@MessageData,@CreateTime,@UtcTime);";

            if (dbTransaction == null)
            {
                using var connection = new NpgsqlConnection(_options.Value.DbConnectionStr);
                connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
                _logger.LogInformation($"insert message in {GetTableName()} table successfully. messageId={message.Id}");
            }
            else
            {
                IDbTransaction dbTrans = null;
                switch (dbTransaction)
                {
                    case IDbTransaction dbTran:
                        dbTrans = dbTran;
                        break;
                    case IDbContextTransaction dbContextTransaction:
                        dbTrans = dbContextTransaction.GetDbTransaction();
                        break;
                }
                var conn = dbTrans?.Connection;
                conn?.ExecuteNonQuery(sql, dbTrans, sqlParams);
            }
        }

        public virtual string GetTableName()
        {
            return $"{_options.Value.DbSchema}.{_options.Value.TableName}";
        }
    }
}
