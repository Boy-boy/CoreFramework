using System.Collections.Generic;

namespace Core.Permission.Storage
{
    public class PageResultDto<T>
    {
        public int Count { get; set; }

        public List<T> Items { get; set; }
    }
}