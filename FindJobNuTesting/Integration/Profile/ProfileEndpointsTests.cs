using System.Net;
using System.Text;
using System.Text.Json;

namespace FindjobnuTesting.Integration.Profile
{
    public class ProfileEndpointsTests : IClassFixture<FindjobnuApiFactory>
    {
        private readonly FindjobnuApiFactory _factory;
        private readonly HttpClient _client;
        public ProfileEndpointsTests(FindjobnuApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetProfile_Unauthorized_WhenUserMismatch()
        {
            // No auth token present -> Unauthorized
            var response = await _client.GetAsync("/api/profile/someUser");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateProfile_Forbidden_WhenUserMismatch()
        {
            var payload = JsonSerializer.Serialize(new { userId = "abc" });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/profile", content);
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetBasicInfo_Unauthorized_WhenNoAuth()
        {
            var response = await _client.GetAsync("/api/profile/test/basicinfo");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
