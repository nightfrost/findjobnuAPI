using FindjobnuService.Models;
using FindjobnuService.Repositories.Context;
using System.Diagnostics;
using System.Text.Json;

namespace FindjobnuService.Services
{
    /// <summary>
    /// Service for importing LinkedIn profile data using the Python script
    /// </summary>
    public class LinkedInProfileService : ILinkedInProfileService
    {
        private readonly string _scriptDirectory;
        private readonly string _pythonExecutable;
        private readonly string _linkedInEmail;
        private readonly string _linkedInPassword;
        private readonly FindjobnuContext _context;
        private readonly ILogger _logger;

        /// <summary>
        /// Initialize the LinkedIn Profile Service
        /// </summary>
        /// <param name="scriptDirectory">Directory containing the Python script</param>
        /// <param name="pythonExecutable">Path to Python executable (default: "python")</param>
        /// <param name="linkedInEmail">LinkedIn account email</param>
        /// <param name="linkedInPassword">LinkedIn account password</param>
        public LinkedInProfileService(
            FindjobnuContext context,
            string scriptDirectory,
            string linkedInEmail,
            string linkedInPassword,
            ILogger logger)
        {
            _scriptDirectory = scriptDirectory;
            _pythonExecutable = "python";
            _linkedInEmail = linkedInEmail;
            _linkedInPassword = linkedInPassword;
            _context = context;
            _logger = logger;

            if (!Directory.Exists(_scriptDirectory))
            {
                throw new DirectoryNotFoundException($"Script directory not found: {_scriptDirectory}");
            }

            var scriptPath = Path.Combine(_scriptDirectory, "main.py");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException($"Python script not found: {scriptPath}");
            }
        }

        /// <summary>
        /// Get LinkedIn profile information for the specified user ID
        /// </summary>
        /// <param name="userId">LinkedIn user ID (public identifier)</param>
        /// <returns>LinkedIn profile result</returns>
        public async Task<LinkedInProfileResult> GetProfileAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return new LinkedInProfileResult
                {
                    Success = false,
                    Error = "User ID is required"
                };
            }

            try
            {
                var scriptPath = Path.Combine(_scriptDirectory, "main.py");
                var arguments = $"\"{scriptPath}\" \"{userId}\"";

                // Add credentials if provided
                if (!string.IsNullOrWhiteSpace(_linkedInEmail))
                {
                    arguments += $" --email \"{_linkedInEmail}\"";
                }
                if (!string.IsNullOrWhiteSpace(_linkedInPassword))
                {
                    arguments += $" --password \"{_linkedInPassword}\"";
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = _pythonExecutable,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _scriptDirectory
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return new LinkedInProfileResult
                    {
                        Success = false,
                        Error = "Failed to start the Python script process"
                    };
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                var output = await outputTask;
                var error = await errorTask;

                if (process.ExitCode != 0)
                {
                    return new LinkedInProfileResult
                    {
                        Success = false,
                        Error = string.IsNullOrWhiteSpace(error) ? "Unknown error occurred" : error.Trim()
                    };
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    return new LinkedInProfileResult
                    {
                        Success = false,
                        Error = "No output received from script"
                    };
                }

                // Parse the JSON response
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = JsonSerializer.Deserialize<LinkedInProfileResult>(output, options);
                return result ?? new LinkedInProfileResult
                {
                    Success = false,
                    Error = "Failed to parse script output"
                };
            }
            catch (Exception ex)
            {
                return new LinkedInProfileResult
                {
                    Success = false,
                    Error = $"Exception occurred: {ex.Message}"
                };
            }
        }

        public async Task<bool> SaveProfileAsync(string userid, Profile profile)
        {
            try
            {
                // Ensure required fields are set to avoid NULL constraint errors
                profile.BasicInfo ??= new BasicInfo();
                profile.HasJobAgent = profile.JobAgent?.Enabled ?? false;

                await _context.Profiles.AddAsync(profile);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error saving profile. {ExceptionMessage}", ex.Message);
                return false;
            }
        }
    }
}