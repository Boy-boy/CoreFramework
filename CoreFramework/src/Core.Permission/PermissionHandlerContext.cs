using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Core.Permission
{
    public class PermissionHandlerContext
    {
        private readonly HttpContext _httpContext;
        private readonly List<string> _permissions;

        private bool _failCalled;
        private bool _succeedCalled;

        public PermissionHandlerContext(HttpContext httpContext, List<string> permissions)
        {
            _httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));
            _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        }
        public HttpContext HttpContext => _httpContext;

        public List<string> Permissions => _permissions;

        public virtual bool HasSucceeded => !_failCalled && _succeedCalled;

        public virtual bool HasFailed => _failCalled;

        public virtual void Fail() => _failCalled = true;

        public virtual void Succeed() => _succeedCalled = true;

    }
}
