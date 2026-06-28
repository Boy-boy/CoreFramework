using Core.EmailClient.Storage;
using Core.EmailClient.Storage.Model;
using Core.Json.SystemTextJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

namespace Core.EmailClient.Mysql
{
    internal class MysqlStorage : IEmailStorage
    {
        private readonly ILogger<MysqlStorage> _logger;
        private readonly MysqlEmailStorageOptions _options;

        public MysqlStorage(IOptions<MysqlEmailStorageOptions> options,
            ILogger<MysqlStorage> logger)
        {
            _logger = logger;
            _options = options.Value;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var sql = $@"
CREATE TABLE IF NOT EXISTS {GetTableName()} (
  Id VARCHAR(200) NOT NULL,
  Sender VARCHAR(200) NOT NULL,
  SenderAddress VARCHAR(200) NOT NULL,
  Recipients json NOT NULL,
  Cc json NULL,
  Bcc json NULL,
  Subject VARCHAR(200) NOT NULL,
  Body Text NOT NULL,
  BodyType int NOT NULL,
  MailFiles json  NULL,
  LinkedResources json  NULL,
  CreatorId VARCHAR(200) NOT NULL,
  CreateTime DATETIME(6) NOT NULL,
  UpdateTime DATETIME(6) NOT NULL,
  IsSend bool NOT NULL,
  PRIMARY KEY (Id)
)ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

            try
            {
                await using var connection = new MySqlConnection(_options.DbConnection);
                connection.ExecuteNonQuery(sql);

                _logger.LogInformation($"initial email message table successfully. table name is [{GetTableName()}]");
            }
            catch (Exception e)
            {
                _logger.LogInformation($"initial email message table failed. table name is [{GetTableName()}],error message is {e.Message}");
            }
            await Task.CompletedTask;
        }

