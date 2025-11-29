namespace JobAgentWorkerService.Services
{
    public class JobAgentSchedulerOptions
    {
        // Polling interval in seconds
        public int IntervalSeconds { get; set; } = 300; // 5 minutes default
    }
}
