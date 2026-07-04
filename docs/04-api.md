# 04 - API Endpoints

Todas as rotas estao sob o prefixo `/api/auth`.

## Convencoes

- **Controller**: `AuthController` (`app/Controllers/Auth/AuthController.cs`)
- **Formato de data**: `DD/MM/YYYY` (string) em todas as entradas e saidas
- **IDs**: GUIDs como strings
- **Respostas**: Objetos anonimos (nao ha envelope padrao como `ApiResponse<T>`)
- **Soft delete**: Nenhum endpoint remove registros fisicamente; todos usam `DeletedAt`

---

## 1. POST /api/auth/login

- **Autenticacao**: Anonima
- **DTO Request**: `LoginRequest`
- **DTO Response**: Objeto anonimo

### Request Body

```json
{
  "email": "user@example.com",
  "password": "senha123"
}
```

### LoginRequest

```csharp
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### Respostas

**200 OK**:

```json
{
  "token": "eyJhbGci...",
  "refreshToken": "base64string...",
  "data": {
    "userName": "Felipe",
    "userCode": "1234",
    "account_id": "guid...",
    "email": "user@example.com",
    "action": "user_logged_in"
  }
}
```

**401 Unauthorized** (email/senha incorretos):

```json
{
  "action": "wrong_password_or_email"
}
```

### Logica

1. Busca usuario por `Email` e `Password` (comparacao em texto puro, sem hash)
2. Se nao encontrar, retorna 401
3. Gera JWT via `JwtService.GenerateToken()`
4. Cria `RefreshToken` (Base64 de 64 bytes aleatorios, expira em 7 dias)
5. Salva refresh token no banco
6. Retorna 200 com token, refresh token e dados do usuario

---

## 2. POST /api/auth/register

- **Autenticacao**: Anonima
- **DTO Request**: `RegisterRequest`
- **DTO Response**: Objeto anonimo

### Request Body

```json
{
  "name": "Felipe",
  "email": "felipe@example.com",
  "password": "senha123"
}
```

### RegisterRequest

```csharp
public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### Respostas

**200 OK**:

```json
{
  "action": "user_registered",
  "token": "eyJhbGci...",
  "refreshToken": {
    "token": "base64string...",
    "userId": "guid...",
    "expiresAt": "2026-07-11T...",
    "revoked": false
  }
}
```

**400 Bad Request** (email ja existe):

```json
{
  "action": "user_already_exists"
}
```

### Logica

1. Verifica se email ja existe (`_context.Users.AnyAsync`)
2. Se existir, retorna 400
3. Gera `UserCode` aleatorio de 4 digitos, garantindo unicidade com nome
4. Cria `User` com `Id = Guid.NewGuid()`, `CreatedAt = DateTime.UtcNow`
5. Salva usuario
6. Cria calendario pessoal automatico ("Meus Eventos", cor `#7c3aed`, `IsPersonal = true`)
7. Cria participacao no calendario pessoal com `Role = "Owner"`
8. Gera JWT e Refresh Token
9. Salva refresh token
10. Retorna 200

### Regras de negocio no registro

- Email deve ser unico
- Combinacao `Name + UserCode` deve ser unica (indice no banco)
- Ao registrar, um calendario pessoal e criado automaticamente
- Senha armazenada em texto puro (sem hash)

---

## 3. POST /api/auth/refresh

- **Autenticacao**: Anonima
- **DTO Request**: `RefreshTokenRequest`
- **DTO Response**: Objeto anonimo

### Request Body

```json
{
  "refreshToken": "base64string..."
}
```

### RefreshTokenRequest

