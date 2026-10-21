using Microsoft.EntityFrameworkCore;
using ResourceBooking.Api.Models;

namespace ResourceBooking.Api.Data;

public class BookingDbContext(DbContextOptions<BookingDbContext> options)
    : DbContext(options)
{
    public DbSet<Office> Offices => Set<Office>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Office>()
            .Property(o => o.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Office>()
            .Property(o => o.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Resource>()
            .Property(r => r.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Resource>()
            .Property(r => r.Type)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Resource)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.User)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Booking_ValidInterval",
                "\"StartUtc\" < \"EndUtc\""));
    }
}