using FindjobnuService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;


namespace FindjobnuService.Repositories.Context
{
    public class FindjobnuContext(DbContextOptions<FindjobnuContext> options) : DbContext(options)
    {
        public DbSet<JobIndexPosts> JobIndexPosts { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Cities> Cities { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<Interest> Interests { get; set; }
        public DbSet<Accomplishment> Accomplishments { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<JobKeyword> JobKeywords { get; set; }
        public DbSet<JobAgent> JobAgents { get; set; }

        private static class ListStringConverterHelpers
        {
            public static string? ToCsv(List<string>? v) => v == null ? null : string.Join(",", v);
            public static List<string> FromCsv(string? v) => string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

            public static string? ToJson(List<string>? v) => v == null ? null : JsonConvert.SerializeObject(v);
            public static List<string> FromJsonWithCsvFallback(string? v)
            {
                if (string.IsNullOrWhiteSpace(v)) return new List<string>();
                try
                {
                    var list = JsonConvert.DeserializeObject<List<string>>(v);
                    if (list != null) return list;
                }
                catch { }
                return FromCsv(v);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<JobIndexPosts>().ToTable("JobIndexPostingsExtended").HasKey(s => s.JobID);
            modelBuilder.Entity<JobIndexPosts>().HasIndex(s => s.JobUrl).IsUnique();
            modelBuilder.Entity<Cities>().ToTable("Cities").HasKey(s => s.Id);
            modelBuilder.Entity<Profile>().HasKey(p => p.Id);
            modelBuilder.Entity<Profile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            var keywordsConverter = new ValueConverter<List<string>?, string>(
                v => v == null ? null : string.Join(",", v),
                v => string.IsNullOrWhiteSpace(v) ? new List<string>() : v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
            );
            var keywordsComparer = new ValueComparer<List<string>?>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v != null ? v.GetHashCode() : 0)),
                c => c == null ? null : c.ToList()
            );

            modelBuilder.Entity<Profile>()
                .Property(p => p.Keywords)
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

            // Profile relationships
            modelBuilder.Entity<Profile>()
                .OwnsOne(p => p.BasicInfo, b =>
                {
                    b.Property(bi => bi.FirstName).IsRequired().HasMaxLength(50);
                    b.Property(bi => bi.LastName).IsRequired().HasMaxLength(100);
                    b.Property(bi => bi.PhoneNumber).HasMaxLength(100);
                });
            modelBuilder.Entity<Profile>()
                .HasMany(p => p.Experiences)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Profile>()
                .HasMany(p => p.Educations)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Profile>()
                .HasMany(p => p.Interests)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Profile>()
                .HasMany(p => p.Accomplishments)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Profile>()
                .HasMany(p => p.Contacts)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Profile>()
                .HasMany(p => p.Skills)
                .WithOne(e => e.Profile)
                .HasForeignKey(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one: Profile <-> JobAgent
            modelBuilder.Entity<Profile>()
                .HasOne(p => p.JobAgent)
                .WithOne(ja => ja.Profile)
                .HasForeignKey<JobAgent>(ja => ja.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
