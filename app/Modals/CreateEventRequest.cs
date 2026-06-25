

public class CreateEventRequest
{
    public string Date { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public List<string> Users_ids { get; set; } = new();
}


public class EventResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;

    public List<ParticipantResponse> Participants { get; set; } = [];
}

public class ParticipantResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}