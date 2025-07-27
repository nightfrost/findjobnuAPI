using AuthService.Data;
using AuthService.Entities;
using AuthService.MessageBroker;
using AuthService.MessageBroker.Services;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly IRabbitMQPublisher<EmailConfirmationDto> _rabbitMQPublisher;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ApplicationDbContext dbContext, IRabbitMQPublisher<EmailConfirmationDto> publisher)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbContext = dbContext;
            _rabbitMQPublisher = publisher;
        }

        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                PhoneNumber = request.Phone
            };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var domain = _configuration["Domain"] ?? "";
                var confirmationLink = $"https://{domain}/api/auth/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";
                var body = $"Please confirm your account by clicking this link: <a href=\"{confirmationLink}\">Confirm Email</a>";
                var message = new EmailConfirmationDto
                {
                    Email = user.Email!,
                    Body = body,
                    ConfiirmationLink = confirmationLink
                };

                await _rabbitMQPublisher.PublishMessageAsync(message, RabbitMQQueues.EmailConfirmationQueue);




#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                //Task.Run(() =>
                //{
                //    var token = _userManager.GenerateEmailConfirmationTokenAsync(user).GetAwaiter().GetResult();

                //    var confirmationLink = $"https://auth.findjob.nu/api/auth/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";

                //    Console.WriteLine("Attempting to send confirmation email...");
                //    try
                //    {
                //        SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your account by clicking this link: <a href=\"{confirmationLink}\">Confirm Email</a>");
                //        Console.WriteLine("Email sent successfully.");
                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine($"Email sending failed: {ex}");
                //    }
                //});
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed


                return await GenerateAuthResponseAsync(user);
            }

            Console.WriteLine("Registration Errors: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            return null;
        }

        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return null;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                return await GenerateAuthResponseAsync(user);
            }

            return null;
        }

        public async Task<AuthResponse?> RefreshTokenAsync(TokenRefreshRequest request)
        {
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                return null;
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var storedRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.UserId == user.Id);

            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                if (storedRefreshToken != null && !storedRefreshToken.IsActive)
                {
                    await RevokeDescendantRefreshTokens(storedRefreshToken, user);
                }
                return null;
            }

            storedRefreshToken.Revoked = DateTime.UtcNow;
            storedRefreshToken.ReplacedByToken = GenerateRefreshToken(); _dbContext.RefreshTokens.Update(storedRefreshToken);

            var newAuthResponse = await GenerateAuthResponseAsync(user, storedRefreshToken.ReplacedByToken);

            await _dbContext.SaveChangesAsync();
            return newAuthResponse;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken)
        {
            var storedRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                return false;
            }

            storedRefreshToken.Revoked = DateTime.UtcNow;
            _dbContext.RefreshTokens.Update(storedRefreshToken);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }


        private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user, string? existingRefreshToken = null)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())             };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var accessTokenExpirationMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"]!);
            var refreshTokenExpirationDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"]!);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(accessTokenExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            string refreshTokenValue;

            if (existingRefreshToken != null)
            {
                refreshTokenValue = existingRefreshToken;
            }
            else
            {
                refreshTokenValue = GenerateRefreshToken();
                var newRefreshToken = new RefreshToken
                {
                    Token = refreshTokenValue,
                    Expires = DateTime.UtcNow.AddDays(refreshTokenExpirationDays),
                    UserId = user.Id,
                    Created = DateTime.UtcNow
                };
                await _dbContext.RefreshTokens.AddAsync(newRefreshToken);
                await _dbContext.SaveChangesAsync();
            }


            return new AuthResponse
            {
                UserId = user.Id,
                Email = user.Email!,
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                AccessTokenExpiration = expires
            };
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]!)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token algorithm");
                }

                return principal;
            }
            catch (SecurityTokenExpiredException)
            {
                var tokenHandlerExpired = new JwtSecurityTokenHandler();
                var expiredToken = tokenHandlerExpired.ReadJwtToken(token);
                var claims = new ClaimsIdentity(expiredToken.Claims);


                if (!claims.HasClaim(c => c.Type == JwtRegisteredClaimNames.Jti))
                {
                    claims.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
                }

                return new ClaimsPrincipal(claims);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation error: {ex.Message}");
                return null;
            }
        }

        private async Task RevokeDescendantRefreshTokens(RefreshToken refreshToken, ApplicationUser user)
        {
            var refreshTokens = await _dbContext.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.IsActive && rt.Created > refreshToken.Created)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.Revoked = DateTime.UtcNow;
                _dbContext.RefreshTokens.Update(token);
            }
            await _dbContext.SaveChangesAsync();
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"] ?? "25");
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var from = smtpSection["From"] ?? "noreply@findjob.nu";

            using var client = new System.Net.Mail.SmtpClient(host, port)
            {
                Credentials = new System.Net.NetworkCredential(username, password),
                EnableSsl = true,
                Timeout = 10000
            };

            var mailMessage = new System.Net.Mail.MailMessage(from, toEmail, subject, htmlMessage)
            {
                IsBodyHtml = true
            };

            await client.SendMailAsync(mailMessage);
        }
    }
}