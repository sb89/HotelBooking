namespace Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    
    public required DateOnly StartDate { get; set; }
    public required DateOnly EndDate { get; set; }
    public required int NoOfGuests { get; set; }
    
    public int HotelRoomId { get; set; }
    public HotelRoom? Room { get; set; }
}