using FindjobnuService.DTOs;
using FindjobnuService.DTOs.Requests;
using FindjobnuService.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FindjobnuService.Endpoints;

public static class CvReadabilityEndpoints
{
    public static void MapCvReadabilityEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cv").WithTags("CV");

        group.MapPost("/analyze", async Task<Results<Ok<CvReadabilityResult>, BadRequest<string>>> (
            [FromForm] UploadCvRequest request,
            [FromServices] ICvReadabilityService service,
            CancellationToken ct) =>
        {
            try
            {
                var file = request.File;
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
        .Accepts<UploadCvRequest>("multipart/form-data")
        .Produces<CvReadabilityResult>(StatusCodes.Status200OK)
        .Produces<string>(StatusCodes.Status400BadRequest)
        .WithName("AnalyzeCvPdf")
        .DisableAntiforgery();
    }
}
