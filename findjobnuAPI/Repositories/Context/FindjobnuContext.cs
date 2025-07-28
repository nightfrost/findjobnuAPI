using findjobnuAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace findjobnuAPI.Repositories.Context
{
    public class FindjobnuContext(DbContextOptions<FindjobnuContext> options) : DbContext(options)
    {
        public DbSet<JobIndexPosts> JobIndexPosts { get; set; }
        public DbSet<Cities> Companies { get; set; }
        public DbSet<UserProfile> UserProfile { get; set; }
        public DbSet<LinkedInProfile> LinkedInProfiles { get; set; }
        public DbSet<LinkedInExperience> LinkedInExperiences { get; set; }
        public DbSet<LinkedInEducation> LinkedInEducations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<JobIndexPosts>().ToTable("JobIndexPostings").HasKey(s => s.JobID);
            modelBuilder.Entity<Cities>().ToTable("Cities").HasKey(s => s.Id);
            modelBuilder.Entity<UserProfile>().HasKey(up => up.Id);
            modelBuilder.Entity<LinkedInProfile>().HasKey(lp => lp.Id);
            modelBuilder.Entity<LinkedInExperience>().HasKey(le => le.Id);
            modelBuilder.Entity<LinkedInEducation>().HasKey(le => le.Id);

            // UserProfile Configuration
            modelBuilder.Entity<UserProfile>()
                .Property(up => up.FirstName)
                .IsRequired()
                .HasMaxLength(50);
            modelBuilder.Entity<UserProfile>()
                .Property(up => up.LastName)
                .IsRequired()
                .HasMaxLength(100);
            modelBuilder.Entity<UserProfile>()
                .Property(up => up.PhoneNumber)
                .HasMaxLength(100);
            modelBuilder.Entity<UserProfile>()
                .HasIndex(up => up.UserId)
                .IsUnique();

            // LinkedIn Profile Configuration
            modelBuilder.Entity<LinkedInProfile>()
                .HasIndex(lp => lp.UserProfileId)
                .IsUnique();

            modelBuilder.Entity<LinkedInProfile>()
                .Property(lp => lp.Summary)
                .HasMaxLength(2000);

            modelBuilder.Entity<LinkedInProfile>()
                .Property(lp => lp.Headline)
                .HasMaxLength(500);

            modelBuilder.Entity<LinkedInProfile>()
                .Property(e => e.Skills)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null));

            modelBuilder.Entity<LinkedInExperience>()
                .Property(le => le.LinkedInProfileId)
                .IsRequired();

            modelBuilder.Entity<LinkedInEducation>()
                .Property(le => le.LinkedInProfileId)
                .IsRequired();

            // Relationship Configuration
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.LinkedInProfile)
                .WithOne()
                .HasForeignKey<LinkedInProfile>(lp => lp.UserProfileId)
                .HasPrincipalKey<UserProfile>(up => up.UserId);
        }
    }
}
