using AgenderBackend.Api.Models;

public class Event
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#653294";
    public Guid? CalendarId { get; set; }
    public Calendar? Calendar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<EventParticipant> Participants { get; set; } = [];
}

public class EventParticipant
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Role { get; set; } = "Member";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
