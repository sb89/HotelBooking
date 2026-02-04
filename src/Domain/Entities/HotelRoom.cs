namespace Domain.Entities;

public class HotelRoom
{
    public int Id { get; set; }
    public required int RoomNumber { get; set; }
    public required HotelRoomType RoomType { get; set; }
    public required int Capacity { get; set; }

    public int HotelId { get; set; }
    public Hotel? Hotel { get; set; }
    
    public ICollection<Booking> Bookings { get; set; } = [];
}