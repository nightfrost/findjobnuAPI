using FindjobnuService.DTOs.Responses;
using SharedInfrastructure.Cities;

namespace FindjobnuService.Mappers;

public static class CitiesMapper
{
    public static CityResponse ToDto(City model) => new(model.Id, model.ExternalId, model.Name, model.Slug);
}
