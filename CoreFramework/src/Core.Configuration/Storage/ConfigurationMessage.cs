using System;

namespace Core.Configuration.Storage
{
    public class ConfigurationMessage
    {
        public int Id { get; set; }

        public string Environment { get; set; } = Environments.Product;

        public string Key { get; set; }

        public string Value { get; set; }

        public string Description { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public DateTime UtcTime { get; set; }
    }

    public static class Environments
    {
        public static readonly string Develop = "develop";
        public static readonly string Test = "test";
        public static readonly string Product = "product";
    }
}
