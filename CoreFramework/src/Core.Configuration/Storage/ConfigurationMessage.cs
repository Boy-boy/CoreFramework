using System;

namespace Core.Configuration.Storage
{
    public class ConfigurationMessage
    {
        public ConfigurationMessage(){}
        public ConfigurationMessage(string key,string value,string description)
        {
            Id = Guid.NewGuid().ToString();
            Key = key;
            Value = value;
            Description = description;
            CreateTime = UpdateTime= DateTime.Now;
            UtcTime = DateTime.UtcNow;
        }

        public string Id { get; set; }

        public string Key { get; set; }
        
        public string Value { get; set; }

        public string Description { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }

        public DateTime UtcTime { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
