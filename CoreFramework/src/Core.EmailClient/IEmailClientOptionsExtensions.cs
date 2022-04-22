using Microsoft.Extensions.DependencyInjection;

namespace Core.EmailClient
{
    public interface IEmailClientOptionsExtensions
    {
        void AddServices(IServiceCollection services);
    }
}
