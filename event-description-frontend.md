# Descrição em Eventos — O que o Frontend precisa fazer

## 1. Ao criar um evento (`POST /api/auth/createEvent`)

O campo `Description` agora é aceito no body da requisição.

**Antes:**
```json
{
  "name": "Reunião",
  "date": "2026-07-05",
  "color": "#ff0000",
  "calendar_id": "uuid-opcional",
  "users_ids": ["uuid1", "uuid2"]
}
```

**Depois (campo opcional):**
```json
{
  "name": "Reunião",
  "date": "2026-07-05",
  "description": "Reunião para alinhar as metas do trimestre",
  "color": "#ff0000",
  "calendar_id": "uuid-opcional",
  "users_ids": ["uuid1", "uuid2"]
}
```

> Se não for enviado, o banco salva como `null`.

---

## 2. Ao listar eventos (`GET /api/auth/getListEvents` e `GET /api/auth/getCalendarEvents?calendarId=...`)

O response agora inclui `description` em cada evento:

```json
{
  "data": [
    {
      "id": "uuid",
      "name": "Reunião",
      "date": "2026-07-05",
      "description": "Reunião para alinhar as metas do trimestre", // NOVO
      "color": "#ff0000",
      "calendarId": "uuid-opcional",
      "createdAt": "...",
      "updatedAt": null,
      "deletedAt": null,
      "participants": [...]
    }
  ]
}
```

> O campo pode vir como `null` ou `string`, dependendo se foi preenchido na criação.

---

## 3. UI — Sugestão de implementação

### Criar/Editar evento
- Adicionar um campo de texto (textarea ou input) para "Descrição" no formulário de criação.
- Deve ser opcional — sem validação obrigatória.
- Placeholder sugerido: `"Adicionar descrição..."`

### Card/Item do evento na listagem
- Exibir a descrição abaixo do nome do evento, em fonte menor e cor mais clara.
- Se `description` for `null` ou vazio, não renderizar nada.

Exemplo:
```
📅 Reunião
   Reunião para alinhar as metas do trimestre   ← descrição (opcional)
   🕐 05/07/2026
```

---

## Resumo das mudanças

| Onde            | O que mudou                           |
|-----------------|----------------------------------------|
| Request (criar) | Campo `description` opcional aceito    |
| Response (listar)| Campo `description` incluso no objeto |
| Banco           | Coluna `Description` (TEXT, nullable)  |
