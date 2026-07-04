# 11 - Validacao

## FluentValidation

**Nao implementado**. O projeto nao utiliza a biblioteca FluentValidation.

## DataAnnotations

Apenas um uso de DataAnnotation encontrado:

```csharp
// app/entities/RefreshToken.cs
public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    ...
}
```

O atributo `[Key]` explicita a chave primaria da entidade `RefreshToken` (as demais entidades usam convencao de nome `Id`).

Nao ha validacao de propriedades com atributos como `[Required]`, `[MaxLength]`, `[EmailAddress]`, `[Range]`, etc. nas entidades ou DTOs.

## Validacao pelo ApiController

O atributo `[ApiController]` no controller fornece validacao automatica de `ModelState` baseada em:
- Tipos de dados (ex: `string` vs `int`)
- Nullable annotations (ex: `string` vs `string?`)

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase { ... }
```

Se um campo obrigatorio (non-nullable) nao for enviado, o framework retorna automaticamente `400 Bad Request` com detalhes de validacao no formato `ProblemDetails`.

## Validacoes Manuais no Controller

Como nao ha validacao declarativa (FluentValidation ou DataAnnotations), as validacoes sao feitas manualmente nos metodos do controller:

### Validacao de GUID

```csharp
if (!Guid.TryParse(accountId, out var creatorId))
    return Unauthorized();

if (!Guid.TryParse(calendarId, out var parsedCalendarId))
    return BadRequest(new { message = "calendarId invalido" });

if (!Guid.TryParse(eventId, out var parsedEventId))
    return BadRequest(new { message = "eventId invalido" });
```

### Validacao de formato (NameWithCode)

```csharp
var parts = nameWithCode.Split('#');

if (parts.Length != 2)
{
    return BadRequest("Formato invalido. Use Nome#Codigo");
}
```

### Validacao de existencia

```csharp
var user = await _context.Users.FindAsync(creatorId);
if (user == null)
    return Unauthorized();

if (calendar == null)
    return NotFound(new { message = "Calendario nao encontrado" });

var participant = await _context.CalendarParticipant
    .FirstOrDefaultAsync(cp => cp.CalendarId == parsedCalendarId && cp.UserId == userId);
if (participant == null)
    return NotFound(new { message = "Participacao nao encontrada" });
```

### Validacao de autorizacao

```csharp
var hasAccess = calendar.OwnerId == accountId ||
    await _context.CalendarParticipant
        .AnyAsync(cp => cp.CalendarId == parsedCalendarId && cp.UserId == userId);

if (!hasAccess)
    return Forbid();

if (calendar.AccountId != userId)
    return Forbid();
```

### Validacao de unicidade

```csharp
if (await _context.Users.AnyAsync(u => u.Email == request.Email))
{
    return BadRequest(new
    {
        action = ActionsRequest.Error.Register.UserAlreadyExists
    });
}
```

## Validacao de Datas

A validacao de datas e feita comparando strings. Nao ha parse para `DateTime` ou `DateOnly`:

```csharp
// startDate e endDate sao comparados como strings YYYYMMDD
var parts = startDate.Split('/');
var startComparable = parts[2] + parts[1] + parts[0];
query = query.Where(e =>
    string.Compare(
        e.Date.Substring(6, 4) + e.Date.Substring(3, 2) + e.Date.Substring(0, 2),
        startComparable
    ) >= 0);
```

**Observacao**: Nao ha validacao de formato de data (se a string esta no formato `DD/MM/YYYY` correto). Se o formato for invalido, `string.Compare` ou `Substring` podem gerar excecoes.

## Exceptions de Validacao

Nao ha exception customizada para validacao. Os erros de validacao sao retornados como:
- `400 Bad Request` com mensagem descritiva
- `401 Unauthorized` (token invalido/ausente)
- `403 Forbidden` (sem permissao)
- `404 Not Found` (entidade nao encontrada)
