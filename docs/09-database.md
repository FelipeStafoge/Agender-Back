# 09 - Database

## Banco Utilizado

- **SGBD**: SQLite
- **Provider EF Core**: `Microsoft.EntityFrameworkCore.Sqlite` v10.0.9
- **Arquivo**: `agender.db` (raiz do projeto)
- **Connection String**: `Data Source=agender.db`
- **Modelo**: Arquivo local, single-file database

## Connection String

```json
// appsettings.json
"ConnectionStrings": {
    "DefaultConnection": "Data Source=agender.db"
}
```

## Registro no Program.cs

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
});
```

---

## DbContext

**Arquivo**: `Data/AppDbContext.cs`
**Classe**: `AppDbContext`

### DbSets

| DbSet | Tipo | Tabela |
|---|---|---|
| `Users` | `DbSet<User>` | `Users` |
| `RefreshTokens` | `DbSet<RefreshToken>` | `RefreshTokens` |
| `Events` | `DbSet<Event>` | `Events` |
| `EventParticipants` | `DbSet<EventParticipant>` | `EventParticipants` |
| `Calendar` | `DbSet<Calendar>` | `Calendar` |
| `CalendarParticipant` | `DbSet<CalendarParticipant>` | `CalendarParticipant` |

### Configuracao Fluent API (OnModelCreating)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // EventParticipant: chave composta
    modelBuilder.Entity<EventParticipant>()
        .HasKey(ep => new { ep.EventId, ep.UserId });

    modelBuilder.Entity<EventParticipant>()
        .HasOne(ep => ep.Event)
        .WithMany(e => e.Participants)
        .HasForeignKey(ep => ep.EventId);

    modelBuilder.Entity<EventParticipant>()
        .HasOne(ep => ep.User)
        .WithMany()
        .HasForeignKey(ep => ep.UserId);

    // CalendarParticipant: chave propria + indice unico
    modelBuilder.Entity<CalendarParticipant>()
        .HasKey(cp => cp.Id);

    modelBuilder.Entity<CalendarParticipant>()
        .HasOne(cp => cp.Calendar)
        .WithMany(c => c.CalendarParticipants)
        .HasForeignKey(cp => cp.CalendarId);

    modelBuilder.Entity<CalendarParticipant>()
        .HasOne(cp => cp.User)
        .WithMany(u => u.CalendarParticipants)
        .HasForeignKey(cp => cp.UserId);

    modelBuilder.Entity<CalendarParticipant>()
        .HasIndex(cp => new { cp.CalendarId, cp.UserId })
        .IsUnique();

    // Event -> Calendar: SetNull on delete
    modelBuilder.Entity<Event>()
        .HasOne(e => e.Calendar)
        .WithMany()
        .HasForeignKey(e => e.CalendarId)
        .OnDelete(DeleteBehavior.SetNull);

    // Global Query Filters (soft delete)
    modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
    modelBuilder.Entity<Calendar>().HasQueryFilter(c => c.DeletedAt == null);
    modelBuilder.Entity<CalendarParticipant>().HasQueryFilter(cp => cp.DeletedAt == null);
    modelBuilder.Entity<Event>().HasQueryFilter(e => e.DeletedAt == null);
    modelBuilder.Entity<EventParticipant>().HasQueryFilter(ep => ep.DeletedAt == null);
}
```

---

## Esquema do Banco

### Tabela: Users

| Coluna | Tipo SQLite | Nullable | Descricao |
|---|---|---|---|
| `Id` | TEXT (Guid) | NOT NULL | Primary Key |
| `Name` | TEXT | NOT NULL | Nome do usuario |
| `Email` | TEXT | NOT NULL | Email |
| `Password` | TEXT | NOT NULL | Senha (texto puro) |
| `UserCode` | TEXT | NOT NULL | Codigo 4 digitos |
| `CreatedAt` | TEXT (DateTime) | NOT NULL | Data de criacao |
| `UpdatedAt` | TEXT (DateTime) | NULL | Data de atualizacao |
| `DeletedAt` | TEXT (DateTime) | NULL | Soft delete |

**Indices**:
- `PK_Users`: `Id` (PRIMARY KEY)
- `IX_Users_Name_UserCode`: `(Name, UserCode)` UNIQUE

### Tabela: Calendar

