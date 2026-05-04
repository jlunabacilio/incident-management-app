# Incident Management App

A safety-critical module for registering, tracking, and managing maintenance incidents against aircraft. Built for aviation maintenance personnel, it enforces regulatory compliance (EASA Part-M, FAA AC 43-9C) and guarantees that any aircraft with an open CRITICAL incident is automatically blocked from dispatch.

---

## Features

- **Incident registration** — Log faults with tail number, fault type, severity, description, steps to reproduce, technician, supervisor, and up to 10 photo attachments.
- **Automatic dispatch block** — When a CRITICAL incident is created, the aircraft is immediately set to `GROUNDED` within the same database transaction. No partial states are allowed.
- **Incident lifecycle** — Incidents progress through `Open → InProgress → PendingReview → Resolved → Closed`, with role-based controls on each transition. Incidents can also be `Voided` with justification.
- **Role-based access control** — Six roles: `Technician`, `Supervisor`, `MaintenanceControl`, `SafetyOfficer`, `ChiefEngineer`, `Admin`. All API endpoints enforce JWT authentication and role permissions server-side.
- **Search and filtering** — Filter incidents by tail number, fault type, severity, status, date range, technician, and supervisor with pagination.
- **Dashboard** — Summary of open incidents grouped by severity, including list of grounded aircraft.
- **Audit trail** — Every create, update, and state transition is recorded in an append-only audit log.
- **Notifications** — CRITICAL incidents trigger automated alerts to the Shift Supervisor, Maintenance Control Center, and Safety Officer.

---

## Architecture

```
Frontend (Next.js 15 / App Router)
        │ HTTPS / REST + JWT
Backend (.NET 8 / Clean Architecture / CQRS + MediatR)
  ├── IncidentReport.API          → Controllers, Middleware, Program.cs
  ├── IncidentReport.Application  → Commands, Queries, Handlers, DTOs, Interfaces
  ├── IncidentReport.Domain       → Entities, Enums, State Machine
  └── IncidentReport.Infrastructure → EF Core (SQLite), Repositories, Storage, Auth
```

---

## Running Locally

**Backend:**

```bash
cd src/backend
dotnet run --project IncidentReport.API
```

API runs at: `http://localhost:5000`

**Frontend:**

```bash
cd src/frontend
npm install
npm run dev
```

App runs at: `http://localhost:3000`

---

## Backend — API Reference

All endpoints require JWT authentication (via `Authorization: Bearer <token>` header) unless noted otherwise.

### Auth

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| `POST` | `/api/v1/auth/login` | None | Authenticate and receive JWT |

**Request:**

```json
{ "email": "string", "password": "string" }
```

**Response (200):**

```json
{
  "token": "string",
  "userId": "string",
  "fullName": "string",
  "role": "Technician | Supervisor | MaintenanceControl | SafetyOfficer | ChiefEngineer | Admin",
  "expiresAt": "ISO 8601 datetime"
}
```

---

### Incidents

| Method | Path | Min Role | Description |
|--------|------|----------|-------------|
| `POST` | `/api/v1/incidents` | Technician | Create a new incident (multipart/form-data) |
| `GET` | `/api/v1/incidents` | Technician | List incidents with filters and pagination |
| `GET` | `/api/v1/incidents/:id` | Technician | Get full incident detail |
| `PATCH` | `/api/v1/incidents/:id` | Technician | Update editable fields |
| `POST` | `/api/v1/incidents/:id/transition` | Supervisor+ | Transition incident status |
| `POST` | `/api/v1/incidents/:id/void` | Supervisor+ | Void an incident with justification |
| `GET` | `/api/v1/incidents/:id/audit` | MaintenanceControl+ | Get audit trail for an incident |
| `GET` | `/api/v1/incidents/dashboard` | Technician | Get dashboard summary |

**Create Incident — `POST /api/v1/incidents`**

Content-Type: `multipart/form-data`

| Field | Type | Required |
|-------|------|----------|
| `aircraftTailNumber` | string | Yes |
| `faultType` | `Mechanical \| Electrical \| Hydraulic \| Avionics` | Yes |
| `severity` | `Low \| Medium \| High \| Critical` | Yes |
| `description` | string | Yes |
| `stepsToReproduce` | string | Yes |
| `technicianId` | string | Yes |
| `supervisorId` | string | Yes |
| `attachments` | File[] (max 10, 10 MB each) | No |

