using Microsoft.Extensions.DependencyInjection;

namespace Core.Permission
{
    public interface IPermissionOptionsExtensions
    {
        void AddServices(IServiceCollection services);
    }
}
