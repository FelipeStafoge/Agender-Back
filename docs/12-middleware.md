# 12 - Middlewares

## Middleware Pipeline

Ordem de execucao definida em `Program.cs`:

```csharp
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("VuePolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

## Middlewares Padrao (Built-in)

### 1. Swagger / SwaggerUI

```csharp
app.UseSwagger();
app.UseSwaggerUI();
```

- **Ordem**: 1 e 2 (primeiros apos o build)
- **Responsabilidade**: Expor documentacao OpenAPI/Swagger
- **Disponivel em**: `/swagger` (UI), `/swagger/v1/swagger.json` (JSON)
- **Configuracao**: Swashbuckle com `AddSwaggerGen()` + `AddEndpointsApiExplorer()`

### 2. HTTPS Redirection

```csharp
app.UseHttpsRedirection();
```

- **Ordem**: 3
- **Responsabilidade**: Redireciona requisicoes HTTP para HTTPS
- **Portas**: HTTP 5024, HTTPS 7214

### 3. CORS

```csharp
app.UseCors("VuePolicy");
```

- **Ordem**: 4
- **Politica**: `"VuePolicy"` -- AllowAnyOrigin, AllowAnyHeader, AllowAnyMethod
- **Responsabilidade**: Permitir requisicoes cross-origin do frontend Vue.js

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("VuePolicy", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
```

### 4. Authentication

```csharp
app.UseAuthentication();
```

- **Ordem**: 5 (deve vir antes de Authorization)
- **Responsabilidade**: Autenticar o usuario via JWT Bearer
- **Valida**: Issuer, Audience, Lifetime, SigningKey
- **Popula**: `HttpContext.User` com claims do token

### 5. Authorization

```csharp
app.UseAuthorization();
```

- **Ordem**: 6
- **Responsabilidade**: Verificar atributos `[Authorize]` nos controllers/actions
- **Resultado**: Se nao autenticado e endpoint requer `[Authorize]`, retorna 401

### 6. MapControllers (Endpoint Routing)

```csharp
app.MapControllers();
```

- **Ordem**: 7 (ultimo)
- **Responsabilidade**: Mapear controllers e suas actions para rotas HTTP

---

## Middlewares Customizados

**Nao encontrado**. O projeto nao possui middlewares customizados. Nao ha:
- Middleware de tratamento global de excecoes
- Middleware de logging customizado
- Middleware de request/response
- Middleware de Rate Limiting

---

## Tratamento de Erros

O projeto **nao possui**:
- Global Exception Handler (`UseExceptionHandler`)
- Middleware de tratamento de erros customizado
- `ProblemDetails` configurado via `AddProblemDetails()`

Os erros sao tratados diretamente nos metodos do controller, retornando `IActionResult` com:
- `return Unauthorized(...)`
- `return BadRequest(...)`
- `return NotFound(...)`
- `return Forbid()`
- `return Ok(...)`

Excecoes nao tratadas serao capturadas pelo comportamento padrao do ASP.NET Core (500 Internal Server Error).

---

## Logging

Configurado em `appsettings.json`:

```json
"Logging": {
    "LogLevel": {
        "Default": "Information",
        "Microsoft.AspNetCore": "Warning"
    }
}
```

Nao ha logging explicito via `ILogger<T>` nos controllers ou services. Nao ha chamadas a `_logger.LogInformation()`, `_logger.LogError()`, etc.

---

## Resumo da Pipeline

```
HTTP Request
    |
    v
[Swagger] -- documentacao (apenas development)
    |
    v
[HTTPS Redirection] -- redireciona HTTP -> HTTPS
    |
    v
[CORS "VuePolicy"] -- AllowAnyOrigin, AllowAnyHeader, AllowAnyMethod
    |
    v
[Authentication JWT] -- valida token, popula User
    |
    v
[Authorization] -- verifica [Authorize]
    |
    v
[MapControllers -> AuthController] -- processa requisicao
    |
    v
HTTP Response
```
