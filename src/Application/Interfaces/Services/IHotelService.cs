using Domain.Entities;

namespace Infrastructure.Interfaces.Services;

public interface IHotelService
{
    Task<List<Hotel>> Search(string? name = null, CancellationToken cancellationToken = default); 
}