using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class HotelService(ApplicationDbContext dbContext) : IHotelService
{
    public Task<List<Hotel>> Search(string? name = null, CancellationToken cancellationToken = default)
    {
        // TODO: Implement pagination, due to limited data then no need for now, nice extra at end
        
        var query = dbContext.Hotels.AsQueryable();

        if (name != null)
        {
            query = query.Where(h => h.Name.Contains(name));
        }

        return query.ToListAsync(cancellationToken);
    }
}