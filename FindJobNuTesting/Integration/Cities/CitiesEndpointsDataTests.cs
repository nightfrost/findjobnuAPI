using FindjobnuService.Repositories.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace FindjobnuTesting.Integration
{
    public class CitiesEndpointsDataTests : IClassFixture<FindjobnuApiFactory>
    {
        private readonly FindjobnuApiFactory _factory;
        private readonly HttpClient _client;
        public CitiesEndpointsDataTests(FindjobnuApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            Seed();
        }

        private void Seed()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FindjobnuContext>();
            if (!db.Cities.Any())
            {
                db.Cities.AddRange(
                    new SharedInfrastructure.Cities.City { Name = "New York" },
                    new SharedInfrastructure.Cities.City { Name = "Los Angeles" },
                    new SharedInfrastructure.Cities.City { Name = "København" }
                );
                db.SaveChanges();
            }
        }

        [Fact]
        public async Task GetAllCities_ReturnsData()
        {
            var response = await _client.GetAsync("/api/Cities");
            response.EnsureSuccessStatusCode();
            var cities = await response.Content.ReadFromJsonAsync<List<FindjobnuService.DTOs.Responses.CityResponse>>();
            Assert.NotNull(cities);
            Assert.True(cities!.Count >= 3);
        }

        [Fact]
        public async Task SearchCities_ReturnsMatches()
        {
            var response = await _client.GetAsync("/api/Cities/search?query=Køben");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cities = await response.Content.ReadFromJsonAsync<List<FindjobnuService.DTOs.Responses.CityResponse>>();
            Assert.NotNull(cities);
            Assert.Contains(cities!, c => c.Name.Contains("København", StringComparison.OrdinalIgnoreCase));
        }
    }
}
