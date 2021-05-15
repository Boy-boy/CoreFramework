using Core.Configuration.Storage;
using Microsoft.Extensions.Configuration;

namespace Core.Configuration.SqlServer
{
    public class SqlServerConfigurationSource : DbConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var storage = new SqlServerConfigurationStorage(this);
            ConfigurationStorageExtensions.ConfigurationStorage = storage;
            return new SqlServerConfigurationProvider(this, storage);
        }
    }
}
