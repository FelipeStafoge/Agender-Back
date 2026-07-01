using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgenderBackend.Api.Models;
using AgenderBackend.Services;
using AgenderBackend.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;


namespace AgenderBackend.Api.Controllers;



[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly AppDbContext _context;

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(64)
        );
    }
    public AuthController(
        JwtService jwtService,
        AppDbContext context)
    {
        _jwtService = jwtService;
        _context = context;
    }



    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Email == request.Email &&
                u.Password == request.Password);

        if (user == null)
        {
            return Unauthorized(new
            {
                action = ActionsRequest.Error.Login.WrongPasswordOrEmail
            });
        }

        var token = _jwtService.GenerateToken(user.Email, user.Id);

        var refreshToken = new RefreshToken
        {
            Token = GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            token,
            refreshToken = refreshToken.Token,
            data = new
            {
                userName = user.Name,
                userCode = user.UserCode,
                account_id = user.Id,
                email = user.Email,
                action = ActionsRequest.Success.Login.UserLoggedIn
            }
        });
    }


    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
        {
            return BadRequest(new
            {
                action = ActionsRequest.Error.Register.UserAlreadyExists
            });
        }

        string userCode;

        do
        {
            userCode = Random.Shared.Next(1000, 9999).ToString();
        }
        while (await _context.Users.AnyAsync(
            u => u.Name == request.Name &&
                 u.UserCode == userCode
        ));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Password = request.Password,
            UserCode = userCode,
            CreatedAt = DateTime.UtcNow
        };


        _context.Users.Add(user);

        await _context.SaveChangesAsync();

        var personalCalendar = new Calendar
        {
            Id = Guid.NewGuid(),
            AccountId = user.Id,
            Name = "Meus Eventos",
            DefaultColor = "#7c3aed",
            OwnerId = user.Id.ToString(),
            IsPersonal = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Calendar.Add(personalCalendar);

        var personalCalendarParticipant = new CalendarParticipant
        {
            Id = Guid.NewGuid(),
            CalendarId = personalCalendar.Id,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            Role = "Owner"
        };

        _context.CalendarParticipant.Add(personalCalendarParticipant);

        var token = _jwtService.GenerateToken(user.Email, user.Id);

        var refreshToken = new RefreshToken
        {
            Token = GenerateRefreshToken(),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            action = ActionsRequest.Success.Register.UserRegistered,
            token,
            refreshToken
        });


    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
       RefreshTokenRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t =>
                t.Token == request.RefreshToken &&
                !t.Revoked);

        if (storedToken == null)
        {
            return Unauthorized();
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.Id == storedToken.UserId);

        if (user == null)
        {
            return Unauthorized();
        }

        var newToken = _jwtService.GenerateToken(
            user.Email,
            user.Id
        );

        return Ok(new
        {
            token = newToken
        });
    }

    [Authorize]
    [HttpPost("createEvent")]
    public async Task<IActionResult> CreateEvent(CreateEventRequest request)
    {
        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var creatorId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(creatorId);

        if (user == null)
            return Unauthorized();

        Guid? calendarId = null;

        if (!string.IsNullOrEmpty(request.Calendar_id) && Guid.TryParse(request.Calendar_id, out var parsedCalendarId))
        {
            calendarId = parsedCalendarId;
        }

        var eventColor = request.Color;

        if (calendarId.HasValue)
        {
            var calendar = await _context.Calendar.FindAsync(calendarId.Value);

            if (calendar != null)
            {
                eventColor = calendar.DefaultColor;
            }
        }

        var now = DateTime.UtcNow;

        var newEvent = new Event
        {
            Id = Guid.NewGuid(),
            AccountId = creatorId,
            Name = request.Name,
            Date = request.Date,
            Color = eventColor,
            CalendarId = calendarId,
            CreatedAt = now
        };

        _context.Events.Add(newEvent);


        var participants = new List<EventParticipant>
    {
        new EventParticipant
        {
            EventId = newEvent.Id,
            UserId = creatorId,
            Role = "Owner",
            CreatedAt = now
        }
    };

        foreach (var userId in request.Users_ids)
        {

            if (!Guid.TryParse(userId, out var parsedUserId))
                continue;

            participants.Add(new EventParticipant
            {
                EventId = newEvent.Id,
                UserId = parsedUserId,
                Role = "Member",
                CreatedAt = now
            });

        }

        _context.EventParticipants.AddRange(participants);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Evento criado com participantes",
            eventId = newEvent.Id
        });
    }


    [Authorize]
    [HttpGet("getListEvents")]
    public async Task<IActionResult> GetListEvents()
    {

        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


        var user = await _context.Users.FindAsync(Guid.Parse(accountId!));


        if (user == null)
        {
            return Unauthorized();
        }

        var listEvents = await _context.Events
            .Where(e =>
                (e.CalendarId == null && e.Participants.Any(p => p.UserId == user.Id)) ||
                (e.CalendarId != null && e.Calendar!.CalendarParticipants.Any(p => p.UserId == user.Id)))
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .Select(e => new EventResponse
            {
                Id = e.Id,
                Name = e.Name,
                Date = e.Date,
                Color = e.Color,
                CalendarId = e.CalendarId,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                DeletedAt = e.DeletedAt,
                Participants = e.Participants.Select(p => new ParticipantResponse
                {
                    UserId = p.UserId,
                    Name = p.User.Name,
                    Role = p.Role,
                    CreatedAt = p.CreatedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            data = listEvents
        });
    }

    [Authorize]
    [HttpGet("getCalendarEvents")]
    public async Task<IActionResult> GetCalendarEvents([FromQuery] string calendarId)
    {
        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var userId))
            return Unauthorized();

        if (!Guid.TryParse(calendarId, out var parsedCalendarId))
            return BadRequest(new { message = "calendarId inválido" });

        var calendar = await _context.Calendar
            .FirstOrDefaultAsync(c => c.Id == parsedCalendarId);

        if (calendar == null)
            return NotFound(new { message = "Calendário não encontrado" });

        var hasAccess = calendar.OwnerId == accountId ||
            await _context.CalendarParticipant
                .AnyAsync(cp => cp.CalendarId == parsedCalendarId && cp.UserId == userId);

        if (!hasAccess)
            return Forbid();

        var listEvents = await _context.Events
            .Where(e => e.CalendarId == parsedCalendarId)
            .Include(e => e.Participants)
                .ThenInclude(p => p.User)
            .Select(e => new EventResponse
            {
                Id = e.Id,
                Name = e.Name,
                Date = e.Date,
                Color = e.Color,
                CalendarId = e.CalendarId,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                DeletedAt = e.DeletedAt,
                Participants = e.Participants.Select(p => new ParticipantResponse
                {
                    UserId = p.UserId,
                    Name = p.User.Name,
                    Role = p.Role,
                    CreatedAt = p.CreatedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            data = listEvents
        });
    }

    [Authorize]
    [HttpGet("getUserInfo")]
    public async Task<IActionResult> GetUserInfo([FromQuery] string nameWithCode)
    {
        var parts = nameWithCode.Split('#');

        if (parts.Length != 2)
        {
            return BadRequest("Formato inválido. Use Nome#Codigo");
        }

        var name = parts[0];
        var code = parts[1];

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Name == name && u.UserCode == code);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new { name = user.Name, email = user.Email, id = user.Id, userCode = user.UserCode, createdAt = user.CreatedAt, updatedAt = user.UpdatedAt, deletedAt = user.DeletedAt });
    }

    [Authorize]
    [HttpPost("createCalendar")]
    public async Task<IActionResult> CreateCalendar(CreateCalendarRequest request)
    {
        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var creatorId))
            return Unauthorized();

        var user = await _context.Users.FindAsync(creatorId);

        if (user == null)
            return Unauthorized();

        var now = DateTime.UtcNow;

        var newCalendar = new Calendar
        {
            Id = Guid.NewGuid(),
            AccountId = creatorId,
            Name = request.Name,
            DefaultColor = request.DefaultColor,
            OwnerId = creatorId.ToString(),
            IsPersonal = false,
            CreatedAt = now
        };

        _context.Calendar.Add(newCalendar);

        var participants = new List<CalendarParticipant>
    {
        new CalendarParticipant
        {
            Id = Guid.NewGuid(),
            CalendarId = newCalendar.Id,
            UserId = creatorId,
            CreatedAt = now,
           Role = "Owner"
        }
    };

        foreach (var userId in request.Users_ids)
        {

            if (!Guid.TryParse(userId, out var parsedUserId))
                continue;

            participants.Add(new CalendarParticipant
            {
                CalendarId = newCalendar.Id,
                UserId = parsedUserId,
                Id = Guid.NewGuid(),
                CreatedAt = now,
                Role = "Member"
            });

        }

        _context.CalendarParticipant.AddRange(participants);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Novo calendario criado com sucesso",
            eventId = newCalendar.Id
        });
    }



    [Authorize]
    [HttpGet("getListCalendar")]
    public async Task<IActionResult> GetListCalendar()
    {

        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


        var user = await _context.Users.FindAsync(Guid.Parse(accountId!));


        if (user == null)
        {
            return Unauthorized();
        }

        var listCalendars = await _context.Calendar
            .Where(c => c.OwnerId == accountId || c.CalendarParticipants.Any(p => p.UserId == user.Id))
            .Include(c => c.CalendarParticipants)
                .ThenInclude(p => p.User)
            .Select(c => new CalendarResponse
            {
                Id = c.Id,
                Name = c.Name,
                Date = c.Date,
                Color = c.DefaultColor,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                DeletedAt = c.DeletedAt,
                Participants = c.CalendarParticipants.Select(p => new ParticipantResponse
                {
                    UserId = p.UserId,
                    Name = p.User.Name,
                    Role = p.Role,
                    CreatedAt = p.CreatedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(new
        {
            data = listCalendars
        });
    }

    [Authorize]
    [HttpPost("leaveCalendar/{calendarId}")]
    public async Task<IActionResult> LeaveCalendar(string calendarId)
    {
        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var userId))
            return Unauthorized();

        if (!Guid.TryParse(calendarId, out var parsedCalendarId))
            return BadRequest(new { message = "calendarId inválido" });

        var participant = await _context.CalendarParticipant
            .FirstOrDefaultAsync(cp => cp.CalendarId == parsedCalendarId && cp.UserId == userId);

        if (participant == null)
            return NotFound(new { message = "Participação não encontrada" });

        participant.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Você saiu do calendário" });
    }

    [Authorize]
    [HttpDelete("deleteCalendar/{calendarId}")]
    public async Task<IActionResult> DeleteCalendar(string calendarId)
    {
        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var userId))
            return Unauthorized();

        if (!Guid.TryParse(calendarId, out var parsedCalendarId))
            return BadRequest(new { message = "calendarId inválido" });

        var calendar = await _context.Calendar
            .FirstOrDefaultAsync(c => c.Id == parsedCalendarId);

        if (calendar == null)
            return NotFound(new { message = "Calendário não encontrado" });

        if (calendar.AccountId != userId)
            return Forbid();

        calendar.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Calendário deletado" });
    }

    [Authorize]
    [HttpPost("leaveEvent/{eventId}")]
    public async Task<IActionResult> LeaveEvent(string eventId)
    {
        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var userId))
            return Unauthorized();

        if (!Guid.TryParse(eventId, out var parsedEventId))
            return BadRequest(new { message = "eventId inválido" });

        var participant = await _context.EventParticipants
            .FirstOrDefaultAsync(ep => ep.EventId == parsedEventId && ep.UserId == userId);

        if (participant == null)
            return NotFound(new { message = "Participação não encontrada" });

        participant.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Você saiu do evento" });
    }

    [Authorize]
    [HttpDelete("deleteEvent/{eventId}")]
    public async Task<IActionResult> DeleteEvent(string eventId)
    {
        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var userId))
            return Unauthorized();

        if (!Guid.TryParse(eventId, out var parsedEventId))
            return BadRequest(new { message = "eventId inválido" });

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == parsedEventId);

        if (eventEntity == null)
            return NotFound(new { message = "Evento não encontrado" });

        if (eventEntity.AccountId != userId)
            return Forbid();

        eventEntity.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Evento deletado" });
    }

}

