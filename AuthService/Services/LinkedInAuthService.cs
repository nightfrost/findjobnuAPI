using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using AuthService.Models;
using Microsoft.AspNetCore.Identity;
using AuthService.Entities;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Protocols.Configuration;

namespace AuthService.Services
{
    public class LinkedInAuthService : ILinkedInAuthService
    {
        private readonly IConfiguration _config;
        private readonly IAuthService _authService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private const string LINKEDIN_SCOPE = "openid email profile r_basicprofile";
        private const string LINKEDIN_AUTH_URL = "https://www.linkedin.com/oauth/v2/authorization";
        private const string LINKEDIN_TOKEN_URL = "https://www.linkedin.com/oauth/v2/accessToken";
        private const string LINKEDIN_USERINFO_URL = "https://api.linkedin.com/v2/userinfo";
        private const string LINKEDIN_PROFILE_URL = "https://api.linkedin.com/v2/me";
        private const string LINKEDIN_VANITY_BASE_URL = "https://www.linkedin.com/in/";

        public LinkedInAuthService(IConfiguration config, IAuthService authService, IHttpClientFactory httpClientFactory, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _config = config;
            _authService = authService;
            _httpClientFactory = httpClientFactory;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        private string GenerateLinkedInPassword(string id)
        {
            var secretKey = _config["LinkedInOAuth:PasswordSecretKey"];
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("LinkedIn password secret key is not configured.");
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(id));
            // Use base64url encoding (no padding, +/ replaced)
            var base64 = Convert.ToBase64String(hash).TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return $"LinkedIn-Ext-{base64}";
        }

