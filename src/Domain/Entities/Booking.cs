namespace Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int NoOfGuests { get; set; }
    
    public int HotelRoomId { get; set; }
    public HotelRoom? Room { get; set; }
    
    public ICollection<Booking> Bookings { get; set; } = [];
}