```csharp
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

### Respostas

**200 OK**:

```json
{
  "token": "eyJhbGci..."
}
```

**401 Unauthorized** (token invalido, revogado, expirado ou usuario nao encontrado)

### Logica

1. Busca refresh token nao revogado
2. Se nao encontrado, retorna 401
3. Se expirado, retorna 401
4. Busca usuario associado ao token
5. Se nao encontrado, retorna 401
6. Gera novo JWT
7. Retorna 200 com novo token

**Observacao**: O refresh token antigo **nao e revogado** apos uso. Apenas novos tokens JWT sao gerados.

---

## 4. POST /api/auth/createEvent

- **Autenticacao**: `[Authorize]` (requer JWT)
- **DTO Request**: `CreateEventRequest`
- **DTO Response**: Objeto anonimo

### Request Body

```json
{
  "date": "04/07/2026",
  "name": "Reuniao de Time",
  "description": "Reuniao semanal de alinhamento",
  "color": "#ff5733",
  "calendar_id": "guid...",
  "users_ids": ["guid1...", "guid2..."]
}
```

### CreateEventRequest

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

### Respostas

**200 OK**:

```json
{
  "message": "Evento criado com participantes",
  "eventId": "guid..."
}
```

**401 Unauthorized** (token invalido, usuario nao encontrado)

### Logica

1. Extrai `accountId` do claim `NameIdentifier`
2. Parseia para `Guid`
3. Busca usuario -- se nao encontrar, retorna 401
4. Se `calendar_id` for fornecido e valido:
   - Busca o calendario
   - Se encontrado, **substitui a cor do evento** pela `DefaultColor` do calendario
5. Cria `Event` com `Id = Guid.NewGuid()`
6. Adiciona criador como participante `EventParticipant` com `Role = "Owner"`
7. Adiciona usuarios adicionais como `EventParticipant` com `Role = "Member"`
8. Salva no banco
9. Retorna 200

### Regras de negocio

- Se o evento pertence a um calendario, a cor do evento e sobrescrita pela cor padrao do calendario
- O criador sempre e adicionado como Owner
- IDs de usuario invalidos sao ignorados silenciosamente (nao geram erro)

---

## 5. GET /api/auth/getListEvents

- **Autenticacao**: `[Authorize]`
- **DTO Response**: Objeto anonimo com `EventResponse` + `ParticipantResponse`

### Query Parameters

| Parametro | Tipo | Obrigatorio | Descricao |
|---|---|---|---|
| `startDate` | `string?` | Nao | Data inicio no formato `DD/MM/YYYY` |
| `endDate` | `string?` | Nao | Data fim no formato `DD/MM/YYYY` |

### Exemplo

```
GET /api/auth/getListEvents?startDate=01/06/2026&endDate=31/07/2026
```

### Respostas

**200 OK**:

```json
{
  "data": [
    {
      "id": "guid...",
      "name": "Reuniao de Time",
      "date": "04/07/2026",
      "description": "Reuniao semanal de alinhamento",
      "color": "#7c3aed",
      "calendarId": "guid...",
      "createdAt": "2026-07-04T...",
      "updatedAt": null,
      "deletedAt": null,
      "participants": [
        {
          "userId": "guid...",
          "name": "Felipe",
          "role": "Owner",
          "createdAt": "2026-07-04T..."
        }
      ]
    }
  ]
}
```

**401 Unauthorized** (usuario nao encontrado)

### EventResponse

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

### ParticipantResponse

```csharp
public class ParticipantResponse
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### Logica

1. Busca usuario autenticado
2. Filtra eventos onde:
   - Evento sem calendario (`CalendarId == null`) E usuario e participante direto, **OU**
   - Evento com calendario E usuario e participante do calendario
3. Aplica filtro de data se fornecido (compara strings no formato `YYYYMMDD`)
4. Inclui participantes com dados do usuario (`ThenInclude`)
5. Retorna lista mapeada para `EventResponse`

### Filtro de data

A comparacao de datas e feita convertendo a string `DD/MM/YYYY` para `YYYYMMDD` e usando `string.Compare`:

```csharp
// Exemplo: "04/07/2026" vira "20260704"
var parts = startDate.Split('/');
var startComparable = parts[2] + parts[1] + parts[0];
query = query.Where(e =>
    string.Compare(e.Date.Substring(6, 4) + e.Date.Substring(3, 2) + e.Date.Substring(0, 2), startComparable) >= 0);
```

---

## 6. GET /api/auth/getCalendarEvents

- **Autenticacao**: `[Authorize]`

### Query Parameters

| Parametro | Tipo | Obrigatorio | Descricao |
|---|---|---|---|
| `calendarId` | `string` | **Sim** | GUID do calendario |
| `startDate` | `string?` | Nao | Data inicio `DD/MM/YYYY` |
| `endDate` | `string?` | Nao | Data fim `DD/MM/YYYY` |

### Exemplo

```
GET /api/auth/getCalendarEvents?calendarId=guid...&startDate=01/06/2026&endDate=31/07/2026
```

### Respostas

**200 OK**: Mesmo formato de `getListEvents` (`EventResponse` + `ParticipantResponse`)

**400 Bad Request**: `{ "message": "calendarId invalido" }`

**403 Forbidden**: Usuario sem acesso ao calendario

