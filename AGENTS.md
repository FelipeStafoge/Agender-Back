# AGENTS.md

## Objetivo

Fornecer regras objetivas para agentes de IA implementarem novas funcionalidades no projeto AgenderBackend. Toda regra aqui e baseada exclusivamente nos padroes ja existentes no codigo.

---

## Regras Gerais

- **NUNCA** acesse `AppDbContext` fora do Controller (nao crie Repository ou Service com DbContext).
- **NUNCA** retorne entidades diretamente nos endpoints; use DTOs de response ou objetos anonimos.
- **SEMPRE** use soft delete (`DeletedAt = DateTime.UtcNow`) para remocoes. NUNCA use `_context.Remove()`.
- **SEMPRE** use `async/await` em todas as operacoes de banco.
- **NAO** adicione bibliotecas novas (AutoMapper, FluentValidation, Serilog, etc.) sem justificativa.

---

## Arquitetura

O fluxo de uma nova funcionalidade deve seguir rigorosamente o padrao existente:

```
HTTP Request
    |
    v
[AuthController] -- toda logica de negocio fica aqui
    |
    v
[AppDbContext] -- acesso direto ao banco (sem Repository)
    |
    v
[PostgreSQL]
```

**Nao existem** camadas Repository nem Services de negocio separadas. Toda logica de negocio, validacao e queries ficam no Controller. A unica excecao e `JwtService` para geracao de tokens.

---

## Controllers

- Todos os endpoints ficam em `app/Controllers/Auth/AuthController.cs` com rota `[Route("api/auth")]`.
- Use `[ApiController]` na classe.
- Todo metodo retorna `async Task<IActionResult>`.
- Injete `JwtService` e `AppDbContext` via construtor:

```csharp
private readonly JwtService _jwtService;
private readonly AppDbContext _context;

public AuthController(JwtService jwtService, AppDbContext context)
{
    _jwtService = jwtService;
    _context = context;
}
```

- Extraia o usuario autenticado do JWT:

```csharp
var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (!Guid.TryParse(accountId, out var userId))
    return Unauthorized();
```

- Endpoints publicos nao tem `[Authorize]`. Endpoints protegidos usam `[Authorize]`.
- Para verificar ownership: `if (entity.AccountId != userId) return Forbid();`

---

## Services

- **Nao crie Services de negocio** (UserService, EventService, CalendarService).
- O unico Service existente e `JwtService` (`app/Services/JwtService.cs`), exclusivo para geracao de JWT.
- Caso uma logica de token precise ser expandida, mantenha em `JwtService`.
- Registre no `Program.cs` com `builder.Services.AddScoped<JwtService>();`

---

## Repositories

- **Nao existem** e **nao devem ser criados**.
- Toda query e feita diretamente no Controller usando `_context.DbSet.Where(...)`, `_context.DbSet.FirstOrDefaultAsync(...)`, `_context.DbSet.FindAsync(...)`.

---

## DTOs

- Request DTOs: criar em `app/Modals/` com sufixo `Request` (ex: `CreateEventRequest`).
- Response DTOs: criar no mesmo arquivo do request relacionado, com sufixo `Response` (ex: `EventResponse`).
- DTOs compartilhados (ex: `ParticipantResponse`) ficam no mesmo arquivo do response principal.
- Para respostas simples de criacao/atualizacao, use **objeto anonimo**:

```csharp
return Ok(new { message = "Criado com sucesso", id = entity.Id });
```

- Propriedades de string inicializam com `string.Empty`, listas com `[]` ou `new()`.

---

## Entities

- Toda entidade **deve** ter `DateTime CreatedAt` e `DateTime? DeletedAt` (exceto `RefreshToken`).
- IDs sao sempre `Guid` gerados com `Guid.NewGuid()`.
- Relacionamentos configurados via **Fluent API** no `OnModelCreating` do `AppDbContext` (nao use DataAnnotations).
- Entidades de juncao:
  - `EventParticipant`: chave composta `(EventId, UserId)`, sem Id proprio.
  - `CalendarParticipant`: Id proprio + indice unico `(CalendarId, UserId)`.
- Navegacoes obrigatorias usam `= null!`. Listas de navegacao usam `= []`.
- Datas de negocio (Event.Date) sao `string` no formato `DD/MM/YYYY`. Nao use `DateTime` ou `DateOnly`.

---

## Banco de Dados

- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL`. Connection string via `IConfiguration` e variaveis de ambiente (`ConnectionStrings__DefaultConnection`).

### DbContext

- Toda nova entidade ganha um `DbSet<T>` em `AppDbContext` e um **Global Query Filter** para soft delete:

```csharp
modelBuilder.Entity<NovaEntidade>().HasQueryFilter(e => e.DeletedAt == null);
```

### Relacionamentos

- Configure sempre no `OnModelCreating` com Fluent API (HasOne/WithMany/HasForeignKey).
- Especifique `OnDelete` explicitamente (Cascade ou SetNull).

### Migrations

- Apos alterar entidades: `dotnet ef migrations add NomeDescritivo`
- Aplicar: `dotnet ef database update`
- Nunca edite migrations manualmente.

---

## Validacoes

- **Nao ha FluentValidation nem DataAnnotations** para validacao de entrada.
- Validacoes sao manuais nos metodos do Controller:

```csharp
if (!Guid.TryParse(id, out var guid))
    return BadRequest(new { message = "id invalido" });

var entity = await _context.Tabela.FindAsync(guid);
if (entity == null)
    return NotFound(new { message = "Entidade nao encontrada" });
```

---

## Autenticacao

- Use `[Authorize]` no endpoint para exigir autenticacao JWT.
- Endpoints anonimos (login, register, refresh) nao usam `[Authorize]`.
- Para gerar JWT: `_jwtService.GenerateToken(email, userId)`.
- Para gerar refresh token: metodo privado `GenerateRefreshToken()` no controller (64 bytes aleatorios, Base64, 7 dias de expiracao).
- Claims disponiveis: `ClaimTypes.NameIdentifier` (userId), `ClaimTypes.Email` (email).

---

## Dependency Injection

- Registre **apenas** no `Program.cs`.
- Services: `builder.Services.AddScoped<Servico>();`
- DbContext: `builder.Services.AddDbContext<AppDbContext>(...)`
- **Nao use interfaces** para injecao. Injete classes concretas diretamente.

---

## Logging

- **Nao implementado** no codigo. Nao injete `ILogger<T>`, nao adicione chamadas de log. O framework cuida do log padrao.

---

## Tratamento de Erros

- **Nao ha middleware global de excecoes** nem try-catch nos controllers.
- Trate erros com retornos condicionais:
  - `return Unauthorized(...)` -- token invalido, credenciais erradas
  - `return BadRequest(new { message = "..." })` -- entrada invalida
  - `return NotFound(new { message = "..." })` -- entidade nao encontrada
  - `return Forbid()` -- sem permissao (nao e owner)
- Codigos de acao/erro registre em `app/utils/ActionsRequest.cs`:

```csharp
public static class ActionsRequest
{
    public static class Error
    {
        public static class Modulo { public const string Codigo = "codigo"; }
    }
    public static class Success
    {
        public static class Modulo { public const string Codigo = "codigo"; }
    }
}
```

---

## Convencoes

- Classes: PascalCase (`AuthController`, `JwtService`, `LoginRequest`)
- Metodos publicos: PascalCase (`CreateEvent`, `GetListCalendar`)
- Campos privados: `_camelCase` (`_context`, `_jwtService`)
- Propriedades: PascalCase (`Id`, `Name`, `CreatedAt`)
- Namespaces: Controller em `AgenderBackend.Api.Controllers`, Entidades em `AgenderBackend.Api.Models`, Data em `AgenderBackend.Data`, Services em `AgenderBackend.Services`
- **Nao ha interfaces** para services ou repositories
- **Nao usa CancellationToken** em nenhum endpoint
- **Nao usa AutoMapper** -- mapeamento manual via `.Select()` ou inicializacao de objeto
- **Nao usa AsNoTracking** -- queries de listagem usam `.Select()` (projecao), queries pontuais usam tracking padrao

---

## Antes de Escrever Codigo

Sempre verifique se ja existe no projeto:

- [ ] Endpoint similar no `AuthController`?
- [ ] DTO similar em `app/Modals/`?
- [ ] Entidade similar em `app/entities/`?
- [ ] Codigo de acao em `ActionsRequest`?
- [ ] Regra de negocio que pode ser reaproveitada?

**Se existir, reutilize. Nao duplique.**

---

## Ao Finalizar

- [ ] Nenhum codigo duplicado
- [ ] Nenhuma regra de negocio duplicada
- [ ] Nao quebrou o fluxo Controller -> DbContext (nao criou Repository/Service desnecessario)
- [ ] Todos os metodos sao `async Task<IActionResult>`
- [ ] DTOs de entrada usados nos parametros, nao entidades
- [ ] Respostas usam DTOs de response ou objetos anonimos
- [ ] Soft delete implementado (campo `DeletedAt` + Query Filter)
- [ ] Nova entidade registrada no `AppDbContext` (DbSet + Query Filter)
- [ ] Migration gerada se houve alteracao de schema
- [ ] Novo serviço registrado no `Program.cs` (se aplicavel)
