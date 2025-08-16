using findjobnuAPI.Endpoints;
using findjobnuAPI.Repositories.Context;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.IdentityModel.Protocols.Configuration;
using Serilog;

namespace findjobnuAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            // Set up Serilog for console and MSSqlServer
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.MSSqlServer(
                    connectionString: builder.Configuration.GetConnectionString("FindjobnuConnection")!,
                    sinkOptions: new Serilog.Sinks.MSSqlServer.MSSqlServerSinkOptions
                    {
                        TableName = "Logs",
                        AutoCreateSqlTable = true
                    })
                .CreateLogger();

            builder.Host.UseSerilog();

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
            builder.Services.AddScoped<IProfileService, ProfileService>(provider =>
            {
                var db = provider.GetRequiredService<FindjobnuContext>();
                var jobService = provider.GetRequiredService<IJobIndexPostsService>();
                var logger = provider.GetRequiredService<ILogger<ProfileService>>();
                return new ProfileService(db, jobService, logger);
            });
            builder.Services.AddScoped<IJobIndexPostsService, JobIndexPostsService>(provider =>
            {
                var db = provider.GetRequiredService<FindjobnuContext>();
                var logger = provider.GetRequiredService<ILogger<JobIndexPostsService>>();
                return new JobIndexPostsService(db, logger);
            });
            builder.Services.AddScoped<ILinkedInProfileService>(provider =>
            {
                var config = provider.GetRequiredService<IConfiguration>();
                var context = provider.GetRequiredService<FindjobnuContext>();
                var logger = provider.GetRequiredService<ILogger<LinkedInProfileService>>();
                var scriptDirectory = config["LinkedIn:ScriptDirectory"] ?? "";
                var linkedInEmail = config["LinkedIn:Email"] ?? "";
                var linkedInPassword = config["LinkedIn:Password"] ?? "";
                return new LinkedInProfileService(context, scriptDirectory, linkedInEmail, linkedInPassword, logger);
            });

            // CV readability analyzer
            builder.Services.AddScoped<ICvReadabilityService, CvReadabilityService>();

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
            app.MapProfileEndpoints();
            app.MapCvReadabilityEndpoints();

            app.Run();
        }
    }
}