**404 Not Found**: `{ "message": "Calendario nao encontrado" }`

**401 Unauthorized**

### Logica

1. Valida `accountId` e `calendarId` como GUIDs
2. Busca o calendario
3. Verifica acesso: usuario e owner **OU** participante do calendario
4. Se sem acesso, retorna 403 (`Forbid()`)
5. Filtra eventos pelo `CalendarId`
6. Aplica filtro de data
7. Inclui participantes
8. Retorna lista

---

## 7. GET /api/auth/getUserInfo

- **Autenticacao**: `[Authorize]`

### Query Parameters

| Parametro | Tipo | Obrigatorio | Descricao |
|---|---|---|---|
| `nameWithCode` | `string` | **Sim** | Formato `Nome#1234` |

### Exemplo

```
GET /api/auth/getUserInfo?nameWithCode=Felipe#1234
```

### Respostas

**200 OK**:

```json
{
  "name": "Felipe",
  "email": "felipe@example.com",
  "id": "guid...",
  "userCode": "1234",
  "createdAt": "2026-07-01T...",
  "updatedAt": null,
  "deletedAt": null
}
```

**400 Bad Request**: `"Formato invalido. Use Nome#Codigo"` (quando nao contem `#`)

**404 Not Found**: Usuario nao encontrado

### Logica

1. Faz split de `nameWithCode` por `#`
2. Deve ter exatamente 2 partes
3. Busca usuario por `Name` e `UserCode`
4. Se nao encontrar, retorna 404
5. Retorna dados do usuario

---

## 8. POST /api/auth/createCalendar

- **Autenticacao**: `[Authorize]`
- **DTO Request**: `CreateCalendarRequest`
- **DTO Response**: Objeto anonimo

### Request Body

```json
{
  "name": "Equipe Dev",
  "defaultColor": "#3b82f6",
  "users_ids": ["guid1...", "guid2..."]
}
```

### CreateCalendarRequest

```csharp
public class CreateCalendarRequest
{
    public string Name { get; set; } = string.Empty;
    public string DefaultColor { get; set; } = string.Empty;
    public List<string> Users_ids { get; set; } = new();
}
```

### Respostas

**200 OK**:

```json
{
  "message": "Novo calendario criado com sucesso",
  "eventId": "guid..."
}
```

**401 Unauthorized**

### Logica

1. Extrai usuario autenticado
2. Cria `Calendar` com `IsPersonal = false`, `OwnerId = creatorId.ToString()`
3. Adiciona criador como `CalendarParticipant` com `Role = "Owner"`
4. Adiciona usuarios convidados como `CalendarParticipant` com `Role = "Member"`
5. IDs invalidos sao ignorados (nao geram erro)
6. Salva
7. Retorna 200

---

## 9. GET /api/auth/getListCalendar

- **Autenticacao**: `[Authorize]`

### Exemplo

```
GET /api/auth/getListCalendar
```

### Respostas

**200 OK**:

```json
{
  "data": [
    {
      "id": "guid...",
      "name": "Equipe Dev",
      "date": [],
      "color": "#3b82f6",
      "createdAt": "2026-07-04T...",
      "updatedAt": null,
      "deletedAt": null,
      "participants": [
        {
          "userId": "guid...",
          "name": "Felipe",
          "role": "Owner",
          "createdAt": "2026-07-04T..."
        }
      ]
    }
  ]
}
```

**401 Unauthorized**

### CalendarResponse

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

### Logica

1. Busca usuario autenticado
2. Filtra calendarios onde usuario e owner **OU** participante
3. Inclui participantes com dados do usuario
4. Retorna lista mapeada para `CalendarResponse`

---

## 10. POST /api/auth/leaveCalendar/{calendarId}

- **Autenticacao**: `[Authorize]`

### Path Parameters

| Parametro | Tipo | Descricao |
|---|---|---|
| `calendarId` | `string` (rota) | GUID do calendario |

### Exemplo

```
POST /api/auth/leaveCalendar/guid-aqui...
```

### Respostas

**200 OK**:

```json
{
  "message": "Voce saiu do calendario"
}
```

**400 Bad Request**: `{ "message": "calendarId invalido" }`

**401 Unauthorized**

**404 Not Found**: `{ "message": "Participacao nao encontrada" }`

### Logica

1. Valida `accountId` e `calendarId`
2. Busca participacao do usuario no calendario
3. Se nao encontrada, retorna 404
4. Aplica soft delete: `participant.DeletedAt = DateTime.UtcNow`
5. Salva
6. Retorna 200

