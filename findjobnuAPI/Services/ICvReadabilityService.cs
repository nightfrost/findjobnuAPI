using FindjobnuService.DTOs;

namespace FindjobnuService.Services;

public interface ICvReadabilityService
{
    Task<CvReadabilityResult> AnalyzeAsync(IFormFile pdfFile, CancellationToken cancellationToken = default);
}
