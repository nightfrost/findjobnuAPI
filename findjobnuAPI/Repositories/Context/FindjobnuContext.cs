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
        public DbSet<Cities> Companies { get; set; }
        public DbSet<UserProfile> UserProfile { get; set; }
        public DbSet<LinkedInProfile> LinkedInProfiles { get; set; }
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

            // Use Newtonsoft.Json serialization and value comparer for Keywords
            var keywordsConverter = new ValueConverter<List<string>, string>(
                v => v == null ? null : JsonConvert.SerializeObject(v),
                v => string.IsNullOrEmpty(v) ? new List<string>() : JsonConvert.DeserializeObject<List<string>>(v)
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

            // LinkedInProfile relationships
            modelBuilder.Entity<LinkedInProfile>()
                .OwnsOne(l => l.BasicInfo);
            modelBuilder.Entity<LinkedInProfile>()
                .HasMany(l => l.Experiences)
                .WithOne(e => e.LinkedInProfile)
                .HasForeignKey(e => e.LinkedInProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LinkedInProfile>()
                .HasMany(l => l.Educations)
                .WithOne(e => e.LinkedInProfile)
                .HasForeignKey(e => e.LinkedInProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LinkedInProfile>()
                .HasMany(l => l.Interests)
                .WithOne(e => e.LinkedInProfile)
                .HasForeignKey(e => e.LinkedInProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LinkedInProfile>()
                .HasMany(l => l.Accomplishments)
                .WithOne(e => e.LinkedInProfile)
                .HasForeignKey(e => e.LinkedInProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<LinkedInProfile>()
                .HasMany(l => l.Contacts)
                .WithOne(e => e.LinkedInProfile)
                .HasForeignKey(e => e.LinkedInProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // LinkedInProfile <-> UserProfile (required one-to-one)
            modelBuilder.Entity<LinkedInProfile>()
                .HasOne(l => l.UserProfile)
                .WithOne()
                .HasForeignKey<LinkedInProfile>(l => l.UserProfileId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