        public async Task<IResult> HandleCallbackAsync(HttpContext context)
        {
            var code = context.Request.Query["code"].ToString();
            if (string.IsNullOrEmpty(code))
                return Results.BadRequest("Missing code from LinkedIn. Cannot Proceed.");

            var clientId = _config["LinkedInOAuth:ClientId"] ?? throw new InvalidConfigurationException("Missing LinkedInOAuth:ClientId configuration.");
            var clientSecret = _config["LinkedInOAuth:ClientSecret"] ?? throw new InvalidConfigurationException("Missing LinkedInOAuth:ClientSecret configuration.");
            var redirectUri = _config["LinkedInOAuth:RedirectUri"] ?? throw new InvalidConfigurationException("Missing LinkedInOAuth:RedirectUri configuration.");
            var findjobnuUri = _config["LinkedInOAuth:FindJobNuFrontendUrl"] ?? throw new InvalidConfigurationException("Missing LinkedInOAuth:FindJobNuUri configuration.");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
                return Results.BadRequest("LinkedIn OAuth configuration is missing. Cannot Proceed.");

            var http = _httpClientFactory.CreateClient();
            var tokenResponse = await http.PostAsync(
                LINKEDIN_TOKEN_URL,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "authorization_code" },
                    { "code", code },
                    { "redirect_uri", redirectUri },
                    { "client_id", clientId },
                    { "client_secret", clientSecret }
                }));
            if (!tokenResponse.IsSuccessStatusCode)
                return Results.BadRequest("Failed to get access token");

            var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
            using var tokenDoc = JsonDocument.Parse(tokenJson);
            var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userInfoResponse = await http.GetAsync(LINKEDIN_USERINFO_URL);
            if (!userInfoResponse.IsSuccessStatusCode)
                return Results.BadRequest("Failed to fetch LinkedIn user info");

            var profileResponse = await http.GetAsync(LINKEDIN_PROFILE_URL);
            if (!profileResponse.IsSuccessStatusCode)
                return Results.BadRequest("Failed to fetch LinkedIn profile");

            var userInfoJson = await userInfoResponse.Content.ReadAsStringAsync();
            var profileJson = await profileResponse.Content.ReadAsStringAsync();
            using var userInfoDoc = JsonDocument.Parse(userInfoJson);
            using var profileDoc = JsonDocument.Parse(profileJson);
            var email = userInfoDoc.RootElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
            var firstName = userInfoDoc.RootElement.TryGetProperty("given_name", out var firstNameProp) ? firstNameProp.GetString() : null;
            var lastName = userInfoDoc.RootElement.TryGetProperty("family_name", out var lastNameProp) ? lastNameProp.GetString() : null;
            var id = userInfoDoc.RootElement.TryGetProperty("sub", out var idProp) ? idProp.GetString() : null;
            var vanityUrl = profileDoc.RootElement.TryGetProperty("vanityName", out var vanityProp) ? vanityProp.GetString() : null;
            var headline = profileDoc.RootElement.TryGetProperty("localizedHeadline", out var headlineProp) ? headlineProp.GetString() : null;

            if (string.IsNullOrEmpty(email))
                return Results.BadRequest("Email not found in LinkedIn user info. Cannot create account.");
            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
                return Results.BadRequest("First name or last name not found in LinkedIn user info. Cannot create account.");
            if (string.IsNullOrEmpty(id))
                return Results.BadRequest("LinkedIn ID not found in user info. Cannot create account.");

            var linkedInPassword = GenerateLinkedInPassword(id);

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                if (user.IsLinkedInUser == false)
                {
                    user.HasVerifiedLinkedIn = true;
                    user.LinkedInId = id;
                    user.LinkedInProfileUrl = string.Concat(LINKEDIN_VANITY_BASE_URL + vanityUrl) ?? string.Empty;
                    user.LinkedInHeadline = headline ?? string.Empty;
                    user.EmailConfirmed = true;
                    user.LastLinkedInSync = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                    return Results.Ok("Account has been linked");
                }

                var signInResult = await _signInManager.CheckPasswordSignInAsync(user, linkedInPassword, lockoutOnFailure: false);
                if (signInResult.Succeeded)
                {
                    var authResponse = await _authService.LoginAsync(new LoginRequest { Email = email, Password = linkedInPassword }, isLinkedInUser: true);
                    if (authResponse == null || authResponse.AuthResponse == null || !authResponse.Success)
                        return Results.BadRequest("Failed to create auth response for existing LinkedIn user.");
                    authResponse.AuthResponse.FindJobNuUri = findjobnuUri;
                    return RedirectIfSuccess(authResponse);
                }
                return Results.BadRequest("Failed to sign in existing user using LinkedIn values.");
            } else
            {
                var applicationUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    LinkedInId = id,
                    LinkedInProfileUrl = string.Concat(LINKEDIN_VANITY_BASE_URL + vanityUrl) ?? string.Empty,
                    LinkedInHeadline = headline ?? string.Empty,
                    IsLinkedInUser = true,
                    HasVerifiedLinkedIn = true,
                    EmailConfirmed = true,
                    LastLinkedInSync = DateTime.UtcNow
                };
                var registerResult = await _userManager.CreateAsync(applicationUser, linkedInPassword);

                if (registerResult.Succeeded)
                {
                    var signInResult = await _signInManager.CheckPasswordSignInAsync(applicationUser, linkedInPassword, lockoutOnFailure: false);
                    if (signInResult.Succeeded)
                    {
                        var authResponse = await _authService.LoginAsync(new LoginRequest { Email = email, Password = linkedInPassword }, isLinkedInUser: true);
                        if (authResponse == null || authResponse.AuthResponse == null  || !authResponse.Success)
                            return Results.BadRequest("Failed to create auth response for new LinkedIn user.");
                        authResponse.AuthResponse.FindJobNuUri = findjobnuUri;
                        return RedirectIfSuccess(authResponse);
                    }
                    return Results.BadRequest("Failed to sign in new user using LinkedIn values.");
                }

                return Results.BadRequest("Something went wrong.");
            }
        }

        public string GetLoginUrl()
        {
            var clientId = _config["LinkedInOAuth:ClientId"];
            var redirectUri = _config["LinkedInOAuth:RedirectUri"];
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(redirectUri))
                throw new ArgumentException("LinkedIn OAuth configuration is missing. Cannot generate login URL.");

            var state = Guid.NewGuid().ToString();
            var authUrl = $"{LINKEDIN_AUTH_URL}?response_type=code&client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri ?? throw new ArgumentNullException(nameof(redirectUri)))}&state={state}&scope={Uri.EscapeDataString(LINKEDIN_SCOPE)}";
            return authUrl;
        }

        private IResult RedirectIfSuccess(LoginResult loginResult)
        {
            if (loginResult.Success && loginResult.AuthResponse != null)
            {
                var findjobnuUri = _config["LinkedInOAuth:FindJobNuFrontendUrl"] ?? throw new InvalidConfigurationException("Missing LinkedInOAuth:FindJobNuUri configuration.");
                var query = findjobnuUri + $"?userId={Uri.EscapeDataString(loginResult.AuthResponse.UserId)}" +
                            $"&email={Uri.EscapeDataString(loginResult.AuthResponse.Email)}" +
                            $"&firstName={Uri.EscapeDataString(loginResult.AuthResponse.FirstName)}" +
                            $"&lastName={Uri.EscapeDataString(loginResult.AuthResponse.LastName)}" +
                            $"&accessToken={Uri.EscapeDataString(loginResult.AuthResponse.AccessToken)}" +
                            $"&refreshToken={Uri.EscapeDataString(loginResult.AuthResponse.RefreshToken)}" +
                            $"&accessTokenExpiration={Uri.EscapeDataString(loginResult.AuthResponse.AccessTokenExpiration.ToString("o"))}" +
                            $"&isLinkedInUser={Uri.EscapeDataString(loginResult.AuthResponse.LinkedInId ?? "")}";
                return Results.Redirect($"{query}");
            }
            else if (loginResult.ErrorMessage != null)
            {
                return Results.BadRequest(loginResult.ErrorMessage);
            }

            return Results.BadRequest("Login failed for an unknown reason.");
            
        }
    }
}
