using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.IdentityModel.Protocols.Configuration;
using Serilog;
using FindjobnuService.Services;
using FindjobnuService.Repositories.Context;
using FindjobnuService.Endpoints;

namespace FindjobnuService
{
    public partial class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console();

            // Guard MSSQL sink for CI/Testing environments
            var isCi = builder.Environment.IsEnvironment("CI") || builder.Environment.IsEnvironment("Testing") ||
                       string.Equals(Environment.GetEnvironmentVariable("CI"), "true", StringComparison.OrdinalIgnoreCase);
            if (!isCi)
            {
                var sqlConn = builder.Configuration.GetConnectionString("FindjobnuConnection");
                if (!string.IsNullOrWhiteSpace(sqlConn))
                {
                    loggerConfig = loggerConfig.WriteTo.MSSqlServer(
                        connectionString: sqlConn,
                        sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                        {
                            TableName = "Logs",
                            AutoCreateSqlTable = true
                        });
                }
            }

            Log.Logger = loggerConfig.CreateLogger();
            builder.Host.UseSerilog();

            // Use InMemory for Testing, otherwise SQL Server
            if (builder.Environment.IsEnvironment("Testing"))
            {
                builder.Services.AddDbContext<FindjobnuContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTestsDb"));
            }
            else
            {
                var connectionString = builder.Configuration.GetConnectionString("FindjobnuConnection") ?? throw new InvalidConfigurationException("Connection string 'FindjobnuConnection' not found.");
                builder.Services.AddDbContext<FindjobnuContext>(options =>
                    options.UseSqlServer(connectionString));
            }

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

            // Ensure JWT bearer is the default scheme for authenticate/challenge
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
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
            builder.Services.AddScoped<IProfileService, ProfileService>();
            builder.Services.AddScoped<IJobIndexPostsService, JobIndexPostsService>();
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

            builder.Services.AddScoped<ICvReadabilityService, CvReadabilityService>();
            builder.Services.AddScoped<IJobAgentService, JobAgentService>();

            // Register Swagger services for minimal APIs/endpoints
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ConfiguredCors", policy =>
                {
                    policy
                        .WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            });

            app.UseCors("ConfiguredCors");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FindjobnuService API v1");
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHttpsRedirection();
            }

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapJobIndexPostsEndpoints();
            app.MapCitiesEndpoints();
            app.MapProfileEndpoints();
            app.MapCvReadabilityEndpoints();
            app.MapJobAgentEndpoints();

            app.Run();
        }
    }
}
