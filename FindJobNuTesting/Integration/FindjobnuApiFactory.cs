using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using FindjobnuService.Repositories.Context;
using System.Linq;
using Microsoft.AspNetCore.Hosting;

namespace FindjobnuTesting.Integration
{
    public class FindjobnuApiFactory : WebApplicationFactory<FindjobnuService.Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
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
