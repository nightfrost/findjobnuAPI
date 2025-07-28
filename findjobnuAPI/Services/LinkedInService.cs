using findjobnuAPI.Models;
using findjobnuAPI.Models.LinkedIn;
using findjobnuAPI.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace findjobnuAPI.Services
{
    public class LinkedInService : ILinkedInService
    {
        private readonly HttpClient _httpClient;
        private readonly FindjobnuContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LinkedInService> _logger;

        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _baseUrl = "https://api.linkedin.com/v2";
        private readonly string _authUrl = "https://www.linkedin.com/oauth/v2/authorization";
        private readonly string _tokenUrl = "https://www.linkedin.com/oauth/v2/accessToken";

        public LinkedInService(
            HttpClient httpClient, 
            FindjobnuContext context, 
            IConfiguration configuration,
            ILogger<LinkedInService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _configuration = configuration;
            _logger = logger;

            _clientId = _configuration["LinkedIn:ClientId"] ?? throw new InvalidOperationException("LinkedIn ClientId not configured");
            _clientSecret = _configuration["LinkedIn:ClientSecret"] ?? throw new InvalidOperationException("LinkedIn ClientSecret not configured");
        }

        public string GetAuthorizationUrl(string redirectUri, string state)
        {
            var scopes = "r_liteprofile r_emailaddress r_fullprofile w_member_social";
            var queryParams = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = _clientId,
                ["redirect_uri"] = redirectUri,
                ["state"] = state,
                ["scope"] = scopes
            };

            var queryString = string.Join("&", queryParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            return $"{_authUrl}?{queryString}";
        }

        public async Task<LinkedInAuthResponse?> ExchangeCodeForTokenAsync(string authorizationCode, string redirectUri)
        {
            try
            {
                var requestData = new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = authorizationCode,
                    ["redirect_uri"] = redirectUri,
                    ["client_id"] = _clientId,
                    ["client_secret"] = _clientSecret
                };

                var content = new FormUrlEncodedContent(requestData);
                var response = await _httpClient.PostAsync(_tokenUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<LinkedInAuthResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });
                    return authResponse;
                }

                _logger.LogError("Failed to exchange code for token. Status: {StatusCode}, Response: {Response}", 
                    response.StatusCode, await response.Content.ReadAsStringAsync());
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exchanging authorization code for token");
                return null;
            }
        }

        public async Task<bool> ConnectLinkedInAccountAsync(string userId, string authorizationCode, string redirectUri)
        {
            try
            {
                // Exchange code for token
                var authResponse = await ExchangeCodeForTokenAsync(authorizationCode, redirectUri);
                if (authResponse?.AccessToken == null)
                {
                    return false;
                }

                // Get basic profile information
                var userProfile = await GetLinkedInUserProfileAsync(authResponse.AccessToken);
                if (userProfile == null)
                {
                    return false;
                }

                // Create or update LinkedIn profile
                var existingProfile = await _context.LinkedInProfiles
                    .FirstOrDefaultAsync(lp => lp.UserProfileId == userId);

                if (existingProfile != null)
                {
                    // Update existing profile
                    existingProfile.LinkedInId = userProfile.Id;
                    existingProfile.LinkedInProfileUrl = userProfile.PublicProfileUrl;
                    existingProfile.Summary = userProfile.Summary;
                    existingProfile.Headline = userProfile.Headline;
                    existingProfile.Industry = userProfile.Industry;
                    existingProfile.Location = userProfile.Location?.Name;
                    existingProfile.ProfilePictureUrl = userProfile.ProfilePictureUrl;
                    existingProfile.AccessToken = EncryptToken(authResponse.AccessToken);
                    existingProfile.TokenExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn);
                    existingProfile.RefreshToken = !string.IsNullOrEmpty(authResponse.RefreshToken) 
                        ? EncryptToken(authResponse.RefreshToken) 
                        : existingProfile.RefreshToken;
                    existingProfile.LastSyncedAt = DateTime.UtcNow;
                }
                else
                {
                    // Create new profile
                    var linkedInProfile = new LinkedInProfile
                    {
                        UserProfileId = userId,
                        LinkedInId = userProfile.Id,
                        LinkedInProfileUrl = userProfile.PublicProfileUrl,
                        Summary = userProfile.Summary,
                        Headline = userProfile.Headline,
                        Industry = userProfile.Industry,
                        Location = userProfile.Location?.Name,
                        ProfilePictureUrl = userProfile.ProfilePictureUrl,
                        AccessToken = EncryptToken(authResponse.AccessToken),
                        TokenExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn),
                        RefreshToken = !string.IsNullOrEmpty(authResponse.RefreshToken) 
                            ? EncryptToken(authResponse.RefreshToken) 
                            : null,
                        LastSyncedAt = DateTime.UtcNow
                    };

                    _context.LinkedInProfiles.Add(linkedInProfile);
                }

                await _context.SaveChangesAsync();

                // Perform initial sync of detailed information
                await SyncLinkedInProfileAsync(userId, forceRefresh: true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting LinkedIn account for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SyncLinkedInProfileAsync(string userId, bool forceRefresh = false)
        {
            try
            {
                var linkedInProfile = await _context.LinkedInProfiles
                    .FirstOrDefaultAsync(lp => lp.UserProfileId == userId);
                if (linkedInProfile == default)                 {
                    _logger.LogWarning("LinkedIn profile not found for user {UserId}", userId);
                    return false;
                }

                var linkedInEducation = await _context.LinkedInEducations
                    .FirstOrDefaultAsync(le => le.LinkedInProfileId == linkedInProfile.Id);
                var linkedInExperience = await _context.LinkedInExperiences
                    .FirstOrDefaultAsync(le => le.LinkedInProfileId == linkedInProfile.Id);

                if (linkedInProfile?.AccessToken == null)
                {
                    return false;
                }

                // Check if token is expired and try to refresh
                if (linkedInProfile.TokenExpiresAt <= DateTime.UtcNow)
                {
                    var refreshSuccess = await RefreshLinkedInTokenAsync(userId);
                    if (!refreshSuccess)
                    {
                        return false;
                    }
                    // Reload the profile after refresh
                    linkedInProfile = await _context.LinkedInProfiles
                        .FirstOrDefaultAsync(lp => lp.UserProfileId == userId);
                }

                if (!forceRefresh && linkedInProfile!.LastSyncedAt.HasValue && 
                    linkedInProfile.LastSyncedAt.Value.AddHours(24) > DateTime.UtcNow)
                {
                    return true; // Recently synced, skip unless force refresh
                }

                var accessToken = DecryptToken(linkedInProfile!.AccessToken!);

                // Sync work experience
                var workExperience = await GetLinkedInPositionsAsync(accessToken);
                if (workExperience != null)
                {
                    if (linkedInExperience != default) {
                        _context.LinkedInExperiences.RemoveRange(_context.LinkedInExperiences.Where(le => le.LinkedInProfileId == linkedInProfile.Id));
                        await _context.LinkedInExperiences.AddRangeAsync(workExperience);
                    } else
                    {
                        await _context.LinkedInExperiences.AddRangeAsync(workExperience);
                    }
                }

                // Sync education
                var education = await GetLinkedInEducationAsync(accessToken);
                if (education != null)
                {
                    if (linkedInEducation != default) {
                        _context.LinkedInEducations.RemoveRange(_context.LinkedInEducations.Where(le => le.LinkedInProfileId == linkedInProfile.Id));
                        await _context.LinkedInEducations.AddRangeAsync(education);
                    } else
                    {
                        await _context.LinkedInEducations.AddRangeAsync(education);
                    }
                }

                // Sync skills
                var skills = await GetLinkedInSkillsAsync(accessToken);
                if (skills != null)
                {
                    linkedInProfile.Skills = skills;
                }

                linkedInProfile.LastSyncedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing LinkedIn profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<LinkedInProfile?> GetLinkedInProfileAsync(string userId)
        {
            return await _context.LinkedInProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(lp => lp.UserProfileId == userId);
        }

        public async Task<bool> DisconnectLinkedInAccountAsync(string userId)
        {
            try
            {
                var linkedInProfile = await _context.LinkedInProfiles
                    .FirstOrDefaultAsync(lp => lp.UserProfileId == userId);

                if (linkedInProfile != null)
                {
                    _context.LinkedInProfiles.Remove(linkedInProfile);
                    await _context.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting LinkedIn account for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> RefreshLinkedInTokenAsync(string userId)
        {
            try
            {
                var linkedInProfile = await _context.LinkedInProfiles
                    .FirstOrDefaultAsync(lp => lp.UserProfileId == userId);

                if (linkedInProfile?.RefreshToken == null)
                {
                    return false;
                }

                var refreshToken = DecryptToken(linkedInProfile.RefreshToken);
                var requestData = new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = _clientId,
                    ["client_secret"] = _clientSecret
                };

                var content = new FormUrlEncodedContent(requestData);
                var response = await _httpClient.PostAsync(_tokenUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var authResponse = JsonSerializer.Deserialize<LinkedInAuthResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });

                    if (authResponse?.AccessToken != null)
                    {
                        linkedInProfile.AccessToken = EncryptToken(authResponse.AccessToken);
                        linkedInProfile.TokenExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn);
                        if (!string.IsNullOrEmpty(authResponse.RefreshToken))
                        {
                            linkedInProfile.RefreshToken = EncryptToken(authResponse.RefreshToken);
                        }

                        await _context.SaveChangesAsync();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing LinkedIn token for user {UserId}", userId);
                return false;
            }
        }

        public async Task<List<Models.LinkedInExperience>?> GetWorkExperienceAsync(string userId)
        {
            var profile = await GetLinkedInProfileAsync(userId);
            return profile?.WorkExperience;
        }

        public async Task<List<Models.LinkedInEducation>?> GetEducationAsync(string userId)
        {
            var profile = await GetLinkedInProfileAsync(userId);
            return profile?.Education;
        }

        public async Task<List<string>?> GetSkillsAsync(string userId)
        {
            var profile = await GetLinkedInProfileAsync(userId);
            return profile?.Skills;
        }

        #region Private Helper Methods

        private async Task<LinkedInUserProfileResponse?> GetLinkedInUserProfileAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/people/~");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<LinkedInUserProfileResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LinkedIn user profile");
                return null;
            }
        }

        private async Task<List<Models.LinkedInExperience>?> GetLinkedInPositionsAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/people/~/positions");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var positionsResponse = JsonSerializer.Deserialize<LinkedInPositionsResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return positionsResponse?.Values?.Select(p => new Models.LinkedInExperience
                    {
                        CompanyName = p.Company?.Name,
                        JobTitle = p.Title,
                        Description = p.Summary,
                        StartDate = ConvertLinkedInDate(p.StartDate),
                        EndDate = ConvertLinkedInDate(p.EndDate),
                        IsCurrent = p.IsCurrent,
                        Location = p.Location?.Name
                    }).ToList();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LinkedIn positions");
                return null;
            }
        }

        private async Task<List<Models.LinkedInEducation>?> GetLinkedInEducationAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/people/~/educations");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var educationsResponse = JsonSerializer.Deserialize<Models.LinkedIn.LinkedInEducationsResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return educationsResponse?.Values?.Select(e => new Models.LinkedInEducation
                    {
                        SchoolName = e.SchoolName,
                        Degree = e.Degree,
                        FieldOfStudy = e.FieldOfStudy,
                        StartDate = ConvertLinkedInDate(e.StartDate),
                        EndDate = ConvertLinkedInDate(e.EndDate),
                        Description = e.Notes
                    }).ToList();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LinkedIn education");
                return null;
            }
        }

        private async Task<List<string>?> GetLinkedInSkillsAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
                var response = await _httpClient.GetAsync($"{_baseUrl}/people/~/skills");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var skillsResponse = JsonSerializer.Deserialize<LinkedInSkillsResponse>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                    return skillsResponse?.Values?.Select(s => s.Name).Where(name => !string.IsNullOrEmpty(name)).ToList()!;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting LinkedIn skills");
                return null;
            }
        }

        private DateTime? ConvertLinkedInDate(Models.LinkedIn.LinkedInDate? linkedInDate)
        {
            if (linkedInDate == null || linkedInDate.Year == 0)
                return null;

            try
            {
                return new DateTime(linkedInDate.Year, linkedInDate.Month > 0 ? linkedInDate.Month : 1, linkedInDate.Day > 0 ? linkedInDate.Day : 1);
            }
            catch
            {
                return null;
            }
        }

        private string EncryptToken(string token)
        {
            // Simple encryption - in production, use proper encryption with key management
            var key = Encoding.UTF8.GetBytes(_configuration["Encryption:Key"] ?? "DefaultEncryptionKey123456789012");
            using var aes = Aes.Create();
            aes.Key = key.Take(32).ToArray();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var encryptedBytes = encryptor.TransformFinalBlock(tokenBytes, 0, tokenBytes.Length);
            
            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
            
            return Convert.ToBase64String(result);
        }

        private string DecryptToken(string encryptedToken)
        {
            // Simple decryption - in production, use proper encryption with key management
            var key = Encoding.UTF8.GetBytes(_configuration["Encryption:Key"] ?? "DefaultEncryptionKey123456789012");
            var data = Convert.FromBase64String(encryptedToken);
            
            using var aes = Aes.Create();
            aes.Key = key.Take(32).ToArray();
            
            var iv = new byte[aes.IV.Length];
            var encryptedBytes = new byte[data.Length - iv.Length];
            
            Buffer.BlockCopy(data, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(data, iv.Length, encryptedBytes, 0, encryptedBytes.Length);
            
            aes.IV = iv;
            
            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        #endregion
    }
}