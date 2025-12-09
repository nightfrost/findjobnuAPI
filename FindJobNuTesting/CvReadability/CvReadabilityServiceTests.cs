using FindjobnuService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace FindjobnuTesting.CvReadability
{
    public class CvReadabilityServiceTests
    {
        private ICvReadabilityService CreateService()
        {
            var logger = new Mock<ILogger<CvReadabilityService>>().Object;
            return new CvReadabilityService(logger);
        }

        private IFormFile MakeFile(string name, byte[] content, string contentType = "application/pdf")
        {
            return new FormFile(new MemoryStream(content), 0, content.Length, name, name)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        [Fact]
        public async Task AnalyzeAsync_Throws_WhenFileNull()
        {
            var svc = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() => svc.AnalyzeAsync(null!, default));
        }

        [Fact]
        public async Task AnalyzeAsync_Throws_WhenFileTooLarge()
        {
            var svc = CreateService();
            var big = new byte[10 * 1024 * 1024 + 1];
            var file = MakeFile("big.pdf", big);
            await Assert.ThrowsAsync<ArgumentException>(() => svc.AnalyzeAsync(file, default));
        }
    }
}
