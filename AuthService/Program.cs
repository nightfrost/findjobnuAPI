using AuthService.Data;
using AuthService.Endpoints;
using AuthService.Entities;
using AuthService.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AuthService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FindjobnuConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ClockSkew = TimeSpan.Zero 
    };
});

builder.Services.AddAuthorization();
builder.Services.AddLogging();
builder.Services.AddScoped<IAuthService, AuthService.Services.AuthService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ILinkedInAuthService, LinkedInAuthService>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AuthService API", Version = "v1" });

    // Define the security scheme for JWT Bearer tokens
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"JWT Authorization header using the Bearer scheme.
                      Enter 'Bearer' [space] and then your token in the text input below.
                      Example: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Add security requirement to operations that use the "Bearer" scheme
    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

// CORS: read allowed origins from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

bool IsOriginAllowed(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var o)) return false;

    foreach (var pattern in allowedOrigins)
    {
        if (string.IsNullOrWhiteSpace(pattern)) continue;

        // Wildcard subdomain support like https://*.findjob.nu
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
            var portOk = p.IsDefaultPort || p.Port == -1 || p.Port == o.Port; // allow any port if not specified
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
        // To support cookies, also call .AllowCredentials() and ensure origins are not "*"
    });
});

var app = builder.Build();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
});

// Enable CORS before other middleware
app.UseCors("ConfiguredCors");


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.MapAuthEndpoints();
app.MapLinkedInAuthEndpoints(); 

app.Run();