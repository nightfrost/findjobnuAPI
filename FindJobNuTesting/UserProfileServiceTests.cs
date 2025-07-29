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
            var user = new UserProfile { Id = 1, UserId = "user1", FirstName = "John", LastName = "Doe", Keywords = new List<string> { "developer", "csharp" } };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var result = await service.GetByUserIdAsync("user1");

            Assert.NotNull(result);
            Assert.Equal("John", result!.FirstName);
            Assert.Contains("developer", result.Keywords);
        }

        [Fact]
        public async Task CreateAsync_AddsUserProfile()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { UserId = "user2", FirstName = "Jane", LastName = "Smith", Keywords = new List<string> { "qa" } };

            var result = await service.CreateAsync(user);

            Assert.NotNull(result);
            Assert.Equal("Jane", result!.FirstName);
            Assert.Contains("qa", result.Keywords);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUserProfile_WhenUserIdMatches()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 2, UserId = "user3", FirstName = "Alice", LastName = "Brown", Keywords = new List<string> { "old" } };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var updatedUser = new UserProfile { UserId = "user3", FirstName = "Alicia", LastName = "Brown", Keywords = new List<string> { "new" } };
            var result = await service.UpdateAsync(2, updatedUser, "user3");

            Assert.True(result);
            var dbUser = await context.UserProfile.FindAsync(2);
            Assert.Equal("Alicia", dbUser!.FirstName);
            Assert.Contains("new", dbUser.Keywords);
        }

        [Fact]
        public async Task GetSavedJobsByUserIdAsync_ReturnsSavedJobs()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 3, UserId = "user4", FirstName = "Bob", LastName = "Smith", SavedJobPosts = new List<string> { "1", "2" }, Keywords = new List<string> { "test" } };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var result = await service.GetSavedJobsByUserIdAsync("user4");

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("1", result);
            Assert.Contains("2", result);
        }

        [Fact]
        public async Task GetSavedJobsByUserIdAsync_ReturnsEmptyList_WhenUserNotFound()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var result = await service.GetSavedJobsByUserIdAsync("no_user");
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task SaveJobAsync_AddsJobToSavedList()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 4, UserId = "user5", FirstName = "Eve", LastName = "Adams", SavedJobPosts = new List<string>(), Keywords = new List<string> { "save" } };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var result = await service.SaveJobAsync("user5", "99");
            Assert.True(result);
            var dbUser = await context.UserProfile.FirstOrDefaultAsync(u => u.UserId == "user5");
            Assert.Contains("99", dbUser!.SavedJobPosts);
        }

        [Fact]
        public async Task SaveJobAsync_DoesNotAddDuplicateJob()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 5, UserId = "user6", FirstName = "Sam", LastName = "Lee", SavedJobPosts = new List<string> { "100" }, Keywords = new List<string> { "dup" } };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var result = await service.SaveJobAsync("user6", "100");
            Assert.False(result);
        }

        [Fact]
        public async Task SaveJobAsync_ReturnsFalse_WhenUserNotFound()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var result = await service.SaveJobAsync("no_user", "1");
            Assert.False(result);
        }

        [Fact]
        public async Task RemoveSavedJobAsync_RemovesJobFromSavedList()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 6, UserId = "user7", FirstName = "Tom", LastName = "Jones", SavedJobPosts = new List<string> { "200" }, Keywords = new List<string> { "remove" } };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var result = await service.RemoveSavedJobAsync("user7", "200");
            Assert.True(result);
            var dbUser = await context.UserProfile.FirstOrDefaultAsync(u => u.UserId == "user7");
            Assert.DoesNotContain("200", dbUser!.SavedJobPosts);
        }

        [Fact]
        public async Task RemoveSavedJobAsync_ReturnsFalse_WhenJobNotInList()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = new UserProfile { Id = 8, UserId = "user8", FirstName = "Ann", LastName = "White", SavedJobPosts = new List<string> { "300" }, Keywords = new List<string> { "notfound" } };
            context.UserProfile.Add(user);
            await context.SaveChangesAsync();

            var result = await service.RemoveSavedJobAsync("user8", "999");
            Assert.False(result);
        }

        [Fact]
        public async Task RemoveSavedJobAsync_ReturnsFalse_WhenUserNotFound()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var result = await service.RemoveSavedJobAsync("no_user", "1");
            Assert.False(result);
        }
    }
}
