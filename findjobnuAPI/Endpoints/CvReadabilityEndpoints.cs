using findjobnuAPI.DTOs;
using findjobnuAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace findjobnuAPI.Endpoints;

public static class CvReadabilityEndpoints
{
    public static void MapCvReadabilityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cv").WithTags("CV");

        group.MapPost("/analyze", async Task<Results<Ok<CvReadabilityResult>, BadRequest<string>>> (
            [FromForm] IFormFile file,
            [FromServices] ICvReadabilityService service,
            CancellationToken ct) =>
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return TypedResults.BadRequest("No file uploaded.");
                }

                // Quick endpoint-level file size guard consistent with service
                const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
                if (file.Length > MaxFileSizeBytes)
                {
                    return TypedResults.BadRequest("File too large. Max allowed size is 10 MB.");
                }

                var result = await service.AnalyzeAsync(file, ct);
                return TypedResults.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
        })
        .Accepts<IFormFile>("multipart/form-data")
        .Produces<CvReadabilityResult>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .WithName("AnalyzeCvPdf")
        .DisableAntiforgery()
        .WithOpenApi();
    }
}