        public async Task<string> AddAsync(AddEmailModel model, object dbTransaction = null, CancellationToken cancellationToken = default)
        {
            var id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            object[] sqlParams =
            {
                new MySqlParameter("@Id", id),
                new MySqlParameter("@Sender", model.Sender),
                new MySqlParameter("@SenderAddress", model.SenderAddress),
                new MySqlParameter("@Recipients",model.Recipients.ToJson()),
                new MySqlParameter("@Cc",model.Cc?.ToJson()),
                new MySqlParameter("@Bcc",model.Bcc?.ToJson()),
                new MySqlParameter("@Subject", model.Subject),
                new MySqlParameter("@Body", model.Body),
                new MySqlParameter("@BodyType", (int)model.BodyType),
                new MySqlParameter("@MailFiles", model.MailFiles?.ToJson()),
                new MySqlParameter("@LinkedResources",model.LinkedResources?.ToJson()),
                new MySqlParameter("@CreatorId", model.CreatorId),
                new MySqlParameter("@CreateTime",now),
                new MySqlParameter("@UpdateTime", now),
                new MySqlParameter("@IsSend", model.IsSend)
            };

            var sql = $@"INSERT INTO {GetTableName()} (Id,Sender,SenderAddress,Recipients,Cc,Bcc,Subject,Body,BodyType,MailFiles,LinkedResources,CreatorId,CreateTime,UpdateTime,IsSend) 
VALUES (@Id,@Sender,@SenderAddress,@Recipients,@Cc,@Bcc,@Subject,@Body,@BodyType,@MailFiles,@LinkedResources,@CreatorId,@CreateTime,@UpdateTime,@IsSend);";

            if (dbTransaction == null)
            {
                await using var connection = new MySqlConnection(_options.DbConnection);
                connection.ExecuteNonQuery(sql, sqlParams);
                _logger.LogInformation($"insert message in {GetTableName()} table successfully. messageId={id}");
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

            return id;
        }

        public async Task<int> UpdateAsync(UpdateEmailModel model, CancellationToken cancellationToken = default)
        {
            var messageEntity = await GetAsync(model.Id, cancellationToken);
            if (messageEntity == null) return 0;

            var dateTime = DateTime.Now;
            object[] sqlParams =
            {
                new MySqlParameter("@Id", model.Id),
                new MySqlParameter("@IsSend", model.IsSend),
                new MySqlParameter("@UpdateTime", dateTime)
            };
            var sql = $@"UPDATE {GetTableName()} 
SET IsSend=@IsSend,UpdateTime=@UpdateTime
WHERE Id=@Id";

            await using var connection = new MySqlConnection(_options.DbConnection);
            var executeRows = connection.ExecuteNonQuery(sql, sqlParams);

            return await Task.FromResult(executeRows);
        }

        public async Task<List<EmailMessage>> GetAsync(QueryEmailModel query, CancellationToken cancellationToken = default)
        {
            var result = new List<EmailMessage>();
            if (cancellationToken.IsCancellationRequested) return result;

            var sqlParams = new List<object>();
            var sqlWhere = new StringBuilder("WHERE 1=1 ");
            if (query.IsSend.HasValue)
            {
                sqlWhere.Append("AND IsSend=@IsSend ");
                sqlParams.Add(new MySqlParameter("@IsSend", query.IsSend.Value));
            }

            var sql = $@"SELECT * FROM {GetTableName()} {sqlWhere}";
            await using var connection = new MySqlConnection(_options.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result.Add(new EmailMessage
                {
                    Id = reader["Id"].ToString(),
                    Sender = reader["Sender"].ToString(),
                    SenderAddress = reader["SenderAddress"].ToString(),
                    Recipients = reader["Recipients"].ToString().ToObject<List<string>>(),
                    Cc = reader["Cc"].ToString().ToObject<List<string>>(),
                    Bcc = reader["Bcc"].ToString().ToObject<List<string>>(),
                    Subject = reader["Subject"].ToString(),
                    Body = reader["Body"].ToString(),
                    BodyType = (MailTextFormat)Enum.Parse(typeof(MailTextFormat), reader["BodyType"].ToString()),
                    MailFiles = reader["MailFiles"].ToString().ToObject<List<MailFile>>(),
                    LinkedResources = reader["LinkedResources"].ToString().ToObject<List<MailFile>>(),
                    CreatorId = reader["CreatorId"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    IsSend = Convert.ToBoolean(reader["IsSend"].ToString())
                });
            }
            return await Task.FromResult(result);
        }

        public async Task<EmailMessage> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return null;
            var result = new EmailMessage();

            object[] sqlParams =
            {
                new MySqlParameter("@Id", id)
            };

            var sql = $@"SELECT * FROM {GetTableName()} WHERE Id=@Id";
            await using var connection = new MySqlConnection(_options.DbConnection);
            var reader = connection.ExecuteQuery(sql, sqlParams.ToArray());
            while (reader.Read())
            {
                result = new EmailMessage
                {
                    Id = reader["Id"].ToString(),
                    Sender = reader["Sender"].ToString(),
                    SenderAddress = reader["SenderAddress"].ToString(),
                    Recipients = reader["Recipients"].ToString().ToObject<List<string>>(),
                    Cc = reader["Cc"].ToString().ToObject<List<string>>(),
                    Bcc = reader["Bcc"].ToString().ToObject<List<string>>(),
                    Subject = reader["Subject"].ToString(),
                    Body = reader["Body"].ToString(),
                    BodyType = (MailTextFormat)Enum.Parse(typeof(MailTextFormat), reader["BodyType"].ToString()),
                    MailFiles = reader["MailFiles"].ToString().ToObject<List<MailFile>>(),
                    LinkedResources = reader["LinkedResources"].ToString().ToObject<List<MailFile>>(),
                    CreatorId = reader["CreatorId"].ToString(),
                    CreateTime = Convert.ToDateTime(reader["CreateTime"].ToString()),
                    UpdateTime = Convert.ToDateTime(reader["UpdateTime"].ToString()),
                    IsSend = Convert.ToBoolean(reader["IsSend"].ToString())
                };
            }
            return await Task.FromResult(result);
        }

        public virtual string GetTableName()
        {
            return $"{_options.DbTable}";
        }
    }
}
