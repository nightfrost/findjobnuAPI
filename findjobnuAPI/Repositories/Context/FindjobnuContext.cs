using findjobnuAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace findjobnuAPI.Repositories.Context
{
    public class FindjobnuContext(DbContextOptions<FindjobnuContext> options) : DbContext(options)
    {
        public DbSet<JobIndexPosts> JobIndexPosts { get; set; }
        public DbSet<Cities> Companies { get; set; }
        public DbSet<UserProfile> UserProfile { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<JobIndexPosts>().ToTable("JobIndexPostingsExtended").HasKey(s => s.JobID);
            modelBuilder.Entity<JobIndexPosts>().HasIndex(s => s.JobUrl).IsUnique();
            modelBuilder.Entity<Cities>().ToTable("Cities").HasKey(s => s.Id);
            modelBuilder.Entity<UserProfile>().HasKey(up => up.Id);

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
            modelBuilder.Entity<UserProfile>()
                .Property(up => up.Keywords)
                .HasConversion(
                    v => string.Join(",", v ?? new List<string>()),
                    v => v.Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList()
                );
            modelBuilder.Entity<JobIndexPosts>()
                .Property(j => j.Keywords)
                .HasConversion(
                    v => string.Join(",", v ?? new List<string>()),
                    v => v.Split(',', System.StringSplitOptions.RemoveEmptyEntries).ToList()
                );
        }
    }
}
