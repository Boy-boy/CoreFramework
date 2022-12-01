using System;

namespace Core.Infrastructure.Recursive
{
    public class RecursiveException : Exception
    {
        public RecursiveException(string message)
        : base(message)
        {

        }
    }
}
