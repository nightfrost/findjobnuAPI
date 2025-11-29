using System.Net;
using Xunit;
using FindjobnuTesting.Integration;

namespace FindjobnuTesting.Integration.Cities
{
    public class CitiesEndpointsTests : IClassFixture<FindjobnuApiFactory>
    {
        private readonly HttpClient _client;
        public CitiesEndpointsTests(FindjobnuApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCityById_ReturnsNoContent_WhenNotFound()
        {
            var response = await _client.GetAsync("/api/Cities/9999");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task SearchCities_NoQuery_ReturnsNoContent()
        {
            var response = await _client.GetAsync("/api/Cities/search?query=");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
