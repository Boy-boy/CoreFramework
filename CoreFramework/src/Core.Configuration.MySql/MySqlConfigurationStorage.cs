using Core.Configuration.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace Core.Configuration.MySql
{
    public class MySqlConfigurationStorage : ConfigurationStorageBase
    {
        private readonly MySqlConfigurationSource _source;

        public MySqlConfigurationStorage(MySqlConfigurationSource source)
        {
            _source = source;
        }

        public override async Task AddAsync(ConfigurationMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            object[] sqlParams =
            {
                new MySqlParameter("@Id", message.Id),
                new MySqlParameter("@Key", message.Key),
                new MySqlParameter("@Value", message.Value),
                new MySqlParameter("@Description", message.Description),
                new MySqlParameter("@CreateTime", message.CreateTime),
                new MySqlParameter("@UpdateTime", message.UpdateTime),
                new MySqlParameter("@UtcTime", message.UtcTime),
                new MySqlParameter("@IsDeleted", message.IsDeleted)
            };

            var sql = $@"INSERT INTO {GetTableName()} (`Id`,`Key`,`Value`,`Description`,`CreateTime`,`UpdateTime`,`UtcTime`,`IsDeleted`) 
VALUES (@Id,@Key,@Value,@Description,@CreateTime,@UpdateTime,@UtcTime,@IsDeleted);";

            using (var connection = new MySqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql, sqlParams);

            Events.Enqueue(new Event(EventType.Add, message.Key, message.Value));
            await Task.CompletedTask;
        }

        public override async Task UpdateAsync(ConfigurationMessage message, CancellationToken cancellationToken = default)
        {
            object[] sqlParams =
            {
                new MySqlParameter("@Id", message.Id),
                new MySqlParameter("@Key", message.Key),
                new MySqlParameter("@Value", message.Value),
                new MySqlParameter("@Description", message.Description),
                new MySqlParameter("@UpdateTime", message.UpdateTime)
            };
            var sql = $@"UPDATE {GetTableName()} 
SET `Key`=@Key,`Value`=@Value,`Description`=@Description,`UpdateTime`=@UpdateTime
WHERE `Id`=@Id";

            using (var connection = new MySqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql, sqlParams);

            Events.Enqueue(new Event(EventType.Update, message.Key, message.Value));
            await Task.CompletedTask;
        }

        public override async Task DeletedAsync(string id, CancellationToken cancellationToken = default)
        {
            var message = await GetAsync(id, cancellationToken);
            object[] sqlParams =
            {
                new MySqlParameter("@Id", id),
                new MySqlParameter("@IsDeleted", true),
                new MySqlParameter("@UpdateTime", DateTime.Now)
            };
            var sql = $@"UPDATE {GetTableName()} SET `IsDeleted`=@IsDeleted,`UpdateTime`=@UpdateTime  WHERE `Id`=@Id";

            using (var connection = new MySqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql, sqlParams);

            Events.Enqueue(new Event(EventType.Deleted, message.Key, message.Value));
            await Task.CompletedTask;
        }

        public override async Task<ConfigurationMessage> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            var result = new ConfigurationMessage();
            if (cancellationToken.IsCancellationRequested) return result;
            object[] sqlParams =
            {
                new MySqlParameter("@Id", id),
                new MySqlParameter("@IsDeleted", false)
            };
            var sql = $@"SELECT * FROM {GetTableName()} WHERE `Id`=@Id AND `IsDeleted`=@IsDeleted";
            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams);
            while (reader.Read())
            {
                result = new ConfigurationMessage
                {
                    Id = reader["Id"].ToString(),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString()),
                    IsDeleted = Convert.ToBoolean(reader["IsDeleted"].ToString())
                };
            }
            return await Task.FromResult(result);
        }

        public override async Task<List<ConfigurationMessage>> GetAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<ConfigurationMessage>();
            if (cancellationToken.IsCancellationRequested) return result;
            object[] sqlParams =
            {
                new MySqlParameter("@IsDeleted", false)
            };
            var sql = $@"SELECT * FROM {GetTableName()} WHERE `IsDeleted`=@IsDeleted";
            using var connection = new MySqlConnection(_source.DbConnectionStr);
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
CREATE TABLE IF NOT EXISTS {GetTableName()} (
  `Id` VARCHAR(200) NOT NULL,
  `Key` VARCHAR(200) NOT NULL,
  `Value` TEXT NOT NULL,
  `Description` TEXT NULL,
  `CreateTime` DATETIME(6) NOT NULL,
  `UpdateTime` DATETIME(6) NOT NULL,
  `UtcTime` DATETIME(6) NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL,
   PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";
            using (var connection = new MySqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql);

            await Task.CompletedTask;
        }

        public virtual string GetTableName()
        {
            return $"{_source.TableName}";
        }
    }
}
