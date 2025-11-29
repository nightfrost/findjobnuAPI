using System.Net;
using Xunit;
using FindjobnuTesting.Integration;

namespace FindjobnuTesting.Integration.JobAgent
{
    public class JobAgentEndpointsUnauthorizedTests : IClassFixture<FindjobnuApiFactory>
    {
        private readonly HttpClient _client;
        public JobAgentEndpointsUnauthorizedTests(FindjobnuApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetJobAgent_Unauthorized_WithoutAuth()
        {
            var response = await _client.GetAsync("/api/jobagent/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrUpdateJobAgent_Unauthorized_WithoutAuth()
        {
            var response = await _client.PostAsync("/api/jobagent/1", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUnsubscribeLink_Unauthorized_WithoutAuth()
        {
            var response = await _client.GetAsync("/api/jobagent/1/unsubscribe-link");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
