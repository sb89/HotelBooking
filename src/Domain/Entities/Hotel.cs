namespace Domain.Entities;

public class Hotel
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public ICollection<HotelRoom> Rooms { get; set; } = [];
}