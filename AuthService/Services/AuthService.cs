using AuthService.Data;
using AuthService.Entities;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
        private readonly ILogger<AuthService> _logger;

        public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, ApplicationDbContext dbContext, ILogger<AuthService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<RegisterResult> RegisterAsync(RegisterRequest request, bool isLinkedInUser = false)
        {
            _logger.LogInformation("RegisterAsync called for email: {Email}, isLinkedInUser: {IsLinkedInUser}", request.Email, isLinkedInUser);
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                PhoneNumber = request.Phone,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsLinkedInUser = isLinkedInUser,
                HasVerifiedLinkedIn = isLinkedInUser,
                LinkedInId = request.LinkedInId
            };
            var result = await _userManager.CreateAsync(user, request.Password);

            if (result.Succeeded)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() =>
                {
                    var token = _userManager.GenerateEmailConfirmationTokenAsync(user).GetAwaiter().GetResult();
                    var domain = _configuration["Domain"] ?? "";

                    var confirmationLink = $"https://auth.findjob.nu/api/auth/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";

                    _logger.LogInformation("Attempting to send confirmation email...");
                    try
                    {
                        SendEmailAsync(user.Email, "Confirm your email", $"Please confirm your account by clicking this link: <a href=\"{confirmationLink}\">Confirm Email</a>");
                        _logger.LogInformation("Email sent successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Email sending failed: {ErrorMessage}", ex);
                    }
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                var authResponse = await GenerateAuthResponseAsync(user);
                _logger.LogInformation("RegisterAsync succeeded for email: {Email}", request.Email);
                return new RegisterResult { Success = true, AuthResponse = authResponse };
            }

            string errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
            _logger.LogError("Registration Errors: {errorMessage}", errorMessage);
            return new RegisterResult { Success = false, ErrorMessage = errorMessage };
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request, bool isLinkedInUser = false)
        {
            _logger.LogInformation("LoginAsync called for email: {Email}, isLinkedInUser: {IsLinkedInUser}", request.Email, isLinkedInUser);
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("LoginAsync failed: No user exists with the given E-mail: {Email}", request.Email);
                return new LoginResult{ AuthResponse = null, ErrorMessage = "No user exists with the given E-mail.", Success = false};
            }

            if (isLinkedInUser && !user.IsLinkedInUser)
            {
                _logger.LogWarning("LoginAsync failed: User is not a LinkedIn user. Email: {Email}", request.Email);
                return new LoginResult { AuthResponse = null, ErrorMessage = "This user is not a LinkedIn user.", Success = false };
            }

            if (!isLinkedInUser && user.IsLinkedInUser)
            {
                _logger.LogWarning("LoginAsync failed: User is a LinkedIn user. Email: {Email}", request.Email);
                return new LoginResult { AuthResponse = null, ErrorMessage = "This user is a LinkedIn user.", Success = false };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var AuthResponse = await GenerateAuthResponseAsync(user);
                if (AuthResponse == null)
                {
                    _logger.LogError("LoginAsync failed: Failed to generate authentication response for email: {Email}", request.Email);
                    return new LoginResult { AuthResponse = null, ErrorMessage = "Failed to generate authentication response.", Success = false };
                }
                _logger.LogInformation("LoginAsync succeeded for email: {Email}", request.Email);
                return new LoginResult { AuthResponse = AuthResponse, Success = true };
            }

            _logger.LogWarning("LoginAsync failed: Invalid credentials for email: {Email}", request.Email);
            return new LoginResult { AuthResponse = null, ErrorMessage = "Invalid credentials.", Success = false };
        }

        public async Task<AuthResponse?> RefreshTokenAsync(TokenRefreshRequest request)
        {
            _logger.LogInformation("RefreshTokenAsync called for AccessToken: {AccessToken}", request.AccessToken);
            var principal = GetPrincipalFromExpiredToken(request.AccessToken);
            if (principal == null)
            {
                _logger.LogWarning("RefreshTokenAsync failed: Invalid principal from access token.");
                return null;
            }

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("RefreshTokenAsync failed: No userId in token.");
                return null;
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("RefreshTokenAsync failed: User not found for userId: {UserId}", userId);
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
                _logger.LogWarning("RefreshTokenAsync failed: Refresh token not found or not active for userId: {UserId}", userId);
                return null;
            }

            storedRefreshToken.Revoked = DateTime.UtcNow;
            storedRefreshToken.ReplacedByToken = GenerateRefreshToken(); _dbContext.RefreshTokens.Update(storedRefreshToken);

            var newAuthResponse = await GenerateAuthResponseAsync(user, storedRefreshToken.ReplacedByToken);

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("RefreshTokenAsync succeeded for userId: {UserId}", userId);
            return newAuthResponse;
        }

        public async Task<bool> RevokeRefreshTokenAsync(string userId, string refreshToken)
        {
            _logger.LogInformation("RevokeRefreshTokenAsync called for userId: {UserId}", userId);
            var storedRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

            if (storedRefreshToken == null || !storedRefreshToken.IsActive)
            {
                _logger.LogWarning("RevokeRefreshTokenAsync failed: Refresh token not found or not active for userId: {UserId}", userId);
                return false;
            }

            storedRefreshToken.Revoked = DateTime.UtcNow;
            _dbContext.RefreshTokens.Update(storedRefreshToken);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("RevokeRefreshTokenAsync succeeded for userId: {UserId}", userId);
            return true;
        }

        public async Task<bool> ConfirmEmailAsync(string userId, string token)
        {
            _logger.LogInformation("ConfirmEmailAsync called for userId: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ConfirmEmailAsync failed: User not found for userId: {UserId}", userId);
                return false;
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("ConfirmEmailAsync succeeded for userId: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("ConfirmEmailAsync failed for userId: {UserId}", userId);
            }
            return result.Succeeded;
        }
        public async Task<IdentityResult> UpdatePasswordAsync(string userId, string currentPassword, string newPassword)
        {
            _logger.LogInformation("UpdatePasswordAsync called for userId: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("UpdatePasswordAsync failed: User not found for userId: {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("UpdatePasswordAsync succeeded for userId: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("UpdatePasswordAsync failed for userId: {UserId}", userId);
            }
            return result;
        }

        public async Task<IdentityResult> LockoutUserAsync(string userId, DateTimeOffset? lockoutEnd = null)
        {
            _logger.LogInformation("LockoutUserAsync called for userId: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("LockoutUserAsync failed: User not found for userId: {UserId}", userId);
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }
            // Default lockout: 1 year if not specified
            var end = lockoutEnd ?? DateTimeOffset.UtcNow.AddYears(1);
            var result = await _userManager.SetLockoutEndDateAsync(user, end);
            if (result.Succeeded)
            {
                _logger.LogInformation("LockoutUserAsync succeeded for userId: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("LockoutUserAsync failed for userId: {UserId}", userId);
            }
            return result;
        }

        private async Task<AuthResponse> GenerateAuthResponseAsync(ApplicationUser user, string? existingRefreshToken = null)
        {
            // Logging is not required for private methods unless specifically requested
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
                FirstName = user.FirstName,
                LastName = user.LastName,
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                AccessTokenExpiration = expires,
                LinkedInId = user.LinkedInId,
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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetSection("JwtSettings")["SecretKey"]!)),
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
                _logger.LogError("Token validation error: {exception}", ex);
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

        public async Task<Tuple<bool, string?>> IsLinkedInUserOrHasVerifiedTheirLinkedIn(string userId)
        {
            _logger.LogInformation("IsLinkedInUserOrHasVerifiedTheirLinkedIn called for userId: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("IsLinkedInUserOrHasVerifiedTheirLinkedIn failed: User not found for userId: {UserId}", userId);
                return new Tuple<bool, string?>(false, null);
            }

            if (user.IsLinkedInUser || user.HasVerifiedLinkedIn)
            {
                _logger.LogInformation("IsLinkedInUserOrHasVerifiedTheirLinkedIn: User is LinkedIn user or has verified LinkedIn. userId: {UserId}", userId);
                return new Tuple<bool, string?>(true, user.LinkedInId);
            }

            _logger.LogInformation("IsLinkedInUserOrHasVerifiedTheirLinkedIn: User is not LinkedIn user and has not verified LinkedIn. userId: {UserId}", userId);
            return new Tuple<bool, string?>(false, null);
        }

        public async Task<UserInformationResult> GetUserInformationAsync(string userId)
        {
            _logger.LogInformation("GetUserInformationAsync called for userId: {UserId}", userId);
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("GetUserInformationAsync failed: User not found for userId: {UserId}", userId);
                return new UserInformationResult
                {
                    Success = false,
                    ErrorMessage = "User not found."
                };
            }

            _logger.LogInformation("GetUserInformationAsync succeeded for userId: {UserId}", userId);
            return new UserInformationResult
            {
                Success = true,
                UserInformation = MapToUserInformationDTO(user)
            };
        }

        private static UserInformationDTO MapToUserInformationDTO(ApplicationUser user)
        {
            return new UserInformationDTO
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber,
                IsLinkedInUser = user.IsLinkedInUser,
                HasVerifiedLinkedIn = user.HasVerifiedLinkedIn,
                LinkedInId = user.LinkedInId ?? "",
                LinkedInProfileUrl = user.LinkedInProfileUrl,
                LinkedInHeadline = user.LinkedInHeadline,
                LastLinkedInSync = user.LastLinkedInSync,
                CreatedAt = user.CreatedAt
            };
        }
    }
}