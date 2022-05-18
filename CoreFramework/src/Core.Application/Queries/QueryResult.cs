namespace Core.Application.Queries
{
    public interface IQueryResult
    {
    }

    public abstract class QueryPagedResult : IQueryResult
    {
        protected QueryPagedResult(IQueryPaging queryPaging)
        {
            PageIndex = queryPaging.PageIndex;
            PageSize = queryPaging.PageSize;
        }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalPages
        {
            get
            {
                if (TotalCount <= 0)
                    return 0;
                if (PageSize <= 1)
                {
                    return TotalCount;
                }
                var pageCount = TotalCount / PageSize;
                if (TotalCount % PageSize > 0)
                {
                    pageCount++;
                }
                return pageCount;
            }
        }

        public int TotalCount { get; set; }
    }
}
