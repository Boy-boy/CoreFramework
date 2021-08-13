using Core.Configuration.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public override async Task<int> AddAsync(CreateMessageModel message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return 0;

            var exist = await ExistAsync(message.Key, cancellationToken);
            if (exist)
                throw new Exception($"The configuration key for the {_source.Environment} environment already exists");

            var dateTime = DateTime.Now;
            var dateUtcTime = DateTime.UtcNow;
            object[] sqlParams =
            {
                new MySqlParameter("@Key", message.Key),
                new MySqlParameter("@Value", message.Value),
                new MySqlParameter("@Environment", _source.Environment),
                new MySqlParameter("@Description", message.Description),
                new MySqlParameter("@CreateTime", dateTime),
                new MySqlParameter("@UpdateTime", dateTime),
                new MySqlParameter("@UtcTime", dateUtcTime)
            };

            var sql = $@"INSERT INTO {GetTableName()} (`Key`,`Value`,`Environment`,`Description`,`CreateTime`,`UpdateTime`,`UtcTime`) 
VALUES (@Key,@Value,@Environment,@Description,@CreateTime,@UpdateTime,@UtcTime);";

            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);

            if (executeRows <= 0) return executeRows;

            var events = new ConcurrentQueue<Event>();
            events.Enqueue(new Event(EventType.Add, message.Key, message.Value));
            InvokeEvent(events);
            return await Task.FromResult(executeRows);
        }

        public override async Task<int> UpdateAsync(ModifyMessageModel message, CancellationToken cancellationToken = default)
        {
            var messageEntity = await GetAsync(message.Id, cancellationToken);
            if (messageEntity == null) return 0;

            if (messageEntity.Key != message.Key)
            {
                var exist = await ExistAsync(message.Key, cancellationToken);
                if (exist)
                    throw new Exception($"The configuration key for the {_source.Environment} environment already exists");
            }
            var dateTime = DateTime.Now;
            object[] sqlParams =
            {
                new MySqlParameter("@Id", message.Id),
                new MySqlParameter("@Key", message.Key),
                new MySqlParameter("@Value", message.Value),
                new MySqlParameter("@Description", message.Description),
                new MySqlParameter("@UpdateTime", dateTime)
            };
            var sql = $@"UPDATE {GetTableName()} 
SET `Key`=@Key,`Value`=@Value,`Description`=@Description,`UpdateTime`=@UpdateTime
WHERE `Id`=@Id";

            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);

            if (executeRows <= 0) return executeRows;

            var events = new ConcurrentQueue<Event>();
            events.Enqueue(new Event(EventType.Deleted, messageEntity.Key, messageEntity.Value));
            events.Enqueue(new Event(EventType.Add, message.Key, message.Value));
            InvokeEvent(events);
            return await Task.FromResult(executeRows);
        }

        public override async Task<int> DeletedAsync(int id, CancellationToken cancellationToken = default)
        {
            var message = await GetAsync(id, cancellationToken);
            if (message == null) return 0;

            object[] sqlParams =
            {
                new MySqlParameter("@Id", id)
            };
            var sql = $@"DELETE FROM {GetTableName()} WHERE `Id`=@Id";

            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);
            if (executeRows <= 0) return executeRows;

            var events = new ConcurrentQueue<Event>();
            events.Enqueue(new Event(EventType.Deleted, message.Key, message.Value));
            InvokeEvent(events);
            return await Task.FromResult(executeRows);
        }

        public override async Task<PageResultDto<ConfigurationMessage>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default)
        {
            var result = new PageResultDto<ConfigurationMessage>();
            if (cancellationToken.IsCancellationRequested) return result;
            var sqlParams = new List<object>
            {
                new MySqlParameter("@Environment", _source.Environment)
            };

            var sqlWhere = new StringBuilder("WHERE `Environment`=@Environment ");
            if (query.Id.HasValue)
            {
                sqlWhere = sqlWhere.Append("AND `Id`=@Id ");
                sqlParams.Add(new MySqlParameter("@Id", query.Id));
            }
            if (!string.IsNullOrEmpty(query.Key))
            {
                sqlWhere = sqlWhere.Append("AND `Key` LIKE CONCAT('%',@Key,'%') ");
                sqlParams.Add(new MySqlParameter("@Key", query.Key));
            }
            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere} ORDER BY `UpdateTime` DESC LIMIT {query.PageIndex * query.PageSize},{query.PageSize}";
            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            var list = new List<ConfigurationMessage>();
            while (reader.Read())
            {
                list.Add(new ConfigurationMessage
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString())
                });
            }

            result.Count = await GetCountAsync(cancellationToken);
            result.Items = list;
            return await Task.FromResult(result);
        }

        public override async Task<List<ConfigurationMessage>> GetAsync(string environment, CancellationToken cancellationToken = default)
        {
            var result = new List<ConfigurationMessage>();
            if (cancellationToken.IsCancellationRequested) return result;

            var sqlParams = new List<object>();
            var sqlWhere = new StringBuilder("WHERE 1=1 ");
            if (!string.IsNullOrEmpty(environment))
            {
                sqlWhere.Append("AND Environment=@Environment");
                sqlParams.Add(new MySqlParameter("@Environment", environment));
            }

            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere}";
            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result.Add(new ConfigurationMessage
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString())
                });
            }
            return await Task.FromResult(result);
        }

        public override async Task<ConfigurationMessage> GetAsync(int id, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            var result = new ConfigurationMessage();

            object[] sqlParams =
            {
                new MySqlParameter("@Id", id)
            };

            var sql = $@"SELECT * FROM {GetTableName()} WHERE `Id`=@Id";
            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result = new ConfigurationMessage
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString())
                };
            }
            return await Task.FromResult(result);
        }

        public override async Task<bool> ExistAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }
            object[] sqlParams =
             {
                new MySqlParameter("@Key", key),
                new MySqlParameter("@Environment", _source.Environment)
            };

            var sql = $"SELECT COUNT(1) AS count FROM {GetTableName()} WHERE `Key`=@Key AND `Environment`=@Environment";
            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams);
            var count = 0;
            while (reader.Read())
            {
                count += Convert.ToInt32(Convert.ToInt32(reader["count"]));
            }
            return await Task.FromResult(count > 0);
        }

        public override async Task<int> GetCountAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return 0;

            object[] sqlParams =
            {
                new MySqlParameter("@Environment", _source.Environment)
            };

            var sql = $"SELECT COUNT(1) AS count FROM {GetTableName()} WHERE `Environment`=@Environment";
            using var connection = new MySqlConnection(_source.DbConnectionStr);
            var reader = connection.ExecuteQuery(sql, sqlParams);
            var count = 0;
            while (reader.Read())
            {
                count += Convert.ToInt32(Convert.ToInt32(reader["count"]));
            }
            return await Task.FromResult(count);
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var sql = $@"
CREATE TABLE IF NOT EXISTS {GetTableName()} (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Key` VARCHAR(200) NOT NULL,
  `Value` TEXT NOT NULL,
  `Environment` VARCHAR(20) NOT NULL,
  `Description` TEXT NULL,
  `CreateTime` DATETIME(6) NOT NULL,
  `UpdateTime` DATETIME(6) NOT NULL,
  `UtcTime` DATETIME(6) NOT NULL,
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
