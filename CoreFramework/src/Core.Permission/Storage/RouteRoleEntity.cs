using System;
using System.Collections.Generic;

namespace Core.Permission.Storage
{
    public class RouteRoleEntity
    {
        public int Id { get; set; }

        public List<string> Roles { get; set; }

        public string ApiName { get; set; }

        public string ApiRoute { get; set; }

        public bool IsValid { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }
}
