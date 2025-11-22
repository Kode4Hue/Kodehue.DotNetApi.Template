using Application.Account.Identity.DTOs;
using Application.Common.Results;

namespace Application.Account.Identity.Services
{
    public interface IIdentityService
    {
        Task<Result<IdentityUser>> CreateInitialIdentityUserAsync(
            string email, string password,
            CancellationToken cancellationToken = default);

        Task<NoContentResult> DeleteIdentityUserAsync(string identityUserId, CancellationToken cancellationToken);

    }
}
