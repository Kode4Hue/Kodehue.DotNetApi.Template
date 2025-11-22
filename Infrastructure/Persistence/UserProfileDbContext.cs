using Domain.Account.Users;
using Domain.Common.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    internal class UserProfileDbContext: DbContext
    {
        public UserProfileDbContext(DbContextOptions<UserProfileDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfileEntity> UserProfiles { get; set; }
        public DbSet<OutboxEntity> OutboxMessages { get; set; }
    }
}