**Response (201) — IncidentDto:**

```json
{
  "id": "guid",
  "incidentNumber": "INC-YYYY-NNNNNN",
  "aircraftTailNumber": "string",
  "faultType": "string",
  "severity": "string",
  "status": "string",
  "description": "string",
  "stepsToReproduce": "string",
  "technicianId": "string",
  "supervisorId": "string",
  "createdBy": "string",
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601",
  "resolvedAt": "ISO 8601 | null",
  "closedAt": "ISO 8601 | null",
  "attachments": [
    {
      "id": "guid",
      "fileName": "string",
      "mimeType": "string",
      "sizeBytes": 0,
      "uploadedAt": "ISO 8601",
      "uploadedBy": "string",
      "url": "string | null"
    }
  ]
}
```

**List Incidents — `GET /api/v1/incidents`**

Query parameters: `tailNumber`, `faultType`, `severity`, `status`, `dateFrom`, `dateTo`, `technicianId`, `supervisorId`, `page` (default 1), `pageSize` (default 20).

**Response (200) — PagedResult\<IncidentSummaryDto\>:**

```json
{
  "items": [
    {
      "id": "guid",
      "incidentNumber": "string",
      "aircraftTailNumber": "string",
      "faultType": "string",
      "severity": "string",
      "status": "string",
      "technicianId": "string",
      "createdAt": "ISO 8601"
    }
  ],
  "total": 0,
  "page": 1,
  "pageSize": 20
}
```

**Update Incident — `PATCH /api/v1/incidents/:id`**

```json
{
  "description": "string",
  "stepsToReproduce": "string",
  "technicianId": "string",
  "supervisorId": "string"
}
```

**Transition — `POST /api/v1/incidents/:id/transition`**

```json
{ "newStatus": "Open | InProgress | PendingReview | Resolved | Closed" }
```

**Void — `POST /api/v1/incidents/:id/void`**

```json
{ "justification": "string" }
```

**Dashboard — `GET /api/v1/incidents/dashboard`**

```json
{
  "totalOpen": 0,
  "lowCount": 0,
  "mediumCount": 0,
  "highCount": 0,
  "criticalCount": 0,
  "groundedAircraft": ["string"]
}
```

---

### Aircraft

| Method | Path | Min Role | Description |
|--------|------|----------|-------------|
| `GET` | `/api/v1/aircraft/:tailNumber/status` | Technician | Get aircraft operational status |
| `POST` | `/api/v1/aircraft/:tailNumber/unground` | SafetyOfficer+ | Unground an aircraft |

**Get Status — Response (200):**

```json
{
  "tailNumber": "string",
  "status": "Available | InService | Grounded | Maintenance",
  "groundedByIncidentId": "guid | null",
  "groundedAt": "ISO 8601 | null"
}
```

**Unground — Response (200):**

```json
{ "message": "Aircraft 'XX-YYY' has been ungrounded." }
```

---

## Backend — Domain Model

### Entities

| Entity | Location |
|--------|----------|
| `Incident` | `Domain/Entities/Incident.cs` |
| `IncidentAttachment` | `Domain/Entities/IncidentAttachment.cs` |
| `Aircraft` | `Domain/Entities/Aircraft.cs` |
| `AppUser` | `Domain/Entities/AppUser.cs` |
| `AuditEvent` | `Domain/Entities/AuditEvent.cs` |
| `IncidentStateMachine` | `Domain/Entities/IncidentStateMachine.cs` |

### Enumerations

| Enum | Values |
|------|--------|
| `FaultType` | `Mechanical`, `Electrical`, `Hydraulic`, `Avionics` |
| `Severity` | `Low`, `Medium`, `High`, `Critical` |
| `IncidentStatus` | `Open`, `InProgress`, `PendingReview`, `Resolved`, `Closed`, `Voided` |
| `AircraftStatus` | `Available`, `InService`, `Grounded`, `Maintenance` |
| `UserRole` | `Technician`, `Supervisor`, `MaintenanceControl`, `SafetyOfficer`, `ChiefEngineer`, `Admin` |

