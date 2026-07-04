# 08 - Repositories

## Nao Existem Repositories

O projeto **nao possui** uma camada de repositorio separada. O `AuthController` acessa o `AppDbContext` diretamente.

Todas as queries e operacoes de banco de dados sao feitas inline nos metodos do controller, usando os DbSets expostos pelo `AppDbContext`.

---

## DbSets Disponiveis (AppDbContext)

```csharp
public DbSet<User> Users => Set<User>();
public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
public DbSet<Event> Events => Set<Event>();
public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
public DbSet<Calendar> Calendar => Set<Calendar>();
public DbSet<CalendarParticipant> CalendarParticipant => Set<CalendarParticipant>();
```

---

## Queries Realizadas nos Endpoints

### Login

```csharp
var user = await _context.Users
    .FirstOrDefaultAsync(u =>
        u.Email == request.Email &&
        u.Password == request.Password);
```

- **DbSet**: `Users`
- **Metodo**: `FirstOrDefaultAsync`
- **Filtro**: Email e Password (texto puro)

### Register (verificacao de email)

```csharp
await _context.Users.AnyAsync(u => u.Email == request.Email)
```

- **DbSet**: `Users`
- **Metodo**: `AnyAsync`
- **Filtro**: Email

### Register (verificacao de codigo unico)

```csharp
await _context.Users.AnyAsync(
    u => u.Name == request.Name &&
         u.UserCode == userCode)
```

- **DbSet**: `Users`
- **Metodo**: `AnyAsync`

### Register (criacao de usuario)

```csharp
_context.Users.Add(user);
await _context.SaveChangesAsync();
```

### Refresh

```csharp
var storedToken = await _context.RefreshTokens
    .FirstOrDefaultAsync(t =>
        t.Token == request.RefreshToken &&
        !t.Revoked);
```

- **DbSet**: `RefreshTokens`
- **Metodo**: `FirstOrDefaultAsync`
- **Filtro**: Token string e Revoked == false

### Refresh (busca usuario)

```csharp
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Id == storedToken.UserId);
```

### CreateEvent

```csharp
var user = await _context.Users.FindAsync(creatorId);
```

- **DbSet**: `Users`
- **Metodo**: `FindAsync` (busca por PK)

```csharp
var calendar = await _context.Calendar.FindAsync(calendarId.Value);
```

### GetListEvents

```csharp
var user = await _context.Users.FindAsync(Guid.Parse(accountId!));
```

```csharp
var query = _context.Events
    .Where(e =>
        (e.CalendarId == null && e.Participants.Any(p => p.UserId == user.Id)) ||
        (e.CalendarId != null && e.Calendar!.CalendarParticipants.Any(p => p.UserId == user.Id)));
```

- **DbSet**: `Events`
- **Filtro principal**: Evento sem calendario onde usuario e participante direto, OU evento com calendario onde usuario e participante do calendario

**Filtro de data** (comparacao de strings):

```csharp
if (!string.IsNullOrEmpty(startDate))
{
    var parts = startDate.Split('/');
    var startComparable = parts[2] + parts[1] + parts[0];
    query = query.Where(e =>
        string.Compare(e.Date.Substring(6, 4) + e.Date.Substring(3, 2) + e.Date.Substring(0, 2), startComparable) >= 0);
}
```

**Includes**:

```csharp
var listEvents = await query
    .Include(e => e.Participants)
        .ThenInclude(p => p.User)
    .Select(e => new EventResponse { ... })
    .ToListAsync();
```

- **Eager Loading**: `Include` + `ThenInclude` para carregar participantes e usuarios
- **Projecao**: `.Select()` para mapear para `EventResponse`

### GetCalendarEvents

```csharp
var calendar = await _context.Calendar
    .FirstOrDefaultAsync(c => c.Id == parsedCalendarId);
```

```csharp
var hasAccess = calendar.OwnerId == accountId ||
    await _context.CalendarParticipant
        .AnyAsync(cp => cp.CalendarId == parsedCalendarId && cp.UserId == userId);
```

```csharp
var query = _context.Events
    .Where(e => e.CalendarId == parsedCalendarId);
```

- Mesmo padrao de filtro de data e Include/ThenInclude de `GetListEvents`

### GetUserInfo

```csharp
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Name == name && u.UserCode == code);
```

### CreateCalendar

```csharp
var user = await _context.Users.FindAsync(creatorId);
```

### GetListCalendar

```csharp
var user = await _context.Users.FindAsync(Guid.Parse(accountId!));
```

```csharp
var listCalendars = await _context.Calendar
    .Where(c => c.OwnerId == accountId || c.CalendarParticipants.Any(p => p.UserId == user.Id))
    .Include(c => c.CalendarParticipants)
        .ThenInclude(p => p.User)
    .Select(c => new CalendarResponse { ... })
    .ToListAsync();
```

### LeaveCalendar

```csharp
var participant = await _context.CalendarParticipant
    .FirstOrDefaultAsync(cp => cp.CalendarId == parsedCalendarId && cp.UserId == userId);
```

### DeleteCalendar

```csharp
var calendar = await _context.Calendar
    .FirstOrDefaultAsync(c => c.Id == parsedCalendarId);
```

### LeaveEvent

```csharp
var participant = await _context.EventParticipants
    .FirstOrDefaultAsync(ep => ep.EventId == parsedEventId && ep.UserId == userId);
```

### DeleteEvent

```csharp
var eventEntity = await _context.Events
    .FirstOrDefaultAsync(e => e.Id == parsedEventId);
```

---

## Padroes Comuns de Query

### Metodos de Query Utilizados

| Metodo | DbSet | Uso |
|---|---|---|
| `FindAsync(Guid)` | `Users`, `Calendar` | Busca por PK |
| `FirstOrDefaultAsync(predicate)` | `Users`, `RefreshTokens`, `Events`, `Calendar`, `CalendarParticipant`, `EventParticipants` | Busca com filtro |
| `AnyAsync(predicate)` | `Users`, `CalendarParticipant` | Verificacao de existencia |
| `Where(predicate)` | `Events`, `Calendar` | Filtro para queries compostas |
| `Include(nav).ThenInclude(nav)` | `Events`, `Calendar` | Eager loading |
| `Select(projection)` | `Events`, `Calendar` | Projecao para DTO |
| `ToListAsync()` | `Events`, `Calendar` | Materializacao |
| `Add(entity)` | `Users`, `Events`, `Calendar`, `RefreshTokens` | Insercao |
| `AddRange(entities)` | `EventParticipants`, `CalendarParticipant` | Insercao em lote |
| `SaveChangesAsync()` | Todos | Persistencia |

### Paginacao

**Nao implementada**. Os endpoints `getListEvents` e `getListCalendar` retornam todos os registros sem limite/paginacao.

### Ordenacao

**Nao implementada**. Nao ha `.OrderBy()` ou `.OrderByDescending()` nas queries.

### AsNoTracking

**Nao utilizado**. Nao ha `.AsNoTracking()` em nenhuma query, embora as queries de listagem com `.Select()` (projecao) ja evitem tracking.

### Filtros

Os filtros sao implementados diretamente nos metodos do controller, sem uma camada de especificacao ou repository pattern.
