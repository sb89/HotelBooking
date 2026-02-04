using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Infrastructure.Models;
using Infrastructure.Results.HotelRooms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class HotelRoomsService(ApplicationDbContext dbContext, ILogger<HotelRoomsService> logger) : IHotelRoomsService
{
    public async Task<SearchAvailableResult> SearchAvailable(SearchAvailableCriteria criteria, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching for available rooms: {CheckInDate}-{CheckOutDate} {NumberOfGuests} guests for {HotelId}", 
            criteria.CheckInDate, criteria.CheckoutDate, criteria.NumberOfGuests, criteria.HotelId);
        
        var hotel = await dbContext.Hotels.FindAsync([criteria.HotelId], cancellationToken);
        if (hotel == null)
        {
            logger.LogInformation("Hotel not found: {HotelId}", criteria.HotelId);
            return new SearchAvailableResult.HotelNotFound();
        }
        
        var availableRooms = await dbContext.HotelRooms
            .Where(r => r.HotelId == criteria.HotelId)
            .Where(r => !r.Bookings.Any(b =>
                b.CheckInDate < criteria.CheckoutDate && b.CheckOutDate > criteria.CheckInDate))
            .ToListAsync(cancellationToken);
        
        var totalCapacity = availableRooms.Sum(r => r.Capacity);

        if (totalCapacity < criteria.NumberOfGuests)
        {
            logger.LogInformation("Capacity not available");
            return new SearchAvailableResult.Success([]);
        }
        
        logger.LogInformation("Search returned available rooms");
        return new SearchAvailableResult.Success(availableRooms);
    }
}