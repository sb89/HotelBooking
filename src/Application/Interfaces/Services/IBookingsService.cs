using Infrastructure.Results.Bookings;

namespace Infrastructure.Interfaces.Services;

public interface IBookingsService
{
    Task<CreateBookingResult> CreateBooking(DateOnly startDate, DateOnly endDate, int roomId, int numberOfGuests, 
        CancellationToken cancellationToken = default);
}