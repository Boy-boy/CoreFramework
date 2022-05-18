using MediatR;

namespace Core.Application.Queries
{
    public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : IQueryResult
    {
    }

    public interface IQueryPaging
    {
        int PageIndex { get; set; }

        int PageSize { get; set; }
    }

    public interface IQueryPaging<out TResponse> : IQueryPaging, IQuery<TResponse>
        where TResponse : IQueryResult
    {
    }


    public abstract class QueryPaging<TResponse> : IQueryPaging<TResponse>
        where TResponse : QueryPagedResult
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
                if (value <= 0)
                    throw new Exception("分页大小不能小于等于0");
                _pageSize = value;
            }
        }
    }
}
