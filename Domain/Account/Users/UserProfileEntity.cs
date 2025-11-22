namespace Domain.Account.Users
{
    public class UserProfileEntity
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? IdentityUserId { get; set; }
        public required string Email { get; set; }
        public required bool EmailVerified { get; set; }
    }
}
