using findjobnuAPI.Endpoints;
using findjobnuAPI.Repositories.Context;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace findjobnuAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            var connectionString = builder.Configuration.GetConnectionString("FindjobnuConnection") ?? throw new InvalidConfigurationException("Connection string 'FindjobnuConnection' not found.");
            builder.Services.AddDbContext<FindjobnuContext>(options =>
                options.UseSqlServer(connectionString));

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            if (string.IsNullOrEmpty(secretKey) || 
                string.IsNullOrEmpty(issuer) || 
                string.IsNullOrEmpty(audience))
            {
                throw new InvalidConfigurationException("JWT settings are not properly configured in appsettings.json.");
            }

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer, 
                        ValidAudience = audience, 
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
                    };
                });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IJobIndexPostsService, JobIndexPostsService>();
            builder.Services.AddScoped<IWorkProfileService, WorkProfileService>();
            builder.Services.AddScoped<ILinkedInProfileService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var dbContext = provider.GetRequiredService<FindjobnuContext>();
                var scraperSection = config.GetSection("LinkedInScraper") ?? throw new InvalidConfigurationException("LinkedInScraper section in Appsettings missing.");
                var scriptDirectory = scraperSection["LinkedInImporterPath"] ?? throw new InvalidConfigurationException("LinkedInScraper ScriptDirectory path missing.");
                var linkedInEmail = scraperSection["Username"] ?? throw new InvalidConfigurationException("LinkedInScraper E-mail missing.");
                var linkedInPassword = scraperSection["Password"] ?? throw new InvalidConfigurationException("LinkedInScraper Password missing.");
                return new LinkedInProfileService(
                    dbContext,
                    scriptDirectory,
                    linkedInEmail,
                    linkedInPassword,
                    provider.GetRequiredService<ILogger<LinkedInProfileService>>()
                );
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            });

            app.UseCors("AllowAll");

            app.UseSwagger();
            app.UseSwaggerUI();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapJobIndexPostsEndpoints();
            app.MapCitiesEndpoints();
            app.MapUserProfileEndpoints();
            app.MapWorkProfileEndpoints();

            app.Run();
        }
    }
}
