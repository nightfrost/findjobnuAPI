using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.OpenApi;
using System.Globalization;
using FindjobnuService.Models;
using FindjobnuService.Repositories.Context;
using FindjobnuService.DTOs.Responses;
using FindjobnuService.Mappers;
namespace FindjobnuService.Endpoints;

public static class CitiesEndpoints
{
    public static void MapCitiesEndpoints (this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/Cities").WithTags(nameof(Cities));

        group.MapGet("/", async (FindjobnuContext db) =>
        {
            var cities = await db.Cities.ToListAsync();
            return cities.Select(CitiesMapper.ToDto).ToList();
        })
        .WithName("GetAllCities")
        .WithOpenApi();

        group.MapGet("/{id}", async Task<Results<Ok<CityResponse>, NoContent>> (int id, FindjobnuContext db) =>
        {
            return await db.Cities.AsNoTracking()
                .FirstOrDefaultAsync(model => model.Id == id)
                is Cities model
                    ? TypedResults.Ok(CitiesMapper.ToDto(model))
                    : TypedResults.NoContent();
        })
        .WithName("GetCitiesById")
        .WithOpenApi();

        group.MapGet("/search", async Task<Results<Ok<List<CityResponse>>, NoContent>> (string query, FindjobnuContext db) =>
        {
            if (string.IsNullOrWhiteSpace(query))
                return TypedResults.NoContent();

            var normalizedQuery = $"%{query.Trim()}%";
            var results = await db.Cities.AsNoTracking()
                .Where(model => EF.Functions.Like(model.CityName, normalizedQuery))
                .ToListAsync();

            var dtos = results.Select(CitiesMapper.ToDto).ToList();
            return dtos.Any()
                ? TypedResults.Ok(dtos)
                : TypedResults.NoContent();
        })
        .WithName("GetCitiesByQuery")
        .WithOpenApi();
    }
}
