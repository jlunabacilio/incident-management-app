# Project Build Conversation Log

This document captures the full conversation that led to the creation of the **Incident Management App** — an aviation maintenance incident registration system.

---

## 1. README Update

**User:** Help me modify the README to be more consistent with the specs content. Must be in English.

**Kiro:** Read the existing README (just a title) and all three spec files:
- `.kiro/specs/maintenance-incident-registration/requirements.md`
- `.kiro/specs/maintenance-incident-registration/design.md`
- `.kiro/specs/maintenance-incident-registration/tasks.md`

Rewrote the README to cover:
- Project purpose and regulatory context (EASA Part-M, FAA AC 43-9C)
- Feature summary (incident registration, automatic dispatch block, lifecycle, RBAC, audit trail, notifications)
- Architecture diagram from the design doc
- Data model with all entities, fields, and enumerations
- Full API table with paths and minimum required roles
- Critical flow walkthrough for the CRITICAL → GROUNDED transaction
- Non-functional requirements (atomicity, idempotency, audit immutability, retention)
- Implementation status table linking to spec tasks

---

## 2. How to Start Building

**User:** Looks like all the required files are ready and prepared, how do you suggest to me to start creating the maintenance incident registration? I need the UI as well.

**Kiro:** Recommended build order — inside out:

**Phase 1 — Backend Foundation**
1. Scaffold .NET solution (5 projects with Clean Architecture)
2. Domain layer — entities, enums, state machine, domain events
3. Infrastructure — EF Core DbContext, SQLite, IStorageService, repositories
4. Application — CQRS commands/queries with MediatR
5. API — controllers, JWT middleware, role authorization

**Phase 2 — Frontend**
6. Scaffold Next.js 15 with App Router, Tailwind, Zustand, TypeScript strict
7. Types and API client mirroring backend DTOs
8. Components — IncidentForm, IncidentList, IncidentDetail, dashboard cards
9. Pages — /incidents, /incidents/new, /incidents/[id]

**Phase 3 — Tests**
10. xUnit integration tests for CRITICAL transaction atomicity and state machine
11. Vitest for frontend form validation

Proposed **Option A** (full scaffold in one shot) vs **Option B** (phase by phase). User chose Option A.

---

## 3. Full Scaffold — Attempt via CLI

**User:** Please go with option A.

**Kiro:** Discovered .NET wasn't on PATH — installed at `$HOME/.dotnet/dotnet`. Identified two CLI issues:
1. `dotnet` not on PATH — needed full path `$HOME/.dotnet/dotnet`
2. `cwd` parameter mangled on WSL — used absolute paths inline instead

Successfully:
- Created solution file `IncidentReport.sln`
- Created 5 projects: Domain, Application, Infrastructure, API, Tests
- Added all project references (Clean Architecture dependency direction)
- Installed NuGet packages: MediatR 12.4.1, FluentValidation, EF Core SQLite, JWT Bearer, BCrypt, Moq, FluentAssertions
- Fixed MediatR version conflict (removed `MediatR.Extensions.Microsoft.DependencyInjection 11.1.0` — built-in DI in MediatR 12)

**User cancelled** the CLI approach mid-way through frontend scaffolding.

---

## 4. Full Scaffold — Direct File Writing

**User:** Cancel. Please incorporate this guidance and try a different approach.

**Kiro:** Switched to writing all files directly without CLI scaffolding.

### Backend Files Written

**Domain Layer** (`IncidentReport.Domain/`)
- `Enums/Severity.cs` — Low, Medium, High, Critical
- `Enums/FaultType.cs` — Mechanical, Electrical, Hydraulic, Avionics
- `Enums/IncidentStatus.cs` — Open, InProgress, PendingReview, Resolved, Closed, Voided
- `Enums/AircraftStatus.cs` — Available, InService, Grounded, Maintenance
- `Enums/UserRole.cs` — Technician, Supervisor, MaintenanceControl, SafetyOfficer, ChiefEngineer, Admin
- `Entities/Incident.cs` — full domain entity with validation, state transitions, attachment management
- `Entities/IncidentAttachment.cs` — value object with MIME type and size validation
- `Entities/Aircraft.cs` — with Ground() and Unground() methods
- `Entities/AuditEvent.cs` — immutable after construction
- `Entities/AppUser.cs` — with role and active status
- `Entities/IncidentStateMachine.cs` — valid transitions and role requirements

