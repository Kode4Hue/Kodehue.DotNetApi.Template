using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Application.Common.Behaviors;
using Mediator;

namespace Application.Common.Extensions
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddValidatorsFromAssembly(typeof(AssemblyMarker).Assembly);

            // Register pipeline behaviours
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            services.AddMediator(options =>
            {
                options.ServiceLifetime = ServiceLifetime.Transient;
            });

            return services;
        }
    }
}
