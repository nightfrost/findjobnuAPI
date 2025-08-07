using findjobnuAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;


namespace findjobnuAPI.Repositories.Context
{
    public class FindjobnuContext(DbContextOptions<FindjobnuContext> options) : DbContext(options)
    {
        public DbSet<JobIndexPosts> JobIndexPosts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cities> Companies { get; set; }
        public DbSet<UserProfile> UserProfile { get; set; }
        public DbSet<WorkProfile> WorkProfiles { get; set; } // Renamed from LinkedInProfiles
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<Interest> Interests { get; set; }
        public DbSet<Accomplishment> Accomplishments { get; set; }
        public DbSet<Contact> Contacts { get; set; }

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

            // Use comma-separated serialization and value comparer for Keywords
            var keywordsConverter = new ValueConverter<List<string>, string>(
                v => v == null ? null : string.Join(",", v),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
            );
            var keywordsComparer = new ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                c => c == null ? null : c.ToList()
            );

            modelBuilder.Entity<UserProfile>()
                .Property(up => up.Keywords)
                .HasConversion(keywordsConverter)
                .Metadata.SetValueComparer(keywordsComparer);
            modelBuilder.Entity<JobIndexPosts>()
                .Property(j => j.Keywords)
                .HasConversion(keywordsConverter)
                .Metadata.SetValueComparer(keywordsComparer);

            // Many-to-many: JobIndexPosts <-> Category
            modelBuilder.Entity<JobIndexPosts>()
                .HasMany(j => j.Categories)
                .WithMany(c => c.JobIndexPosts)
                .UsingEntity<Dictionary<string, object>>(
                    "JobCategories",
                    j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryID").HasPrincipalKey("CategoryID").OnDelete(DeleteBehavior.Cascade),
                    c => c.HasOne<JobIndexPosts>().WithMany().HasForeignKey("JobID").HasPrincipalKey("JobID").OnDelete(DeleteBehavior.Cascade),
                    je =>
                    {
                        je.HasKey("JobID", "CategoryID");
                        je.ToTable("JobCategories");
                    }
                );

            // WorkProfile relationships (renamed from LinkedInProfile)
            modelBuilder.Entity<WorkProfile>()
                .OwnsOne(l => l.BasicInfo);
            modelBuilder.Entity<WorkProfile>()
                .HasMany(l => l.Experiences)
                .WithOne(e => e.WorkProfile)
                .HasForeignKey(e => e.WorkProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkProfile>()
                .HasMany(l => l.Educations)
                .WithOne(e => e.WorkProfile)
                .HasForeignKey(e => e.WorkProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkProfile>()
                .HasMany(l => l.Interests)
                .WithOne(e => e.WorkProfile)
                .HasForeignKey(e => e.WorkProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkProfile>()
                .HasMany(l => l.Accomplishments)
                .WithOne(e => e.WorkProfile)
                .HasForeignKey(e => e.WorkProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<WorkProfile>()
                .HasMany(l => l.Contacts)
                .WithOne(e => e.WorkProfile)
                .HasForeignKey(e => e.WorkProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // WorkProfile <-> UserProfile (required one-to-one)
            modelBuilder.Entity<WorkProfile>()
                .HasOne(l => l.UserProfile)
                .WithOne()
                .HasForeignKey<WorkProfile>(l => l.UserProfileId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
