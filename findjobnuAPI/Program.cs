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
using Microsoft.OpenApi.Models;

namespace FindjobnuService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

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

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FindjobnuService API",
                    Version = "v1",
                    Description = "API documentation for FindjobnuService"
                });

                c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
                c.CustomSchemaIds(type => type.FullName);

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter only the token.",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new List<string>()
                    }
                });
            });

            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

            bool IsOriginAllowed(string origin)
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var o)) return false;

                foreach (var pattern in allowedOrigins)
                {
                    if (string.IsNullOrWhiteSpace(pattern)) continue;

                    if (pattern.Contains("*"))
                    {
                        if (pattern.StartsWith("https://*.", StringComparison.OrdinalIgnoreCase))
                        {
                            var domain = pattern.Substring("https://*.".Length);
                            if (string.Equals(o.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
                                (string.Equals(o.Host, domain, StringComparison.OrdinalIgnoreCase) ||
                                 o.Host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase)))
                            {
                                return true;
                            }
                        }
                        continue;
                    }

                    if (Uri.TryCreate(pattern, UriKind.Absolute, out var p))
                    {
                        var schemeOk = string.Equals(o.Scheme, p.Scheme, StringComparison.OrdinalIgnoreCase);
                        var hostOk = string.Equals(o.Host, p.Host, StringComparison.OrdinalIgnoreCase);
                        var portOk = p.IsDefaultPort || p.Port == -1 || p.Port == o.Port;
                        if (schemeOk && hostOk && portOk)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ConfiguredCors", policy =>
                {
                    policy
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .SetIsOriginAllowed(IsOriginAllowed);
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
