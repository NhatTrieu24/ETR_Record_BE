using ETR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ETR.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // NO DATA SEEDING IN C#. Seeding is handled via EF Core Migrations purely in SQL.
        await Task.CompletedTask;
    }
}
