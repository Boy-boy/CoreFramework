using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Permission
{
    public class DefaultPermissionHandlerProvider : IPermissionHandlerProvider
    {
        private readonly IEnumerable<IPermissionHandler> _handlers;

        public DefaultPermissionHandlerProvider(IEnumerable<IPermissionHandler> handlers)
        {
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers)); ;
        }

        public Task<IEnumerable<IPermissionHandler>> GetHandlersAsync()
        {
            return Task.FromResult(_handlers);
        }
    }
}
