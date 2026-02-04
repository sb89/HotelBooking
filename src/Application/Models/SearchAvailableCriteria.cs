namespace Infrastructure.Models;

public class SearchAvailableCriteria
{
    public required int HotelId { get; set; }

    public required DateOnly CheckInDate { get; set; }

    public required DateOnly CheckoutDate { get; set; }

    public required int NumberOfGuests { get; set; }
}