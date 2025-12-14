namespace FindjobnuService.DTOs.Responses;

public record CityResponse(int Id, Guid ExternalId, string Name, string Slug);