| Coluna | Tipo SQLite | Nullable | Descricao |
|---|---|---|---|
| `Id` | TEXT (Guid) | NOT NULL | Primary Key |
| `AccountId` | TEXT (Guid) | NOT NULL | ID do criador |
| `Name` | TEXT | NOT NULL | Nome |
| `Date` | TEXT | NOT NULL | Lista de datas (primitive collection) |
| `DefaultColor` | TEXT | NOT NULL | Cor padrao hex |
| `OwnerId` | TEXT | NOT NULL | ID do dono (string) |
| `IsPersonal` | INTEGER (bool) | NOT NULL | Calendario pessoal |
| `CreatedAt` | TEXT (DateTime) | NOT NULL | Data de criacao |
| `UpdatedAt` | TEXT (DateTime) | NULL | Data de atualizacao |
| `DeletedAt` | TEXT (DateTime) | NULL | Soft delete |

**Indices**:
- `PK_Calendar`: `Id` (PRIMARY KEY)

### Tabela: CalendarParticipant

| Coluna | Tipo SQLite | Nullable | Descricao |
|---|---|---|---|
| `Id` | TEXT (Guid) | NOT NULL | Primary Key |
| `CalendarId` | TEXT (Guid) | NOT NULL | FK -> Calendar |
| `UserId` | TEXT (Guid) | NOT NULL | FK -> Users |
| `Role` | TEXT | NOT NULL | "Owner" / "Member" |
| `CreatedAt` | TEXT (DateTime) | NOT NULL | Data de criacao |
| `UpdatedAt` | TEXT (DateTime) | NULL | Data de atualizacao |
| `DeletedAt` | TEXT (DateTime) | NULL | Soft delete |

**Indices**:
- `PK_CalendarParticipant`: `Id` (PRIMARY KEY)
- `IX_CalendarParticipant_UserId`: `UserId`
- `IX_CalendarParticipant_CalendarId_UserId`: `(CalendarId, UserId)` UNIQUE

**Foreign Keys**:
- `FK_CalendarParticipant_Calendar_CalendarId`: `CalendarId` -> `Calendar(Id)` ON DELETE CASCADE
- `FK_CalendarParticipant_Users_UserId`: `UserId` -> `Users(Id)` ON DELETE CASCADE

### Tabela: Events

| Coluna | Tipo SQLite | Nullable | Descricao |
|---|---|---|---|
| `Id` | TEXT (Guid) | NOT NULL | Primary Key |
| `AccountId` | TEXT (Guid) | NOT NULL | ID do criador |
| `Name` | TEXT | NOT NULL | Nome do evento |
| `Date` | TEXT | NOT NULL | Data `DD/MM/YYYY` |
| `Description` | TEXT | NULL | Descricao |
| `Color` | TEXT | NOT NULL | Cor hex |
| `CalendarId` | TEXT (Guid) | NULL | FK -> Calendar |
| `CreatedAt` | TEXT (DateTime) | NOT NULL | Data de criacao |
| `UpdatedAt` | TEXT (DateTime) | NULL | Data de atualizacao |
| `DeletedAt` | TEXT (DateTime) | NULL | Soft delete |

**Indices**:
- `PK_Events`: `Id` (PRIMARY KEY)
- `IX_Events_CalendarId`: `CalendarId`

**Foreign Keys**:
- `FK_Events_Calendar_CalendarId`: `CalendarId` -> `Calendar(Id)` ON DELETE SET NULL

### Tabela: EventParticipants

| Coluna | Tipo SQLite | Nullable | Descricao |
|---|---|---|---|
| `EventId` | TEXT (Guid) | NOT NULL | PK composta / FK -> Events |
| `UserId` | TEXT (Guid) | NOT NULL | PK composta / FK -> Users |
| `Role` | TEXT | NOT NULL | "Owner" / "Member" |
| `CreatedAt` | TEXT (DateTime) | NOT NULL | Data de criacao |
| `UpdatedAt` | TEXT (DateTime) | NULL | Data de atualizacao |
| `DeletedAt` | TEXT (DateTime) | NULL | Soft delete |

**Indices**:
- `PK_EventParticipants`: `(EventId, UserId)` (COMPOSITE PRIMARY KEY)
- `IX_EventParticipants_UserId`: `UserId`

**Foreign Keys**:
- `FK_EventParticipants_Events_EventId`: `EventId` -> `Events(Id)` ON DELETE CASCADE
- `FK_EventParticipants_Users_UserId`: `UserId` -> `Users(Id)` ON DELETE CASCADE

### Tabela: RefreshTokens

| Coluna | Tipo SQLite | Nullable | Descricao |
|---|---|---|---|
| `Id` | TEXT (Guid) | NOT NULL | Primary Key |
| `Token` | TEXT | NOT NULL | Token Base64 |
| `UserId` | TEXT (Guid) | NOT NULL | FK -> Users |
| `ExpiresAt` | TEXT (DateTime) | NOT NULL | Data de expiracao |
| `Revoked` | INTEGER (bool) | NOT NULL | Revogado |

