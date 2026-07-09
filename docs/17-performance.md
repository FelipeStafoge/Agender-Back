# 17 - Performance

## Caching

**Nao implementado**. Nao ha:
- `IMemoryCache` ou `IDistributedCache`
- Caching de respostas (`[ResponseCache]`)
- Output caching
- Caching de segundo nivel do EF Core

---

## AsNoTracking

**Nao utilizado**. Nenhuma query usa `.AsNoTracking()`. No entanto, as queries de listagem (`getListEvents`, `getListCalendar`, `getCalendarEvents`) usam `.Select()` para projecao em DTOs, o que efetivamente evita tracking, ja que as entidades nao sao materializadas.

Para queries que materializam entidades (como `FirstOrDefaultAsync`, `FindAsync`), o tracking esta ativo, o que gera sobrecarga de snapshot.

---

## Paginacao

**Nao implementada**. Os endpoints de listagem (`getListEvents`, `getListCalendar`, `getCalendarEvents`) retornam **todos** os registros sem limite. Para conjuntos de dados grandes, isso pode causar:
- Alto consumo de memoria
- Respostas lentas
- Timeout

Nao ha uso de `.Skip()` e `.Take()`.

---

## Includes (Eager Loading)

As queries de listagem usam `Include` + `ThenInclude`:

```csharp
.Include(e => e.Participants)
    .ThenInclude(p => p.User)
```

Isso gera **joins** no SQL, carregando:
- Evento
- Participantes do evento
- Dados do usuario de cada participante

Para calendarios:

```csharp
.Include(c => c.CalendarParticipants)
    .ThenInclude(p => p.User)
```

Nao ha `AsSplitQuery()` para evitar explosao cartesiana (problema comum com multiplos Includes). Com PostgreSQL, o impacto pode ser significativo com multiplos Includes.

---

## Lazy Loading

**Nao configurado**. O projeto nao usa lazy loading. Nao ha:
- Pacote `Microsoft.EntityFrameworkCore.Proxies`
- Chamada a `.UseLazyLoadingProxies()`
- Propriedades `virtual`

---

## Eager Loading

Usado via `Include()` e `ThenInclude()` nas queries de listagem. Para buscas pontuais (`FindAsync`, `FirstOrDefaultAsync`), nao ha Include -- apenas a entidade principal e carregada.

---

## Queries Complexas

### Filtro de Eventos do Usuario (GetListEvents)

```csharp
var query = _context.Events
    .Where(e =>
        (e.CalendarId == null && e.Participants.Any(p => p.UserId == user.Id)) ||
        (e.CalendarId != null && e.Calendar!.CalendarParticipants.Any(p => p.UserId == user.Id)));
```

Esta query usa `Any` em colecoes navegadas, o que o EF Core traduz para subconsultas `EXISTS`. Duas condicoes OR podem tornar a query custosa com muitos dados.

### Filtro de Data (Comparacao de Strings)

```csharp
query = query.Where(e =>
    string.Compare(
        e.Date.Substring(6, 4) + e.Date.Substring(3, 2) + e.Date.Substring(0, 2),
        startComparable
    ) >= 0);
```

Esta operacao de string **nao e traduzida para SQL** -- o EF Core nao consegue converter `string.Compare` com `Substring` para SQL. Isso pode causar **avaliacao no cliente** (client-side evaluation), carregando todos os eventos na memoria antes de filtrar. Alternativa: usar `string.CompareTo()` ou converter para campo de data nativo.

### Acesso a Calendarios (GetListCalendar)

```csharp
var listCalendars = await _context.Calendar
    .Where(c => c.OwnerId == accountId || c.CalendarParticipants.Any(p => p.UserId == user.Id))
    ...
```

Similar a consulta de eventos, usa `Any` para verificar participacao.

---

## Indices no Banco de Dados

| Tabela | Indice | Tipo |
|---|---|---|
| `Users` | `(Name, UserCode)` | UNIQUE |
| `CalendarParticipant` | `(CalendarId, UserId)` | UNIQUE |
| `CalendarParticipant` | `UserId` | Non-unique |
| `EventParticipants` | `(EventId, UserId)` | Composite PK |
| `EventParticipants` | `UserId` | Non-unique |
| `Events` | `CalendarId` | Non-unique |
| `RefreshTokens` | `UserId` | Non-unique |

Alem dos indices acima, todas as PKs sao indexadas automaticamente.

---

## Observacoes de Performance

### Pontos de Atencao

1. **Filtro de data via string**: Potencial client-side evaluation. Se confirmado, todos os eventos sao carregados em memoria antes da filtragem.
2. **Sem paginacao**: Listagens retornam todos os dados -- problemático com muitos registros.
3. **Projecao com Select**: As queries de listagem projetam para DTO com `.Select()`, o que e bom (evita carregar colunas desnecessarias e evita tracking), mas o Include e feito antes do Select.
4. **Tracking desnecessario**: `FindAsync` e `FirstOrDefaultAsync` mantem tracking, mas nao e necessario em buscas de validacao (ex: verificar se usuario existe).
5. **Multiplos SaveChanges**: `Register` chama `SaveChangesAsync` duas vezes -- uma apos criar usuario, outra apos calendario/refresh token.

### Aspectos Positivos

1. **PostgreSQL local**: Banco de dados no mesmo servidor ou container Docker, latencia baixa.
2. **Projecao com Select**: Evita o problema de N+1 e carrega apenas dados necessarios.
3. **Chaves estrangeiras indexadas**: `UserId` em tabelas de juncao tem indices.
4. **Indices unicos**: Garantem integridade com performance.
