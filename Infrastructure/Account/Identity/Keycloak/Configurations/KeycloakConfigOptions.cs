using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Account.Identity.Keycloak.Configurations
{
    public class KeycloakConfigOptions
    {
        public string? Realm { get; set; }
        public Dictionary<string, KeycloakClientCredentials>? Clients { get; set; }
        public string?  BaseUrl { get; set; }
    }
}
