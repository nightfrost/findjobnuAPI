using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FindjobnuService.MessageContracts;
using FindjobnuService.Repositories.Context;
using FindjobnuService.Models;

namespace JobAgentWorkerService.Services
{
    // Background scheduler: periodically scans for due JobAgents and publishes messages
    public class JobAgentScheduler : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<JobAgentScheduler> _logger;
        private readonly IPublishEndpoint _publisher;
        private readonly JobAgentSchedulerOptions _options;

        public JobAgentScheduler(IServiceProvider services, ILogger<JobAgentScheduler> logger, IPublishEndpoint publisher, IOptions<JobAgentSchedulerOptions> options)
        {
            _services = services;
            _logger = logger;
            _publisher = publisher;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JobAgentScheduler started with interval {IntervalSeconds}s", _options.IntervalSeconds);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<FindjobnuContext>();

                    var now = DateTime.UtcNow;
                    // Find enabled agents due to send (NextSendAt <= now) or never scheduled (NextSendAt null)
                    var dueAgents = await db.JobAgents
                        .Include(ja => ja.Profile)
                        .Where(ja => ja.Enabled && (ja.NextSendAt == null || ja.NextSendAt <= now))
                        .AsNoTracking()
                        .ToListAsync(stoppingToken);

                    foreach (var agent in dueAgents)
                    {
                        if (agent.Profile == null) continue;
                        await _publisher.Publish(new JobAgentEmailRequested(agent.ProfileId, agent.Profile.UserId, agent.Frequency.ToString()), stoppingToken);
                        _logger.LogInformation("Published JobAgentEmailRequested for ProfileId={ProfileId}", agent.ProfileId);

                        // Update schedule
                        using var updateScope = _services.CreateScope();
                        var updDb = updateScope.ServiceProvider.GetRequiredService<FindjobnuContext>();
                        var tracked = await updDb.JobAgents.FirstOrDefaultAsync(x => x.Id == agent.Id, stoppingToken);
                        if (tracked != null)
                        {
                            tracked.LastSentAt = now;
                            tracked.NextSendAt = GetNextSendAt(now, tracked.Frequency);
                            await updDb.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in JobAgentScheduler loop");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds)), stoppingToken);
                }
                catch (TaskCanceledException) { }
            }
        }

        private static DateTime GetNextSendAt(DateTime from, JobAgentFrequency freq)
        {
            return freq switch
            {
                JobAgentFrequency.Daily => from.AddDays(1),
                JobAgentFrequency.Monthly => from.AddMonths(1),
                _ => from.AddDays(7) // Weekly default
            };
        }
    }
}
