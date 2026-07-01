using Microsoft.EntityFrameworkCore;
using AgenderBackend.Api.Models;

namespace AgenderBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EventParticipant
        modelBuilder.Entity<EventParticipant>()
            .HasKey(ep => new { ep.EventId, ep.UserId });

        modelBuilder.Entity<EventParticipant>()
            .HasOne(ep => ep.Event)
            .WithMany(e => e.Participants)
            .HasForeignKey(ep => ep.EventId);

        modelBuilder.Entity<EventParticipant>()
            .HasOne(ep => ep.User)
            .WithMany()
            .HasForeignKey(ep => ep.UserId);

        // CalendarParticipant
        modelBuilder.Entity<CalendarParticipant>()
            .HasKey(cp => cp.Id);

        modelBuilder.Entity<CalendarParticipant>()
            .HasOne(cp => cp.Calendar)
            .WithMany(c => c.CalendarParticipants)
            .HasForeignKey(cp => cp.CalendarId);

        modelBuilder.Entity<CalendarParticipant>()
            .HasOne(cp => cp.User)
            .WithMany(u => u.CalendarParticipants)
            .HasForeignKey(cp => cp.UserId);

        // Impede o mesmo usuário de participar duas vezes do mesmo calendário
        modelBuilder.Entity<CalendarParticipant>()
            .HasIndex(cp => new { cp.CalendarId, cp.UserId })
            .IsUnique();

        // Event -> Calendar (optional)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Calendar)
            .WithMany()
            .HasForeignKey(e => e.CalendarId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<Calendar> Calendar => Set<Calendar>();
    public DbSet<CalendarParticipant> CalendarParticipant => Set<CalendarParticipant>();
}