**Application Layer** (`IncidentReport.Application/`)
- `Interfaces/IIncidentRepository.cs`
- `Interfaces/IAircraftRepository.cs`
- `Interfaces/IAuditRepository.cs`
- `Interfaces/IUserRepository.cs`
- `Interfaces/IStorageService.cs`
- `Interfaces/ITokenService.cs`
- `DTOs/IncidentDto.cs` — IncidentDto, AttachmentDto, IncidentSummaryDto, DashboardSummaryDto
- `DTOs/AuthDto.cs` — LoginRequest, LoginResponse
- `Commands/CreateIncidentCommand.cs`
- `Commands/UpdateIncidentCommand.cs`
- `Commands/TransitionIncidentCommand.cs`
- `Commands/VoidIncidentCommand.cs`
- `Commands/UngroundAircraftCommand.cs`
- `Queries/GetIncidentsQuery.cs` — with PagedResult
- `Queries/GetIncidentByIdQuery.cs`
- `Queries/GetDashboardSummaryQuery.cs`
- `Queries/GetAuditTrailQuery.cs`
- `Handlers/CreateIncidentHandler.cs` — atomic CRITICAL dispatch block
- `Handlers/UpdateIncidentHandler.cs`
- `Handlers/TransitionIncidentHandler.cs` — auto-ungrounds on Close of CRITICAL
- `Handlers/VoidIncidentHandler.cs`
- `Handlers/UngroundAircraftHandler.cs`
- `Handlers/GetIncidentsHandler.cs`
- `Handlers/GetIncidentByIdHandler.cs`
- `Handlers/GetDashboardSummaryHandler.cs`
- `Handlers/GetAuditTrailHandler.cs`

**Infrastructure Layer** (`IncidentReport.Infrastructure/`)
- `Persistence/AppDbContext.cs` — EF Core DbContext with all entity configs, indexes, and seed data
- `Repositories/IncidentRepository.cs` — with full filter/pagination support
- `Repositories/AircraftRepository.cs`
- `Repositories/AuditRepository.cs` — append-only
- `Repositories/UserRepository.cs`
- `Storage/LocalFileStorageService.cs` — saves to `/uploads/{incidentId}/{filename}`
- `Auth/JwtTokenService.cs` — generates JWT with role claims
- `DependencyInjection.cs` — registers all services

**API Layer** (`IncidentReport.API/`)
- `Controllers/IncidentsController.cs` — full CRUD + transition + void + audit + dashboard
- `Controllers/AircraftController.cs` — status + unground
- `Controllers/AuthController.cs` — login with BCrypt verification
- `Middleware/ExceptionMiddleware.cs` — maps domain exceptions to HTTP status codes
- `Program.cs` — JWT auth, CORS, MediatR, EnsureCreated, static file serving
- `appsettings.json`

### Frontend Files Written

**Types & Lib**
- `types/index.ts` — all DTOs, enums, form inputs mirroring backend
- `lib/api.ts` — typed fetch client for all endpoints
- `lib/auth.ts` — localStorage token management
- `lib/utils.ts` — severity/status color helpers, date/byte formatters
- `lib/stateMachine.ts` — client-side transition rules matching backend

**Hooks**
- `hooks/useAuth.ts` — login, logout, persisted user state
- `hooks/useIncidents.ts` — paginated incident list + dashboard data

**UI Components** (`components/ui/`)
- `Button.tsx` — primary, secondary, danger, ghost variants with loading state
- `Input.tsx` — Input, Select, Textarea, FormField with error display
- `Badge.tsx` — SeverityBadge (color-coded), StatusBadge
- `Modal.tsx` — accessible modal with Escape key support

