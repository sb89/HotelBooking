namespace Web.DTOs.Bookings;

public class GetBookingResult
{
    public required int BookingReference { get; set; }

    public required string HotelName { get; set; }

    public required int RoomNumber { get; set; }

    public required DateOnly CheckInDate { get; set; }
    
    public required DateOnly CheckOutDate { get; set; }

    public required int NumberOfGuests { get; set; }
}