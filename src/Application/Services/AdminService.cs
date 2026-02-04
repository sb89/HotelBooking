using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class AdminService(ApplicationDbContext dbContext, ILogger<AdminService> logger) : IAdminService
{
    public Task Seed(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Seeding database");
        
        var hotel1 = new Hotel { Name = "Hotel 1" };
        hotel1.Rooms =
        [
            CreateRoom(1, HotelRoomType.Single),
            CreateRoom(2, HotelRoomType.Single),
            CreateRoom(3, HotelRoomType.Double),
            CreateRoom(4, HotelRoomType.Double),
            CreateRoom(5, HotelRoomType.Deluxe),
            CreateRoom(6, HotelRoomType.Deluxe)
        ];
        
        var hotel2 = new Hotel { Name = "Hotel 2" };
        hotel2.Rooms =
        [
            CreateRoom(1, HotelRoomType.Single),
            CreateRoom(2, HotelRoomType.Single),
            CreateRoom(3, HotelRoomType.Double),
            CreateRoom(4, HotelRoomType.Double),
            CreateRoom(5, HotelRoomType.Deluxe),
            CreateRoom(6, HotelRoomType.Deluxe)
        ];
        
        var hotel3 = new Hotel { Name = "Hotel 3" };
        hotel3.Rooms =
        [
            CreateRoom(1, HotelRoomType.Single),
            CreateRoom(2, HotelRoomType.Single),
            CreateRoom(3, HotelRoomType.Double),
            CreateRoom(4, HotelRoomType.Double),
            CreateRoom(5, HotelRoomType.Deluxe),
            CreateRoom(6, HotelRoomType.Deluxe)
        ];
        
        List<Hotel> hotels = [hotel1, hotel2, hotel3];
        
        dbContext.Hotels.AddRange(hotels);
        
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task Reset(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Resetting database");
        
        await dbContext.Hotels.ExecuteDeleteAsync(cancellationToken);
        
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private HotelRoom CreateRoom(int roomNumber, HotelRoomType hotelRoomType)
    {
        var capacity = hotelRoomType switch
        {
            HotelRoomType.Single => 1,
            HotelRoomType.Double => 2,
            HotelRoomType.Deluxe => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(hotelRoomType))
        };

        return new HotelRoom
        {
            RoomNumber = roomNumber,
            RoomType = hotelRoomType,
            Capacity = capacity
        };
    }
}