using Core.EventBus.Storage;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using System.Collections.Generic;
using System;

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
  Id uuid NOT NULL,
  Version INT NOT NULL,
  AssemblyName TEXT NOT NULL,
  MessageName TEXT NOT NULL,
  MessageData TEXT NOT NULL,
  CreateTime timestamp(6) NOT NULL,
  UtcTime timestamp(6) NOT NULL,
  PRIMARY KEY (Id)
);";

            await using var connection = new NpgsqlConnection(_options.Value.DbConnection);
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
                new NpgsqlParameter("@AssemblyName", message.AssemblyName),
                new NpgsqlParameter("@MessageName", message.MessageName),
                new NpgsqlParameter("@MessageData", message.MessageData),
                new NpgsqlParameter("@CreateTime", message.CreateTime),
                new NpgsqlParameter("@UtcTime", message.UtcTime)
            };

            var sql = $@"INSERT INTO {GetTableName()} (Id,Version,AssemblyName,MessageName,MessageData,CreateTime,UtcTime) 
VALUES (@Id,@Version,@AssemblyName,@MessageName,@MessageData,@CreateTime,@UtcTime);";

            if (dbTransaction == null)
            {
                using var connection = new NpgsqlConnection(_options.Value.DbConnection);
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

        public List<MediumMessage> GetMessages(int maxCount)
        {
            object[] sqlParams =
            {
                new NpgsqlParameter("@Limit",maxCount),
            };

            var sql = $@"SELECT * FROM {GetTableName()} ORDER BY UtcTime LIMIT @Limit;";
            using var connection = new NpgsqlConnection(_options.Value.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams: sqlParams);
            var list = new List<MediumMessage>();
            while (reader.Read())
            {
                list.Add(new MediumMessage
                {
                    Id = Guid.Parse(reader["Id"].ToString() ?? string.Empty),
                    Version = Convert.ToInt32(reader["Version"].ToString()),
                    AssemblyName = reader["AssemblyName"].ToString(),
                    MessageName = reader["MessageName"].ToString(),
                    MessageData = reader["MessageData"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString())
                });
            }
            return list;
        }

        public void Delete(Guid id)
        {
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id",id),
            };

            var sql = $@"DELETE FROM {GetTableName()} WHERE Id=@Id;";
            using var connection = new NpgsqlConnection(_options.Value.DbConnection);
            connection.ExecuteNonQuery(sql, sqlParams: sqlParams);
        }

        private string GetTableName()
        {
            return $"{_options.Value.DbSchema}.{_options.Value.DbTable}";
        }
    }
}
