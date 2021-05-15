using Core.Configuration.Storage;
using Microsoft.Extensions.Configuration;

namespace Core.Configuration.PostgreSql
{
    public class PostgreSqlConfigurationSource : DbConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var storage = new PostgreSqlConfigurationStorage(this);
            ConfigurationStorageExtensions.ConfigurationStorage = storage;
            return new PostgreSqlConfigurationProvider(this, storage);
        }
    }
}
