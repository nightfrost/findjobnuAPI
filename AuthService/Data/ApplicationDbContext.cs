using AuthService.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SharedInfrastructure.Cities;

namespace AuthService.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<City> Cities => Set<City>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure RefreshToken relationships
            builder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade); // Cascade delete if user is removed

            // Ensure the UserId in RefreshToken is indexed for faster lookups
            builder.Entity<RefreshToken>()
                .HasIndex(rt => rt.UserId);

            // Ensure the Token string in RefreshToken is unique
            builder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            builder.Entity<City>(entity =>
            {
                entity.ToTable("Cities");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Slug).IsRequired().HasMaxLength(128);
                entity.Property(c => c.ExternalId).IsRequired();
                entity.HasIndex(c => c.Name);
                entity.HasIndex(c => c.Slug).IsUnique();
                entity.HasIndex(c => c.ExternalId).IsUnique();
            });
        }
    }
}