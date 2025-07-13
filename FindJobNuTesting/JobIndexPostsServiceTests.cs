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

            context.JobIndexPosts.AddRange(
                new JobIndexPosts { JobID = 1, JobTitle = "Developer", JobLocation = "NY", Category = "IT", Published = DateTime.UtcNow.AddDays(-1) },
                new JobIndexPosts { JobID = 2, JobTitle = "Designer", JobLocation = "LA", Category = "Design", Published = DateTime.UtcNow.AddDays(-2) }
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
    }
}