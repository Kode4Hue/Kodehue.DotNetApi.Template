using Infrastructure.Account.Identity.Keycloak.Configurations;
using Microsoft.Extensions.Options;

namespace Infrastructure.Account.Identity.Keycloak.Utilities
{
    public static class KeycloakClientResolver
    {
      //  private readonly KeycloakConfigOptions _options;

        //public KeycloakClientResolver(IOptions<KeycloakConfigOptions> options)
        //{
        //    _options = options.Value;
        //}

        public static KeycloakClientCredentials GetClient(
            KeycloakConfigOptions options,
            string name)
        {

            if (options.Clients is not null && options.Clients.TryGetValue(name, out var client))
            {
                return client;
            }

            throw new KeyNotFoundException($"Keycloak client '{name}' was not found.");
        }
    }
}
