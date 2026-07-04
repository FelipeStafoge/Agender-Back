# 18 - Contexto para IA (AI Context)

Este arquivo contem **instrucoes obrigatorias** para qualquer IA ou desenvolvedor que for modificar este projeto. Siga estas regras rigorosamente.

---

## Regras de Arquitetura

### 1. Nao existe camada Repository
- **NUNCA** crie uma camada Repository separada.
- O `AppDbContext` e acessado **diretamente** pelo `AuthController`.
- Use diretamente: `_context.Users.FirstOrDefaultAsync(...)`, `_context.Events.Where(...)`, etc.

### 2. Nao existe camada Service (exceto JwtService)
- **NAO** crie Services separados para logica de negocio (UserService, EventService, CalendarService).
- Toda logica de negocio fica nos metodos do `AuthController`.
- A unica excecao e `JwtService` -- mantenha apenas a geracao de JWT la.

### 3. Controller unico
- Todos os endpoints ficam em `AuthController` (`app/Controllers/Auth/AuthController.cs`).
- Rota base: `[Route("api/auth")]`.
- **NAO** crie controllers adicionais a menos que seja estritamente necessario.

### 4. Acesso ao banco
- Use **sempre** `AppDbContext` via injecao no construtor do controller.
- **NUNCA** use `new AppDbContext()` ou faca `IDbContextFactory` sem necessidade.

---

## Regras de Dados e Entidades

### 5. Soft Delete e obrigatorio
- Toda nova entidade deve ter campo `DateTime? DeletedAt`.
- Registre Global Query Filter no `OnModelCreating` do `AppDbContext`.
- Operacoes de delecao **sempre** usam soft delete (`entity.DeletedAt = DateTime.UtcNow`).
- **NUNCA** use `_context.Remove()` fisicamente.

### 6. Guids como IDs
- Todas as entidades usam `Guid Id` como chave primaria.
- Use `Guid.NewGuid()` para gerar novos IDs (nao deixe o banco gerar).

### 7. Datas de negocio sao strings
- Datas de eventos sao armazenadas como `string` no formato `DD/MM/YYYY`.
- **NAO** mude para `DateTime`, `DateOnly`, ou `DateTimeOffset` -- o frontend espera string.
- Timestamps (`CreatedAt`, `UpdatedAt`, `DeletedAt`) usam `DateTime.UtcNow`.

### 8. Senhas em texto puro (ATUALMENTE)
- O projeto atual armazena e compara senhas em texto puro.
- **NAO** implemente hash de senha a menos que seja explicitamente solicitado.
- Se for implementar hash, atualize TODOS os endpoints que lidam com senha.

### 9. Navegacoes e Eager Loading
- Use `Include()` + `ThenInclude()` para carregar dados relacionados.
- Use `.Select()` para projetar para DTOs de resposta.
- Nao ha Lazy Loading configurado -- requer Include para navegar.

---

## Regras de API e Controllers

### 10. DTOs sao obrigatorios para entrada
- Crie DTOs de request na pasta `app/Modals/` (mantenha a grafia "Modals").
- **SEMPRE** use DTOs como parametros dos metodos do controller.
- **NUNCA** receba entidades diretamente nos endpoints.

### 11. Use DTOs de Response para listagens
- Use `EventResponse`, `CalendarResponse`, `ParticipantResponse` como modelo.
- Para criacao/atualizacao, use objetos anonimos (padrao existente): `return Ok(new { message = "...", id = ... })`.

### 12. Retorne IActionResult
- Todo endpoint retorna `Task<IActionResult>`.
- Use `return Ok(...)`, `return BadRequest(...)`, `return NotFound(...)`, `return Unauthorized()`, `return Forbid()`.

### 13. Extraia usuario do JWT
```csharp
var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (!Guid.TryParse(accountId, out var userId))
    return Unauthorized();
```

---

## Regras de Autenticacao e Autorizacao

### 14. Endpoints autenticados usam [Authorize]
```csharp
[Authorize]
[HttpPost("createEvent")]
public async Task<IActionResult> CreateEvent(CreateEventRequest request)
```

### 15. Verifique ownership para delete
```csharp
if (entity.AccountId != userId)
    return Forbid();
```

### 16. Use JwtService para gerar tokens
- **SEMPRE** use `_jwtService.GenerateToken(email, userId)` para gerar JWT.
- Mantenha expiracao de 2 horas.
- Mantenha claims: `NameIdentifier` (userId) e `Email`.

### 17. Refresh Token
- Use `GenerateRefreshToken()` (metodo privado no controller) para gerar.
- Expiracao: 7 dias.
- Salve no banco via `_context.RefreshTokens.Add(...)`.

---

## Regras de Injecao de Dependencias

### 18. Sempre use construtor injection
```csharp
private readonly JwtService _jwtService;
private readonly AppDbContext _context;

public AuthController(JwtService jwtService, AppDbContext context)
{
    _jwtService = jwtService;
    _context = context;
}
```

