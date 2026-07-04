# 16 - Tratamento de Erros

## Abordagem de Tratamento de Erros

O projeto **nao possui** um mecanismo centralizado de tratamento de erros. Os erros sao tratados de forma manual e descentralizada nos metodos do controller.

---

## Exceptions

### Exceptions Customizadas

**Nao encontrado**. Nao ha classes de exception customizadas (como `DomainException`, `BusinessRuleException`, `NotFoundException`, etc.).

### Exceptions do Framework

As unicas exceptions que podem ocorrer sao as do proprio .NET/EF Core, como:
- `NullReferenceException` (uso de `!` null-forgiving operator)
- `FormatException` (parse de GUID ou datas invalidas)
- `DbUpdateException` (violacao de constraints)
- `InvalidOperationException` (sequencias vazias, etc.)

### Try-Catch

**Nao utilizado**. Nao ha blocos `try-catch` nos metodos do controller. Se uma exception ocorrer, o ASP.NET Core retornara 500 Internal Server Error com o comportamento padrao.

---

## Global Exception Handler

**Nao implementado**. Nao ha:
- `app.UseExceptionHandler()` no `Program.cs`
- Middleware de tratamento de excecoes customizado
- Filtro de excecao (`IExceptionFilter`)

---

## ProblemDetails

**Nao implementado**. Nao ha:
- Configuracao `AddProblemDetails()` no `Program.cs`
- Retorno de `ProblemDetails` nos endpoints
- `[ProducesResponseType]` com documentacao de erros

---

## Retornos HTTP

### Codigos Utilizados

| HTTP Status | Metodo do Controller | Quando |
|---|---|---|
| `200 OK` | `return Ok(...)` | Operacao bem sucedida |
| `400 Bad Request` | `return BadRequest(...)` | Dados invalidos (email duplicado, GUID invalido, formato invalido) |
| `401 Unauthorized` | `return Unauthorized(...)` | Token ausente/invalido, credenciais incorretas, usuario nao encontrado |
| `403 Forbidden` | `return Forbid()` | Usuario sem permissao (nao e Owner) |
| `404 Not Found` | `return NotFound(...)` | Entidade nao encontrada, participacao nao encontrada |
| `500 Internal Server Error` | (automatico, nao tratado) | Excecoes nao capturadas |

### Padrao de Resposta de Erro

Os erros retornam objetos anonimos com mensagens:

```csharp
// Login com credenciais erradas
return Unauthorized(new
{
    action = ActionsRequest.Error.Login.WrongPasswordOrEmail
});

// Email ja registrado
return BadRequest(new
{
    action = ActionsRequest.Error.Register.UserAlreadyExists
});

// GUID invalido
return BadRequest(new { message = "calendarId invalido" });

// Nao encontrado
return NotFound(new { message = "Calendario nao encontrado" });

// Sem acesso
return Forbid();
```

**Observacao**: O formato da resposta de erro nao e consistente. Alguns usam `action`, outros usam `message`. `Forbid()` retorna corpo vazio.

---

## Codigos de Acao/Erro (ActionsRequest)

**Arquivo**: `app/utils/ActionsRequest.cs`

```csharp
public static class ActionsRequest
{
    public static class Error
    {
        public static class Register
        {
            public const string UserAlreadyExists = "user_already_exists";
        }

        public static class Login
        {
            public const string WrongPasswordOrEmail = "wrong_password_or_email";
        }
    }

    public static class Success
    {
        public static class Register
        {
            public const string UserRegistered = "user_registered";
        }

        public static class Login
        {
            public const string UserLoggedIn = "user_logged_in";
        }
    }
}
```

### Codigos de Erro

| Constante | Valor | Usado em |
|---|---|---|
| `Error.Register.UserAlreadyExists` | `"user_already_exists"` | `POST /api/auth/register` (400) |
| `Error.Login.WrongPasswordOrEmail` | `"wrong_password_or_email"` | `POST /api/auth/login` (401) |

### Codigos de Sucesso

| Constante | Valor | Usado em |
|---|---|---|
| `Success.Register.UserRegistered` | `"user_registered"` | `POST /api/auth/register` (200) |
| `Success.Login.UserLoggedIn` | `"user_logged_in"` | `POST /api/auth/login` (200) |

---

## Logs

### Configuracao de Logging

```json
// appsettings.json
"Logging": {
    "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
    }
}
```

### Logging no Codigo

**Nao implementado**. Nao ha injecao de `ILogger<T>` em controllers ou services. Nao ha chamadas de log (`LogInformation`, `LogError`, `LogWarning`) no codigo de aplicacao.

---

## Mensagens de Erro (Resumo)

| Endpoint | Condicao | Status | Mensagem |
|---|---|---|---|
| `login` | Email/senha incorretos | 401 | `{ action: "wrong_password_or_email" }` |
| `register` | Email ja existe | 400 | `{ action: "user_already_exists" }` |
| `refresh` | Token nao encontrado/revogado | 401 | *(sem corpo)* |
| `refresh` | Token expirado | 401 | *(sem corpo)* |
| `refresh` | Usuario nao encontrado | 401 | *(sem corpo)* |
| `createEvent` | Token invalido | 401 | *(sem corpo)* |
| `createEvent` | Usuario nao encontrado | 401 | *(sem corpo)* |
| `getCalendarEvents` | calendarId invalido | 400 | `{ message: "calendarId invalido" }` |
| `getCalendarEvents` | Calendario nao encontrado | 404 | `{ message: "Calendario nao encontrado" }` |
| `getCalendarEvents` | Sem acesso | 403 | *(sem corpo)* |
| `getUserInfo` | Formato invalido | 400 | `"Formato invalido. Use Nome#Codigo"` |
| `getUserInfo` | Usuario nao encontrado | 404 | *(sem corpo)* |
| `leaveCalendar` | calendarId invalido | 400 | `{ message: "calendarId invalido" }` |
| `leaveCalendar` | Participacao nao encontrada | 404 | `{ message: "Participacao nao encontrada" }` |
| `deleteCalendar` | calendarId invalido | 400 | `{ message: "calendarId invalido" }` |
| `deleteCalendar` | Calendario nao encontrado | 404 | `{ message: "Calendario nao encontrado" }` |
| `deleteCalendar` | Nao e Owner | 403 | *(sem corpo)* |
| `leaveEvent` | eventId invalido | 400 | `{ message: "eventId invalido" }` |
| `leaveEvent` | Participacao nao encontrada | 404 | `{ message: "Participacao nao encontrada" }` |
| `deleteEvent` | eventId invalido | 400 | `{ message: "eventId invalido" }` |
| `deleteEvent` | Evento nao encontrado | 404 | `{ message: "Evento nao encontrado" }` |
| `deleteEvent` | Nao e Owner | 403 | *(sem corpo)* |
