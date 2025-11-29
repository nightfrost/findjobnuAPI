using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FindjobnuService.Repositories.Context;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FindjobnuTesting.Integration
{
    public class FindjobnuApiFactory : WebApplicationFactory<FindjobnuService.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Ensure test-friendly configuration: load env vars and disable MSSQL logging sink
            builder.ConfigureAppConfiguration((context, cfg) =>
            {
                cfg.AddEnvironmentVariables();
                // Optional: add an in-memory override to force console-only logging
                var overrides = new Dictionary<string, string>
                {
                    // Configure Serilog to use only Console in tests
                    {"Serilog:Using:0", "Serilog.Sinks.Console"},
                    {"Serilog:WriteTo:0:Name", "Console"},
                    {"Serilog:MinimumLevel:Default", "Information"}
                };
                cfg.AddInMemoryCollection(overrides);
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FindjobnuContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddDbContext<FindjobnuContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestsDb");
                });
            });
        }
    }
}
