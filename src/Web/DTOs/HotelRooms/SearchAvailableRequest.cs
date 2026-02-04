namespace Web.DTOs.HotelRooms;

public class SearchAvailableRequest
{
    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int NumberOfGuests { get; set; }
}