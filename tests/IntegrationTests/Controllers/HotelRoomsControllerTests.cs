using System.Net;
using System.Net.Http.Json;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SearchAvailableResult = Web.DTOs.HotelRooms.SearchAvailableResult;

namespace IntegrationTests.Controllers;

public class HotelRoomsControllerTests(WebAppFactory factory) : IClassFixture<WebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();
    
    [Fact]
    public async Task SearchAvailable_NoBookings_ReturnsAllRooms()
    {
        // Arrange
        var hotelId = await SeedHotelWithRooms();
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        // Act
        var response = await MakeRequest(hotelId, checkIn, checkOut, 1);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.Equal(6, rooms.Count);
    }
    
    [Fact]
    public async Task SearchAvailable_HotelNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentHotelId = 999;
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        // Act
        var response = await MakeRequest(nonExistentHotelId, checkIn, checkOut, 1);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(problemDetails);
    }
    
    [Fact]
    public async Task SearchAvailable_RoomFullyBooked_DoesNotReturnRoom()
    {
        // Arrange
        var hotelId = await SeedHotelWithRooms();
        var roomId = await GetFirstRoomId(hotelId);

        var bookingCheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var bookingCheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        await CreateBooking(roomId, bookingCheckIn, bookingCheckOut, 1);

        // Act
        var response = await MakeRequest(hotelId, bookingCheckIn, bookingCheckOut, 1);

        // Assert
        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.DoesNotContain(rooms, r => r.RoomId == roomId);
        Assert.Equal(5, rooms.Count);
    }
    
    [Fact]
    public async Task SearchAvailable_PartialOverlap_DoesNotReturnRoom()
    {
        // Arrange
        var hotelId = await SeedHotelWithRooms();
        var roomId = await GetFirstRoomId(hotelId);

        // Book room for 7-10 days from now
        var bookingCheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var bookingCheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        await CreateBooking(roomId, bookingCheckIn, bookingCheckOut, 1);

        // Act - Search for 8-12 days from now, overlaps with above
        var searchCheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(8));
        var searchCheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(12));
        var response = await MakeRequest(hotelId, searchCheckIn, searchCheckOut, 1);

        // Assert
        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.DoesNotContain(rooms, r => r.RoomId == roomId);
    }
    
    [Fact]
    public async Task SearchAvailable_InsufficientTotalCapacity_ReturnsEmptyList()
    {
        // Arrange
        var hotelId = await SeedHotelWithRooms();
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        // Act
        // Hotel has total capacity 14
        var response = await MakeRequest(hotelId, checkIn, checkOut, 20);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.Empty(rooms);
    }
    
    [Fact]
    public async Task SearchAvailable_BackToBackBookings_ReturnsRoom()
    {
        // Arrange
        var hotelId = await SeedHotelWithRooms();
        var roomId = await GetFirstRoomId(hotelId);

        // Book room for 7-10 days from now, checkout on 10th day
        var bookingCheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var bookingCheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        await CreateBooking(roomId, bookingCheckIn, bookingCheckOut, 1);

        // Act - Search for 10-13 days from now (checkin on 10th = no overlap)
        var searchCheckIn = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var searchCheckOut = DateOnly.FromDateTime(DateTime.Today.AddDays(13));
        var response = await MakeRequest(hotelId, searchCheckIn, searchCheckOut, 1);

        // Assert
        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.Contains(rooms, r => r.RoomId == roomId);
    }
    
    [Fact] 
    public async Task SearchAvailable_MultipleRoomsBooked_ReturnsOnlyAvailable()
    {
        // Arrange
        var hotelId = await SeedHotelWithRooms();
        var allRoomIds = await GetAllRoomIds(hotelId);

        // Book first 3 rooms
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        await CreateBooking(allRoomIds[0], checkIn, checkOut, 1);
        await CreateBooking(allRoomIds[1], checkIn, checkOut, 2);
        await CreateBooking(allRoomIds[2], checkIn, checkOut, 1);

        // Act
        var response = await MakeRequest(hotelId, checkIn, checkOut, 1);

        // Assert
        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.Equal(3, rooms.Count);
        Assert.DoesNotContain(rooms, r => allRoomIds.Take(3).Contains(r.RoomId));
    }

    [Fact]
    public async Task SearchAvailable_OnlyReturnsRoomsForSpecifiedHotel()
    {
        // Arrange
        var hotel1Id = await SeedHotelWithRooms("Hotel 1");
        await SeedHotelWithRooms("Hotel 2");

        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        // Act - Search for hotel 1
        var response = await MakeRequest(hotel1Id, checkIn, checkOut, 1);

        // Assert
        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.NotEmpty(rooms);
        Assert.All(rooms, room => Assert.Equal(hotel1Id, room.HotelId));
    }

    [Fact]
    public async Task SearchAvailable_ReturnsRoomDetails()
    {
        // Arrange
        var hotelId = await SeedHotelWithRooms();
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));

        // Act
        var response = await MakeRequest(hotelId, checkIn, checkOut, 1);

        // Assert
        var rooms = await response.Content.ReadFromJsonAsync<List<SearchAvailableResult>>();
        Assert.NotNull(rooms);
        Assert.NotEmpty(rooms);

        var firstRoom = rooms[0];
        Assert.Equal(hotelId, firstRoom.HotelId);
        Assert.True(firstRoom.RoomId > 0);
        Assert.True(firstRoom.RoomNumber > 0);
        Assert.True(Enum.IsDefined(typeof(HotelRoomType), firstRoom.HotelRoomType));
    }

    private Task<HttpResponseMessage> MakeRequest(int hotelId, DateOnly checkIn, DateOnly checkOut, int numberOfGuests)
    {
        return _client.GetAsync(
            $"/api/v1/hotels/{hotelId}/rooms?checkInDate={checkIn:yyyy-MM-dd}&checkOutDate={checkOut:yyyy-MM-dd}&numberOfGuests={numberOfGuests}");
    }

    private async Task<int> SeedHotelWithRooms(string hotelName = "Test Hotel 1")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var hotel = new Hotel { Name = hotelName };
        hotel.Rooms = new List<HotelRoom>
        {
            new() { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 },
            new() { RoomNumber = 2, RoomType = HotelRoomType.Single, Capacity = 1 },
            new() { RoomNumber = 3, RoomType = HotelRoomType.Double, Capacity = 2 },
            new() { RoomNumber = 4, RoomType = HotelRoomType.Double, Capacity = 2 },
            new() { RoomNumber = 5, RoomType = HotelRoomType.Deluxe, Capacity = 4 },
            new() { RoomNumber = 6, RoomType = HotelRoomType.Deluxe, Capacity = 4 }
        };

        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();

        return hotel.Id;
    }
    
    private async Task<int> GetFirstRoomId(int hotelId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return (await GetAllRoomIds(hotelId)).First();
    }
    
    private async Task<List<int>> GetAllRoomIds(int hotelId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.HotelRooms
            .Where(r => r.HotelId == hotelId)
            .OrderBy(r => r.RoomNumber)
            .Select(r => r.Id)
            .ToListAsync();
    }
    
    private async Task CreateBooking(int roomId, DateOnly checkIn, DateOnly checkOut, int guests)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var booking = new Booking
        {
            HotelRoomId = roomId,
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
            NoOfGuests = guests
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
    }
    
    private async Task ClearDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Bookings.ExecuteDeleteAsync();
        await db.HotelRooms.ExecuteDeleteAsync();
        await db.Hotels.ExecuteDeleteAsync();
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await ClearDatabase();
    }
    
}