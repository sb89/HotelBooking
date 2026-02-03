using Domain.Entities;
using Infrastructure.Data;
using Infrastructure.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AdminService(ApplicationDbContext dbContext) : IAdminService
{
    public Task Seed(CancellationToken cancellationToken = default)
    {
        var hotel1 = new Hotel { Name = "Hotel 1" };
        var hotel2 = new Hotel { Name = "Hotel 2" };
        var hotel3 = new Hotel { Name = "Hotel 3" };
        
        List<Hotel> hotels = [hotel1, hotel2, hotel3];
        
        dbContext.Hotels.AddRange(hotels);
        
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task Reset(CancellationToken cancellationToken = default)
    {
        dbContext.Hotels.ExecuteDeleteAsync(cancellationToken);
        
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}