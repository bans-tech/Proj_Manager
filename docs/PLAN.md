# Project Plan (.NET Stack)

This plan is execution-oriented and uses checklists, tests, and explicit success criteria for each phase.

## Phase 1: Finalize Plan and Repo Baseline

Checklist:
- [x] Confirm .NET stack choices from `AGENTS.md`.
- [x] Confirm directory layout (`backend/`, `frontend/`, `scripts/`, `docs/`).
- [x] Add/refresh `frontend/AGENTS.md` describing current frontend behavior and integration expectations.
- [x] Confirm user sign-off before implementation.

Tests:
- [x] N/A (planning phase).

Success Criteria:
- [x] User approves this plan and architecture.
- [x] Repo has clear implementation guidance docs.

## Phase 2: Backend and Container Scaffolding

Checklist:
- [x] Create ASP.NET Core Web API in `backend/`.
- [x] Configure static file hosting and a health endpoint.
- [x] Add multi-stage Dockerfile for combined app runtime.
- [x] Add start/stop scripts for Windows (`.ps1`), macOS/Linux (`.sh`) in `scripts/`.
- [x] Validate app runs locally in Docker.

Tests:
- [x] Backend unit test project created and test runner working.
- [x] Integration test confirms `GET /api/health` returns 200.
- [x] Docker run test confirms app starts and responds.

Success Criteria:
- [x] `docker` startup serves sample content and API responds.
- [x] Start/stop scripts work on target OS shells.

## Phase 3: Frontend Integration into ASP.NET Core

Checklist:
- [x] Integrate the Blazor frontend into the hosted app flow at `/`.
- [x] Configure ASP.NET Core to serve the frontend at `/`.
- [x] Ensure client-side routes (if any) fall back correctly.

Tests:
- [x] Integration test confirms `/` returns frontend app shell.
- [x] Smoke test confirms static JS/CSS assets are served.

Success Criteria:
- [x] Blazor Kanban UI renders from ASP.NET host.
- [x] No standalone frontend server required in MVP runtime.

## Phase 4: MVP Authentication Flow

Checklist:
- [ ] Implement login endpoint with hardcoded credentials: `user` / `password`.
- [ ] Implement session/cookie-based auth middleware.
- [ ] Add logout endpoint.
- [ ] Frontend login gate: unauthenticated users see login first.

Tests:
- [ ] Unit tests for auth service credential checks.
- [ ] Integration tests for login success/failure and logout.
- [ ] Frontend test verifies login gate behavior.

Success Criteria:
- [ ] Unauthenticated users cannot access Kanban API routes.
- [ ] Login enables Kanban UI; logout returns to login screen.

## Phase 5: Database Model and Persistence

Checklist:
- [ ] Add SQLite and EF Core setup.
- [ ] Define normalized entities for `User`, `Board`, `Column`, and `Card`.
- [ ] Ensure migrations auto-apply only in local/dev.
- [ ] Document schema and persistence decisions in `docs/`.

Tests:
- [ ] Unit tests for repository/service operations.
- [ ] Integration tests using SQLite test DB for create/read/update flows.

Success Criteria:
- [ ] DB file is created automatically if missing.
- [ ] Kanban state persists across app restarts.

## Phase 6: Kanban Backend API

Checklist:
- [ ] Add endpoints to fetch board state for signed-in user.
- [ ] Add endpoints to update columns/cards and card movement.
- [ ] Add validation and minimal error handling.

Tests:
- [ ] Integration tests cover read/update/move operations.
- [ ] Authorization tests confirm user isolation.

Success Criteria:
- [ ] API fully supports Kanban CRUD/move operations required by UI.
- [ ] Board updates are persisted reliably.

## Phase 7: Frontend to Backend Wiring

Checklist:
- [ ] Replace mock/local state persistence with backend API calls.
- [ ] Keep drag/drop and edit UX functional with server-backed state.
- [ ] Add loading/error handling for API interactions.

Tests:
- [ ] Blazor component/unit tests for API integration points.
- [ ] End-to-end flow test: login, load board, edit card, move card, refresh persists state.

Success Criteria:
- [ ] Kanban behavior remains intact with persistent backend data.
- [ ] Page refresh shows latest saved board state.

## Phase 8: OpenRouter Connectivity

Checklist:
- [ ] Add OpenRouter client in backend service layer.
- [ ] Load `OPENROUTER_API_KEY` from environment.
- [ ] Use model `openai/gpt-oss-120b`.
- [ ] Add a simple connectivity check path (for development/testing).

Tests:
- [ ] Integration test with mocked HTTP for OpenRouter client behavior.
- [ ] Manual connectivity test using prompt `2+2`.

Success Criteria:
- [ ] Backend can successfully call OpenRouter with configured model.
- [ ] Error paths are logged and returned cleanly.

## Phase 9: Structured AI Kanban Actions

Checklist:
- [ ] Send Kanban JSON + user prompt + conversation history to AI.
- [ ] Define strict response schema with:
  - `assistantMessage`
  - optional `kanbanOperations` (create/edit/move/delete card, create/rename/reorder/delete column)
- [ ] Validate AI responses against schema before applying changes.
- [ ] Reject unknown fields and invalid operation payloads.
- [ ] Apply valid operations transactionally and persist.

Tests:
- [ ] Unit tests for schema parsing/validation.
- [ ] Integration tests for operation application rules and failures.
- [ ] Security tests for rejecting malformed/unexpected operation payloads.

Success Criteria:
- [ ] AI can reliably suggest chat replies and optional Kanban updates.
- [ ] Invalid AI output never corrupts board state.

## Phase 10: AI Sidebar UX

Checklist:
- [ ] Build sidebar chat UI in frontend with message history.
- [ ] Connect chat submit to backend AI endpoint.
- [ ] Reflect AI-generated Kanban updates in UI immediately.
- [ ] Keep UX responsive with clear loading/error states.

Tests:
- [ ] Blazor component tests for chat render/send/error states.
- [ ] End-to-end scenario: user asks AI to update cards; board updates and rerenders.

Success Criteria:
- [ ] Sidebar supports practical chat workflows.
- [ ] Kanban updates from AI appear automatically without manual reload.

## Definition of Done (MVP)

- [ ] App runs locally via Docker in one container.
- [ ] Hardcoded login flow works end to end.
- [ ] One-board-per-user persistence works with SQLite.
- [ ] Drag/drop Kanban edits are persisted through backend APIs.
- [ ] AI sidebar can chat and apply valid structured Kanban updates.
- [ ] Core automated tests are green for backend, frontend, and integration paths.
