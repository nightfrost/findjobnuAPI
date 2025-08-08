using Xunit;
using findjobnuAPI.Services;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace findjobnuAPI.Tests.Services
{
    public class WorkProfileServiceTests
    {
        private WorkProfileService GetServiceWithInMemoryDb(out FindjobnuContext context)
        {
            var options = new DbContextOptionsBuilder<FindjobnuContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            context = new FindjobnuContext(options);
            return new WorkProfileService(context);
        }

        private UserProfile CreateUserProfile(FindjobnuContext context, string userId = "user1")
        {
            var user = new UserProfile { Id = 1, UserId = userId, FirstName = "Test", LastName = "User" };
            context.UserProfile.Add(user);
            context.SaveChanges();
            return user;
        }

        [Fact]
        public async Task CreateAsync_AddsWorkProfile()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = CreateUserProfile(context);
            var profile = new WorkProfile { UserProfileId = user.Id, UserProfile = user, BasicInfo = new BasicInfo { Name = "Test" } };

            var result = await service.CreateAsync(profile);

            Assert.NotNull(result);
            Assert.Equal(user.Id, result!.UserProfileId);
            Assert.Equal("Test", result.BasicInfo!.Name);
        }

        [Fact]
        public async Task GetByUserProfileIdAsync_ReturnsProfileWithRelations()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = CreateUserProfile(context);
            var profile = new WorkProfile
            {
                UserProfileId = user.Id,
                UserProfile = user,
                BasicInfo = new BasicInfo { Name = "Test" },
                Experiences = new List<Experience> { new Experience { PositionTitle = "Dev" } },
                Educations = new List<Education> { new Education { Institution = "Uni" } },
                Interests = new List<Interest> { new Interest { Title = "Coding" } },
                Accomplishments = new List<Accomplishment> { new Accomplishment { Title = "Award" } },
                Contacts = new List<Contact> { new Contact { Name = "Contact1" } }
            };
            context.WorkProfiles.Add(profile);
            context.SaveChanges();

            var result = await service.GetByUserProfileIdAsync(user.Id);

            Assert.NotNull(result);
            Assert.Equal("Test", result!.BasicInfo!.Name);
            Assert.Single(result.Experiences!);
            Assert.Single(result.Educations!);
            Assert.Single(result.Interests!);
            Assert.Single(result.Accomplishments!);
            Assert.Single(result.Contacts!);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAllRelations()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = CreateUserProfile(context, "user2");
            var profile = new WorkProfile
            {
                UserProfileId = user.Id,
                UserProfile = user,
                BasicInfo = new BasicInfo { Name = "Old" },
                Experiences = new List<Experience> { new Experience { PositionTitle = "OldExp" } },
                Educations = new List<Education> { new Education { Institution = "OldUni" } },
                Interests = new List<Interest> { new Interest { Title = "OldInterest" } },
                Accomplishments = new List<Accomplishment> { new Accomplishment { Title = "OldAcc" } },
                Contacts = new List<Contact> { new Contact { Name = "OldContact" } }
            };
            context.WorkProfiles.Add(profile);
            context.SaveChanges();

            var updated = new WorkProfile
            {
                UserProfileId = user.Id,
                UserProfile = user,
                BasicInfo = new BasicInfo { Name = "New" },
                Experiences = new List<Experience> { new Experience { PositionTitle = "NewExp" } },
                Educations = new List<Education> { new Education { Institution = "NewUni" } },
                Interests = new List<Interest> { new Interest { Title = "NewInterest" } },
                Accomplishments = new List<Accomplishment> { new Accomplishment { Title = "NewAcc" } },
                Contacts = new List<Contact> { new Contact { Name = "NewContact" } }
            };

            var result = await service.UpdateAsync(profile.Id, updated, "user2");
            Assert.True(result);

            var dbProfile = await context.WorkProfiles
                .Include(wp => wp.Experiences)
                .Include(wp => wp.Educations)
                .Include(wp => wp.Interests)
                .Include(wp => wp.Accomplishments)
                .Include(wp => wp.Contacts)
                .FirstOrDefaultAsync(wp => wp.Id == profile.Id);

            Assert.NotNull(dbProfile);
            Assert.Equal("New", dbProfile!.BasicInfo!.Name);
            Assert.Single(dbProfile.Experiences!);
            Assert.Equal("NewExp", dbProfile.Experiences!.First().PositionTitle);
            Assert.Single(dbProfile.Educations!);
            Assert.Equal("NewUni", dbProfile.Educations!.First().Institution);
            Assert.Single(dbProfile.Interests!);
            Assert.Equal("NewInterest", dbProfile.Interests!.First().Title);
            Assert.Single(dbProfile.Accomplishments!);
            Assert.Equal("NewAcc", dbProfile.Accomplishments!.First().Title);
            Assert.Single(dbProfile.Contacts!);
            Assert.Equal("NewContact", dbProfile.Contacts!.First().Name);
        }

        [Fact]
        public async Task DeleteAsync_RemovesWorkProfile()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = CreateUserProfile(context, "user3");
            var profile = new WorkProfile { UserProfileId = user.Id, UserProfile = user, BasicInfo = new BasicInfo { Name = "ToDelete" } };
            context.WorkProfiles.Add(profile);
            context.SaveChanges();

            var result = await service.DeleteAsync(profile.Id, "user3");
            Assert.True(result);
            Assert.Null(await context.WorkProfiles.FindAsync(profile.Id));
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenNotFoundOrUnauthorized()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = CreateUserProfile(context, "user4");
            var profile = new WorkProfile { UserProfileId = user.Id, UserProfile = user, BasicInfo = new BasicInfo { Name = "Test" } };
            context.WorkProfiles.Add(profile);
            context.SaveChanges();

            var updated = new WorkProfile { UserProfileId = user.Id, UserProfile = user, BasicInfo = new BasicInfo { Name = "Updated" } };
            var result = await service.UpdateAsync(999, updated, "user4");
            Assert.False(result);
            result = await service.UpdateAsync(profile.Id, updated, "wronguser");
            Assert.False(result);
        }

        [Fact]
        public async Task CreateAsync_AddsWorkProfileWithSkills()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = CreateUserProfile(context);
            var profile = new WorkProfile
            {
                UserProfileId = user.Id,
                UserProfile = user,
                BasicInfo = new BasicInfo { Name = "Test" },
                Skills = new List<Skill> { new Skill { Name = "C#", Proficiency = SkillProficiency.Expert } }
            };

            var result = await service.CreateAsync(profile);

            Assert.NotNull(result);
            Assert.Single(result!.Skills!);
            Assert.Equal("C#", result.Skills!.First().Name);
            Assert.Equal(SkillProficiency.Expert, result.Skills!.First().Proficiency);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesSkills()
        {
            var service = GetServiceWithInMemoryDb(out var context);
            var user = CreateUserProfile(context, "user5");
            var profile = new WorkProfile
            {
                UserProfileId = user.Id,
                UserProfile = user,
                BasicInfo = new BasicInfo { Name = "Old" },
                Skills = new List<Skill> { new Skill { Name = "Java", Proficiency = SkillProficiency.Beginner } }
            };
            context.WorkProfiles.Add(profile);
            context.SaveChanges();

            var updated = new WorkProfile
            {
                UserProfileId = user.Id,
                UserProfile = user,
                BasicInfo = new BasicInfo { Name = "New" },
                Skills = new List<Skill> { new Skill { Name = "Python", Proficiency = SkillProficiency.Advanced } }
            };

            var result = await service.UpdateAsync(profile.Id, updated, "user5");
            Assert.True(result);

            var dbProfile = await context.WorkProfiles
                .Include(wp => wp.Skills)
                .FirstOrDefaultAsync(wp => wp.Id == profile.Id);

            Assert.NotNull(dbProfile);
            Assert.Single(dbProfile!.Skills!);
            Assert.Equal("Python", dbProfile.Skills!.First().Name);
            Assert.Equal(SkillProficiency.Advanced, dbProfile.Skills!.First().Proficiency);
        }
    }
}
