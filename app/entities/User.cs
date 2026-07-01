using Microsoft.EntityFrameworkCore;

namespace AgenderBackend.Api.Models;

[Index(nameof(Name), nameof(UserCode), IsUnique = true)]
public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<CalendarParticipant> CalendarParticipants { get; set; } = [];
}
