using FindjobnuService.Models;
using FindjobnuService.Repositories.Context;
using FindjobnuService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

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

        private FindjobnuContext GetDbContextWithKeywords()
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new FindjobnuContext(options);

            var itCategory = new Category { Name = "IT" };
            context.Categories.Add(itCategory);
            var job1 = new JobIndexPosts { JobID = 10, JobTitle = "QA Engineer", JobDescription = "", JobLocation = "Copenhagen", Categories = new List<Category> { itCategory }, Published = DateTime.UtcNow.AddDays(-1) };
            var job2 = new JobIndexPosts { JobID = 20, JobTitle = "Systems Admin", JobDescription = "", JobLocation = "Aarhus", Categories = new List<Category> { itCategory }, Published = DateTime.UtcNow.AddDays(-2) };
            context.JobIndexPosts.AddRange(job1, job2);
            context.JobKeywords.AddRange(
                new JobKeyword { JobID = 10, Keyword = "automation" },
                new JobKeyword { JobID = 10, Keyword = "selenium" },
                new JobKeyword { JobID = 20, Keyword = "powershell" }
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
            var itCategoryId = context.Categories.First(c => c.Name == "IT").CategoryID;

            var result = await service.SearchAsync(null, "NY", itCategoryId, null, null, 1, 20);

            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal("Developer", result.Items.First().JobTitle);
        }

        //[Fact]
        //public async Task SearchAsync_IsCaseInsensitive()
        //{
        //    using var context = GetDbContextWithData();
        //    var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
        //    var service = new JobIndexPostsService(context, logger);

        //    var result = await service.SearchAsync("developer", null, null, null, null, 1, 20);

        //    Assert.NotNull(result);
        //    Assert.Contains(result.Items, j => j.JobTitle == "Developer");
        //}

        //[Fact]
        //public async Task SearchAsync_MatchesJobKeywordsTable()
        //{
        //    using var context = GetDbContextWithKeywords();
        //    var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
        //    var service = new JobIndexPostsService(context, logger);

        //    var result = await service.SearchAsync("automation", null, null, null, null, 1, 20);

        //    Assert.NotNull(result);
        //    Assert.Single(result.Items);
        //    Assert.Equal(10, result.Items.First().JobID);
        //}

        //[Fact]
        //public async Task SearchAsync_MatchesSerializedKeywords()
        //{
        //    using var context = GetDbContextWithKeywords();
        //    var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
        //    var service = new JobIndexPostsService(context, logger);

        //    var result = await service.SearchAsync("powershell", null, null, null, null, 1, 20);

        //    Assert.NotNull(result);
        //    Assert.Single(result.Items);
        //    Assert.Equal(20, result.Items.First().JobID);
        //}

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
            Assert.Contains(response.Categories, c => c.Name == "IT");
            Assert.Contains(response.Categories, c => c.Name == "Design");
            Assert.Equal(2, response.Categories.Count);
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

        //[Fact]
        //public async Task GetRecommendedJobs_UsesProfileKeywords()
        //{
        //    var options = new DbContextOptionsBuilder<FindjobnuContext>()
        //        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        //        .Options;
        //    using var context = new FindjobnuContext(options);
        //    var logger = new Mock<ILogger<JobIndexPostsService>>().Object;

        //    var itCategory = new Category { Name = "IT" };
        //    context.Categories.Add(itCategory);
        //    var job1 = new JobIndexPosts { JobID = 101, JobTitle = "C# Developer", JobDescription = "", JobLocation = "Copenhagen", Categories = new List<Category> { itCategory }, Published = DateTime.UtcNow };
        //    var job2 = new JobIndexPosts { JobID = 102, JobTitle = "Graphic Designer", JobDescription = "", JobLocation = "Copenhagen", Categories = new List<Category> { itCategory }, Published = DateTime.UtcNow };
        //    context.JobIndexPosts.AddRange(job1, job2);

        //    var profile = new Profile
        //    {
        //        UserId = "user1",
        //        BasicInfo = new BasicInfo { FirstName = "T", LastName = "U", JobTitle = "Developer" },
        //        Skills = new List<Skill> { new Skill { Name = "C#" } }
        //    };
        //    context.Profiles.Add(profile);
        //    context.SaveChanges();

        //    var service = new JobIndexPostsService(context, logger);
        //    var result = await service.GetRecommendedJobsByUserAndProfile("user1", 1, 20);

        //    Assert.NotNull(result);
        //    Assert.Contains(result.Items, j => j.JobID == 101);
        //    Assert.DoesNotContain(result.Items, j => j.JobID == 102);
        //}

        //[Fact]
        //public async Task GetRecommendedJobs_UsesJobKeywordsTable()
        //{
        //    var options = new DbContextOptionsBuilder<FindjobnuContext>()
        //        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        //        .Options;
        //    using var context = new FindjobnuContext(options);
        //    var logger = new Mock<ILogger<JobIndexPostsService>>().Object;

        //    var itCategory = new Category { Name = "IT" };
        //    context.Categories.Add(itCategory);
        //    var job1 = new JobIndexPosts { JobID = 201, JobTitle = "QA", JobDescription = "", JobLocation = "Copenhagen", Categories = new List<Category> { itCategory }, Published = DateTime.UtcNow };
        //    context.JobIndexPosts.Add(job1);
        //    context.JobKeywords.Add(new JobKeyword { JobID = 201, Keyword = "selenium" });

        //    var profile = new Profile
        //    {
        //        UserId = "user2",
        //        BasicInfo = new BasicInfo { FirstName = "T", LastName = "U" },
        //        Keywords = new List<string> { "selenium" }
        //    };
        //    context.Profiles.Add(profile);
        //    context.SaveChanges();

        //    var service = new JobIndexPostsService(context, logger);
        //    var result = await service.GetRecommendedJobsByUserAndProfile("user2", 1, 20);

        //    Assert.NotNull(result);
        //    Assert.Contains(result.Items, j => j.JobID == 201);
        //}

        [Fact]
        public async Task SearchAsync_LocationTokenMatchesMultipleDistricts()
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new FindjobnuContext(options);
            var category = new Category { Name = "IT" };
            context.Categories.Add(category);
            context.JobIndexPosts.AddRange(
                new JobIndexPosts { JobID = 100, JobTitle = "Backend Dev", JobLocation = "København K", Categories = new List<Category> { category }, Published = DateTime.UtcNow },
                new JobIndexPosts { JobID = 200, JobTitle = "Frontend Dev", JobLocation = "København V", Categories = new List<Category> { category }, Published = DateTime.UtcNow }
            );
            context.SaveChanges();

            var logger = new Mock<ILogger<JobIndexPostsService>>().Object;
            var service = new JobIndexPostsService(context, logger);

            var result = await service.SearchAsync(null, "København K", category.CategoryID, null, null, 1, 20);

            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Items, j => j.JobLocation == "København V");
            Assert.Contains(result.Items, j => j.JobLocation == "København K");
        }
    }
}