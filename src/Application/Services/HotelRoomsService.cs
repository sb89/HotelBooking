using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Infrastructure.Models;
using Infrastructure.Results.HotelRooms;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class HotelRoomsService(ApplicationDbContext dbContext) : IHotelRoomsService
{
    public async Task<SearchAvailableResult> SearchAvailable(SearchAvailableCriteria criteria, CancellationToken cancellationToken = default)
    {
        var hotel = await dbContext.Hotels.FindAsync([criteria.HotelId], cancellationToken);
        if (hotel == null)
        {
            return new SearchAvailableResult.HotelNotFound();
        }
        
        var availableRooms = await dbContext.HotelRooms
            .Where(r => r.HotelId == criteria.HotelId)
            .Where(r => !r.Bookings.Any(b =>
                b.CheckInDate < criteria.CheckoutDate && b.CheckOutDate > criteria.CheckInDate))
            .ToListAsync(cancellationToken);
        
        var totalCapacity = availableRooms.Sum(r => r.Capacity);

        if (totalCapacity < criteria.NumberOfGuests)
            return new SearchAvailableResult.Success([]);
        
        return new SearchAvailableResult.Success(availableRooms);
    }
}