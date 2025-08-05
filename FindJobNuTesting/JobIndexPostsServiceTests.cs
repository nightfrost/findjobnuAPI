using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using findjobnuAPI.Services;

namespace findjobnuAPI.Tests.Services
{
    public class JobIndexPostsServiceTests
    {
        private FindjobnuContext GetDbContextWithData()
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new FindjobnuContext(options);

            var itCategory = new Category { Name = "IT" };
            var designCategory = new Category { Name = "Design" };
            context.Categories.AddRange(itCategory, designCategory);
            context.JobIndexPosts.AddRange(
                new JobIndexPosts { JobID = 1, JobTitle = "Developer", JobLocation = "NY", Categories = new List<Category> { itCategory }, Published = DateTime.UtcNow.AddDays(-1) },
                new JobIndexPosts { JobID = 2, JobTitle = "Designer", JobLocation = "LA", Categories = new List<Category> { designCategory }, Published = DateTime.UtcNow.AddDays(-2) }
            );
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task GetAllAsync_ReturnsPagedList()
        {
            var context = GetDbContextWithData();
            var service = new JobIndexPostsService(context);

            var result = await service.GetAllAsync(1, 10);

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task SearchAsync_FiltersByLocationAndCategory()
        {
            var context = GetDbContextWithData();
            var service = new JobIndexPostsService(context);

            var result = await service.SearchAsync(null, "NY", "IT", null, null, 1);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Developer", result.Items.First().JobTitle);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectJob()
        {
            var context = GetDbContextWithData();
            var service = new JobIndexPostsService(context);

            var job = await service.GetByIdAsync(1);

            Assert.NotNull(job);
            Assert.Equal("Developer", job.JobTitle);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsDistinctCategories()
        {
            var context = GetDbContextWithData();
            var service = new JobIndexPostsService(context);

            var categories = await service.GetCategoriesAsync();

            Assert.Contains("IT", categories);
            Assert.Contains("Design", categories);
            Assert.Equal(2, categories.Count);
        }

        [Fact]
        public async Task GetSavedJobsByUserId_ReturnsPagedList_WhenUserHasSavedJobs()
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new FindjobnuContext(options);
            // Add categories
            var itCategory = new Category { Name = "IT" };
            context.Categories.Add(itCategory);
            // Add jobs
            var job1 = new JobIndexPosts { JobID = 10, JobTitle = "Dev", Categories = new List<Category> { itCategory }, JobLocation = "NY", Published = DateTime.UtcNow };
            var job2 = new JobIndexPosts { JobID = 20, JobTitle = "QA", Categories = new List<Category> { itCategory }, JobLocation = "NY", Published = DateTime.UtcNow };
            context.JobIndexPosts.AddRange(job1, job2);
            // Add user with saved jobs
            var user = new UserProfile { Id = 1, UserId = "userX", FirstName = "Test", LastName = "User", SavedJobPosts = new List<string> { "10", "20" } };
            context.UserProfile.Add(user);
            context.SaveChanges();

            var service = new JobIndexPostsService(context);
            var pagedList = await service.GetSavedJobsByUserId("userX", 1);

            Assert.NotNull(pagedList);
            Assert.Equal(2, pagedList.TotalCount);
            Assert.Equal(2, pagedList.Items.Count());
        }

        [Fact]
        public async Task GetSavedJobsByUserId_ReturnsEmpty_WhenUserHasNoSavedJobs()
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new FindjobnuContext(options);
            var user = new UserProfile { Id = 2, UserId = "userY", FirstName = "Test", LastName = "User", SavedJobPosts = new List<string>() };
            context.UserProfile.Add(user);
            context.SaveChanges();

            var service = new JobIndexPostsService(context);
            var pagedList = await service.GetSavedJobsByUserId("userY", 1);

            Assert.NotNull(pagedList);
            Assert.Equal(0, pagedList.TotalCount);
            Assert.Empty(pagedList.Items);
        }

        [Fact]
        public async Task GetSavedJobsByUserId_ReturnsEmpty_WhenUserNotFound()
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new FindjobnuContext(options);
            var service = new JobIndexPostsService(context);
            var pagedList = await service.GetSavedJobsByUserId("no_such_user", 1);
            Assert.NotNull(pagedList);
            Assert.Equal(0, pagedList.TotalCount);
            Assert.Empty(pagedList.Items);
        }
    }
}