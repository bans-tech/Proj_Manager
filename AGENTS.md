# The Project Management MVP Web App (.NET Stack)

## Business Requirements

This project is building a Project Management app. Key features:
- A user can sign in.
- When signed in, the user sees a Kanban board representing their project.
- The Kanban board has fixed columns that can be renamed.
- The cards on the Kanban board can be moved with drag and drop, and edited.
- There is an AI chat feature in a sidebar; the AI can create, edit, or move one or more cards.

## MVP Limitations

- Authentication is a single hardcoded login (`user` / `password`) for MVP UX only, while the database schema supports multiple users.
- Each signed-in user has exactly one Kanban board in MVP.
- The app runs locally in Docker for MVP.

## Technical Decisions (.NET)

- .NET 9 stack.
- ASP.NET Core hosts everything:
  - API endpoints for auth, Kanban, and AI chat.
  - Static file hosting for the frontend at `/`.
- Frontend:
  - Blazor frontend in `frontend/`.
  - Blazor app is served by ASP.NET Core at `/`.
  - Frontend calls backend JSON APIs.
- Persistence:
  - SQLite local database file.
  - Normalized relational schema (`Users`, `Boards`, `Columns`, `Cards`).
  - Auto-migrate only in local/dev.
- AI:
  - OpenRouter for model calls.
  - `OPENROUTER_API_KEY` loaded from `.env`.
  - Model: `openai/gpt-oss-120b`.
  - Strict server-side schema validation for AI responses; reject unknown fields.
- Packaging/runtime:
  - Single Docker image/container for local MVP runs.
- Scripts:
  - Start/stop scripts for Windows, macOS, Linux in `scripts/`.

## Starting Point

A working frontend-only Kanban demo already exists in `frontend/`. It must be integrated into the .NET-hosted app and wired to backend APIs.

## Color Scheme

- Accent Yellow: `#ecad0a` (accent lines, highlights)
- Blue Primary: `#209dd7` (links, key sections)
- Purple Secondary: `#753991` (submit buttons, important actions)
- Dark Navy: `#032147` (main headings)
- Gray Text: `#888888` (supporting text, labels)

## Coding Standards

1. Use current stable .NET and library versions with idiomatic patterns.
2. Keep it simple: no over-engineering, no unnecessary defensive programming, no extra features.
3. Keep docs concise; README stays minimal; no emojis.
4. When issues occur, identify and prove root cause before fixing.

## Working Documentation

- Planning and execution docs live under `docs/`.
- Review `docs/PLAN.md` before implementation work.
