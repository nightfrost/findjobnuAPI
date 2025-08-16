using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using findjobnuAPI.DTOs;

namespace findjobnuAPI.Services;

public interface ICvReadabilityService
{
    Task<CvReadabilityResult> AnalyzeAsync(IFormFile pdfFile, CancellationToken cancellationToken = default);
}
