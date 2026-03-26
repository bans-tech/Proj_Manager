# Database Approach (Phase 5)

## Stack

- Provider: SQLite
- ORM: Entity Framework Core
- Database file:
  - Development: `backend/Pm.Api/pm.dev.db`
  - Fallback/default: `backend/Pm.Api/pm.db`

## Schema (Normalized)

### `users`
- `id` (GUID, PK)
- `username` (TEXT, required, unique)

### `boards`
- `id` (GUID, PK)
- `user_id` (GUID, FK -> `users.id`, unique)
- `name` (TEXT, required)

### `columns`
- `id` (GUID, PK)
- `board_id` (GUID, FK -> `boards.id`)
- `name` (TEXT, required)
- `position` (INTEGER, required)
- unique index: (`board_id`, `position`)

### `cards`
- `id` (GUID, PK)
- `board_id` (GUID, FK -> `boards.id`)
- `column_id` (GUID, FK -> `columns.id`)
- `title` (TEXT, required)
- `description` (TEXT, required)
- `position` (INTEGER, required)
- unique index: (`column_id`, `position`)

## Migration Strategy

- Migrations are source-controlled under `backend/Pm.Api/Data/Migrations`.
- The API auto-applies migrations only in local/dev environments:
  - `Development`
  - `Local`
- Startup migration path:
  1. `Database.Migrate()`
  2. Seed MVP user (`user`) and default board/columns if missing

## Why Normalized Instead of JSON Blob

- Clear ownership and relational integrity
- Simple future expansion (multi-user, filters, reporting)
- Easier targeted updates (rename column, move card, reorder)

## Current Seed Data

On local/dev startup, if empty, the app creates:
- user: `user`
- board: `Main Board`
- columns: `Backlog`, `Ready`, `In progress`, `Review`, `Done`
