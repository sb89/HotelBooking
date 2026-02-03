namespace Domain.Entities;

public class HotelRoom
{
    public int Id { get; set; }
    public int RoomNumber { get; set; }
    public HotelRoomType RoomType { get; set; }
    public int Capacity { get; set; }

    public int HotelId { get; set; }
    public Hotel? Hotel { get; set; }
    
    public ICollection<Booking> Bookings { get; set; } = [];
}