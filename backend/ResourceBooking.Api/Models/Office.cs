namespace ResourceBooking.Api.Models;

public class Office
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = "Europe/Madrid";

    public List<Resource> Resources { get; set; } = [];
}