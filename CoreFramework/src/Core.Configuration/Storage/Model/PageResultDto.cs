using System.Collections.Generic;

namespace Core.Configuration.Storage
{
    public class PageResultDto<T>
    {
        public int Count { get; set; }

        public List<T> Items { get; set; }
    }
}