using System.Collections.Concurrent;
using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Infrastructure.Results.Bookings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class BookingService(ApplicationDbContext dbContext, ILogger<BookingService> logger) : IBookingsService
{
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> CreateBookingLock = new();

    public async Task<CreateBookingResult> CreateBooking(DateOnly checkInDate, DateOnly checkOutDate, int roomId, 
        int numberOfGuests, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Attempting to create booking: {CheckInDate}-{CheckOutDate}, {NumberOfGuests} guests for room {RoomId}", 
            checkInDate, checkOutDate, numberOfGuests, roomId);
        
        var room = await dbContext.HotelRooms.FindAsync([roomId], cancellationToken);
        if (room == null)
        {
            logger.LogInformation("Booking declined: Room {RoomId} not found", roomId);
            return new CreateBookingResult.RoomNotFound();
        }

        if (numberOfGuests > room.Capacity)
        {
            logger.LogInformation(
                "Booking declined: {Guests} guests exceeds capacity {Capacity} for room {RoomId}",
                numberOfGuests, room.Capacity, roomId);
            return new CreateBookingResult.CapacityExceeded();
        }
        
        // This locking mechanism will only work if a single instance of the api is running, consider alternatives (ie SQL Locking)
        // if scaling required
        var roomLock = CreateBookingLock.GetOrAdd(roomId, _ => new SemaphoreSlim(1, 1));

        await roomLock.WaitAsync(cancellationToken);

        try
        {
            var hasOverlap = await dbContext.Bookings
                .AnyAsync(b =>
                    b.HotelRoomId == roomId &&
                    b.CheckInDate < checkOutDate &&
                    b.CheckOutDate > checkInDate,
                    cancellationToken);

            if (hasOverlap)
            {
                logger.LogInformation(
                    "Booking declined: Room {RoomId} unavailable for {CheckIn}-{CheckOut}",
                    roomId, checkInDate, checkOutDate);
                return new CreateBookingResult.RoomNoLongerAvailable();
            }
               

            var booking = new Booking
            {
                HotelRoomId = roomId,
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                NoOfGuests =  numberOfGuests
            };
            
            dbContext.Bookings.Add(booking);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation(
                "Booking {BookingId} created successfully for room {RoomId}",
                booking.Id, roomId);

            return new CreateBookingResult.Success(booking.Id);
        }
        finally
        {
            roomLock.Release();
        }
    }

    public Task<Booking?> Get(int bookingReference, CancellationToken cancellationToken = default)
    {
        return dbContext.Bookings
            .Include(x => x.Room)
            .ThenInclude(x => x!.Hotel)
            .FirstOrDefaultAsync(x => x.Id == bookingReference, cancellationToken);
    }
}