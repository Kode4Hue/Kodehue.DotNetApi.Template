namespace Application.Account.Users.DTOs
{
    public class UserProfile
    {
        public string Id { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? IdentityUserId { get; set; }

    }
}