**Incident Components** (`components/incidents/`)
- `IncidentCard.tsx` — card with severity/status badges, CRITICAL highlight
- `IncidentForm.tsx` — full registration form with CRITICAL warning banner, drag-and-drop photo upload, client-side validation
- `IncidentFiltersPanel.tsx` — filter bar with all search fields
- `AuditTimeline.tsx` — chronological audit event list

**Pages** (`app/`)
- `layout.tsx` — root layout with metadata
- `globals.css` — Tailwind import
- `page.tsx` — fleet dashboard with severity counts, grounded aircraft alert, quick links
- `login/page.tsx` — login form with demo credentials shown
- `incidents/page.tsx` — paginated incident list with filters and pagination
- `incidents/new/page.tsx` — new incident registration page
- `incidents/[id]/page.tsx` — detail view with transitions, void modal, audit timeline, unground action

---

## 5. Bug Fixes

### Fix 1 — EF Core Seed Data Type Mismatch

**Error:**
```
System.InvalidOperationException: The seed entity for entity type 'Aircraft' cannot be added
because the value provided for the property 'OperationalStatus' is not compatible with the
property type 'IncidentReport.Domain.Enums.AircraftStatus'.
```

**Cause:** Seed data used string values (`"Available"`) but EF Core's `HasData` bypasses value converters and requires the actual enum type.

**Fix:** Changed seed data to use enum values directly:
```csharp
// Before
new { OperationalStatus = "Available", Role = "Technician" }

// After
new { OperationalStatus = AircraftStatus.Available, Role = UserRole.Technician }
```

### Fix 2 — Static Files Illegal Path

**Error:**
```
Unable to add filesystem: <illegal path>
```

**Cause:** `PhysicalFileProvider` throws if the `uploads/` directory doesn't exist at startup.

**Fix:** Create the directory before registering the provider:
```csharp
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
```

### Fix 3 — No EF Migrations (Migrate() vs EnsureCreated())

**Cause:** `db.Database.Migrate()` was called but no migrations had been generated.

**Fix:** Switched to `db.Database.EnsureCreated()` for local SQLite dev — creates schema and seeds data automatically on first run.

### Fix 4 — Wrong Port (5121 vs 5000)

**Cause:** The `launchSettings.json` generated by `dotnet new webapi` defaulted to port 5121. The frontend `.env.local` pointed to port 5000.

**Fix:** Updated `launchSettings.json` to pin port 5000:
```json
"applicationUrl": "http://localhost:5000"
```

---

## 6. .gitignore

Created root `.gitignore` covering:
- .NET: `bin/`, `obj/`, `*.db`, `.vs/`, `.idea/`
- Node/Next.js: `node_modules/`, `.next/`, `.env*.local`
- Uploads: `src/backend/IncidentReport.API/uploads/`
- OS: `.DS_Store`, `Thumbs.db`

---

## Running the App

```bash
# Backend — Terminal 1
export PATH="$PATH:$HOME/.dotnet"
cd src/backend
dotnet run --project IncidentReport.API
# → http://localhost:5000

# Frontend — Terminal 2
cd src/frontend
npm run dev
# → http://localhost:3000
```

### Demo Credentials

| Email | Password | Role |
|-------|----------|------|
| tech@demo.com | Password123! | Technician |
| supervisor@demo.com | Password123! | Supervisor |
| safety@demo.com | Password123! | SafetyOfficer |

---

## Tech Stack Summary

| Layer | Technology |
|-------|-----------|
| Backend framework | .NET 8, ASP.NET Core Web API |
| Architecture | Clean Architecture + CQRS (MediatR) |
| ORM | Entity Framework Core 8 |
| Database | SQLite (local dev) |
| Auth | JWT Bearer + BCrypt |
| Validation | FluentValidation |
| File storage | Local filesystem (`/uploads`) → swap to S3 |
| Frontend framework | Next.js 15 (App Router) |
| Language | TypeScript (strict) |
| Styles | Tailwind CSS |
| State | Zustand-ready hooks |
| Icons | lucide-react |
| Tests | xUnit (backend), Vitest (frontend) |
