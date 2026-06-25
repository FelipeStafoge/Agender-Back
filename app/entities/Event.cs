using AgenderBackend.Api.Models;

public class Event
{

    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Color { get; set; } = "#653294";

    public List<EventParticipant> Participants { get; set; } = [];
}

public class EventParticipant
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}