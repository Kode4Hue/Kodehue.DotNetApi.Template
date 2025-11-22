using Application.Account.Users.DTOs;
using Application.Common.Results;

namespace Application.Account.Users.Services
{
    public interface IUserProfileService
    {
        Task<Result<CreateUserProfileResponse>> CreateInitalUserProfile(
            string email,
            string identityUserId,
            bool emailVerified = false,
            CancellationToken cancellationToken = default);
        Task<Result<UserProfile?>> GetById(
            string id, CancellationToken cancellationToken = default);
    }
}
