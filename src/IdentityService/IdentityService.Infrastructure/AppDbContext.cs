using IdentityService.Domain.Entities;
using IdentityService.Infrastructure.Configurations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IdentityService.Infrastructure.Extensions;

namespace IdentityService.Infrastructure
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<OtpEntry> OtpEntries { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new RoleConfiguration());

            builder.Entity<ClientApp>()
                .Property(c => c.AllowedAudiences)
                .HasStringListConversion();

            builder.Entity<ClientApp>()
                .Property(c => c.AllowedScopes)
                .HasStringListConversion();

            builder.Entity<ClientApp>()
                .Property(c => c.RedirectUris)
                .HasStringListConversion();
        }
    }
}
