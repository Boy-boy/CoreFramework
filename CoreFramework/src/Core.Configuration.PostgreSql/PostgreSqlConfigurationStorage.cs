using Core.Configuration.Storage;
using Npgsql;
using System;
using System.Collections.Generic;
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

        public override async Task AddAsync(ConfigurationMessage message, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", message.Id),
                new NpgsqlParameter("@Key", message.Key),
                new NpgsqlParameter("@Value", message.Value),
                new NpgsqlParameter("@Description", message.Description),
                new NpgsqlParameter("@CreateTime", message.CreateTime),
                new NpgsqlParameter("@UpdateTime", message.UpdateTime),
                new NpgsqlParameter("@UtcTime", message.UtcTime),
                new NpgsqlParameter("@IsDeleted", message.IsDeleted)
            };

            var sql = $@"INSERT INTO {GetTableName()} (Id,Key,Value,Description,CreateTime,UpdateTime,UtcTime,IsDeleted) 
VALUES (@Id,@Key,@Value,@Description,@CreateTime,@UpdateTime,@UtcTime,@IsDeleted);";

            using (var connection = new NpgsqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql, sqlParams);

            Events.Enqueue(new Event(EventType.Add, message.Key, message.Value));
            await Task.CompletedTask;
        }

        public override async Task UpdateAsync(ConfigurationMessage message, CancellationToken cancellationToken = default)
        {
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", message.Id),
                new NpgsqlParameter("@Key", message.Key),
                new NpgsqlParameter("@Value", message.Value),
                new NpgsqlParameter("@Description", message.Description),
                new NpgsqlParameter("@UpdateTime", message.UpdateTime)
            };
            var sql = $@"UPDATE {GetTableName()} 
SET Key=@Key,Value=@Value,Description=@Description,UpdateTime=@UpdateTime
WHERE Id=@Id";

            using (var connection = new NpgsqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql, sqlParams);

            Events.Enqueue(new Event(EventType.Update, message.Key, message.Value));
            await Task.CompletedTask;
        }

        public override async Task DeletedAsync(string id, CancellationToken cancellationToken = default)
        {
            var message = await GetAsync(id, cancellationToken);
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", id),
                new NpgsqlParameter("@IsDeleted", true),
                new NpgsqlParameter("@UpdateTime", DateTime.Now)
            };
            var sql = $@"UPDATE {GetTableName()} SET IsDeleted=@IsDeleted,UpdateTime=@UpdateTime WHERE Id=@Id";

            using (var connection = new NpgsqlConnection(_source.DbConnectionStr))
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
                new NpgsqlParameter("@Id", id),
                new NpgsqlParameter("@IsDeleted", false)
            };
            var sql = $@"SELECT * FROM {GetTableName()} WHERE Id=@Id AND IsDeleted=@IsDeleted";
            using var connection = new NpgsqlConnection(_source.DbConnectionStr);
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
                new NpgsqlParameter("@IsDeleted", false)
            };
            var sql = $@"SELECT * FROM {GetTableName()} WHERE IsDeleted=@IsDeleted";
            using var connection = new NpgsqlConnection(_source.DbConnectionStr);
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
CREATE SCHEMA IF NOT EXISTS {_source.DbSchema};

CREATE TABLE IF NOT EXISTS {GetTableName()} (
  Id VARCHAR(200) NOT NULL,
  Key VARCHAR(200) NOT NULL,
  Value TEXT NOT NULL,
  Description TEXT NULL,
  CreateTime timestamp(6) NOT NULL,
  UpdateTime timestamp(6) NOT NULL,
  UtcTime timestamp(6) NOT NULL,
  IsDeleted boolean NOT NULL,
  PRIMARY KEY (Id)
);";
            using (var connection = new NpgsqlConnection(_source.DbConnectionStr))
                connection.ExecuteNonQuery(sql);

            await Task.CompletedTask;
        }

        public virtual string GetTableName()
        {
            return $"{_source.DbSchema}.{_source.TableName}";
        }
    }
}
