using FindjobnuService.DTOs.Responses;
using FindjobnuService.Models;

namespace FindjobnuService.Mappers;

public static class CitiesMapper
{
    public static CityResponse ToDto(Cities model) => new(model.Id, model.CityName);
}
