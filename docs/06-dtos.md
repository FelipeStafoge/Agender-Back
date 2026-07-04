# 06 - DTOs

DTOs estao na pasta `app/Modals/` (typo de "Models"). Sao divididos em Request DTOs (entrada) e Response DTOs (saida).

---

## Request DTOs

### LoginRequest

**Arquivo**: `app/Modals/LoginRequest.cs:4`
**Usado em**: `POST /api/auth/login`

```csharp
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Email` | `string` | Email do usuario |
| `Password` | `string` | Senha em texto puro |

---

### RegisterRequest

**Arquivo**: `app/Modals/RegisterRequest.cs:3`
**Usado em**: `POST /api/auth/register`

```csharp
public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Name` | `string` | Nome do usuario |
| `Email` | `string` | Email (deve ser unico) |
| `Password` | `string` | Senha em texto puro |

**Diferenca da entidade User**: `RegisterRequest` nao possui `Id`, `UserCode`, `CreatedAt`, etc. -- esses sao gerados no registro.

---

### RefreshTokenRequest

**Arquivo**: `app/Modals/RefreshTokenRequest.cs:1`
**Usado em**: `POST /api/auth/refresh`

```csharp
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `RefreshToken` | `string` | Token de refresh (Base64) |

---

### CreateEventRequest

**Arquivo**: `app/Modals/CreateEventRequest.cs:1`
**Usado em**: `POST /api/auth/createEvent`

```csharp
public class CreateEventRequest
{
    public string Date { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = string.Empty;
    public string? Calendar_id { get; set; }
    public List<string> Users_ids { get; set; } = new();
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Date` | `string` | Data `DD/MM/YYYY` |
| `Name` | `string` | Nome do evento |
| `Description` | `string?` | Descricao opcional |
| `Color` | `string` | Cor em hex (pode ser sobrescrita pelo calendario) |
| `Calendar_id` | `string?` | GUID do calendario (opcional) |
| `Users_ids` | `List<string>` | Lista de GUIDs de usuarios convidados |

**Diferenca da entidade Event**: Nao possui `Id`, `AccountId` (extraido do JWT), `Calendar` (navigation), `Participants`, timestamps.

---

### CreateCalendarRequest

**Arquivo**: `app/Modals/CreateCalendarRequest.cs:1`
**Usado em**: `POST /api/auth/createCalendar`

```csharp
public class CreateCalendarRequest
{
    public string Name { get; set; } = string.Empty;
    public string DefaultColor { get; set; } = string.Empty;
    public List<string> Users_ids { get; set; } = new();
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Name` | `string` | Nome do calendario |
| `DefaultColor` | `string` | Cor padrao em hex |
| `Users_ids` | `List<string>` | Lista de GUIDs de usuarios convidados |

**Diferenca da entidade Calendar**: Nao possui `Id`, `AccountId`, `OwnerId`, `IsPersonal`, `Date`, `CalendarParticipants`, timestamps.

---

### AddCalendarParticipantRequest

**Arquivo**: `app/Modals/CalendarParticipantRequest.cs:1`
**Usado em**: Nao utilizado nos endpoints atuais

```csharp
public class AddCalendarParticipantRequest
{
    public string UserId { get; set; } = string.Empty;
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `UserId` | `string` | GUID do usuario a ser adicionado |

---

## Response DTOs

### EventResponse

**Arquivo**: `app/Modals/CreateEventRequest.cs:11`
**Usado em**: `GET /api/auth/getListEvents`, `GET /api/auth/getCalendarEvents`

```csharp
public class EventResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = string.Empty;
    public Guid? CalendarId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public List<ParticipantResponse> Participants { get; set; } = [];
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Id` | `Guid` | ID do evento |
| `Name` | `string` | Nome do evento |
| `Date` | `string` | Data `DD/MM/YYYY` |
| `Description` | `string?` | Descricao |
| `Color` | `string` | Cor em hex |
| `CalendarId` | `Guid?` | ID do calendario (se houver) |
| `CreatedAt` | `DateTime` | Data de criacao |
| `UpdatedAt` | `DateTime?` | Data de atualizacao |
| `DeletedAt` | `DateTime?` | Data de exclusao logica |
| `Participants` | `List<ParticipantResponse>` | Lista de participantes |

**Diferenca da entidade Event**: Nao expoe `AccountId`, `Calendar` (navigation property). Inclui `Participants` como lista de `ParticipantResponse` em vez de `EventParticipant`.

---

### CalendarResponse

**Arquivo**: `app/Modals/CreateCalendarRequest.cs:8`
**Usado em**: `GET /api/auth/getListCalendar`

```csharp
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
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `Id` | `Guid` | ID do calendario |
| `Name` | `string` | Nome do calendario |
| `Date` | `List<string>` | Lista de datas |
| `Color` | `string` | Cor padrao (`DefaultColor` da entidade) |
| `CreatedAt` | `DateTime` | Data de criacao |
| `UpdatedAt` | `DateTime?` | Data de atualizacao |
| `DeletedAt` | `DateTime?` | Data de exclusao logica |
| `Participants` | `List<ParticipantResponse>` | Lista de participantes |

**Diferenca da entidade Calendar**: Nao expoe `AccountId`, `OwnerId`, `IsPersonal`, `CalendarParticipants` (navigation). `Color` vem de `DefaultColor`.

---

### ParticipantResponse

**Arquivo**: `app/Modals/CreateEventRequest.cs:26`
**Usado em**: `EventResponse` e `CalendarResponse`

```csharp
public class ParticipantResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

| Propriedade | Tipo | Descricao |
|---|---|---|
| `UserId` | `Guid` | ID do usuario |
| `Name` | `string` | Nome do usuario (da entidade User navegada) |
| `Role` | `string` | "Owner" ou "Member" |
| `CreatedAt` | `DateTime` | Data de entrada como participante |

---

### UserCalendar

**Arquivo**: `app/Modals/UserCalender.cs:3`
**Usado em**: Nao utilizado nos endpoints atuais

```csharp
public class UserCalendar
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
```

**Observacao**: Esta classe parece ser um DTO legado ou planejado para uso futuro, mas nao e referenciada nos endpoints atuais. Possui `Id` como `int` (diferente dos `Guid` usados nas entidades ativas) e referencia `User` diretamente.

---

## Resumo: DTOs vs Entidades

| DTO | Entidade Relacionada | Direcao | Uso |
|---|---|---|---|
| `LoginRequest` | `User` | Entrada | Login |
| `RegisterRequest` | `User` | Entrada | Registro |
| `RefreshTokenRequest` | `RefreshToken` | Entrada | Refresh token |
| `CreateEventRequest` | `Event` | Entrada | Criar evento |
| `CreateCalendarRequest` | `Calendar` | Entrada | Criar calendario |
| `AddCalendarParticipantRequest` | `CalendarParticipant` | Entrada | Nao utilizado |
| `EventResponse` | `Event` | Saida | Listar eventos |
| `CalendarResponse` | `Calendar` | Saida | Listar calendarios |
| `ParticipantResponse` | `EventParticipant` / `CalendarParticipant` | Saida | Participantes nas listagens |
| `UserCalendar` | `Calendar` / `User` | - | Nao utilizado |
