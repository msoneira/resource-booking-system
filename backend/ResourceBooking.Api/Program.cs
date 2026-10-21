using Microsoft.EntityFrameworkCore;
using ResourceBooking.Api.Data;
using ResourceBooking.Api.Dtos;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("VueFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors("VueFrontend");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<BookingDbContext>();

    await DbInitializer.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/resources", async (
    BookingDbContext db,
    string? type,
    int? officeId,
    CancellationToken cancellationToken) =>
{
    var query = db.Resources
        .AsNoTracking()
        .Where(r => r.IsActive);

    if (!string.IsNullOrWhiteSpace(type))
    {
        query = query.Where(r => r.Type == type);
    }

    if (officeId.HasValue)
    {
        query = query.Where(r => r.OfficeId == officeId.Value);
    }

    var resources = await query
        .OrderBy(r => r.Name)
        .Select(r => new
        {
            r.Id,
            r.Name,
            r.Type,
            r.OfficeId,
            Office = r.Office!.Name,
            City = r.Office.City
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(resources);
})
.WithName("GetResources")
.WithSummary("Lists resources filtered by type and office");

app.MapPost("/api/bookings", async (
    CreateBookingRequest request,
    BookingDbContext db,
    CancellationToken cancellationToken) =>
{
    if (request.Start >= request.End)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["End"] = ["The end time must be later than the start time."]
            });
    }

    var resourceExists = await db.Resources.AnyAsync(
        r => r.Id == request.ResourceId && r.IsActive,
        cancellationToken);

    if (!resourceExists)
    {
        return Results.NotFound(new
        {
            error = "The resource does not exist or is inactive."
        });
    }

    var userExists = await db.Users.AnyAsync(
        u => u.Id == request.UserId,
        cancellationToken);

    if (!userExists)
    {
        return Results.NotFound(new
        {
            error = "The user does not exist."
        });
    }

    var startUtc = request.Start.UtcDateTime;
    var endUtc = request.End.UtcDateTime;

    var hasConflict = await db.Bookings.AnyAsync(
        b => b.ResourceId == request.ResourceId
             && b.Status == "Confirmed"
             && b.StartUtc < endUtc
             && b.EndUtc > startUtc,
        cancellationToken);

    if (hasConflict)
    {
        return Results.Conflict(new
        {
            error = "The resource is already booked for this time slot."
        });
    }

    var booking = new ResourceBooking.Api.Models.Booking
    {
        ResourceId = request.ResourceId,
        UserId = request.UserId,
        StartUtc = startUtc,
        EndUtc = endUtc,
        Status = "Confirmed"
    };

    db.Bookings.Add(booking);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created(
        $"/api/bookings/{booking.Id}",
        new
        {
            booking.Id,
            booking.ResourceId,
            booking.UserId,
            booking.StartUtc,
            booking.EndUtc,
            booking.Status
        });
})
.WithName("CreateBooking")
.WithSummary("Creates a booking if the resource is available");

app.MapGet("/api/bookings", async (
    int userId,
    BookingDbContext db,
    CancellationToken cancellationToken) =>
{
    var userExists = await db.Users.AnyAsync(
        u => u.Id == userId,
        cancellationToken);

    if (!userExists)
    {
        return Results.NotFound(new
        {
            error = "The user does not exist."
        });
    }

    var bookings = await db.Bookings
        .AsNoTracking()
        .Where(b => b.UserId == userId)
        .OrderBy(b => b.StartUtc)
        .Select(b => new
        {
            b.Id,
            b.ResourceId,
            ResourceName = b.Resource!.Name,
            OfficeName = b.Resource.Office!.Name,
            b.StartUtc,
            b.EndUtc,
            b.Status
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(bookings);
})
.WithName("GetBookingsByUser")
.WithSummary("Returns bookings for a user");

app.MapDelete("/api/bookings/{id:int}", async (
    int id,
    int userId,
    BookingDbContext db,
    CancellationToken cancellationToken) =>
{
    var booking = await db.Bookings.FirstOrDefaultAsync(
        b => b.Id == id,
        cancellationToken);

    if (booking is null)
    {
        return Results.NotFound(new
        {
            error = "The booking does not exist."
        });
    }

    if (booking.UserId != userId)
    {
        return Results.Forbid();
    }

    if (booking.Status == "Cancelled")
    {
        return Results.Conflict(new
        {
            error = "The booking has already been cancelled."
        });
    }

    booking.Status = "Cancelled";

    await db.SaveChangesAsync(cancellationToken);

    return Results.NoContent();
})
.WithName("CancelBooking")
.WithSummary("Cancels a booking belonging to the specified user");

app.MapGet("/api/resources/{resourceId:int}/bookings", async (
    int resourceId,
    DateOnly date,
    BookingDbContext db,
    CancellationToken cancellationToken) =>
{
    var resource = await db.Resources
        .AsNoTracking()
        .Where(r => r.Id == resourceId && r.IsActive)
        .Select(r => new
        {
            r.Id,
            OfficeTimeZoneId = r.Office!.TimeZoneId
        })
        .FirstOrDefaultAsync(cancellationToken);

    if (resource is null)
    {
        return Results.NotFound(new
        {
            error = "The resource does not exist or is inactive."
        });
    }

    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(
        resource.OfficeTimeZoneId);

    var localStart = date.ToDateTime(TimeOnly.MinValue);
    var localEnd = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

    var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone);
    var endUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone);

    var bookings = await db.Bookings
        .AsNoTracking()
        .Where(b =>
            b.ResourceId == resourceId &&
            b.Status == "Confirmed" &&
            b.StartUtc < endUtc &&
            b.EndUtc > startUtc)
        .OrderBy(b => b.StartUtc)
        .Select(b => new
        {
            b.Id,
            b.StartUtc,
            b.EndUtc,
            b.Status
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new
    {
        TimeZoneId = resource.OfficeTimeZoneId,
        Bookings = bookings.Select(b => new
        {
            b.Id,
            Start = TimeZoneInfo.ConvertTime(
                new DateTimeOffset(
                    DateTime.SpecifyKind(
                        b.StartUtc,
                        DateTimeKind.Utc)),
                timeZone),
            End = TimeZoneInfo.ConvertTime(
                new DateTimeOffset(
                    DateTime.SpecifyKind(
                        b.EndUtc,
                        DateTimeKind.Utc)),
                timeZone),
            b.Status
        })
    });
})
.WithName("GetResourceBookingsByDay")
.WithSummary("Returns confirmed bookings for a resource on a local office date");

app.Run();
