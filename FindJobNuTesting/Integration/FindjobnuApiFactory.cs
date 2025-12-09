using FindjobnuService.Repositories.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FindjobnuTesting.Integration
{
    public class FindjobnuApiFactory : WebApplicationFactory<FindjobnuService.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Force Testing environment early so Program uses the in-memory database
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, cfg) =>
            {
                cfg.AddEnvironmentVariables();
                var overrides = new Dictionary<string, string>
                {
                    {"Serilog:Using:0", "Serilog.Sinks.Console"},
                    {"Serilog:WriteTo:0:Name", "Console"},
                    {"Serilog:MinimumLevel:Default", "Information"},
                    {"ConnectionStrings:FindjobnuConnection", string.Empty}
                };
                cfg.AddInMemoryCollection(overrides);
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing DbContext registrations for FindjobnuContext
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<FindjobnuContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }
                var contextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(FindjobnuContext));
                if (contextDescriptor != null)
                {
                    services.Remove(contextDescriptor);
                }
                var factoryDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextFactory<FindjobnuContext>));
                if (factoryDescriptor != null)
                {
                    services.Remove(factoryDescriptor);
                }

                // Register a single InMemory provider for tests
                services.AddDbContext<FindjobnuContext>(options =>
                {
                    options.UseInMemoryDatabase("IntegrationTestsDb");
                });
            });
        }
    }
}