### CQRS — Commands & Queries

| Type | Name | Description |
|------|------|-------------|
| Command | `CreateIncidentCommand` | Creates incident, grounds aircraft if Critical |
| Command | `UpdateIncidentCommand` | Updates editable fields |
| Command | `TransitionIncidentCommand` | Moves incident to next status |
| Command | `VoidIncidentCommand` | Voids incident with justification |
| Command | `UngroundAircraftCommand` | Releases aircraft from grounded state |
| Query | `GetIncidentsQuery` | Paginated list with filters |
| Query | `GetIncidentByIdQuery` | Single incident detail |
| Query | `GetAuditTrailQuery` | Audit events for an entity |
| Query | `GetDashboardSummaryQuery` | Open incident counts by severity |

### Service Interfaces

| Interface | Purpose |
|-----------|---------|
| `IIncidentRepository` | Incident persistence |
| `IAircraftRepository` | Aircraft lookup and status updates |
| `IAuditRepository` | Append-only audit event storage |
| `IUserRepository` | User lookup for auth |
| `ITokenService` | JWT generation |
| `IStorageService` | File storage abstraction (local filesystem) |

---

## Frontend — Pages

| Route | File | Description |
|-------|------|-------------|
| `/` | `app/page.tsx` | Home / redirect |
| `/login` | `app/login/page.tsx` | Login form |
| `/incidents` | `app/incidents/page.tsx` | Incident list with filters and dashboard |
| `/incidents/new` | `app/incidents/new/page.tsx` | New incident registration form |
| `/incidents/[id]` | `app/incidents/[id]/page.tsx` | Incident detail, transitions, audit trail |

---

## Frontend — Components

### UI Components (`components/ui/`)

| Component | Props | Description |
|-----------|-------|-------------|
| `Button` | `variant?: 'primary' \| 'secondary' \| 'danger' \| 'ghost'`, `loading?: boolean`, + native button attrs | Styled button with loading spinner |
| `Input` | Native `<input>` attributes | Styled text input |
| `Select` | Native `<select>` attributes | Styled select dropdown |
| `Textarea` | Native `<textarea>` attributes | Styled textarea |
| `FormField` | `label: string`, `error?: string`, `children: ReactNode` | Label + input wrapper with error display |
| `Modal` | `open: boolean`, `onClose: () => void`, `title: string`, `size?: 'sm' \| 'md' \| 'lg' \| 'xl'`, `children` | Overlay modal with Escape-to-close |
| `SeverityBadge` | `severity: Severity` | Color-coded severity pill |
| `StatusBadge` | `status: IncidentStatus` | Color-coded status pill |
| `Navbar` | — | Top navigation bar with user info, incidents link, and logout |

### Incident Components (`components/incidents/`)

| Component | Props | Description |
|-----------|-------|-------------|
| `IncidentCard` | `incident: IncidentSummaryDto` | Clickable card showing incident number, tail number, severity, status, fault type, and date |
| `IncidentForm` | `onSuccess: () => void`, `onCancel: () => void` | Full incident creation form with validation, file upload (max 10), and Critical grounding warning |
| `IncidentFiltersPanel` | `filters: IncidentFilters`, `onChange: (partial) => void`, `onReset: () => void` | Filter bar for tail number, fault type, severity, status, and date range |
| `AuditTimeline` | `events: AuditEvent[]` | Vertical timeline showing audit events with actor, type, timestamps, and before/after values |

---

## Frontend — Hooks

| Hook | Returns | Description |
|------|---------|-------------|
| `useAuth()` | `{ user, loading, login, logout }` | Manages JWT auth state in localStorage. `login(email, password)` authenticates via API; `logout()` clears stored credentials. |
| `useIncidents(initialFilters)` | `{ result, loading, error, filters, fetch, updateFilters }` | Fetches paginated incidents with filter support. `updateFilters(partial)` resets to page 1 and re-fetches. |
| `useDashboard()` | `{ data, loading, error, fetch }` | Fetches dashboard summary (open counts by severity, grounded aircraft). |

---

## Frontend — Lib Utilities

