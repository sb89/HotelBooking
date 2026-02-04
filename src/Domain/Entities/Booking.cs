namespace Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    
    public required DateOnly CheckInDate { get; set; }
    public required DateOnly CheckOutDate { get; set; }
    public required int NoOfGuests { get; set; }
    
    public int HotelRoomId { get; set; }
    public HotelRoom? Room { get; set; }
}