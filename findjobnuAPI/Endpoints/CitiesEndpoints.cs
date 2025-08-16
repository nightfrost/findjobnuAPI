using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using findjobnuAPI.Models;
using findjobnuAPI.Repositories.Context;
using System.Globalization;
namespace findjobnuAPI.Endpoints;

public static class CitiesEndpoints
{
    public static void MapCitiesEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Cities").WithTags(nameof(Cities));

        group.MapGet("/", async (FindjobnuContext db) =>
        {
            return await db.Cities.ToListAsync();
        })
        .WithName("GetAllCities")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<Cities>, NoContent>> (int id, FindjobnuContext db) =>
        {
            return await db.Cities.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Cities model
                    ? TypedResults.Ok(model)
                    : TypedResults.NoContent();
        })
        .WithName("GetCitiesById")
        .WithOpenApi();

        group.MapGet("/search", async Task<Results<Ok<List<Cities>>, NoContent>> (string query, FindjobnuContext db) =>
        {
            if (string.IsNullOrWhiteSpace(query))
                return TypedResults.NoContent();

            var normalizedQuery = $"%{query.Trim()}%";
            var results = await db.Cities.AsNoTracking()
                .Where(model => EF.Functions.Like(model.CityName, normalizedQuery))
                .ToListAsync();

            return results.Any()
                ? TypedResults.Ok(results)
                : TypedResults.NoContent();
        })
        .WithName("GetCitiesByQuery")
        .WithOpenApi();
    }
}
