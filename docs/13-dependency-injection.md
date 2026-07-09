# 13 - Injecao de Dependencias

## Registro de Servicos (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Controllers
builder.Services.AddControllers();

// 2. Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. JwtService - Scoped
builder.Services.AddScoped<JwtService>();

// 4. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("VuePolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// 5. Authentication (JWT)
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });

// 6. DbContext (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});
```

---

## Tabela de Servicos e Tempo de Vida

| Servico | Tipo de Registro | Tempo de Vida | Justificativa |
|---|---|---|---|
| Controllers | `AddControllers()` | **Transient** (padrao) | Nova instancia por request |
| `JwtService` | `AddScoped<JwtService>()` | **Scoped** | Compartilha ciclo de vida com DbContext; uma instancia por request |
| `AppDbContext` | `AddDbContext<AppDbContext>()` | **Scoped** (padrao EF Core) | DbContext nao e thread-safe; uma instancia por request |
| CORS Policy `"VuePolicy"` | `AddCors()` | **Singleton** (politica de configuracao) | Configuracao global que nao muda |
| JWT Authentication | `AddAuthentication().AddJwtBearer()` | **Singleton** (configuracao) | Configuracao de autenticacao global |
| SwaggerGen | `AddSwaggerGen()` | **Singleton** | Configuracao global |
| EndpointsApiExplorer | `AddEndpointsApiExplorer()` | **Singleton** | Configuracao global |

---

## Tempo de Vida Explicado

### Singleton

Criado uma vez e compartilhado por toda a aplicacao. Usado para:
- Configuracoes de framework (Autenticacao, Swagger, CORS, Endpoints)
- Politicas e opcoes que nao mudam entre requests

### Scoped

Criado uma vez por request HTTP. Usado para:
- `JwtService`: recebe `IConfiguration` (singleton) como dependencia; tempo de vida scoped e suficiente
- `AppDbContext`: Entity Framework Core recomenda DbContext por request; evita problemas de concorrencia e mantem tracking isolado

### Transient

Criado toda vez que e injetado. Usado para:
- Controllers: padrao do ASP.NET Core (`AddControllers()`); mesmo sendo transient, recebem dependencias scoped/transient normalmente

---

## Servicos Registrados que NAO existem

Nao ha registro para:
- `IHttpContextAccessor` -- nao e usado
- `AutoMapper` -- nao e usado
- `FluentValidation` -- nao e usado
- `MemoryCache` / `DistributedCache` -- nao e usado
- `HttpClient` / `IHttpClientFactory` -- nao e usado
- `ProblemDetails` -- nao e usado
- Qualquer interface customizada (`IUserRepository`, `IEventService`, etc.)

---

## Dependencias por Classe

### AuthController

```csharp
public AuthController(
    JwtService jwtService,        // Scoped
    AppDbContext context)          // Scoped
```

### JwtService

```csharp
public JwtService(
    IConfiguration configuration)  // Singleton (built-in)
```

### AppDbContext

```csharp
public AppDbContext(
    DbContextOptions<AppDbContext> options)  // Configurado via AddDbContext
```

---

## Observacao sobre Interfaces

O projeto **nao utiliza interfaces** para injecao de dependencias. As classes concretas (`JwtService`, `AppDbContext`) sao injetadas diretamente. Nao ha:

```csharp
// Nao existe:
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
```

Isso torna o acoplamento mais rigido e dificulta testes unitarios, mas e o padrao adotado neste projeto.
