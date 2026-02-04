using System.Net;
using System.Net.Http.Json;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Web.DTOs.Bookings;

namespace IntegrationTests.Controllers;

public class BookingsControllerTests(WebAppFactory factory) : IClassFixture<WebAppFactory>, IAsyncLifetime
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateBooking_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var room = await SeedHotelWithRoom();
        var request = new CreateBookingRequest
        {
            RoomId = room.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            NumberOfGuests = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        Assert.NotNull(result);
        Assert.True(result.ContainsKey("bookingId"));
        Assert.True(result["bookingId"] > 0);
    }
    
    [Fact]
    public async Task CreateBooking_RoomNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateBookingRequest
        {
            RoomId = 99999,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            NumberOfGuests = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(problemDetails);
    }
    
    [Fact]
    public async Task CreateBooking_ExceedsCapacity_ReturnsUnprocessableEntity()
    {
        // Arrange
        var room = await SeedHotelWithRoom(capacity: 2);
        var request = new CreateBookingRequest
        {
            RoomId = room.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            NumberOfGuests = 5
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", request);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(problemDetails);
    }
    
    [Fact]
    public async Task CreateBooking_RoomAlreadyBooked_ReturnsConflict()
    {
        // Arrange
        var room = await SeedHotelWithRoom();

        var firstRequest = new CreateBookingRequest
        {
            RoomId = room.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            NumberOfGuests = 1
        };
        await _client.PostAsJsonAsync("/api/v1/bookings", firstRequest);

        var conflictingRequest = new CreateBookingRequest
        {
            RoomId = room.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(8)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(11)),
            NumberOfGuests = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", conflictingRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(problemDetails);
    }
    
    [Fact]
    public async Task CreateBooking_BackToBackBookings_ReturnsCreated()
    {
        // Arrange
        var room = await SeedHotelWithRoom();
        
        var firstRequest = new CreateBookingRequest
        {
            RoomId = room.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            NumberOfGuests = 1
        };
        await _client.PostAsJsonAsync("/api/v1/bookings", firstRequest);

        var secondRequest = new CreateBookingRequest
        {
            RoomId = room.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(13)),
            NumberOfGuests = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
    
    [Fact]
    public async Task CreateBooking_PersistsToDatabase()
    {
        // Arrange
        var room = await SeedHotelWithRoom();
        var request = new CreateBookingRequest
        {
            RoomId = room.Id,
            CheckInDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CheckOutDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            NumberOfGuests = 1
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", request);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, int>>();
        var bookingId = result!["bookingId"];

        // Assert
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var booking = await db.Bookings.FindAsync(bookingId);

        Assert.NotNull(booking);
        Assert.Equal(room.Id, booking.HotelRoomId);
        Assert.Equal(request.CheckInDate, booking.CheckInDate);
        Assert.Equal(request.CheckOutDate, booking.CheckOutDate);
        Assert.Equal(request.NumberOfGuests, booking.NoOfGuests);
    }
    
    [Fact]
    public async Task GetBooking_ExistingBooking_ReturnsCorrectDetails()
    {
        // Arrange
        var room = await SeedHotelWithRoom();
        var checkIn = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var checkOut = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
        var guests = 2;

        var bookingId = await CreateBookingInDatabase(room.Id, checkIn, checkOut, guests);

        // Act
        var response = await _client.GetAsync($"/api/v1/bookings/{bookingId}");

        // Assert
        var booking = await response.Content.ReadFromJsonAsync<GetBookingResult>();
        Assert.NotNull(booking);
        Assert.Equal(bookingId, booking.BookingReference);
        Assert.Equal(checkIn, booking.CheckInDate);
        Assert.Equal(checkOut, booking.CheckOutDate);
        Assert.Equal(guests, booking.NumberOfGuests);
        Assert.Equal(room.Hotel!.Name, booking.HotelName);
        Assert.Equal(room.RoomNumber, booking.RoomNumber);
    }
    
    [Fact]
    public async Task GetBooking_NonExistentBooking_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/bookings/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(problemDetails);
    }
    
    [Fact]
    public async Task GetBooking_MultipleBookings_ReturnsCorrectOne()
    {
        // Arrange
        var room1 = await SeedHotelWithRoom(hotelName: "Hotel 1", roomNumber: 1);
        var room2 = await SeedHotelWithRoom(hotelName: "Hotel 2", roomNumber: 2);

        await CreateBookingInDatabase(room1.Id);
        var booking2Id = await CreateBookingInDatabase(room2.Id);

        // Act
        var response = await _client.GetAsync($"/api/v1/bookings/{booking2Id}");

        // Assert
        var booking = await response.Content.ReadFromJsonAsync<GetBookingResult>();
        Assert.NotNull(booking);
        Assert.Equal(booking2Id, booking.BookingReference);
        Assert.Equal("Hotel 2", booking.HotelName);
        Assert.Equal(2, booking.RoomNumber);
    }
    
    private async Task<HotelRoom> SeedHotelWithRoom(int capacity = 2, HotelRoomType roomType = HotelRoomType.Double, 
        int roomNumber = 1, string hotelName = "Test Hotel")
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var hotel = new Hotel { Name = hotelName };
        var room = new HotelRoom
        {
            RoomNumber = roomNumber,
            RoomType = roomType,
            Capacity = capacity
        };
        hotel.Rooms.Add(room);

        db.Hotels.Add(hotel);
        await db.SaveChangesAsync();

        return room;
    }
    
    private async Task<int> CreateBookingInDatabase(
        int roomId,
        DateOnly? checkIn = null,
        DateOnly? checkOut = null,
        int guests = 1)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var booking = new Booking
        {
            HotelRoomId = roomId,
            CheckInDate = checkIn ?? DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            CheckOutDate = checkOut ?? DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            NoOfGuests = guests
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        return booking.Id;
    }

    public Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await ClearDatabase();
    }
    
    private async Task ClearDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await db.Bookings.ExecuteDeleteAsync();
        await db.HotelRooms.ExecuteDeleteAsync();
        await db.Hotels.ExecuteDeleteAsync();
    }
}