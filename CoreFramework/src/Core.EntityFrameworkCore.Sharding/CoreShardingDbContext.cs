using System;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Core.EntityFrameworkCore.Sharding
{
    public class CoreShardingDbContext : CoreDbContext
    {
        public CoreShardingDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public void ChangeConnection(string connection)
        {
            Database.GetDbConnection().ConnectionString = connection;
        }

        public void ChangeDatabase(string database)
        {
            if (string.IsNullOrEmpty(database))
            {
                throw new ArgumentNullException(nameof(database));
            }
            var connection = Database.GetDbConnection();
            if (connection.State.HasFlag(ConnectionState.Open))
            {
                connection.ChangeDatabase(database);
            }
            else
            {
                var connectionString = Regex.Replace(connection.ConnectionString, @"(?<=[Dd]atabase=)\w+(?=;)", database, RegexOptions.Singleline);
                connection.ConnectionString = connectionString;
            }
        }

        public void ChangeAllSchema(string schema)
        {
            var model = (Model)Model;
            if (model.ValidateModelIsReadonly())
                return;
            var items = Model.GetEntityTypes();
            foreach (var item in items)
            {
                if (item is IMutableEntityType entityType)
                {
                    entityType.SetSchema(schema);
                }
            }
        }

        public void ChangeSchema<TEntity>(string schema)
        {
            var model = (Model)Model;
            if (model.ValidateModelIsReadonly())
                return;
            if (Model.FindEntityType(typeof(TEntity)) is IMutableEntityType relational)
            {
                relational.SetSchema(schema);
            }
        }

        public void ChangeTable<TEntity>(string tableName)
        {
            var model = (Model)Model;
            if (model.ValidateModelIsReadonly())
                return;
            if (Model.FindEntityType(typeof(TEntity)) is IMutableEntityType relational)
            {
                relational.SetTableName(tableName);
            }
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ((Model)Model).TryFinalizeModel();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            ((Model)Model).TryFinalizeModel();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }
}
