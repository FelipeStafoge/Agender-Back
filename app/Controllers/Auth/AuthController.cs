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
            UserCode = userCode
        };


        _context.Users.Add(user);

        await _context.SaveChangesAsync();


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

        Console.WriteLine($"AccountId: {accountId}");

        var user = await _context.Users.FindAsync(Guid.Parse(accountId!));


        if (user == null)
        {
            return Unauthorized();
        }

        var userEvent = new Event
        {
            AccountId = Guid.Parse(accountId!),
            Date = request.DateTime,
            Id = Guid.NewGuid(),
            Name = user.Name,
        };

        _context.Events.Add(userEvent);
        await _context.SaveChangesAsync();
        return Ok(new
        {
            message = "Criou o evento"
        });
    }

    [Authorize]
    [HttpGet("getListEvents")]
    public async Task<IActionResult> GetListEvents()
    {

        var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        Console.WriteLine($"AccountId: {accountId}");

        var user = await _context.Users.FindAsync(Guid.Parse(accountId!));


        if (user == null)
        {
            return Unauthorized();
        }

        var listEvents = await _context.Events
            .Where(e => e.AccountId == user.Id)
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

        return Ok(new { data = user });
    }

}

