using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

using Moq;
using FindjobnuService.Repositories.Context;
using FindjobnuService.Models;
using FindjobnuService.Services;
using Microsoft.Extensions.Logging;

namespace FindjobnuTesting
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
            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            var service = new JobIndexPostsService(context, logger);

            var result = await service.GetAllAsync(1, 10);

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
        }

        [Fact]
        public async Task SearchAsync_FiltersByLocationAndCategory()
        {
            var context = GetDbContextWithData();
            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            var service = new JobIndexPostsService(context, logger);

            var result = await service.SearchAsync(null, "NY", "IT", null, null, 1);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Developer", result.Items.First().JobTitle);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectJob()
        {
            var context = GetDbContextWithData();
            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            var service = new JobIndexPostsService(context, logger);

            var job = await service.GetByIdAsync(1);

            Assert.NotNull(job);
            Assert.Equal("Developer", job.JobTitle);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsDistinctCategories()
        {
            var context = GetDbContextWithData();
            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            var service = new JobIndexPostsService(context, logger);

            var response = await service.GetCategoriesAsync();

            Assert.True(response.Success);
            Assert.Null(response.ErrorMessage);
            Assert.Contains("IT", response.CategoryAndAmountOfJobs.Keys);
            Assert.Contains("Design", response.CategoryAndAmountOfJobs.Keys);
            Assert.Equal(2, response.CategoryAndAmountOfJobs.Count);
        }

        [Fact]
        public async Task GetSavedJobsByUserId_ReturnsPagedList_WhenUserHasSavedJobs()
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new FindjobnuContext(options);
            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            // Add categories
            var itCategory = new Category { Name = "IT" };
            context.Categories.Add(itCategory);
            // Add jobs
            var job1 = new JobIndexPosts { JobID = 10, JobTitle = "Dev", Categories = new List<Category> { itCategory }, JobLocation = "NY", Published = DateTime.UtcNow };
            var job2 = new JobIndexPosts { JobID = 20, JobTitle = "QA", Categories = new List<Category> { itCategory }, JobLocation = "NY", Published = DateTime.UtcNow };
            context.JobIndexPosts.AddRange(job1, job2);
            // Add user with saved jobs
            var user = new Profile { Id = 1, UserId = "userX", BasicInfo = new BasicInfo { FirstName = "Test", LastName = "User" }, SavedJobPosts = new List<string> { "10", "20" } };
            context.Profiles.Add(user);
            context.SaveChanges();

            var service = new JobIndexPostsService(context, logger);
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
            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            var user = new Profile { Id = 2, UserId = "userY", BasicInfo = new BasicInfo { FirstName = "Test", LastName = "User" }, SavedJobPosts = new List<string>() };
            context.Profiles.Add(user);
            context.SaveChanges();

            var service = new JobIndexPostsService(context, logger);
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
            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            var service = new JobIndexPostsService(context, logger);
            var pagedList = await service.GetSavedJobsByUserId("no_such_user", 1);
            Assert.NotNull(pagedList);
            Assert.Equal(0, pagedList.TotalCount);
            Assert.Empty(pagedList.Items);
        }
    }
}