using FindjobnuService.Mappers;
using SharedInfrastructure.Cities;

namespace FindjobnuTesting;

public class CitiesMapperTests
{
    [Fact]
    public void ToDto_Maps_Id_And_Name()
    {
        var model = new City { Id = 5, Name = "Gotham" };
        var dto = CitiesMapper.ToDto(model);
        Assert.Equal(5, dto.Id);
        Assert.Equal("Gotham", dto.Name);
    }
}
