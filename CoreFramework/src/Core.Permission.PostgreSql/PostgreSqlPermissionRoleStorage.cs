using Core.Permission.Storage;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Json.Newtonsoft;

namespace Core.Permission.PostgreSql
{
    public class PostgreSqlPermissionRoleStorage : IPermissionRoleStorage
    {
        private readonly IOptions<PermissionPostgreSqlOptions> _options;

        public PostgreSqlPermissionRoleStorage(IOptions<PermissionPostgreSqlOptions> options)
        {
            _options = options;
        }

        public async Task<List<RouteRoleEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<RouteRoleEntity>();
            if (cancellationToken.IsCancellationRequested) return result;

            var sqlParams = new List<object>
            {
                new NpgsqlParameter("@IsValid", true)
            };
            var sqlWhere = new StringBuilder("WHERE IsValid=@IsValid ");

            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere} ORDER BY UpdateTime DESC";
            await using var connection = new NpgsqlConnection(_options.Value.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result.Add(new RouteRoleEntity
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Roles = reader["Roles"].ToString().ToObject<List<string>>(),
                    ApiName = reader["ApiName"].ToString(),
                    ApiRoute = reader["ApiRoute"].ToString(),
                    IsValid = Convert.ToBoolean(reader["IsValid"].ToString()),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                });
            }
            return await Task.FromResult(result);
        }

        public async Task<PageResultDto<RouteRoleEntity>> GetAsync(MessageQueryModel query, CancellationToken cancellationToken = default)
        {
            var result = new PageResultDto<RouteRoleEntity>();
            if (cancellationToken.IsCancellationRequested) return result;

            var sqlParams = new List<object>
            {
                new NpgsqlParameter("@IsValid", true)
            };
            var sqlWhere = new StringBuilder("WHERE IsValid=@IsValid ");

            if (!string.IsNullOrEmpty(query.ApiRoute))
            {
                sqlWhere = sqlWhere.Append("AND ApiRoute LIKE CONCAT('%',@ApiRoute,'%')");
                sqlParams.Add(new NpgsqlParameter("@ApiRoute", query.ApiRoute));
            }

            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere} ORDER BY UpdateTime DESC  LIMIT {query.PageSize} OFFSET {query.PageIndex * query.PageSize}";
            await using var connection = new NpgsqlConnection(_options.Value.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            var list = new List<RouteRoleEntity>();
            while (reader.Read())
            {
                list.Add(new RouteRoleEntity
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Roles = reader["Roles"].ToString().ToObject<List<string>>(),
                    ApiName = reader["ApiName"].ToString(),
                    ApiRoute = reader["ApiRoute"].ToString(),
                    IsValid = Convert.ToBoolean(reader["IsValid"].ToString()),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                });
            }
            result.Count = await GetCountAsync(query, cancellationToken);
            result.Items = list;
            return await Task.FromResult(result);
        }

        public async Task<int> GetCountAsync(MessageQueryModel query, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return 0;

            var sqlParams = new List<object>
            {
                new NpgsqlParameter("@IsValid", true)
            };
            var sqlWhere = new StringBuilder("WHERE IsValid=@IsValid ");

            if (!string.IsNullOrEmpty(query.ApiRoute))
            {
                sqlWhere = sqlWhere.Append("AND ApiRoute LIKE CONCAT('%',@ApiRoute,'%')");
                sqlParams.Add(new NpgsqlParameter("@ApiRoute", query.ApiRoute));
            }

            var sql = $"SELECT COUNT(1) AS count FROM {GetTableName()} {sqlWhere}";
            await using var connection = new NpgsqlConnection(_options.Value.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            var count = 0;
            while (reader.Read())
            {
                count += Convert.ToInt32(Convert.ToInt32(reader["count"]));
            }
            return await Task.FromResult(count);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var sql = $@"
CREATE SCHEMA IF NOT EXISTS {_options.Value.DbSchema};

CREATE TABLE IF NOT EXISTS {GetTableName()} (
  Id INT NOT NULL GENERATED BY DEFAULT AS IDENTITY,
  Roles jsonb NOT NULL,
  ApiName VARCHAR(100),
  ApiRoute VARCHAR(100) NOT NULL,
  IsValid boolean default true NOT NULL,
  CreateTime timestamp(6) NOT NULL,
  UpdateTime timestamp(6) NOT NULL,
  PRIMARY KEY (Id)
);";
            await using (var connection = new NpgsqlConnection(_options.Value.DbConnection))
                connection.ExecuteNonQuery(sql);

            await Task.CompletedTask;
        }

        public async Task<int> UpdateAsync(ModifyMessageModel message, CancellationToken cancellationToken = default)
        {
            object[] sqlParams =
            {
                new NpgsqlParameter("@Id", message.Id),
                new NpgsqlParameter("@Roles", message.Roles??new List<string>()),
                new NpgsqlParameter("@UpdateTime", DateTime.Now)
            };
            var sql = $@"UPDATE {GetTableName()} 
SET Roles=@Roles,UpdateTime=@UpdateTime
WHERE Id=@Id";

            await using var connection = new NpgsqlConnection(_options.Value.DbConnection);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);

            return await Task.FromResult(executeRows);
        }

        public virtual string GetTableName()
        {
            return $"{_options.Value.DbSchema}.{_options.Value.DbTable}";
        }
    }
}
