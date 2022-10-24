using System;

namespace Core.Permission.Storage
{
    public class PermissionGrantsEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }

        public bool IsValid { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }
}
