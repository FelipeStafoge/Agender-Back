public class CreateCalendarRequest
{
    public string Name { get; set; } = string.Empty;
    public string DefaultColor { get; set; } = string.Empty;
    public List<string> Users_ids { get; set; } = new();
}

public class CalendarResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Date { get; set; } = [];
    public string Color { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public List<ParticipantResponse> Participants { get; set; } = [];
}
