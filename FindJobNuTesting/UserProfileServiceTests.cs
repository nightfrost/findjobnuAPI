using Xunit;
using Moq;
using findjobnuAPI.Services;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace findjobnuAPI.Tests.Services
{
    public class UserProfileServiceTests
    {
        private UserProfileService GetServiceWithInMemoryDb(out FindjobnuContext context)
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            context = new FindjobnuContext(options);
            return new UserProfileService(context);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUserProfile_WhenExists()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 1, UserId = "user1", FirstName = "John", LastName = "Doe" };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var result = await service.GetByUserIdAsync("user1");

            Assert.NotNull(result);
            Assert.Equal("John", result!.FirstName);
        }

        [Fact]
        public async Task CreateAsync_AddsUserProfile()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { UserId = "user2", FirstName = "Jane", LastName = "Smith" };

            var result = await service.CreateAsync(user);

            Assert.NotNull(result);
            Assert.Equal("Jane", result!.FirstName);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUserProfile_WhenUserIdMatches()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 2, UserId = "user3", FirstName = "Alice", LastName = "Brown" };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var updatedUser = new UserProfile { UserId = "user3", FirstName = "Alicia", LastName = "Brown" };
            var result = await service.UpdateAsync(2, updatedUser, "user3");

            Assert.True(result);
            var dbUser = await context.UserProfile.FindAsync(2);
            Assert.Equal("Alicia", dbUser!.FirstName);
        }
    }
}
