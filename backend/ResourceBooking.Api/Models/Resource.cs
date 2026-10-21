namespace ResourceBooking.Api.Models;

public class Resource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public int OfficeId { get; set; }
    public Office? Office { get; set; }

    public List<Booking> Bookings { get; set; } = [];
}