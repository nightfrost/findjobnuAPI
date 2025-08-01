using findjobnuAPI.Endpoints;
using findjobnuAPI.Repositories.Context;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace findjobnuAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            var connectionString = builder.Configuration.GetConnectionString("FindjobnuConnection") ?? throw new InvalidOperationException("Connection string 'FindjobnuConnection' not found.");
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
                throw new InvalidOperationException("JWT settings are not properly configured in appsettings.json.");
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
            builder.Services.AddScoped<IUserProfileService, UserProfileService>();
            builder.Services.AddScoped<IJobIndexPostsService, JobIndexPostsService>();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<ILinkedInProfileService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var dbContext = provider.GetRequiredService<FindjobnuContext>();
                var scraperSection = config.GetSection("LinkedInScraper");
                var scriptDirectory = scraperSection["LinkedInImporterPath"];
                var pythonExecutable = "python";
                var linkedInEmail = scraperSection["Username"];
                var linkedInPassword = scraperSection["Password"];
                return new LinkedInProfileService(
                    dbContext,
                    scriptDirectory ?? throw new ArgumentNullException(nameof(scriptDirectory)),
                    pythonExecutable,
                    linkedInEmail ?? throw new ArgumentNullException(nameof(linkedInEmail)),
                    linkedInPassword ?? throw new ArgumentNullException(nameof(linkedInPassword))
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

            app.Run();
        }
    }
}
