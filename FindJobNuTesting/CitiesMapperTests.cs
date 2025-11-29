using FindjobnuService.Mappers;
using FindjobnuService.Models;
using Xunit;

namespace FindjobnuTesting;

public class CitiesMapperTests
{
    [Fact]
    public void ToDto_Maps_Id_And_Name()
    {
        var model = new Cities { Id = 5, CityName = "Gotham" };
        var dto = CitiesMapper.ToDto(model);
        Assert.Equal(5, dto.Id);
        Assert.Equal("Gotham", dto.CityName);
    }
}
