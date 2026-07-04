# 14 - Padroes de Codificacao

## Convencoes de Nomenclatura

### Namespaces

| Elemento | Convencao | Exemplo |
|---|---|---|
| Data | `AgenderBackend.Data` | `AppDbContext` |
| Controllers | `AgenderBackend.Api.Controllers` | `AuthController` |
| Entities | `AgenderBackend.Api.Models` | `User` |
| Services | `AgenderBackend.Services` | `JwtService` |
| Entities sem namespace | Namespace raiz (global) | `Event`, `EventParticipant`, `Calendar`, `CalendarParticipant`, `RefreshToken` |
| DTOs sem namespace | Namespace raiz (global) | `LoginRequest`, `EventResponse`, etc. |

**Observacao**: Ha inconsistencia nos namespaces. Algumas entidades estao em `AgenderBackend.Api.Models` (User), outras no namespace global. DTOs estao no namespace global, exceto `LoginRequest` e `RegisterRequest` que estao em `AgenderBackend.Api.Models`.

### Classes

- **PascalCase** para nomes de classes: `AuthController`, `JwtService`, `AppDbContext`, `LoginRequest`
- Sufixo `Controller` para controllers: `AuthController`
- Sufixo `Service` para servicos: `JwtService`
- Sufixo `Request` para DTOs de entrada: `LoginRequest`, `CreateEventRequest`
- Sufixo `Response` para DTOs de saida: `EventResponse`, `CalendarResponse`, `ParticipantResponse`

### Entidades

- Nome da entidade = nome da tabela no singular (excecao: EF Core pluraliza automaticamente para tabelas)
- Entidades de juncao tem nomes compostos: `EventParticipant`, `CalendarParticipant`

### Propriedades

- **PascalCase**: `Id`, `Name`, `Email`, `CreatedAt`, `DeletedAt`
- Tipos anulaveis com `?`: `DateTime?`, `string?`, `Guid?`
- Strings inicializadas com `string.Empty`: `public string Name { get; set; } = string.Empty;`
- Listas inicializadas com `[]`: `public List<EventParticipant> Participants { get; set; } = [];`

### Metodos

- **PascalCase**: `Login()`, `GenerateToken()`, `CreateEvent()`
- Async methods com sufixo `Async` (EF Core methods: `ToListAsync()`, `SaveChangesAsync()`, `FirstOrDefaultAsync()`)
- Metodos do controller seguem nome da acao: `Login`, `Register`, `CreateEvent`, `GetListEvents`

---

## Injecao de Dependencias

- Uso de **construtor injection** em todos os casos
- Dependencias armazenadas em campos `private readonly` com prefixo `_`:
  ```csharp
  private readonly JwtService _jwtService;
  private readonly AppDbContext _context;
  ```
- **Sem interfaces**: Injeta classes concretas diretamente

---

## DTOs

- DTOs de request: classes simples com propriedades publicas
- DTOs de response: usados apenas nos endpoints de listagem (`EventResponse`, `CalendarResponse`)
- Endpoints de criacao/modificacao retornam **objetos anonimos** em vez de DTOs tipados:
  ```csharp
  return Ok(new
  {
      message = "Evento criado com participantes",
      eventId = newEvent.Id
  });
  ```
- Response DTOs sao projetados via `.Select()` do EF Core:
  ```csharp
  .Select(e => new EventResponse
  {
      Id = e.Id,
      Name = e.Name,
      ...
  })
  ```

---

## Controllers

- **1 controller** para toda a API: `AuthController` com rota `[Route("api/auth")]`
- Atributo `[ApiController]` para validacao automatica
- **Acesso direto ao DbContext**: Nao ha camada de servico intermediaria
- Retorno sempre `Task<IActionResult>`
- Todos os metodos sao `async`
- Uso de `await` em todas as chamadas assincronas

---

## Async / Await

- **Todas as actions sao assincronas**: `public async Task<IActionResult>`
- **Todas as chamadas ao banco usam await**: `await _context.Users.FirstOrDefaultAsync(...)`
- Nao ha chamadas bloqueantes como `.Result` ou `.Wait()`
- Nao ha `ConfigureAwait(false)` (nao necessario em ASP.NET Core)

---

## CancellationToken

**Nao implementado**. Nenhum metodo recebe `CancellationToken` como parametro:

```csharp
// Atual:
public async Task<IActionResult> Login(LoginRequest request)

// Nao existe:
public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
```

---

## Nullable

- Projeto com `<Nullable>enable</Nullable>` no `.csproj`
- Propriedades de referencia anulaveis marcadas com `?`:
  ```csharp
  public string? Description { get; set; }
  public DateTime? UpdatedAt { get; set; }
  public Guid? CalendarId { get; set; }
  ```
- Uso do null-forgiving operator `!` em navegacoes obrigatorias:
  ```csharp
  public Event Event { get; set; } = null!;
  public User User { get; set; } = null!;
  ```
- Uso de `!` ao acessar configuracao:
  ```csharp
  builder.Configuration["Jwt:Key"]!
  ```

---

## Tratamento de Excecoes

- **Sem try-catch** nos metodos do controller
- **Sem Global Exception Handler** (middleware ou filtro)
- Erros sao tratados com retornos condicionais (`return Unauthorized()`, `return BadRequest()`, etc.)
- Excecoes nao tratadas resultam em 500 Internal Server Error

---

## Logging

- **Nao implementado** no codigo de aplicacao
- Nao ha injecao de `ILogger<T>` em controllers ou services
- Apenas configuracao basica no `appsettings.json`

---

## EF Core

- Use of **Fluent API** para configuracao (nao DataAnnotations para relacionamentos)
- **Global Query Filters** para soft delete
- **Eager Loading**: `Include()` + `ThenInclude()` para navegacoes
- **Projecao**: `.Select()` para evitar carregar colunas desnecessarias
- **Sem repositorio generico**: DbContext usado diretamente

---

## Formato de Data

- Datas sao strings no formato `DD/MM/YYYY` (ex: `"04/07/2026"`)
- Nao usa `DateTime`, `DateOnly`, ou `DateTimeOffset` para datas de negocio
- Timestamps (`CreatedAt`, `UpdatedAt`, `DeletedAt`) usam `DateTime` em UTC

---

## Soft Delete

- Campo `DeletedAt` (nullable `DateTime`)
- Global Query Filters no DbContext excluem automaticamente registros com `DeletedAt != null`
- Soft delete aplicado em 5 das 6 entidades
- `RefreshToken` nao tem soft delete (usa campo `Revoked`)

---

## Resumo de Padroes

| Aspecto | Padrao |
|---|---|
| Nomenclatura | PascalCase para classes, metodos, propriedades |
| Async | Todos endpoints async |
| CancellationToken | Nao utilizado |
| Nullable | Habilitado, com `?` e `!` |
| DI | Construtor injection, sem interfaces |
| Retorno | `IActionResult` com objetos anonimos |
| Validacao | Manual no controller (sem FluentValidation) |
| Erros | Condicionais + retorno HTTP (sem exceptions) |
| Logging | Nao implementado |
| EF Core | Fluent API, Global Filters, Eager Loading, Projecao |
| Datas | Strings `DD/MM/YYYY` para datas de negocio |
| Soft Delete | `DeletedAt` + Query Filter |
