
using Microsoft.EntityFrameworkCore;
using ResourceBooking.Api.Models;

namespace ResourceBooking.Api.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(BookingDbContext db)
    {
        await db.Database.MigrateAsync();

        if (await db.Offices.AnyAsync())
            return;

        var madrid = new Office
        {
            Name = "Madrid Office",
            City = "Madrid",
            TimeZoneId = "Europe/Madrid"
        };

        var london = new Office
        {
            Name = "London Office",
            City = "London",
            TimeZoneId = "Europe/London"
        };

        var user = new User
        {
            Name = "Maria",
            Email = "maria@example.com"
        };

        db.Offices.AddRange(madrid, london);
        db.Users.Add(user);

        db.Resources.AddRange(
            new Resource
            {
                Name = "Meeting Room A",
                Type = "MeetingRoom",
                Office = madrid
            },
            new Resource
            {
                Name = "Hot Desk 01",
                Type = "HotDesk",
                Office = madrid
            },
            new Resource
            {
                Name = "Projector 01",
                Type = "Equipment",
                Office = london
            }
        );

        await db.SaveChangesAsync();
    }
}
