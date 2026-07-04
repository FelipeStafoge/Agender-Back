# 07 - Services

## JwtService

**Arquivo**: `app/Services/JwtService.cs`
**Namespace**: `AgenderBackend.Services`
**Tempo de vida**: **Scoped**

```csharp
public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(string email, Guid userId)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"]!
            )
        );

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### Responsabilidade

Gerar tokens JWT para autenticacao de usuarios.

### Metodos

| Metodo | Retorno | Descricao |
|---|---|---|
| `GenerateToken(string email, Guid userId)` | `string` | Gera um token JWT com claims de email e ID |

### Detalhes do Token Gerado

| Parametro | Valor |
|---|---|
| Algoritmo | HMAC-SHA256 |
| Issuer | `Jwt:Issuer` do `appsettings.json` ("Agender") |
| Audience | `Jwt:Audience` do `appsettings.json` ("Agender") |
| Claims | `ClaimTypes.NameIdentifier` (userId), `ClaimTypes.Email` (email) |
| Expiracao | 2 horas (`DateTime.UtcNow.AddHours(2)`) |
| Chave | `Jwt:Key` do `appsettings.json` |

### Dependencias

- `IConfiguration`: Para ler `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` do `appsettings.json`

### Exceptions Lancadas

- `NullReferenceException` se `Jwt:Key` for null (uso de `!` null-forgiving operator)

---

## Observacoes Importantes

### Nao existem outros Services

O projeto possui **apenas um Service**: `JwtService`. Toda a logica de negocio (criacao de eventos, calendarios, validacao de regras, etc.) esta implementada diretamente no `AuthController`.

Nao existem:
- `UserService`
- `EventService`
- `CalendarService`
- `AuthService`

### Logica de negocio no Controller

As seguintes responsabilidades estao no Controller e **nao** em Services separados:

1. **Geracao de Refresh Token**: Metodo privado `GenerateRefreshToken()` no controller
2. **Validacao de login**: Diretamente no endpoint `Login()`
3. **Registro de usuario**: Diretamente no endpoint `Register()`
4. **Criacao de evento**: Diretamente no endpoint `CreateEvent()`
5. **Listagem de eventos**: Diretamente nos endpoints `GetListEvents()` e `GetCalendarEvents()`
6. **Busca de usuario por Name#Code**: Diretamente no endpoint `GetUserInfo()`
7. **Criacao de calendario**: Diretamente no endpoint `CreateCalendar()`
8. **Listagem de calendarios**: Diretamente no endpoint `GetListCalendar()`
9. **Soft delete de participante/entidade**: Diretamente nos endpoints de leave/delete
