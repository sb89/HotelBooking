using System.Net;
using System.Net.Http.Json;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Web.DTOs.Hotels;

namespace IntegrationTests.Controllers;

public class HotelsControllerTests(WebAppFactory factory) : IClassFixture<WebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    
    [Fact]
    public async Task SearchHotels_WithNameFilter_ReturnsMatchingHotels()
    {
        // Arrange
        await SeedHotels();

        // Act
        var response = await _client.GetAsync("/api/v1/hotels?name=Grand");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var hotels = await response.Content.ReadFromJsonAsync<List<HotelDto>>();
        Assert.NotNull(hotels);
        Assert.Single(hotels);
        Assert.Contains("Grand", hotels[0].Name);
    }

    
    [Fact]
    public async Task SearchHotels_NoFilter_ReturnsAllHotels()
    {
        // Arrange
        await SeedHotels();

        // Act
        var response = await _client.GetAsync("/api/v1/hotels");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var hotels = await response.Content.ReadFromJsonAsync<List<HotelDto>>();
        Assert.NotNull(hotels);
        Assert.Equal(3, hotels.Count);
    }
    
    private async Task SeedHotels()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Hotels.ExecuteDeleteAsync();

        db.Hotels.AddRange(
            new Hotel { Name = "Grand Hotel" },
            new Hotel { Name = "City Center Hotel" },
            new Hotel { Name = "Beach Resort" }
        );
        await db.SaveChangesAsync();
    }
}