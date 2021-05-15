using System;
using System.Collections.Generic;

namespace Core.Configuration.SqlServer
{
    public class SqlServerConfigurationProvider : DbConfigurationProvider
    {
        public SqlServerConfigurationProvider(SqlServerConfigurationSource source, SqlServerConfigurationStorage storage)
        : base(source, storage)
        {
        }

        public override void Load()
        {
            Data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var configurations = Storage.GetAsync().Result;
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
