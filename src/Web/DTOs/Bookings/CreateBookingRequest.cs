namespace Web.DTOs.Bookings;

public class CreateBookingRequest
{
    public int RoomId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int NumberOfGuests { get; set; }
}