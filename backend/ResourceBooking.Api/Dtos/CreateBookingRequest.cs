namespace ResourceBooking.Api.Dtos;

public record CreateBookingRequest(
    int ResourceId,
    int UserId,
    DateTimeOffset Start,
    DateTimeOffset End
);