using Core.Ddd.Domain.Entities;
using Core.Ddd.Domain.Repositories;
using Core.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Core.EntityFrameworkCore.Sharding
{
    public static class RepositoryExtensions
    {
        public static void ChangeConnection<TAggregateRoot>(IRepository<TAggregateRoot> repository, string connection)
            where TAggregateRoot : class, IEntity
        {
            if (!(repository is Repository<TAggregateRoot> repository1)) return;
            if (repository1.DbContext is CoreShardingDbContext coreShardingDbContext)
            {
                coreShardingDbContext.ChangeConnection(connection);
            }
        }

        public static void ChangeDatabase<TAggregateRoot>(IRepository<TAggregateRoot> repository, string database)
            where TAggregateRoot : class, IEntity
        {
            if (!(repository is Repository<TAggregateRoot> repository1)) return;
            if (repository1.DbContext is CoreShardingDbContext coreShardingDbContext)
            {
                coreShardingDbContext.ChangeDatabase(database);
            }
        }

        public static void ChangeSchema<TAggregateRoot>(IRepository<TAggregateRoot> repository, string schema)
            where TAggregateRoot : class, IEntity
        {
            if (!(repository is Repository<TAggregateRoot> repository1)) return;
            if (repository1.DbContext is CoreShardingDbContext coreShardingDbContext)
            {
                coreShardingDbContext.ChangeSchema<TAggregateRoot>(schema);
            }
        }

        public static void ChangeTable<TAggregateRoot>(IRepository<TAggregateRoot> repository, string tableName)
            where TAggregateRoot : class, IEntity
        {
            if (!(repository is Repository<TAggregateRoot> repository1)) return;
            if (repository1.DbContext is CoreShardingDbContext coreShardingDbContext)
            {
                coreShardingDbContext.ChangeTable<TAggregateRoot>(tableName);
            }
        }
    }
}