| Module | Exports | Description |
|--------|---------|-------------|
| `lib/api.ts` | `login`, `getIncidents`, `getIncident`, `createIncident`, `updateIncident`, `transitionIncident`, `voidIncident`, `getAuditTrail`, `getDashboard`, `getAircraftStatus`, `ungroundAircraft` | Typed HTTP client wrapping all backend endpoints. Handles JWT injection, 401 redirect, and FormData for file uploads. |
| `lib/auth.ts` | `saveAuth`, `getAuth`, `clearAuth`, `hasRole` | localStorage helpers for auth token and user object persistence. |
| `lib/stateMachine.ts` | `IncidentStateMachine.getAllowedTransitions(current, role)` | Client-side state machine that returns allowed next statuses based on current status and user role. |
| `lib/utils.ts` | `severityColor`, `statusColor`, `statusLabel`, `formatDate`, `formatBytes` | UI formatting helpers for badges, dates, and file sizes. |

---

## Frontend — TypeScript Types (`types/index.ts`)

| Type | Kind | Description |
|------|------|-------------|
| `FaultType` | Union | `'Mechanical' \| 'Electrical' \| 'Hydraulic' \| 'Avionics'` |
| `Severity` | Union | `'Low' \| 'Medium' \| 'High' \| 'Critical'` |
| `IncidentStatus` | Union | `'Open' \| 'InProgress' \| 'PendingReview' \| 'Resolved' \| 'Closed' \| 'Voided'` |
| `AircraftStatus` | Union | `'Available' \| 'InService' \| 'Grounded' \| 'Maintenance'` |
| `UserRole` | Union | `'Technician' \| 'Supervisor' \| 'MaintenanceControl' \| 'SafetyOfficer' \| 'ChiefEngineer' \| 'Admin'` |
| `IncidentDto` | Interface | Full incident detail with attachments |
| `IncidentSummaryDto` | Interface | Lightweight incident for list views |
| `AttachmentDto` | Interface | File attachment metadata |
| `DashboardSummaryDto` | Interface | Open incident counts + grounded aircraft |
| `PagedResult<T>` | Interface | Generic paginated response wrapper |
| `AuditEvent` | Interface | Single audit log entry |
| `LoginResponse` | Interface | Auth token + user info from login |
| `AuthUser` | Interface | Client-side auth state (token, userId, fullName, role) |
| `CreateIncidentInput` | Interface | Form input shape for incident creation |
| `IncidentFilters` | Interface | Query filter parameters for incident list |

---

## State Machine — Incident Lifecycle

```
Open ──→ InProgress ──→ PendingReview ──→ Resolved ──→ Closed
                                                         
Any open status ──→ Voided (with justification, Supervisor+)
```

| Transition | Min Role |
|------------|----------|
| Open → InProgress | Technician |
| InProgress → PendingReview | Technician |
| PendingReview → Resolved | Supervisor |
| Resolved → Closed | SafetyOfficer |

---

## Critical Flow — CRITICAL Incident → Dispatch Block

When a CRITICAL incident is submitted, the following steps execute atomically:

1. Validate all input fields, aircraft tail number, technician, and supervisor.
2. Validate and pre-check attachments before opening the transaction.
3. **BEGIN TRANSACTION**
   - Insert incident record (`status = Open`).
   - Update aircraft `operationalStatus = Grounded`, set `groundedByIncidentId`.
   - Insert audit event for the grounding.
   - Insert audit event for incident creation.
4. **COMMIT** — if any step fails, the entire operation rolls back.
5. Upload attachments to local storage (post-commit).

---

## Implementation Status

See [`.kiro/specs/maintenance-incident-registration/`](.kiro/specs/maintenance-incident-registration/) for the full requirements, design, and task breakdown.

| Area | Tasks |
|------|-------|
| Database schema & migrations | Task 1 |
| Domain models & enumerations | Task 2 |
| Repository layer | Task 3 |
| Attachment service | Task 4 |
| Dispatch block service | Task 5 |
| Incident command service | Task 6 |
| Incident query service | Task 7 |
| Notification service | Task 8 |
| REST API controllers | Task 9 |
| Input validation middleware | Task 10 |
| Frontend — registration form | Task 11 |
| Frontend — list & dashboard | Task 12 |
| Frontend — detail & lifecycle | Task 13 |
| End-to-end & integration tests | Task 14 |
