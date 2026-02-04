using ApplicationTests.Factories;
using Domain.Entities;
using Infrastructure.Results.Bookings;
using Infrastructure.Services;

namespace ApplicationTests.ServiceTests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBooking_RoomNotFound_ReturnsRoomNotFound()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 12),
            roomId: 999,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.RoomNotFound>(result);
    }

    [Fact]
    public async Task CreateBooking_CapacityExceeded_ReturnsCapacityExceeded()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 12),
            roomId: room.Id,
            numberOfGuests: 5); // Exceeds capacity of 1

        // Assert
        Assert.IsType<CreateBookingResult.CapacityExceeded>(result);
    }

    [Fact]
    public async Task CreateBooking_NoExistingBookings_ReturnsSuccess()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Double, Capacity = 2 };
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 12),
            roomId: room.Id,
            numberOfGuests: 2);

        // Assert
        var success = Assert.IsType<CreateBookingResult.Success>(result);
        Assert.True(success.BookingId > 0);
    }

    [Fact]
    public async Task CreateBooking_NoOverlapWithExistingBooking_ReturnsSuccess()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 5),
            EndDate = new DateOnly(2025, 1, 9),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 12),
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.Success>(result);
    }

    [Fact]
    public async Task CreateBooking_FullyOverlaps_ReturnsRoomNoLongerAvailable()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 15),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 11),
            endDate: new DateOnly(2025, 1, 13),
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.RoomNoLongerAvailable>(result);
    }

    [Fact]
    public async Task CreateBooking_ExistingBookingStartsOnNewEndDate_ReturnsRoomNoLongerAvailable()
    {
        // Existing booking starts on the last night of new booking
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 12), // Starts on new booking's end date
            EndDate = new DateOnly(2025, 1, 15),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 12), // Last night is Jan 12
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.RoomNoLongerAvailable>(result);
    }

    [Fact]
    public async Task CreateBooking_ExistingBookingEndsOnNewStartDate_ReturnsRoomNoLongerAvailable()
    {
        // Existing booking ends on the first night of new booking
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 8),
            EndDate = new DateOnly(2025, 1, 10), // Last night is Jan 10
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10), // First night is Jan 10
            endDate: new DateOnly(2025, 1, 12),
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.RoomNoLongerAvailable>(result);
    }

    [Fact]
    public async Task CreateBooking_BackToBackBookings_ReturnsSuccess()
    {
        // Guest A checks out Jan 10 (EndDate=9), Guest B checks in Jan 10 (StartDate=10)
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 5),
            EndDate = new DateOnly(2025, 1, 9), // Last night Jan 9, checkout morning of Jan 10
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10), // Check in Jan 10
            endDate: new DateOnly(2025, 1, 12),
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.Success>(result);
    }

    [Fact]
    public async Task CreateBooking_PartialOverlapAtStart_ReturnsRoomNoLongerAvailable()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 8),
            EndDate = new DateOnly(2025, 1, 11),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 14),
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.RoomNoLongerAvailable>(result);
    }

    [Fact]
    public async Task CreateBooking_PartialOverlapAtEnd_ReturnsRoomNoLongerAvailable()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 13),
            EndDate = new DateOnly(2025, 1, 17),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 14),
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.RoomNoLongerAvailable>(result);
    }

    [Fact]
    public async Task CreateBooking_PersistsBookingCorrectly()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Double, Capacity = 2 };
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);
        var startDate = new DateOnly(2025, 1, 10);
        var endDate = new DateOnly(2025, 1, 12);
        var numberOfGuests = 2;

        // Act
        var result = await service.CreateBooking(startDate, endDate, room.Id, numberOfGuests);

        // Assert
        var success = Assert.IsType<CreateBookingResult.Success>(result);
        var booking = await dbContext.Bookings.FindAsync(success.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(startDate, booking.StartDate);
        Assert.Equal(endDate, booking.EndDate);
        Assert.Equal(room.Id, booking.HotelRoomId);
        Assert.Equal(numberOfGuests, booking.NoOfGuests);
    }

    [Fact]
    public async Task CreateBooking_GuestsAtExactCapacity_ReturnsSuccess()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Double, Capacity = 2 };
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 12),
            roomId: room.Id,
            numberOfGuests: 2); // Exactly at capacity

        // Assert
        Assert.IsType<CreateBookingResult.Success>(result);
    }

    [Fact]
    public async Task CreateBooking_OverlapCheckOnlyAppliesToSameRoom()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room1 = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        var room2 = new HotelRoom { RoomNumber = 2, RoomType = HotelRoomType.Single, Capacity = 1 };
        room1.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 12),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room1);
        hotel.Rooms.Add(room2);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext);

        // Act - book room2 for the same dates that room1 is booked
        var result = await service.CreateBooking(
            startDate: new DateOnly(2025, 1, 10),
            endDate: new DateOnly(2025, 1, 12),
            roomId: room2.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.Success>(result);
    }
}
