using FindjobnuService.MessageContracts;
using FindjobnuService.Repositories.Context;
using FindjobnuService.Services;
using JobAgentWorkerService.Repositories;
using JobAgentWorkerService.Services;
using JobAgentWorkerService.Templates;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace JobAgentWorkerService.Consumers
{
    public class JobAgentEmailRequestedConsumer : IConsumer<JobAgentEmailRequested>
    {
        private readonly ILogger<JobAgentEmailRequestedConsumer> _logger;
        private readonly FindjobnuContext _db;
        private readonly AuthDbContext _authDb;
        private readonly IJobIndexPostsService _jobs;
        private readonly IEmailSender _email;
        private readonly IConfiguration _config;

        public JobAgentEmailRequestedConsumer(
            ILogger<JobAgentEmailRequestedConsumer> logger,
            FindjobnuContext db,
            AuthDbContext authDb,
            IJobIndexPostsService jobs,
            IEmailSender email,
            IConfiguration config)
        {
            _logger = logger;
            _db = db;
            _authDb = authDb;
            _jobs = jobs;
            _email = email;
            _config = config;
        }

        public async Task Consume(ConsumeContext<JobAgentEmailRequested> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Consuming JobAgentEmailRequested: ProfileId={ProfileId}, UserId={UserId}, Frequency={Frequency}", msg.ProfileId, msg.UserId, msg.Frequency);

            // Load profile and agent for token
            var profile = await _db.Profiles
                .Include(p => p.BasicInfo)
                .Include(p => p.JobAgent)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == msg.ProfileId);
            if (profile == null)
            {
                _logger.LogWarning("Profile not found: {ProfileId}", msg.ProfileId);
                return;
            }

            // Get user email from Auth database
            var user = await _authDb.AspNetUsers.AsNoTracking().FirstOrDefaultAsync(u => u.Id == msg.UserId);
            var toEmail = user?.Email;
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("No email found for UserId={UserId}; skipping email.", msg.UserId);
                return;
            }

            // Fetch top recommended jobs for this user; page 1
            var recommended = await _jobs.GetRecommendedJobsByUserAndProfile(msg.UserId, 1, 20);
            var items = recommended.Items?.Take(10).ToList() ?? [];

            if (items.Count == 0)
            {
                _logger.LogInformation("No recommended jobs to send for ProfileId={ProfileId}", msg.ProfileId);
                return;
            }

            var firstName = profile.BasicInfo?.FirstName ?? user?.FirstName ?? "there";

            // Build unsubscribe link from PublicBaseUrl and agent token
            var token = profile.JobAgent?.UnsubscribeToken ?? string.Empty;
            var baseUrl = _config["PublicBaseUrl"]?.TrimEnd('/') ?? "https://findjob.nu";
            var unsubscribeLink = string.IsNullOrWhiteSpace(token) ? string.Empty : $"{baseUrl}/api/jobagent/unsubscribe/{token}";

            var html = EmailTemplates.JobRecommendationsHtml(firstName, msg.Frequency, items, unsubscribeLink);
            var subject = $"Your {msg.Frequency} job recommendations";

            await _email.SendAsync(toEmail!, subject, html, context.CancellationToken);
            _logger.LogInformation("Sent job recommendations email to {Email} for ProfileId={ProfileId}", toEmail, msg.ProfileId);
        }
    }
}
