using Core.EventBus.Storage;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Core.EventBus.Mysql
{
    public class MysqlStorage : IStorage
    {
        private readonly IOptions<EventBusMysqlOptions> _options;
        private readonly ILogger<MysqlStorage> _logger;

        public MysqlStorage(
            IOptions<EventBusMysqlOptions> options,
            ILogger<MysqlStorage> logger)
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
CREATE TABLE IF NOT EXISTS {_options.Value.TableName} (
  `Id` VARCHAR(200) NOT NULL,
  `Version` INT NOT NULL,
  `MessageType` TEXT NOT NULL,
  `MessageData` TEXT NOT NULL,
  `CreateTime` DATETIME(6) NOT NULL,
  `UtcTime` DATETIME(6) NOT NULL,
   PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            using (var connection = new MySqlConnection(_options.Value.DbConnectionStr))
                connection.ExecuteNonQuery(sql);

            _logger.LogInformation($"initial message table successfully. table name is [{_options.Value.TableName}]");
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
                new MySqlParameter("@Id", message.Id),
                new MySqlParameter("@Version", message.Version),
                new MySqlParameter("@MessageType", message.MessageType),
                new MySqlParameter("@MessageData", message.MessageData),
                new MySqlParameter("@CreateTime", message.CreateTime),
                new MySqlParameter("@UtcTime", message.UtcTime)
            };

            var sql = $@"INSERT INTO {_options.Value.TableName} (`Id`,`Version`,`MessageType`,`MessageData`,`CreateTime`,`UtcTime`) 
VALUES (@id,@Version,@MessageType,@MessageData,@CreateTime,@UtcTime);";

            if (dbTransaction == null)
            {
                using var connection = new MySqlConnection(_options.Value.DbConnectionStr);
                connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
                _logger.LogInformation($"insert message in {_options.Value.TableName} table successfully. messageId={message.Id}");
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
    }
}
