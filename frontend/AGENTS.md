# Frontend Scope (Blazor)

## Current State

This folder contains the active Blazor frontend implementation.

## Target State

- Frontend framework: Blazor.
- Frontend is served by ASP.NET Core at `/`.
- Frontend communicates with backend JSON APIs for auth, Kanban operations, and AI chat.

## MVP Requirements

- Login screen using hardcoded credentials flow (`user` / `password`) via backend cookie auth.
- Kanban board UI with fixed columns (renamable), drag/drop cards, card edit support.
- AI sidebar chat that can trigger structured Kanban updates returned by backend.

## Testing Expectations

- Blazor component/unit test stack compatible with the chosen .NET test framework.
- Add focused E2E tests only for critical user journeys.

## Implementation Notes

- Keep the UI implementation simple and maintainable.
- Preserve the project color scheme from root `AGENTS.md`.
- Avoid adding frontend-only persistence once backend APIs are available.
