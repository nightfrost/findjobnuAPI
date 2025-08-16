using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace findjobnuAPI.Services
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
        private readonly ILogger<LinkedInProfileService> _logger;

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
            ILogger<LinkedInProfileService> logger)
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
            _logger.LogInformation("Getting LinkedIn profile for userId: {UserId}", userId);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("UserId is required for LinkedIn profile fetch.");
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
                    _logger.LogError("Failed to start the Python script process for userId: {UserId}", userId);
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
                    _logger.LogError("Python script exited with code {ExitCode} for userId: {UserId}. Error: {Error}", process.ExitCode, userId, error);
                    return new LinkedInProfileResult
                    {
                        Success = false,
                        Error = string.IsNullOrWhiteSpace(error) ? "Unknown error occurred" : error.Trim()
                    };
                }

                if (string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogError("No output received from script for userId: {UserId}", userId);
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
                if (result == null)
                {
                    _logger.LogError("Failed to parse script output for userId: {UserId}", userId);
                    return new LinkedInProfileResult
                    {
                        Success = false,
                        Error = "Failed to parse script output"
                    };
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while getting LinkedIn profile for userId: {UserId}", userId);
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
                _logger.LogInformation("Saving LinkedIn profile for userId: {UserId}", userid);
                await _context.Profiles.AddAsync(profile);
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("LinkedIn profile saved for userId: {UserId}", userid);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving LinkedIn profile for userId: {UserId}", userid);
                return false;
            }
        }
    }
}