using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Pinula.API.Context;

namespace Pinula.API.Tests.TestHelpers;

public static class TestDbContextFactory
{
    public static (PinulaDbContext Context, SqliteConnection Connection) CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PinulaDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new PinulaDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}