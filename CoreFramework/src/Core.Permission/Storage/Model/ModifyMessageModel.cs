using System.Collections.Generic;

namespace Core.Permission.Storage
{
    public class ModifyMessageModel
    {
        public int Id { get; set; }

        public List<string> Roles { get; set; }
    }
}
