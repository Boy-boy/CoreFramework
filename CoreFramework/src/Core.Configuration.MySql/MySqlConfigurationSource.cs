using Core.Configuration.Storage;
using Microsoft.Extensions.Configuration;

namespace Core.Configuration.MySql
{
    public class MySqlConfigurationSource : DbConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var storage = new MySqlConfigurationStorage(this);
            ConfigurationStorageExtensions.ConfigurationStorage = storage;
            return new MySqlConfigurationProvider(this, storage);
        }
    }
}
