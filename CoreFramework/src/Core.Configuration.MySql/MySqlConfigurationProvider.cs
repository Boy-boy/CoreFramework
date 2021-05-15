using System;
using System.Collections.Generic;

namespace Core.Configuration.MySql
{
    public class MySqlConfigurationProvider : DbConfigurationProvider
    {
        public MySqlConfigurationProvider(MySqlConfigurationSource source, MySqlConfigurationStorage storage)
        : base(source, storage)
        {
        }

        public override void Load()
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var configurations = Storage.GetAsync().GetAwaiter().GetResult();
            foreach (var configuration in configurations)
            {
                if (Data.ContainsKey(configuration.Key))
                {
                    Data[configuration.Key] = configuration.Value;
                }
                else
                {
                    Data.Add(configuration.Key, configuration.Value);
                }
            }
            OnReload();
        }
    }
}
