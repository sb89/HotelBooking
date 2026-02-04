using Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ApplicationTests.Factories;

public static class DbContextFactory
{
    public static ApplicationDbContext Create()
    {
        // Create in-memory SQLite connection
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);

        // Create schema
        context.Database.EnsureCreated();

        return context;
    }
}