---

## 11. DELETE /api/auth/deleteCalendar/{calendarId}

- **Autenticacao**: `[Authorize]`

### Path Parameters

| Parametro | Tipo | Descricao |
|---|---|---|
| `calendarId` | `string` (rota) | GUID do calendario |

### Exemplo

```
DELETE /api/auth/deleteCalendar/guid-aqui...
```

### Respostas

**200 OK**:

```json
{
  "message": "Calendario deletado"
}
```

**400 Bad Request**: `{ "message": "calendarId invalido" }`

**401 Unauthorized**

**403 Forbidden**: Usuario nao e o dono do calendario

**404 Not Found**: `{ "message": "Calendario nao encontrado" }`

### Logica

1. Valida `accountId` e `calendarId`
2. Busca calendario
3. Verifica se o usuario e o dono (`calendar.AccountId != userId` -> Forbid)
4. Aplica soft delete: `calendar.DeletedAt = DateTime.UtcNow`
5. Salva
6. Retorna 200

**Observacao**: Apenas o dono (`AccountId == userId`) pode deletar. Participantes comuns nao podem.

---

## 12. POST /api/auth/leaveEvent/{eventId}

- **Autenticacao**: `[Authorize]`

### Path Parameters

| Parametro | Tipo | Descricao |
|---|---|---|
| `eventId` | `string` (rota) | GUID do evento |

### Exemplo

```
POST /api/auth/leaveEvent/guid-aqui...
```

### Respostas

**200 OK**:

```json
{
  "message": "Voce saiu do evento"
}
```

**400 Bad Request**: `{ "message": "eventId invalido" }`

**401 Unauthorized**

**404 Not Found**: `{ "message": "Participacao nao encontrada" }`

### Logica

1. Valida `accountId` e `eventId`
2. Busca participacao do usuario no evento
3. Se nao encontrada, retorna 404
4. Aplica soft delete: `participant.DeletedAt = DateTime.UtcNow`
5. Salva
6. Retorna 200

---

## 13. DELETE /api/auth/deleteEvent/{eventId}

- **Autenticacao**: `[Authorize]`

### Path Parameters

| Parametro | Tipo | Descricao |
|---|---|---|
| `eventId` | `string` (rota) | GUID do evento |

### Exemplo

```
DELETE /api/auth/deleteEvent/guid-aqui...
```

### Respostas

**200 OK**:

```json
{
  "message": "Evento deletado"
}
```

**400 Bad Request**: `{ "message": "eventId invalido" }`

**401 Unauthorized**

**403 Forbidden**: Usuario nao e o dono do evento

**404 Not Found**: `{ "message": "Evento nao encontrado" }`

### Logica

1. Valida `accountId` e `eventId`
2. Busca evento
3. Verifica se o usuario e o dono (`eventEntity.AccountId != userId` -> Forbid)
4. Aplica soft delete: `eventEntity.DeletedAt = DateTime.UtcNow`
5. Salva
6. Retorna 200

**Observacao**: Apenas o criador (`AccountId == userId`) pode deletar o evento.

---

## Resumo de Endpoints

| # | Metodo | Rota | Auth | Descricao |
|---|---|---|---|---|
| 1 | POST | `/api/auth/login` | Nao | Login |
| 2 | POST | `/api/auth/register` | Nao | Registro |
| 3 | POST | `/api/auth/refresh` | Nao | Refresh token |
| 4 | POST | `/api/auth/createEvent` | Sim | Criar evento |
| 5 | GET | `/api/auth/getListEvents` | Sim | Listar eventos do usuario |
| 6 | GET | `/api/auth/getCalendarEvents` | Sim | Listar eventos de um calendario |
| 7 | GET | `/api/auth/getUserInfo` | Sim | Buscar info de usuario |
| 8 | POST | `/api/auth/createCalendar` | Sim | Criar calendario |
| 9 | GET | `/api/auth/getListCalendar` | Sim | Listar calendarios do usuario |
| 10 | POST | `/api/auth/leaveCalendar/{id}` | Sim | Sair de calendario |
| 11 | DELETE | `/api/auth/deleteCalendar/{id}` | Sim | Deletar calendario |
| 12 | POST | `/api/auth/leaveEvent/{id}` | Sim | Sair de evento |
| 13 | DELETE | `/api/auth/deleteEvent/{id}` | Sim | Deletar evento |
