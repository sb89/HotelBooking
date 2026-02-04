using Domain.Entities;
using Infrastructure.Results.Bookings;

namespace Infrastructure.Interfaces.Services;

public interface IBookingsService
{
    Task<CreateBookingResult> CreateBooking(DateOnly checkInDate, DateOnly checkOutDate, int roomId, int numberOfGuests, 
        CancellationToken cancellationToken = default);

    Task<Booking?> Get(int bookingReference, CancellationToken cancellationToken = default);
}