using Core.Configuration.Storage;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Configuration.PostgreSql
{
    public class PostgreSqlConfigurationStorage : ConfigurationStorageBase
    {
        private readonly PostgreSqlConfigurationSource _source;

        public PostgreSqlConfigurationStorage(PostgreSqlConfigurationSource source)
        {
            _source = source;
        }

        public override async Task<int> AddAsync(CreateMessageModel message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return 0;

            var exist = await ExistAsync(message.Key, cancellationToken);
            if (exist)
                throw new Exception($"The configuration key for the {_source.Environment} environment and {_source.NameSpace} NameSpace already exists");

            var dateTime = DateTime.Now;
            var dateUtcTime = DateTime.UtcNow;
            object[] sqlParams =
            {
                new NpgsqlParameter("@Key", message.Key),
                new NpgsqlParameter("@Value", message.Value),
                new NpgsqlParameter("@Environment", _source.Environment),
                new NpgsqlParameter("@Description", message.Description),
                new NpgsqlParameter("@NameSpace", _source.NameSpace),
                new NpgsqlParameter("@CreateTime", dateTime),
                new NpgsqlParameter("@UpdateTime", dateTime),
                new NpgsqlParameter("@UtcTime", dateUtcTime)
            };

            var sql = $@"INSERT INTO {GetTableName()} (Key,Value,Environment,Description,NameSpace,CreateTime,UpdateTime,UtcTime) 
VALUES (@Key,@Value,@Environment,@Description,@NameSpace,@CreateTime,@UpdateTime,@UtcTime);";

            using var connection = new NpgsqlConnection(_source.DbConnection);
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
                    throw new Exception($"The configuration key for the {_source.Environment} environment and {_source.NameSpace} NameSpace already exists");
            }

            var dateTime = DateTime.Now;
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", message.Id),
                new NpgsqlParameter("@Key", message.Key),
                new NpgsqlParameter("@Value", message.Value),
                new NpgsqlParameter("@Description", message.Description),
                new NpgsqlParameter("@UpdateTime", dateTime)
            };
            var sql = $@"UPDATE {GetTableName()} 
SET Key=@Key,Value=@Value,Description=@Description,UpdateTime=@UpdateTime
WHERE Id=@Id";

            using var connection = new NpgsqlConnection(_source.DbConnection);
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
                new NpgsqlParameter("@Id", id)
            };
            var sql = $@"DELETE FROM {GetTableName()} WHERE Id=@Id";

            using var connection = new NpgsqlConnection(_source.DbConnection);
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
                new NpgsqlParameter("@Environment", _source.Environment),
                new NpgsqlParameter("@NameSpace", _source.NameSpace)
            };

            var sqlWhere = new StringBuilder("WHERE Environment=@Environment AND NameSpace=@NameSpace ");
            if (query.Id.HasValue)
            {
                sqlWhere = sqlWhere.Append("AND Id=@Id ");
                sqlParams.Add(new NpgsqlParameter("@Id", query.Id));
            }
            if (!string.IsNullOrEmpty(query.Key))
            {
                sqlWhere = sqlWhere.Append("AND Key LIKE CONCAT('%',@Key,'%')");
                sqlParams.Add(new NpgsqlParameter("@Key", query.Key));
            }

            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere} ORDER BY UpdateTime DESC  LIMIT {query.PageSize} OFFSET {query.PageIndex * query.PageSize}";
            using var connection = new NpgsqlConnection(_source.DbConnection);
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
                    Environment = reader["Environment"].ToString(),
                    NameSpace = reader["NameSpace"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    UtcTime = Convert.ToDateTime(reader["UtcTime"].ToString())
                });
            }
            result.Count = await GetCountAsync(cancellationToken);
            result.Items = list;
            return await Task.FromResult(result);
        }

        public override async Task<List<ConfigurationMessage>> GetAsync(string environment, string NameSpace, CancellationToken cancellationToken = default)
        {
            var result = new List<ConfigurationMessage>();
            if (cancellationToken.IsCancellationRequested) return result;

            var sqlParams = new List<object>();
            var sqlWhere = new StringBuilder("WHERE 1=1 ");
            if (!string.IsNullOrEmpty(environment))
            {
                sqlWhere.Append("AND Environment=@Environment ");
                sqlParams.Add(new NpgsqlParameter("@Environment", environment));
            }
            if (!string.IsNullOrEmpty(NameSpace))
            {
                sqlWhere.Append("AND NameSpace=@NameSpace ");
                sqlParams.Add(new NpgsqlParameter("@NameSpace", NameSpace));
            }

            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere}";
            using var connection = new NpgsqlConnection(_source.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result.Add(new ConfigurationMessage
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    Environment = reader["Environment"].ToString(),
                    NameSpace = reader["NameSpace"].ToString(),
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
                new NpgsqlParameter("@Id", id)
            };

            var sql = $@"SELECT * FROM {GetTableName()} WHERE Id=@Id";
            using var connection = new NpgsqlConnection(_source.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result = new ConfigurationMessage
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Key = reader["Key"].ToString(),
                    Value = reader["Value"].ToString(),
                    Description = reader["Description"].ToString(),
                    Environment = reader["Environment"].ToString(),
                    NameSpace = reader["NameSpace"].ToString(),
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
                new NpgsqlParameter("@Key", key),
                new NpgsqlParameter("@Environment", _source.Environment),
                new NpgsqlParameter("@NameSpace", _source.NameSpace)
            };

            var sql = $"SELECT COUNT(1) AS count FROM {GetTableName()} WHERE Key=@Key AND Environment=@Environment AND NameSpace=@NameSpace";
            using var connection = new NpgsqlConnection(_source.DbConnection);
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
                new NpgsqlParameter("@Environment", _source.Environment),
                new NpgsqlParameter("@NameSpace", _source.NameSpace)
            };

            var sql = $"SELECT COUNT(1) AS count FROM {GetTableName()} WHERE Environment=@Environment AND NameSpace=@NameSpace";
            using var connection = new NpgsqlConnection(_source.DbConnection);
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
CREATE SCHEMA IF NOT EXISTS {_source.DbSchema};

CREATE TABLE IF NOT EXISTS {GetTableName()} (
  Id INT NOT NULL GENERATED BY DEFAULT AS IDENTITY,
  Key VARCHAR(200) NOT NULL,
  Value TEXT NOT NULL,
  Environment VARCHAR(20) NOT NULL,
  Description TEXT NULL,
  NameSpace VARCHAR(50) NOT NULL,
  CreateTime timestamp(6) NOT NULL,
  UpdateTime timestamp(6) NOT NULL,
  UtcTime timestamp(6) NOT NULL,
  PRIMARY KEY (Id)
);";
            using (var connection = new NpgsqlConnection(_source.DbConnection))
                connection.ExecuteNonQuery(sql);

            await Task.CompletedTask;
        }

        public virtual string GetTableName()
        {
            return $"{_source.DbSchema}.{_source.DbTable}";
        }
    }
}
