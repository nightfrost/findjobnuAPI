using System.Net;
using System.Net.Http.Json;
using Xunit;
using FindjobnuTesting.Integration;
using FindjobnuService.Repositories.Context;
using FindjobnuService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

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
                    new FindjobnuService.Models.Cities { CityName = "New York" },
                    new FindjobnuService.Models.Cities { CityName = "Los Angeles" },
                    new FindjobnuService.Models.Cities { CityName = "Copenhagen" }
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
            var response = await _client.GetAsync("/api/Cities/search?query=Cope");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var cities = await response.Content.ReadFromJsonAsync<List<FindjobnuService.DTOs.Responses.CityResponse>>();
            Assert.NotNull(cities);
            Assert.Contains(cities!, c => c.CityName.Contains("Copenhagen"));
        }
    }
}
