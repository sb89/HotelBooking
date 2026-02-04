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
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 12),
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
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 12),
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
            StartDate = new DateOnly(2025, 1, 5),
            EndDate = new DateOnly(2025, 1, 9),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 12),
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
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 15),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            StartDate = new DateOnly(2025, 1, 11),
            EndDate = new DateOnly(2025, 1, 13),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms);
    }

    [Fact]
    public async Task SearchAvailable_BookingStartsOnSearchEndDate_DoesNotReturnRoom()
    {
        // Existing booking starts on the last night of the search period
        // Arrange
        await using var dbContext = DbContextFactory.Create();
        var hotel = new Hotel { Name = "Test Hotel" };
        var room = new HotelRoom { RoomNumber = 1, RoomType = HotelRoomType.Single, Capacity = 1 };
        room.Bookings.Add(new Booking
        {
            StartDate = new DateOnly(2025, 1, 12), // Starts on search end date
            EndDate = new DateOnly(2025, 1, 15),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 12), // Last night is Jan 12
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms); // Should NOT be available - both want Jan 12
    }

    [Fact]
    public async Task SearchAvailable_BookingEndsOnSearchStartDate_DoesNotReturnRoom()
    {
        // Existing booking ends on the first night of the search period
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

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            StartDate = new DateOnly(2025, 1, 10), // First night is Jan 10
            EndDate = new DateOnly(2025, 1, 12),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms); // Should NOT be available - both want Jan 10
    }

    [Fact]
    public async Task SearchAvailable_BackToBackBookings_ReturnsRoom()
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

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            StartDate = new DateOnly(2025, 1, 10), // Check in Jan 10
            EndDate = new DateOnly(2025, 1, 12),
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
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 12),
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
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 12),
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
            StartDate = new DateOnly(2025, 1, 8),
            EndDate = new DateOnly(2025, 1, 11),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 14),
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
            StartDate = new DateOnly(2025, 1, 13),
            EndDate = new DateOnly(2025, 1, 17),
            NoOfGuests = 1
        });
        hotel.Rooms.Add(room);
        dbContext.Hotels.Add(hotel);
        await dbContext.SaveChangesAsync();

        var service = new HotelRoomsService(dbContext);
        var criteria = new SearchAvailableCriteria
        {
            HotelId = hotel.Id,
            StartDate = new DateOnly(2025, 1, 10),
            EndDate = new DateOnly(2025, 1, 14),
            NumberOfGuests = 1
        };

        // Act
        var result = await service.SearchAvailable(criteria);

        // Assert
        var success = Assert.IsType<SearchAvailableResult.Success>(result);
        Assert.Empty(success.HotelRooms);
    }
}
