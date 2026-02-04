using Dunet;

namespace Infrastructure.Results.Bookings;

[Union]
public partial record CreateBookingResult
{
    public partial record Success(int BookingId);
    
    public partial record RoomNotFound;
    
    public partial record CapacityExceeded;
    
    public partial record RoomNoLongerAvailable;
}