using AgenderBackend.Api.Models;

public class UserCalendar
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}