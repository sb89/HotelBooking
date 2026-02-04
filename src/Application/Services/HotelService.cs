using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class HotelService(ApplicationDbContext dbContext, ILogger<HotelService> logger) : IHotelService
{
    public Task<List<Hotel>> Search(string? name = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Searching hotels with name {HotelName}", name);
        
        // TODO: Implement pagination, due to limited data then no need for now, nice extra at end
        
        var query = dbContext.Hotels.AsQueryable();

        if (name != null)
        {
            query = query.Where(h => h.Name.Contains(name));
        }
        
        query = query.OrderBy(h => h.Name);

        return query.ToListAsync(cancellationToken);
    }
}