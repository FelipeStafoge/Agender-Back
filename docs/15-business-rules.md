# 15 - Regras de Negocio

## Modulo: Autenticacao e Usuarios

### Registro
1. Email deve ser unico no sistema (`AuthController.cs:87`)
2. Ao registrar, um codigo de usuario (`UserCode`) de 4 digitos e gerado aleatoriamente (`AuthController.cs:99`)
3. A combinacao `Name + UserCode` deve ser unica no sistema (`AuthController.cs:101-104` e `User.cs:5`)
4. Ao registrar, um calendario pessoal ("Meus Eventos") e criado automaticamente (`AuthController.cs:121-131`)
5. O calendario pessoal tem `IsPersonal = true` e cor padrao `#7c3aed` (`AuthController.cs:126-128`)
6. O usuario e adicionado como Owner do seu calendario pessoal (`AuthController.cs:134-141`)
7. Senha e armazenada e comparada em texto puro - sem hash (`AuthController.cs:43-44`)

### Login
8. O login e feito por email e senha (comparacao em texto puro) (`AuthController.cs:42-44`)
9. Ao fazer login, um novo RefreshToken e gerado (64 bytes aleatorios, Base64) com expiracao de 7 dias (`AuthController.cs:56-61`)
10. Em caso de credenciais invalidas, retorna codigo `wrong_password_or_email` (`AuthController.cs:48-52`)

### Token
11. JWT expira em 2 horas (`JwtService.cs:42`)
12. Refresh Token expira em 7 dias (`AuthController.cs:60`)
13. Refresh Token nao e revogado apos uso - apenas se verifica se nao esta expirado e nao revogado (`AuthController.cs:173-175`)
14. Refresh Token expirado retorna 401 (`AuthController.cs:182-185`)
15. Se o usuario associado ao Refresh Token nao existe mais, retorna 401 (`AuthController.cs:191-194`)

---

## Modulo: Eventos

### Criacao
16. O criador do evento e automaticamente adicionado como participante com `Role = "Owner"` (`AuthController.cs:258-266`)
17. Usuarios convidados sao adicionados como `Role = "Member"` (`AuthController.cs:274-281`)
18. Se `calendar_id` for fornecido e o calendario existir, a cor do evento e **substituida** pela `DefaultColor` do calendario (`AuthController.cs:230-238`)
19. Se o calendario nao existir ou `calendar_id` nao for fornecido, usa a cor enviada no request (`AuthController.cs:237`)
20. IDs de usuario invalidos (que nao sao GUIDs) sao **ignorados silenciosamente** - nao geram erro (`AuthController.cs:271-272`)
21. Um evento pode existir sem calendario (`CalendarId = null`) ou vinculado a um calendario (`AuthController.cs:221-226`)

### Listagem
22. Um usuario ve eventos onde e participante direto (evento sem calendario) (`AuthController.cs:310-311`)
23. Um usuario ve eventos de calendarios onde e participante (evento com calendario) (`AuthController.cs:312`)
24. O filtro de data (`startDate`, `endDate`) compara strings no formato `YYYYMMDD` extraidas de `DD/MM/YYYY` (`AuthController.cs:316-328`)
25. A listagem de eventos de calendario (`getCalendarEvents`) requer que o usuario seja Owner **OU** participante do calendario (`AuthController.cs:378-383`)
26. Usuario sem acesso ao calendario recebe `403 Forbidden` (`AuthController.cs:383`)

### Exclusao / Saida
27. Apenas o criador do evento (`AccountId`) pode deleta-lo (`AuthController.cs:668-669`)
28. Sair do evento (leave) remove a participacao do usuario via soft delete (`AuthController.cs:643`)
29. Deletar evento aplica soft delete (`DeletedAt = DateTime.UtcNow`) (`AuthController.cs:671`)
30. Ao deletar um evento, as participacoes nao sao soft-deletadas automaticamente (apenas o evento e marcado)

---

## Modulo: Calendarios

### Criacao
31. O criador do calendario e automaticamente Owner (`AuthController.cs:486-495`)
32. O `OwnerId` do calendario e armazenado como string do GUID (`AuthController.cs:478`)
33. Calendarios criados manualmente tem `IsPersonal = false` (`AuthController.cs:479`)
34. Usuarios convidados sao adicionados como `Role = "Member"` (`AuthController.cs:508-510`)
35. IDs de usuario invalidos sao ignorados silenciosamente (`AuthController.cs:500-501`)
36. Nao ha validacao de nome de calendario duplicado

### Listagem
37. Um usuario ve calendarios onde e Owner (`OwnerId == accountId`) **OU** participante (`AuthController.cs:544`)
38. Nao ha filtro/paginacao na listagem de calendarios

### Exclusao / Saida
39. Apenas o criador do calendario (`AccountId`) pode deleta-lo (`AuthController.cs:615`)
40. Usuario nao-criador tentando deletar recebe `403 Forbidden` (`AuthController.cs:616`)
41. Deletar calendario aplica soft delete (`DeletedAt = DateTime.UtcNow`) (`AuthController.cs:618`)
42. Ao deletar um calendario, eventos vinculados tem `CalendarId` setado como NULL (`OnDelete SetNull`) (`AppDbContext.cs:53`)
43. Sair do calendario (leave) remove a participacao via soft delete (`AuthController.cs:590`)
44. Nao e possivel sair de um calendario onde nao se e participante (retorna 404) (`AuthController.cs:587-588`)

---

## Modulo: Participantes

45. Um usuario pode ser Owner de varios eventos/calendarios
46. Um usuario pode ser Member de varios eventos/calendarios
47. Nao ha validacao que impede um usuario de ser adicionado como participante duplicado para Event (chave composta `(EventId, UserId)` garante unicidade) (`Event.cs:19`)
48. Para Calendar, o indice unico `(CalendarId, UserId)` garante unicidade de participacao (`Calendar.cs:32-33` com Fluent API)
49. Roles sao "Owner" ou "Member" - strings, sem enum

---

## Modulo: Busca de Usuario

50. Busca de usuario e feita pelo formato `Nome#Codigo` (ex: `Felipe#1234`) (`AuthController.cs:435-443`)
51. Se o formato for invalido (nao contem `#` exatamente uma vez), retorna 400 (`AuthController.cs:437-439`)
52. Se usuario nao encontrado, retorna 404 (`AuthController.cs:448-450`)

---

## Modulo: Soft Delete

53. Todas as entidades principais usam soft delete (`DeletedAt != null`) com Global Query Filter (`AppDbContext.cs:55-59`)
54. `RefreshToken` e a unica entidade sem soft delete (nao possui `DeletedAt`)
55. As operacoes de leave/delete nunca removem registros fisicamente
56. Registros com soft delete sao invisiveis para todas as queries automaticamente
57. Nao ha endpoint para "restaurar" registros soft-deletados

---

## Regras Ausentes (nao implementadas)

- Nao ha limite de eventos por calendario
- Nao ha limite de participantes por evento/calendario
- Nao ha validacao de senha (tamanho minimo, complexidade)
- Nao ha verificacao de email (confirmacao)
- Nao ha protecao contra brute force (rate limiting)
- Nao ha validacao de datas (evento no passado, etc.)
- Nao ha notificacoes (convites, alteracoes)
- Nao ha eventos recorrentes
- Nao ha permissoes granulares (apenas Owner/Member)
