using AuthService.Services;
using AuthService.Data;
using AuthService.Entities;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

namespace FindjobnuTesting.Auth
{
    public class AuthServiceBasicTests
    {
        private (AuthService.Services.AuthService svc, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser>) Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            var db = new ApplicationDbContext(options);

            var store = new UserStore<ApplicationUser>(db);
            var userManager = new UserManager<ApplicationUser>(store, null, new PasswordHasher<ApplicationUser>(), new List<IUserValidator<ApplicationUser>>(), new List<IPasswordValidator<ApplicationUser>> { new PasswordValidator<ApplicationUser>() }, null, null, null, new Mock<ILogger<UserManager<ApplicationUser>>>().Object);

            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var identityOptions = Options.Create(new IdentityOptions());
            var claimsFactory = new UserClaimsPrincipalFactory<ApplicationUser>(userManager, identityOptions);
            var signInLogger = new Mock<ILogger<SignInManager<ApplicationUser>>>().Object;
            var schemeProvider = new Mock<IAuthenticationSchemeProvider>().Object;
            var userConfirmation = new Mock<IUserConfirmation<ApplicationUser>>().Object;
            var signInManager = new SignInManager<ApplicationUser>(userManager, contextAccessor.Object, claimsFactory, identityOptions, signInLogger, schemeProvider, userConfirmation);

            var configData = new Dictionary<string, string?>
            {
                {"JwtSettings:SecretKey", "supersecretkey1234567890asdasdasdasdasdasdasdasdasd"},
                {"JwtSettings:Issuer", "testIssuer"},
                {"JwtSettings:Audience", "testAudience"},
                {"JwtSettings:AccessTokenExpirationMinutes", "5"},
                {"JwtSettings:RefreshTokenExpirationDays", "7"}
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData!).Build();
            var logger = new Mock<ILogger<AuthService.Services.AuthService>>().Object;
            var svc = new AuthService.Services.AuthService(userManager, signInManager, configuration, db, logger);
            return (svc, db, userManager, signInManager);
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterAsync_Fails_WithWeakPassword()
        {
            var (svc, _, _, _) = Create();
            var result = await svc.RegisterAsync(new RegisterRequest { Email = "a@b.com", Password = "123", FirstName = "A", LastName = "B" });
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async System.Threading.Tasks.Task RegisterAndLogin_Succeeds()
        {
            var (svc, _, _, _) = Create();
            var reg = await svc.RegisterAsync(new RegisterRequest { Email = "c@d.com", Password = "Str0ngP@ss!", FirstName = "C", LastName = "D" });
            Assert.True(reg.Success);
            Assert.NotNull(reg.AuthResponse);
            var login = await svc.LoginAsync(new LoginRequest { Email = "c@d.com", Password = "Str0ngP@ss!" });
            Assert.True(login.Success);
            Assert.NotNull(login.AuthResponse);
        }

        [Fact]
        public async System.Threading.Tasks.Task Login_Fails_WithWrongPassword()
        {
            var (svc, _, _, _) = Create();
            var reg = await svc.RegisterAsync(new RegisterRequest { Email = "x@y.com", Password = "Str0ngP@ss!", FirstName = "X", LastName = "Y" });
            Assert.True(reg.Success);
            var login = await svc.LoginAsync(new LoginRequest { Email = "x@y.com", Password = "bad" });
            Assert.False(login.Success);
        }

        [Fact]
        public async System.Threading.Tasks.Task RefreshToken_Fails_WithInvalidTokens()
        {
            var (svc, _, _, _) = Create();
            var refresh = await svc.RefreshTokenAsync(new TokenRefreshRequest { AccessToken = "invalid", RefreshToken = "invalid" });
            Assert.Null(refresh);
        }

        [Fact]
        public async System.Threading.Tasks.Task RevokeRefreshToken_Fails_WhenNotFound()
        {
            var (svc, _, _, _) = Create();
            var ok = await svc.RevokeRefreshTokenAsync("nouser", "notoken");
            Assert.False(ok);
        }
    }
}
