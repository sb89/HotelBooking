using ApplicationTests.Factories;
using Domain.Entities;
using Infrastructure.Results.Bookings;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace ApplicationTests.ServiceTests;

public class BookingServiceTests
{
    [Fact]
    public async Task CreateBooking_RoomNotFound_ReturnsRoomNotFound()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 12),
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

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 12),
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

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 12),
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
            CheckInDate = new DateOnly(2025, 1, 5),
            CheckOutDate = new DateOnly(2025, 1, 9),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 12),
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
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckOutDate = new DateOnly(2025, 1, 15),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 11),
            checkOutDate: new DateOnly(2025, 1, 13),
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.RoomNoLongerAvailable>(result);
    }

    [Fact]
    public async Task CreateBooking_ExistingBookingStartsOnNewEndDate_ReturnsSuccess()
    {
        // Existing booking starts on new booking's checkout date (no overlap)
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 12), // CheckIn on new booking's checkout date
            CheckOutDate = new DateOnly(2025, 1, 15),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 12), // Leaves Jan 12
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.Success>(result);
    }

    [Fact]
    public async Task CreateBooking_ExistingBookingEndsOnNewStartDate_ReturnsSuccess()
    {
        // Existing booking's checkout date matches new booking's checkin (no overlap)
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 8),
            CheckOutDate = new DateOnly(2025, 1, 10), // Staying nights Jan 8, 9 (leave morning of 10th)
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10), // Arrives Jan 10
            checkOutDate: new DateOnly(2025, 1, 12), // Leaves Jan 12
            roomId: room.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.Success>(result);
    }

    [Fact]
    public async Task CreateBooking_BackToBackBookings_ReturnsSuccess()
    {
        // Guest A leaves Jan 10, Guest B arrives Jan 10 (no overlap)
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 5),
            CheckOutDate = new DateOnly(2025, 1, 10), // Staying nights Jan 5-9 (leave morning of 10th)
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10), // Arrives Jan 10
            checkOutDate: new DateOnly(2025, 1, 12), // Leaves Jan 12
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
            CheckInDate = new DateOnly(2025, 1, 8),
            CheckOutDate = new DateOnly(2025, 1, 11),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 14),
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
            CheckInDate = new DateOnly(2025, 1, 13),
            CheckOutDate = new DateOnly(2025, 1, 17),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 14),
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

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);
        var checkInDate = new DateOnly(2025, 1, 10);
        var checkOutDate = new DateOnly(2025, 1, 12);
        var numberOfGuests = 2;

        // Act
        var result = await service.CreateBooking(checkInDate, checkOutDate, room.Id, numberOfGuests);

        // Assert
        var success = Assert.IsType<CreateBookingResult.Success>(result);
        var booking = await dbContext.Bookings.FindAsync(success.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(checkInDate, booking.CheckInDate);
        Assert.Equal(checkOutDate, booking.CheckOutDate);
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

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 12),
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
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckOutDate = new DateOnly(2025, 1, 12),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room1);
        hotel.Rooms.Add(room2);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act - book room2 for the same dates that room1 is booked
        var result = await service.CreateBooking(
            checkInDate: new DateOnly(2025, 1, 10),
            checkOutDate: new DateOnly(2025, 1, 12),
            roomId: room2.Id,
            numberOfGuests: 1);

        // Assert
        Assert.IsType<CreateBookingResult.Success>(result);
    }

    [Fact]
    public async Task Get_BookingExists_ReturnsBooking()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 101, RoomType = HotelRoomType.Double, Capacity = 2 };
        hotel.Rooms.Add(room);

        var booking = new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckOutDate = new DateOnly(2025, 1, 12),
            NoOfGuests = 2
        };
        room.Bookings.Add(booking);

        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.Get(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking.Id, result.Id);
        Assert.Equal(new DateOnly(2025, 1, 10), result.CheckInDate);
        Assert.Equal(new DateOnly(2025, 1, 12), result.CheckOutDate);
        Assert.Equal(2, result.NoOfGuests);
        Assert.Equal(room.Id, result.HotelRoomId);
    }

    [Fact]
    public async Task Get_BookingNotFound_ReturnsNull()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.Get(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Get_IncludesRoomDetails()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 101, RoomType = HotelRoomType.Double, Capacity = 2 };
        hotel.Rooms.Add(room);

        var booking = new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckOutDate = new DateOnly(2025, 1, 12),
            NoOfGuests = 2
        };
        room.Bookings.Add(booking);

        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.Get(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Room);
        Assert.Equal(101, result.Room.RoomNumber);
        Assert.Equal(HotelRoomType.Double, result.Room.RoomType);
        Assert.Equal(2, result.Room.Capacity);
    }

    [Fact]
    public async Task Get_IncludesHotelDetails()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Grand Hotel" };
        var room = new HotelRoom { RoomNumber = 101, RoomType = HotelRoomType.Double, Capacity = 2 };
        hotel.Rooms.Add(room);

        var booking = new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckOutDate = new DateOnly(2025, 1, 12),
            NoOfGuests = 2
        };
        room.Bookings.Add(booking);

        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.Get(booking.Id);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Room);
        Assert.NotNull(result.Room.Hotel);
        Assert.Equal("Grand Hotel", result.Room.Hotel.Name);
    }

    [Fact]
    public async Task Get_MultipleBookings_ReturnsCorrectOne()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room1 = new HotelRoom { RoomNumber = 101, RoomType = HotelRoomType.Single, Capacity = 1 };
        var room2 = new HotelRoom { RoomNumber = 102, RoomType = HotelRoomType.Double, Capacity = 2 };
        hotel.Rooms.Add(room1);
        hotel.Rooms.Add(room2);

        var booking1 = new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckOutDate = new DateOnly(2025, 1, 12),
            NoOfGuests = 1
        };
        room1.Bookings.Add(booking1);

        var booking2 = new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 15),
            CheckOutDate = new DateOnly(2025, 1, 17),
            NoOfGuests = 2
        };
        room2.Bookings.Add(booking2);

        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new BookingService(dbContext, NullLogger<BookingService>.Instance);

        // Act
        var result = await service.Get(booking2.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(booking2.Id, result.Id);
        Assert.Equal(new DateOnly(2025, 1, 15), result.CheckInDate);
        Assert.Equal(new DateOnly(2025, 1, 17), result.CheckOutDate);
        Assert.Equal(2, result.NoOfGuests);
        Assert.Equal(102, result.Room!.RoomNumber);
    }
}
