using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hotel>()
            .HasIndex(h => h.Name);
        
        modelBuilder.Entity<HotelRoom>()
            .HasIndex(r => new { r.HotelId, r.RoomNumber })
            .IsUnique();
    }
    
    public DbSet<Hotel> Hotels { get; set; }
    
    public DbSet<HotelRoom> HotelRooms { get; set; }
    
    public DbSet<Booking> Bookings { get; set; }
}