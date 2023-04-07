using System;
using System.Data;

namespace Core.Uow
{
    public interface IUnitOfWorkData
    {
        bool? IsTransactional { get; set; }

        IsolationLevel? IsolationLevel { get; set; }
    }

    public class UnitOfWorkAttribute : Attribute, IUnitOfWorkData
    {
        public bool? IsTransactional { get; set; }

        public IsolationLevel? IsolationLevel { get; set; }


        public UnitOfWorkAttribute()
        {
        }

        public UnitOfWorkAttribute(bool isTransactional)
        {
            IsTransactional = isTransactional;
        }

        public UnitOfWorkAttribute(bool isTransactional, IsolationLevel isolationLevel)
        {
            IsTransactional = isTransactional;
            IsolationLevel = isolationLevel;
        }

        public virtual void SetOptions(UnitOfWorkOptions options)
        {
            if (IsTransactional.HasValue)
            {
                options.IsTransactional = IsTransactional.Value;
            }

            if (IsolationLevel.HasValue)
            {
                options.IsolationLevel = IsolationLevel;
            }
        }
    }
}
