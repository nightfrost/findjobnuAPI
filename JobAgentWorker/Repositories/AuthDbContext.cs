using JobAgentWorkerService.Models;
using Microsoft.EntityFrameworkCore;

namespace JobAgentWorkerService.Repositories
{
    public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
    {
        public DbSet<AspNetUser> AspNetUsers => Set<AspNetUser>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AspNetUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<AspNetUser>().HasKey(u => u.Id);
            modelBuilder.Entity<AspNetUser>().Property(u => u.Id).HasMaxLength(450);
        }
    }
}
