# 01 - Visao Geral do Projeto

## Objetivo do Sistema

AgenderBackend e uma API REST para gerenciamento de calendarios e eventos compartilhados entre usuarios. O sistema permite que usuarios se registrem, criem calendarios (pessoais ou compartilhados) e eventos, podendo convidar outros usuarios como participantes.

## Tecnologias Utilizadas

| Tecnologia | Versao | Proposito |
|---|---|---|
| .NET | 10.0 | Framework |
| ASP.NET Core | 10.0 | Web API |
| C# | 13 | Linguagem |
| Entity Framework Core | 10.0.9 | ORM |
| PostgreSQL | Via Npgsql EF Core | Banco de dados |
| JWT Bearer | 10.0.9 | Autenticacao |
| Swashbuckle | 10.2.1 | Documentacao Swagger/OpenAPI |
| Microsoft.AspNetCore.OpenApi | 10.0.9 | Suporte OpenAPI |

## Framework

- **Target Framework**: `net10.0`
- **Nullable**: habilitado
- **Implicit Usings**: habilitado
- **Modelo de Hosting**: Minimal Hosting Model (tudo em `Program.cs`, sem `Startup.cs`)

## Banco de Dados

- **SGBD**: PostgreSQL (via `Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Arquivo**: `agender.db` (raiz do projeto)
- **Migrations**: 8 migrations via EF Core Migrations

## ORM

Entity Framework Core 10.0.9 com provedor PostgreSQL (Npgsql).

## Estrutura Geral

```
AgenderBackend/
├── Program.cs                 # Entry point (Minimal Hosting)
├── Data/
│   └── AppDbContext.cs        # EF Core DbContext
├── app/
│   ├── Controllers/Auth/      # Controller unico (AuthController)
│   ├── entities/              # Entidades de dominio
│   ├── Modals/                # DTOs de request/response
│   ├── Services/              # Servico JWT
│   └── utils/                 # Constantes de acao/erro
├── Migrations/                # EF Core Migrations
├── Properties/
│   └── launchSettings.json    # Perfis de execucao
├── appsettings.json           # Config principal (JWT, PostgreSQL)
└── appsettings.Development.json
```

## Camadas

O projeto segue uma arquitetura simplificada com apenas 2 camadas efetivas:

| Camada | Responsabilidade |
|---|---|
| **Controller** (`AuthController`) | Recebe requests HTTP, chama logica de negocio, acessa DbContext diretamente |
| **Data** (`AppDbContext`) | Acesso ao banco de dados via Entity Framework Core |

**Observacao**: Nao existem camadas separadas de Service (exceto `JwtService`) nem Repository. A logica de negocio esta concentrada no Controller, que acessa o `AppDbContext` diretamente.

## Dependencias Principais

| Pacote NuGet | Versao |
|---|---|
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 |
| `Microsoft.AspNetCore.OpenApi` | 10.0.9 |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 |
| `Swashbuckle.AspNetCore` | 10.2.1 |

## Organizacao do Projeto

- O projeto possui **1 controller** (`AuthController`) com **13 endpoints** sob a rota `/api/auth`
- **1 servico** dedicado: `JwtService` para geracao de tokens JWT
- **4 entidades**: `User`, `Event`, `Calendar`, `RefreshToken`
- **2 entidades de juncao**: `EventParticipant`, `CalendarParticipant`
- **6 DTOs de request** e **3 DTOs de response**
- **1 classe de constantes**: `ActionsRequest` com codigos de acao/erro
- **Soft delete** aplicado globalmente via EF Core Query Filters com campo `DeletedAt`
