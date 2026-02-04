using ApplicationTests.Factories;
using Domain.Entities;
using Infrastructure.Models;
using Infrastructure.Results.HotelRooms;
using Infrastructure.Services;

namespace ApplicationTests.ServiceTests;

public class HotelRoomsServiceTests
{
    [Fact]
    public async Task SearchAvailable_HotelNotFound_ReturnsHotelNotFound()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = 999,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        Assert.IsType<SearchAvailableResult.HotelNotFound>(result);
    }

    [Fact]
    public async Task SearchAvailable_NoBookings_ReturnsAllRooms()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        hotel.Rooms =
        [
            new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 },
            new HotelRoom { RoomNumber = 2, RoomType = HotelRoomType.Double, Capacity = 2 }
        ];
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Equal(2, success.HotelRooms.Count);
    }

    [Fact]
    public async Task SearchAvailable_BookingDoesNotOverlap_ReturnsRoom()
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

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Single(success.HotelRooms);
    }

    [Fact]
    public async Task SearchAvailable_BookingFullyOverlaps_DoesNotReturnRoom()
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

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 11),
            CheckoutDate = new DateOnly(2025, 1, 13),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms);
    }

    [Fact]
    public async Task SearchAvailable_BookingStartsOnSearchEndDate_ReturnsRoom()
    {
        // Existing booking starts on search checkout date (no overlap)
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 12), // Starts on search end date
            CheckOutDate = new DateOnly(2025, 1, 15),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 12), // Leaves Jan 12
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Single(success.HotelRooms); // Should be available - no overlap
    }

    [Fact]
    public async Task SearchAvailable_BookingEndsOnSearchStartDate_ReturnsRoom()
    {
        // Existing booking checkout matches search checkin (no overlap)
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 8),
            CheckOutDate = new DateOnly(2025, 1, 10), // Leaves Jan 10
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10), // Arrives Jan 10
            CheckoutDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Single(success.HotelRooms); // Should be available - no overlap
    }

    [Fact]
    public async Task SearchAvailable_BackToBackBookings_ReturnsRoom()
    {
        // Guest A leaves Jan 9, Guest B arrives Jan 10 (no overlap)
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            CheckInDate = new DateOnly(2025, 1, 5),
            CheckOutDate = new DateOnly(2025, 1, 9), // Leaves Jan 9
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10), // Arrives Jan 10
            CheckoutDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Single(success.HotelRooms); // Should be available - no overlap
    }

    [Fact]
    public async Task SearchAvailable_InsufficientCapacity_ReturnsEmptyList()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        hotel.Rooms =
        [
            new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 },
            new HotelRoom { RoomNumber = 2, RoomType = HotelRoomType.Single, Capacity = 1 }
        ];
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 5 // More than total capacity (2)
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms);
    }

    [Fact]
    public async Task SearchAvailable_OnlyReturnsRoomsForSpecifiedHotel()
    {
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel1 = new Hotel { Name = "Hotel 1" };
        hotel1.Rooms.Add(new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 });

        var hotel2 = new Hotel { Name = "Hotel 2" };
        hotel2.Rooms.Add(new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 });

        dbContext.Hotels.AddRange(hotel1, hotel2);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel1.Id,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Single(success.HotelRooms);
        Assert.All(success.HotelRooms, r => Assert.Equal(hotel1.Id, r.HotelId));
    }

    [Fact]
    public async Task SearchAvailable_PartialOverlapAtStart_DoesNotReturnRoom()
    {
        // Existing booking overlaps the start of the search period
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

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 14),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms);
    }

    [Fact]
    public async Task SearchAvailable_PartialOverlapAtEnd_DoesNotReturnRoom()
    {
        // Existing booking overlaps the end of the search period
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

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            CheckInDate = new DateOnly(2025, 1, 10),
            CheckoutDate = new DateOnly(2025, 1, 14),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms);
    }
}
