using System;
using System.Threading;

namespace Core.Infrastructure.Timer
{
    public static class NonCapturingTimer
    {
        public static System.Threading.Timer Create(
            TimerCallback callback,
            object state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));
            var flag = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    flag = true;
                }
                return new System.Threading.Timer(callback, state, dueTime, period);
            }
            finally
            {
                if (flag)
                    ExecutionContext.RestoreFlow();
            }
        }
    }
}
