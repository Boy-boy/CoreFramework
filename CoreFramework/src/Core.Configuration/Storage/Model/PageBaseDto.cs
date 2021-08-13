using System;

namespace Core.Configuration.Storage
{
    public class PageBaseDto
    {
        private int _pageIndex;

        private int _pageSize;

        public virtual int PageIndex
        {
            get => _pageIndex;
            set
            {
                if (value - 1 < 0)
                    throw new Exception("分页索引不能小于0");
                _pageIndex = value - 1;
            }
        }

        public virtual int PageSize
        {
            get => _pageSize;
            set
            {
                if (value < 0)
                    throw new Exception("分页大小不能小于0");
                _pageSize = value == 0 ? 10 : value;
            }
        }
    }
}
