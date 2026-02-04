using Domain.Entities;
using Dunet;

namespace Infrastructure.Results.HotelRooms;

[Union]
public partial record SearchAvailableResult
{
    public partial record Success(List<HotelRoom> HotelRooms);

    public partial record HotelNotFound;
}