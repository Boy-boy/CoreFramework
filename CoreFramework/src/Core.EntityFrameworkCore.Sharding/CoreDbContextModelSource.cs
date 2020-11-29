using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Core.EntityFrameworkCore.Sharding
{
    public class CoreDbContextModelSource : IModelSource
    {
        private readonly ModelSourceDependencies _dependencies;

        public CoreDbContextModelSource(ModelSourceDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public IModel GetModel(DbContext context, IConventionSetBuilder conventionSetBuilder)
        {
            return CreateModel(context, conventionSetBuilder);
        }

        protected  IModel CreateModel(
           DbContext context,
            IConventionSetBuilder conventionSetBuilder)
        {
            var modelBuilder = new ModelBuilder(conventionSetBuilder.CreateConventionSet());
            _dependencies.ModelCustomizer.Customize(modelBuilder, context);
            return modelBuilder.Model;
        }
    }
}
