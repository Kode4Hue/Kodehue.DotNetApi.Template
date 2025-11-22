using Microsoft.Extensions.DependencyInjection;

namespace Application.Common.Extensions
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddMediator(options =>
            {
                options.ServiceLifetime = ServiceLifetime.Transient;
            });

            return services;
        }
    }
}
