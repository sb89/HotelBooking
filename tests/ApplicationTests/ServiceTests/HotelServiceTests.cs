using ApplicationTests.Factories;
using Domain.Entities;
using Infrastructure.Services;

namespace ApplicationTests.ServiceTests;

public class HotelServiceTests
{
    [Fact]
    public async Task Search_WithNoName_ReturnsAllHotels()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        dbContext.Hotels.AddRange(
            new Hotel { Name = "Hilton" },
            new Hotel { Name = "Marriott" }
        );
        await dbContext.SaveChangesAsync();

        var service = new HotelService(dbContext);

        // Act
        var result = await service.Search();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Search_WithName_FiltersHotelsByName()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        dbContext.Hotels.AddRange(
            new Hotel { Name = "Hilton London" },
            new Hotel { Name = "Hilton Paris" },
            new Hotel { Name = "Marriott London" }
        );
        await dbContext.SaveChangesAsync();

        var service = new HotelService(dbContext);

        // Act
        var result = await service.Search("Hilton");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, h => Assert.Contains("Hilton", h.Name));
    }

    [Fact]
    public async Task Search_WithNameThatDoesNotMatch_ReturnsEmptyList()
    {
        // Arrange
        var dbContext = DbContextFactory.Create();
        dbContext.Hotels.Add(new Hotel { Name = "Hilton" });
        await dbContext.SaveChangesAsync();

        var service = new HotelService(dbContext);

        // Act
        var result = await service.Search("NonExistent");

        // Assert
        Assert.Empty(result);
    }
}