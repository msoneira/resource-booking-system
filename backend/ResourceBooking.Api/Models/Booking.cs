namespace ResourceBooking.Api.Models;

public class Booking
{
    public int Id { get; set; }

    public int ResourceId { get; set; }
    public Resource? Resource { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }

    public string Status { get; set; } = "Confirmed";
}