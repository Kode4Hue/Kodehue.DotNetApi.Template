using Application.Account.Identity.Services;
using Application.Account.Users.Services;
using Application.Common.Outbox.Services;
using Infrastructure.Account.Identity.Keycloak.Configurations;
using Infrastructure.Account.Identity.Keycloak.Services;
using Infrastructure.Account.Users.Services;
using Infrastructure.Common.Outbox.Services;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Infrastructure.Common.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddUserProfileDbContext(configuration);
            services.AddKeycloak(configuration);
            services.AddTransient<IOutboxService, OutboxService>();
            services.AddTransient<IUserProfileService, UserProfileService>();
            return services;
        }

        public static IServiceCollection AddKeycloak(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<KeycloakConfigOptions>()
                .Bind(configuration.GetSection("Keycloak"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddHttpClient<IIdentityService, KeycloakIdentityService>((sp, client) =>
            {
                var config =
                    sp.GetRequiredService<IOptions<KeycloakConfigOptions>>().Value;

                if (string.IsNullOrWhiteSpace(config.BaseUrl))
                    throw new ArgumentException("Keycloak BaseUrl is missing.");

                client.BaseAddress = new Uri(config.BaseUrl);
            });

            return services;
        }
    }

    public static class DbContextServiceCollectionExtensions
    {
        public static IServiceCollection AddUserProfileDbContext(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Read connection string from standard ConnectionStrings section or MySql section
            var connectionString = configuration["MySql:ConnectionString"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("MySQL connection string not configured. Set 'ConnectionStrings:MySql' or 'ConnectionStrings:DefaultConnection' or 'MySql:ConnectionString' in configuration.");
            }

            // Read server version if explicitly provided (e.g. "8.0.29"), otherwise let Pomelo auto-detect.
            ServerVersion serverVersion;
            var serverVersionSetting = configuration["MySql:ServerVersion"];
            if (!string.IsNullOrWhiteSpace(serverVersionSetting) && Version.TryParse(serverVersionSetting, out var parsedVersion))
            {
                serverVersion = new MySqlServerVersion(parsedVersion);
            }
            else
            {
                serverVersion = ServerVersion.AutoDetect(connectionString);
            }

            services.AddDbContext<UserProfileDbContext>(options =>
            {
                options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                {
                    mySqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
                });
            });

            return services;
        }
    }

 

}
