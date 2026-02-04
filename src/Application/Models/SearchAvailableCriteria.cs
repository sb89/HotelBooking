namespace Infrastructure.Models;

public class SearchAvailableCriteria
{
    public required int HotelId { get; set; }

    public required DateOnly StartDate { get; set; }

    public required DateOnly EndDate { get; set; }

    public required int NumberOfGuests { get; set; }
}