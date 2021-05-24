using System;

namespace Core.Infrastructure.Exceptions
{
    public class DomainException : Exception
    {
        public DomainException(string message, int errorCode)
        : base(message)
        {
            ErrorCode = errorCode;
        }

        public DomainException(string message, Exception innerException, int errorCode)
        : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        public int ErrorCode { get; set; }
    }
}