### 19. Sempre registre no Program.cs
- **NUNCA** esqueca de registrar novos servicos no `Program.cs`.
- DbContexts: `builder.Services.AddDbContext<T>(...)`
- Services: `builder.Services.AddScoped<T>()`

### 20. Nao use interfaces
- Injete classes concretas diretamente.
- **NAO** crie `IService`/`IRepository` a menos que seja realmente necessario.

---

## Regras de Validacao

### 21. Nao ha FluentValidation
- **NAO** adicione FluentValidation.
- Validacoes sao manuais nos metodos do controller.

### 22. Validacoes manuais
```csharp
if (!Guid.TryParse(id, out var guid))
    return BadRequest(new { message = "id invalido" });

var entity = await _context.Table.FindAsync(guid);
if (entity == null)
    return NotFound(new { message = "Entidade nao encontrada" });
```

---

## Regras de Formato e Estilo

### 23. Async em todos os endpoints
- Todo metodo de controller deve ser `async Task<IActionResult>`.
- Use `await` em todas as chamadas ao banco.

### 24. CancellationToken nao e obrigatorio
- O projeto nao usa `CancellationToken`. Nao precisa adicionar.

### 25. Objetos anonimos para respostas simples
```csharp
return Ok(new { message = "Sucesso", id = entity.Id });
```

### 26. Use ActionsRequest para codigos
```csharp
ActionsRequest.Error.Register.UserAlreadyExists
ActionsRequest.Success.Login.UserLoggedIn
```
Adicione novas constantes aqui, nao espalhe strings magicas.

---

## Regras de EF Core e Migrations

### 27. Fluent API para relacionamentos
- Configure relacionamentos no `OnModelCreating` com Fluent API.
- **NAO** use DataAnnotations para FK/relacionamentos (exceto `[Key]`).

### 28. Migrations
- Apos alterar entidades, gere migration: `dotnet ef migrations add NomeDescritivo`.
- Aplique: `dotnet ef database update`.

---

## Regras de Performance

### 29. Nao ha paginacao (por enquanto)
- **NAO** adicione paginacao a menos que solicitado.

### 30. Nao ha caching
- **NAO** implemente caching a menos que solicitado.

### 31. Use projecao
- Para listagens, use `.Select()` para projetar para DTO.
- Isso evita tracking e melhora performance.

---

## Regras de Tratamento de Erros

### 32. Sem try-catch geral
- **NAO** adicione try-catch em volta de toda a logica.
- Trate erros com retornos condicionais.
- Excecoes inesperadas podem resultar em 500 (comportamento padrao).

### 33. Sem middleware de erro global
- **NAO** crie middleware de tratamento global de excecoes.
- Se necessario, discuta antes de implementar.

---

## Regras Especificas de Negocio (Reutilize, nao duplique)

### 34. Regras de Evento
- Criador vira Owner automaticamente.
- Convidados viram Member.
- Cor do evento pode ser sobrescrita pela `DefaultColor` do calendario.
- IDs invalidos de usuarios sao ignorados (nao gere erro).
- Apenas Owner (`AccountId`) pode deletar evento.
- Evento sem calendario: visivel para participantes diretos.
- Evento com calendario: visivel para participantes do calendario.

### 35. Regras de Calendario
- Ao registrar usuario, calendario pessoal "Meus Eventos" e criado automaticamente com cor `#7c3aed`.
- Calendario pessoal: `IsPersonal = true`.
- Apenas Owner (`AccountId`) pode deletar calendario.
- Ao deletar calendario, eventos tem `CalendarId = null` (SetNull).
- Usuario ve calendarios onde e Owner OU participante.

### 36. Regras de Participante
- `EventParticipant`: chave composta `(EventId, UserId)`, sem Id proprio.
- `CalendarParticipant`: Id proprio, indice unico `(CalendarId, UserId)`.
- Para sair: soft delete no participante.
- Para deletar entidade: apenas Owner pode.
- Role: "Owner" ou "Member" (strings).

### 37. Regras de Usuario
- Email unico.
- `Name + UserCode` unico.
- Busca de usuario: formato `Nome#Codigo`.
- UserCode: 4 digitos, aleatorio, unico por nome.

---

## Checklist para Novas Features

Antes de implementar qualquer feature, verifique:

- [ ] O endpoint vai no `AuthController`?
- [ ] O DTO de request esta em `app/Modals/`?
- [ ] A entidade tem `DeletedAt` + Global Query Filter?
- [ ] A logica de negocio esta no metodo do controller?
- [ ] Nao estou criando Repository ou Service desnecessario?
- [ ] Estou usando `FindAsync`/`FirstOrDefaultAsync` para buscas?
- [ ] Validacao manual para GUIDs e existencia?
- [ ] Soft delete para operacoes de remocao?
- [ ] O metodo e `async Task<IActionResult>`?
- [ ] O novo servico esta registrado no `Program.cs`?
