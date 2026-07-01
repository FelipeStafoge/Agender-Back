using AgenderBackend.Api.Models;

public class Calendar
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Date { get; set; } = [];
    public string DefaultColor { get; set; } = "#653294";
    public string OwnerId { get; set; } = string.Empty;
    public bool IsPersonal { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<CalendarParticipant> CalendarParticipants { get; set; } = [];
}

public class CalendarParticipant
{
    public Guid Id { get; set; }

    public Guid CalendarId { get; set; }
    public Calendar Calendar { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string Role { get; set; } = "Member";
}
