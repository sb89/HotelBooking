using Infrastructure.Models;
using Infrastructure.Results.HotelRooms;

namespace Infrastructure.Interfaces.Services;

public interface IHotelRoomsService
{
    Task<SearchAvailableResult> SearchAvailable(SearchAvailableCriteria criteria);
}