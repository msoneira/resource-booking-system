using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ResourceBooking.Api.Data;
using ResourceBooking.Api.Models;

namespace ResourceBooking.Api.Tests;

public class BookingApiTests
{
    private static async Task SeedDataAsync(BookingApiFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        await db.Database.EnsureCreatedAsync();

        db.Offices.Add(new Office
        {
            Id = 1,
            Name = "Madrid Office",
            City = "Madrid",
            TimeZoneId = "Europe/Madrid"
        });

        db.Resources.Add(new Resource
        {
            Id = 1,
            Name = "Meeting Room Test",
            Type = "MeetingRoom",
            IsActive = true,
            OfficeId = 1
        });

        db.Users.Add(new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com"
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateBooking_WithValidRequest_ReturnsCreated()
    {
        using var factory = new BookingApiFactory();
        await SeedDataAsync(factory);

        using var client = factory.CreateClient();

        var request = new
        {
            resourceId = 1,
            userId = 1,
            start = "2026-10-23T09:00:00Z",
            end = "2026-10-23T10:00:00Z"
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var booking = await db.Bookings.SingleAsync();

        Assert.Equal(1, booking.ResourceId);
        Assert.Equal(1, booking.UserId);
        Assert.Equal("Confirmed", booking.Status);
        Assert.True(booking.EndUtc > booking.StartUtc);
    }

    [Fact]
    public async Task CreateBooking_WithOverlappingTime_ReturnsConflict()
    {
        using var factory = new BookingApiFactory();
        await SeedDataAsync(factory);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

        db.Bookings.Add(new Booking
        {
            ResourceId = 1,
            UserId = 1,
            StartUtc = new DateTime(2026, 10, 23, 9, 0, 0, DateTimeKind.Utc),
            EndUtc = new DateTime(2026, 10, 23, 10, 0, 0, DateTimeKind.Utc),
            Status = "Confirmed"
        });

        await db.SaveChangesAsync();

        using var client = factory.CreateClient();

        var request = new
        {
            resourceId = 1,
            userId = 1,
            start = "2026-10-23T09:30:00Z",
            end = "2026-10-23T10:30:00Z"
        };

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WithInvalidTimeRange_ReturnsBadRequest()
    {
        using var factory = new BookingApiFactory();
        await SeedDataAsync(factory);

        using var client = factory.CreateClient();

        var request = new
        {
            resourceId = 1,
            userId = 1,
            start = "2026-10-23T09:00:00Z",
            end = "2026-10-23T09:00:00Z"
        };

        var response = await client.PostAsJsonAsync(
            "/api/bookings",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CancelBooking_WithCorrectUser_ReturnsNoContent()
    {
        using var factory = new BookingApiFactory();
        await SeedDataAsync(factory);

        int bookingId;

        // Crear una reserva inicial.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<BookingDbContext>();

            var booking = new Booking
            {
                ResourceId = 1,
                UserId = 1,
                StartUtc = new DateTime(
                    2026, 10, 23, 9, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(
                    2026, 10, 23, 10, 0, 0, DateTimeKind.Utc),
                Status = "Confirmed"
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            bookingId = booking.Id;
        }

        using var client = factory.CreateClient();

        var response = await client.DeleteAsync(
            $"/api/bookings/{bookingId}?userId=1");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Comprobar que el cambio se ha guardado.
        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<BookingDbContext>();

        var savedBooking = await verificationDb.Bookings
            .SingleAsync(b => b.Id == bookingId);

        Assert.Equal("Cancelled", savedBooking.Status);
    }

    [Fact]
    public async Task CancelBooking_WithDifferentUser_ReturnsForbidden()
    {
        using var factory = new BookingApiFactory();
        await SeedDataAsync(factory);

        int bookingId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<BookingDbContext>();

            // La reserva pertenece al usuario 1.
            var booking = new Booking
            {
                ResourceId = 1,
                UserId = 1,
                StartUtc = new DateTime(
                    2026, 10, 23, 9, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(
                    2026, 10, 23, 10, 0, 0, DateTimeKind.Utc),
                Status = "Confirmed"
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            bookingId = booking.Id;
        }

        using var client = factory.CreateClient();

        // El usuario 2 intenta cancelar una reserva del usuario 1.
        var response = await client.DeleteAsync(
            $"/api/bookings/{bookingId}?userId=2");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<BookingDbContext>();

        var savedBooking = await verificationDb.Bookings
            .SingleAsync(b => b.Id == bookingId);

        Assert.Equal("Confirmed", savedBooking.Status);
    }
}