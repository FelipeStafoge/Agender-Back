# 03 - Estrutura de Pastas

## Arvore Completa

```
AgenderBackend/
├── .gitignore
├── AgenderBackend.csproj              # Arquivo de projeto .NET 10
├── AgenderBackend.http                # Arquivo de testes HTTP
├── README.md
├── event-description-frontend.md      # Documentacao especifica do frontend
├── docker-compose.yml                 # Servico PostgreSQL local
├── appsettings.json                   # Configuracao principal
├── appsettings.Development.json       # Configuracao de desenvolvimento
├── Program.cs                         # Entry point (Minimal Hosting Model)
├── Properties/
│   └── launchSettings.json            # Perfis de execucao (HTTP:5024, HTTPS:7214)
├── Data/
│   └── AppDbContext.cs                # EF Core DbContext
├── Migrations/
│   ├── AppDbContextModelSnapshot.cs    # Snapshot do modelo atual
│   ├── 20260623162058_InitialCreate.cs
│   ├── 20260623162058_InitialCreate.Designer.cs
│   ├── 20260623192058_AddAccountsIds.cs
│   ├── 20260623192058_AddAccountsIds.Designer.cs
│   ├── 20260624022316_AddEventParticipants.cs
│   ├── 20260624022316_AddEventParticipants.Designer.cs
│   ├── 20260625201608_AddEventColor.cs
│   ├── 20260625201608_AddEventColor.Designer.cs
│   ├── 20260626192639_AddCalendarDateList.cs
│   ├── 20260626192639_AddCalendarDateList.Designer.cs
│   ├── 20260629035401_AddCalendarOwnerAndEventCalendarId.cs
│   ├── 20260629035401_AddCalendarOwnerAndEventCalendarId.Designer.cs
│   ├── 20260701010540_AddRolesAndTimestamps.cs
│   ├── 20260701010540_AddRolesAndTimestamps.Designer.cs
│   ├── 20260702010745_AddEventDescription.cs
│   └── 20260702010745_AddEventDescription.Designer.cs
└── app/
    ├── Controllers/
    │   └── Auth/
    │       └── AuthController.cs       # Controller unico (13 endpoints)
    ├── entities/
    │   ├── Calendar.cs                # Entidade Calendar + CalendarParticipant
    │   ├── Event.cs                   # Entidade Event + EventParticipant
    │   ├── RefreshToken.cs            # Entidade RefreshToken
    │   └── User.cs                    # Entidade User
    ├── Modals/
    │   ├── CalendarParticipantRequest.cs  # DTO: AddCalendarParticipantRequest
    │   ├── CreateCalendarRequest.cs       # DTOs: CreateCalendarRequest + CalendarResponse
    │   ├── CreateEventRequest.cs          # DTOs: CreateEventRequest + EventResponse + ParticipantResponse
    │   ├── LoginRequest.cs               # DTO: LoginRequest
    │   ├── RefreshTokenRequest.cs         # DTO: RefreshTokenRequest
    │   ├── RegisterRequest.cs             # DTO: RegisterRequest
    │   └── UserCalender.cs               # DTO: UserCalendar (nao utilizado)
    ├── Services/
    │   └── JwtService.cs               # Servico de geracao de token JWT
    └── utils/
        └── ActionsRequest.cs           # Constantes de acao/erro
```

## Detalhamento de Cada Pasta

### `Data/`

Contem o `AppDbContext`, a classe central de acesso ao banco de dados via Entity Framework Core.

**Arquivo**: `AppDbContext.cs`

- Configura relacionamentos via Fluent API (`OnModelCreating`)
- Define 6 DbSets
- Aplica Global Query Filters para soft delete em 5 entidades

### `app/Controllers/Auth/`

Contem o unico controller da aplicacao.

**Arquivo**: `AuthController.cs`

- `[ApiController]` -- validacao automatica de ModelState
- `[Route("api/auth")]` -- rota base
- 679 linhas de codigo
- 13 endpoints (3 anonimos + 10 autenticados)
- Dependencias: `JwtService`, `AppDbContext`

### `app/entities/`

Entidades de dominio mapeadas para o banco de dados.

| Arquivo | Classes |
|---|---|
| `User.cs` | `User` |
| `Event.cs` | `Event`, `EventParticipant` |
| `Calendar.cs` | `Calendar`, `CalendarParticipant` |
| `RefreshToken.cs` | `RefreshToken` |

**Observacao**: As entidades de juncao (`EventParticipant`, `CalendarParticipant`) estao declaradas no mesmo arquivo de suas entidades principais.

### `app/Modals/`

DTOs de request e response. **O nome da pasta e "Modals" (typo de "Models")**.

| Arquivo | Classes | Tipo |
|---|---|---|
| `LoginRequest.cs` | `LoginRequest` | Request DTO |
| `RegisterRequest.cs` | `RegisterRequest` | Request DTO |
| `RefreshTokenRequest.cs` | `RefreshTokenRequest` | Request DTO |
| `CreateEventRequest.cs` | `CreateEventRequest`, `EventResponse`, `ParticipantResponse` | Request + Response DTOs |
| `CreateCalendarRequest.cs` | `CreateCalendarRequest`, `CalendarResponse` | Request + Response DTOs |
| `CalendarParticipantRequest.cs` | `AddCalendarParticipantRequest` | Request DTO |
| `UserCalender.cs` | `UserCalendar` | DTO (nao utilizado nos endpoints atuais) |

### `app/Services/`

Servicos de negocio separados do controller.

| Arquivo | Classe | Metodo |
|---|---|---|
| `JwtService.cs` | `JwtService` | `GenerateToken(string email, Guid userId)` |

### `app/utils/`

Utilitarios e constantes.

| Arquivo | Classe | Proposito |
|---|---|---|
| `ActionsRequest.cs` | `ActionsRequest` | Constantes de codigo de acao/erro para respostas |

### `Migrations/`

8 migrations do Entity Framework Core:

| Migration | Data | Descricao |
|---|---|---|
| `InitialCreate` | 2026-06-23 | Criacao inicial: Users, Events, Calendar, RefreshTokens |
| `AddAccountsIds` | 2026-06-23 | Adiciona AccountId em entidades |
| `AddEventParticipants` | 2026-06-24 | Adiciona tabela EventParticipants |
| `AddEventColor` | 2026-06-25 | Adiciona campo Color em Event |
| `AddCalendarDateList` | 2026-06-26 | Adiciona lista Date em Calendar e coluna DefaultColor |
| `AddCalendarOwnerAndEventCalendarId` | 2026-06-29 | Adiciona OwnerId, IsPersonal em Calendar e CalendarId em Event |
| `AddRolesAndTimestamps` | 2026-07-01 | Adiciona Role, CreatedAt, UpdatedAt, DeletedAt em entidades |
| `AddEventDescription` | 2026-07-02 | Adiciona campo Description em Event |

### `Properties/`

Configuracoes de ambiente de desenvolvimento.

| Arquivo | Conteudo |
|---|---|
| `launchSettings.json` | Perfis `http` (porta 5024) e `https` (porta 7214) |