**Indices**:
- `PK_RefreshTokens`: `Id` (PRIMARY KEY)
- `IX_RefreshTokens_UserId`: `UserId`

**Foreign Keys**:
- `FK_RefreshTokens_Users_UserId`: `UserId` -> `Users(Id)` ON DELETE CASCADE

---

## Cascade Delete

| Tabela Origem | Tabela Destino | FK | OnDelete |
|---|---|---|---|
| CalendarParticipant | Calendar | CalendarId | **Cascade** |
| CalendarParticipant | Users | UserId | **Cascade** |
| EventParticipants | Events | EventId | **Cascade** |
| EventParticipants | Users | UserId | **Cascade** |
| RefreshTokens | Users | UserId | **Cascade** |
| Events | Calendar | CalendarId | **SetNull** |

**Nota**: O cascade delete do EF Core so e acionado com `context.Remove()`. Como o sistema usa soft delete (`DeletedAt`), as remocoes fisicas nao ocorrem nos endpoints atuais.

---

## Migrations

### Historico de Migrations (8 migrations)

| # | Nome | Data | Descricao |
|---|---|---|---|
| 1 | `20260623162058_InitialCreate` | 23/06/2026 | Criacao inicial do banco |
| 2 | `20260623192058_AddAccountsIds` | 23/06/2026 | Adiciona AccountId em entidades |
| 3 | `20260624022316_AddEventParticipants` | 24/06/2026 | Adiciona tabela EventParticipants |
| 4 | `20260625201608_AddEventColor` | 25/06/2026 | Adiciona campo Color em Event |
| 5 | `20260626192639_AddCalendarDateList` | 26/06/2026 | Adiciona Date list e DefaultColor em Calendar |
| 6 | `20260629035401_AddCalendarOwnerAndEventCalendarId` | 29/06/2026 | Adiciona OwnerId, IsPersonal e CalendarId |
| 7 | `20260701010540_AddRolesAndTimestamps` | 01/07/2026 | Adiciona Role, CreatedAt, UpdatedAt, DeletedAt |
| 8 | `20260702010745_AddEventDescription` | 02/07/2026 | Adiciona campo Description em Event |

### Comando para gerar migration

```bash
dotnet ef migrations add NomeDaMigration
```

### Comando para aplicar migrations

```bash
dotnet ef database update
```

---

## Seeds

**Nao encontrado**. Nao ha dados de seed (metodo `HasData` no `OnModelCreating`, classes de seed, ou inicializadores).

---

## Diagrama ER (Mermaid)

```mermaid
erDiagram
    Users {
        TEXT Id PK "Guid"
        TEXT Name
        TEXT Email
        TEXT Password
        TEXT UserCode
        TEXT CreatedAt
        TEXT UpdatedAt
        TEXT DeletedAt
    }

    Calendar {
        TEXT Id PK "Guid"
        TEXT AccountId
        TEXT Name
        TEXT Date "List_string_"
        TEXT DefaultColor
        TEXT OwnerId
        INTEGER IsPersonal
        TEXT CreatedAt
        TEXT UpdatedAt
        TEXT DeletedAt
    }

    CalendarParticipant {
        TEXT Id PK "Guid"
        TEXT CalendarId FK
        TEXT UserId FK
        TEXT Role
        TEXT CreatedAt
        TEXT UpdatedAt
        TEXT DeletedAt
    }

    Events {
        TEXT Id PK "Guid"
        TEXT AccountId
        TEXT Name
        TEXT Date
        TEXT Description
        TEXT Color
        TEXT CalendarId FK "nullable"
        TEXT CreatedAt
        TEXT UpdatedAt
        TEXT DeletedAt
    }

    EventParticipants {
        TEXT EventId PK_FK
        TEXT UserId PK_FK
        TEXT Role
        TEXT CreatedAt
        TEXT UpdatedAt
        TEXT DeletedAt
    }

    RefreshTokens {
        TEXT Id PK "Guid"
        TEXT Token
        TEXT UserId FK
        TEXT ExpiresAt
        INTEGER Revoked
    }

    Users ||--o{ CalendarParticipant : "UserId (Cascade)"
    Users ||--o{ EventParticipants : "UserId (Cascade)"
    Users ||--o{ RefreshTokens : "UserId (Cascade)"
    Calendar ||--o{ CalendarParticipant : "CalendarId (Cascade)"
    Calendar ||--o{ Events : "CalendarId (SetNull)"
    Events ||--o{ EventParticipants : "EventId (Cascade)"
```
