using Microsoft.EntityFrameworkCore;
using AgenderBackend.Api.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AgenderBackend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new()
        );

        modelBuilder.Entity<Event>()
            .Property(e => e.AccountsIds)
            .HasConversion(converter);
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Event> Events { get; set; }

}