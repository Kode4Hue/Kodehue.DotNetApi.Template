using Application.Account.Users.DTOs;
using Application.Account.Users.Services;
using Application.Common.Results;
using Domain.Account.Users;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.Account.Users.Services
{
    internal class UserProfileService : IUserProfileService
    {
        private readonly ILogger<UserProfileService> _logger;
        private readonly UserProfileDbContext _dbContext;
        private readonly IHostEnvironment _env;

        public UserProfileService(
            ILogger<UserProfileService> logger,
            UserProfileDbContext dbContext,
            IHostEnvironment env)
        {
            _logger = logger;
            _dbContext = dbContext;
            _env = env;

            _logger.LogInformation("UserProfileService initialized.");
        }

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
                if (parts.Length != 2)
                    return "***";

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

        // --------------------------------------------------------------------
        //  CREATE USER PROFILE
        // --------------------------------------------------------------------
        public async Task<Result<CreateUserProfileResponse>> CreateInitalUserProfile(
            string email,
            string identityUserId,
            bool emailVerified = false,
            CancellationToken cancellationToken = default)
        {
            var safeEmail = GetSafeEmail(email);

            _logger.LogInformation(
                "Starting CreateInitialUserProfile for Email {Email} and IdentityUserId {IdentityUserId}",
                safeEmail, identityUserId);

            try
            {
                // Check for existing profile
                bool exists = await _dbContext.UserProfiles
                    .AnyAsync(up => up.Email == email, cancellationToken);

                if (exists)
                {
                    _logger.LogInformation("UserProfile already exists for Email {Email}", safeEmail);

                    return Result<CreateUserProfileResponse>.CreateErrorResult(
                        new List<ErrorDto>
                        {
                            ErrorDto.CreateFromMessage("A user profile with the provided email already exists.")
                        },
                        ErrorType.Conflict);
                }

                // Create profile
                var userProfile = new UserProfileEntity
                {
                    Email = email,
                    EmailVerified = emailVerified,
                    IdentityUserId = identityUserId
                };

                _logger.LogDebug(
                    "Adding new UserProfileEntity to DbContext for Email {Email}",
                    safeEmail);

                await _dbContext.UserProfiles.AddAsync(userProfile, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Successfully created UserProfile with Id {Id} for Email {Email}",
                    userProfile.Id,
                    safeEmail);

                var response = new CreateUserProfileResponse(userProfile.Id.ToString());

                return Result<CreateUserProfileResponse>.CreateSuccessResult(response);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(
                    ex,
                    "Concurrency error occurred while creating UserProfile for Email {Email}",
                    safeEmail);

                return Result<CreateUserProfileResponse>.CreateErrorResult(
                    new List<ErrorDto>
                    {
                        ErrorDto.CreateFromMessage("A concurrency error occurred while creating the user profile.")
                    },
                    ErrorType.ApplicationError);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(
                    ex,
                    "Database update error occurred while creating UserProfile for Email {Email}",
                    safeEmail);

                return Result<CreateUserProfileResponse>.CreateErrorResult(
                    new List<ErrorDto>
                    {
                        ErrorDto.CreateFromMessage("A database error occurred while creating the user profile.")
                    },
                    ErrorType.ApplicationError);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error occurred while creating UserProfile for Email {Email}",
                    safeEmail);

                return Result<CreateUserProfileResponse>.CreateErrorResult(
                    new List<ErrorDto>
                    {
                        ErrorDto.CreateFromMessage("An unexpected error occurred while creating the user profile.")
                    },
                    ErrorType.ApplicationError);
            }
        }

        // --------------------------------------------------------------------
        //  GET USER PROFILE BY ID
        // --------------------------------------------------------------------
        public async Task<Result<UserProfile?>> GetById(
            string id,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching UserProfile by Id {Id}", id);

            try
            {
                var entity = await _dbContext.UserProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(up => up.Id.ToString() == id, cancellationToken);

                if (entity is null)
                {
                    _logger.LogWarning("UserProfile not found for Id {Id}", id);
                    return Result<UserProfile?>.CreateSuccessResult(null);
                }

                _logger.LogInformation("UserProfile found for Id {Id}", id);

                var profile = new UserProfile
                {
                    Id = entity.Id.ToString(),
                    Email = entity.Email,
                    IdentityUserId = entity.IdentityUserId
                };

                return Result<UserProfile?>.CreateSuccessResult(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving UserProfile with Id {Id}", id);

                return Result<UserProfile?>.CreateErrorResult(
                    new List<ErrorDto>
                    {
                        ErrorDto.CreateFromMessage("An unexpected error occurred while retrieving the user profile.")
                    },
                    ErrorType.ApplicationError);
            }
        }
    }
}
