using Application.Account.Identity.DTOs;
using Application.Account.Identity.Services;
using Application.Common.Results;
using Infrastructure.Account.Identity.Keycloak.Configurations;
using Infrastructure.Account.Identity.Keycloak.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Account.Identity.Keycloak.Services
{
    public class KeycloakIdentityService : IIdentityService
    {
        private readonly HttpClient _httpClient;
        private readonly KeycloakConfigOptions _config;
        private readonly string _realm;
        private readonly KeycloakClientCredentials _adminCredentials;
        private readonly ILogger<KeycloakIdentityService> _logger;
        private readonly IHostEnvironment _env;

        private string? _adminAccessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);

        public KeycloakIdentityService(
            ILogger<KeycloakIdentityService> logger,
            HttpClient httpClient,
            IOptions<KeycloakConfigOptions> options,
            IHostEnvironment env)
        {
            _logger = logger;
            _httpClient = httpClient;
            _config = options.Value;
            _env = env;

            if (string.IsNullOrWhiteSpace(_config.Realm))
                throw new ArgumentNullException(nameof(options), "Keycloak realm is required.");

            _realm = _config.Realm;

            _adminCredentials = KeycloakClientResolver.GetClient(
                options.Value, KeycloakConstants.UserServiceClientName)
                ?? throw new ArgumentNullException(nameof(options), "Admin client credentials missing.");

            if (string.IsNullOrWhiteSpace(_adminCredentials.ClientId) ||
                string.IsNullOrWhiteSpace(_adminCredentials.ClientSecret))
            {
                throw new ArgumentNullException(nameof(options), "Admin client credentials missing clientId or secret.");
            }

            _logger.LogInformation("KeycloakIdentityService initialized for realm {Realm}.", _realm);
        }

        // --------------------------------------------------------------------
        //  URL HELPERS
        // --------------------------------------------------------------------
        private string TokenUrl => $"/realms/{_realm}/protocol/openid-connect/token";
        private string UsersUrl => $"/admin/realms/{_realm}/users";
        private string UserByIdUrl(string id) => $"{UsersUrl}/{id}";

        // --------------------------------------------------------------------
        //  PII SAFE LOGGING HELPERS
        // --------------------------------------------------------------------
        private string GetSafeEmail(string email)
            => _logger.IsEnabled(LogLevel.Debug) && _env.IsDevelopment()
                ? email
                : MaskEmail(email);

        private static string MaskEmail(string email)
        {
            try
            {
                var parts = email.Split('@');
                if (parts.Length != 2) return "***";

                var local = parts[0];
                var domain = parts[1];

                return local.Length <= 1
                    ? $"*@$domain"
                    : $"{local[0]}***@{domain}";
            }
            catch
            {
                return "***";
            }
        }

        private static string ExtractKeycloakErrorMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "Unknown error";

            try
            {
                using var doc = JsonDocument.Parse(body);
                return doc.RootElement.TryGetProperty("errorMessage", out var msg)
                    ? msg.GetString() ?? "Unknown error"
                    : body;
            }
            catch
            {
                return body;
            }
        }

        private void SetBearerToken(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            if (!_httpClient.DefaultRequestHeaders.Accept.Any(h => h.MediaType == "application/json"))
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));
            }
        }

        // --------------------------------------------------------------------
        //  TOKEN
        // --------------------------------------------------------------------
        private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
        {
            await _tokenLock.WaitAsync(cancellationToken);
            _logger.LogDebug("Acquiring Keycloak admin token...");

            try
            {
                if (!string.IsNullOrEmpty(_adminAccessToken) &&
                    DateTime.UtcNow < _tokenExpiry)
                {
                    _logger.LogDebug("Using cached admin token (expires at {Expiry}).", _tokenExpiry);
                    return _adminAccessToken!;
                }

                var formData = new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _adminCredentials.ClientId!,
                    ["client_secret"] = _adminCredentials.ClientSecret!
                };

                using var response = await _httpClient.PostAsync(
                    TokenUrl,
                    new FormUrlEncodedContent(formData),
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    var message = ExtractKeycloakErrorMessage(body);

                    _logger.LogError(
                        "Failed to obtain admin token. Status {Status}. Error: {Error}",
                        (int)response.StatusCode, message);

                    throw new Exception($"Keycloak token request failed: {message}");
                }

                var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                _adminAccessToken = json.GetProperty("access_token").GetString();
                var expiresIn = json.GetProperty("expires_in").GetInt32();

                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 30);

                _logger.LogDebug("Admin token successfully retrieved. Expires at {Expiry}.", _tokenExpiry);

                return _adminAccessToken!;
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        // --------------------------------------------------------------------
        //  CREATE USER
        // --------------------------------------------------------------------
        public async Task<Result<IdentityUser>> CreateInitialIdentityUserAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var safeEmail = GetSafeEmail(email);
            _logger.LogInformation("Starting Keycloak user creation for {Email}.", safeEmail);

            try
            {
                var token = await GetAdminTokenAsync(cancellationToken);
                SetBearerToken(token);

                var response = await SendCreateUserRequest(email, password, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return await HandleUserCreationErrors(email, response, cancellationToken);
                }

                _logger.LogInformation("Keycloak user created for {Email}.", safeEmail);

                var location = response.Headers.Location?.ToString();
                if (string.IsNullOrWhiteSpace(location))
                {
                    _logger.LogError("Missing Location header from Keycloak for user {Email}.", safeEmail);

                    return Result<IdentityUser>.CreateErrorResult(
                        new List<ErrorDto> { ErrorDto.CreateFromMessage("Missing Location header from Keycloak.") },
                        ErrorType.ApplicationError);
                }

                var userId = location.Split('/').Last();
                _logger.LogInformation("Keycloak user created with ID {UserId} for {Email}.", userId, safeEmail);

                var userResponse = await _httpClient.GetAsync(UserByIdUrl(userId), cancellationToken);

                if (!userResponse.IsSuccessStatusCode)
                {
                    var body = await userResponse.Content.ReadAsStringAsync(cancellationToken);
                    var msg = ExtractKeycloakErrorMessage(body);

                    _logger.LogError(
                        "Failed to retrieve Keycloak user {UserId}. Error: {Error}",
                        userId,
                        msg);

                    return Result<IdentityUser>.CreateErrorResult(
                        new List<ErrorDto> { ErrorDto.CreateFromMessage(msg) },
                        ErrorType.ApplicationError);
                }

                var json = await userResponse.Content.ReadAsStringAsync(cancellationToken);
                var user = JsonSerializer.Deserialize<IdentityUser>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("Completed user creation for {Email}.", safeEmail);

                return Result<IdentityUser>.CreateSuccessResult(user!);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error occurred during user creation for {Email}.",
                    safeEmail);

                return Result<IdentityUser>.CreateErrorResult(
                    new List<ErrorDto> { ErrorDto.CreateFromMessage(ex.Message) },
                    ErrorType.ApplicationError);
            }
        }

        private async Task<HttpResponseMessage> SendCreateUserRequest(
            string email,
            string password,
            CancellationToken cancellationToken)
        {
            var safeEmail = GetSafeEmail(email);

            _logger.LogDebug("Sending Keycloak user create request for {Email}.", safeEmail);

            var payload = new
            {
                username = email,
                email = email,
                enabled = true,
                emailVerified = false,
                requiredActions = new[] { "VERIFY_EMAIL" },
                credentials = new[]
                {
                    new {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                }
            };

            return await _httpClient.PostAsJsonAsync(UsersUrl, payload, cancellationToken);
        }

        private async Task<Result<IdentityUser>> HandleUserCreationErrors(
            string email,
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var safeEmail = GetSafeEmail(email);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var msg = ExtractKeycloakErrorMessage(body);

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.Conflict:
                    _logger.LogWarning(
                        "Conflict creating Keycloak user {Email}. Error: {Error}",
                        safeEmail,
                        msg);

                    return Result<IdentityUser>.CreateErrorResult(
                        new List<ErrorDto> { ErrorDto.CreateFromMessage(msg) },
                        ErrorType.Conflict);

                default:
                    _logger.LogError(
                        "Unexpected Keycloak error creating user {Email}. Error: {Error}",
                        safeEmail,
                        msg);

                    return Result<IdentityUser>.CreateErrorResult(
                        new List<ErrorDto> { ErrorDto.CreateFromMessage("Unexpected error.") },
                        ErrorType.ApplicationError);
            }
        }

        // --------------------------------------------------------------------
        //  DELETE USER
        // --------------------------------------------------------------------
        public async Task<NoContentResult> DeleteIdentityUserAsync(
            string identityUserId,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting Keycloak user {UserId}.", identityUserId);

            try
            {
                var token = await GetAdminTokenAsync(cancellationToken);
                SetBearerToken(token);

                using var response = await _httpClient.DeleteAsync(UserByIdUrl(identityUserId), cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully deleted Keycloak user {UserId}.", identityUserId);
                    return NoContentResult.CreateSuccess();
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var msg = ExtractKeycloakErrorMessage(body);

                _logger.LogError(
                    "Failed to delete Keycloak user {UserId}. Status {Status}. Error: {Error}",
                    identityUserId,
                    (int)response.StatusCode,
                    msg);

                return NoContentResult.CreateErrorResult(
                    new List<ErrorDto> { ErrorDto.CreateFromMessage(msg) },
                    ErrorType.ApplicationError);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error deleting Keycloak user {UserId}.",
                    identityUserId);

                return NoContentResult.CreateErrorResult(
                    new List<ErrorDto> { ErrorDto.CreateFromMessage(ex.Message) },
                    ErrorType.ApplicationError);
            }
        }
    }
}
