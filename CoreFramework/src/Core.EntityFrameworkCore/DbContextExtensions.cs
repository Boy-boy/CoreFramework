using Core.Ddd.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Collections.Generic;

namespace Core.EntityFrameworkCore
{
    public static class DbContextExtensions
    {
        public static void TrySoftDeleted(this IEnumerable<EntityEntry> entityEntries)
        {
            foreach (var entry in entityEntries)
            {
                if (entry.State != EntityState.Deleted) continue;

                var entityType = entry.Entity.GetType();
                if (!typeof(ISoftDeleted).IsAssignableFrom(entityType)) continue;

                if (entry.Entity is ISoftDeleted softDeleted)
                    softDeleted.IsDeleted = true;
                entry.State = EntityState.Modified;
            }
        }
    }
}
