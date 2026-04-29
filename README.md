# Incident Management App

A safety-critical module for registering, tracking, and managing maintenance incidents against aircraft. Built for aviation maintenance personnel, it enforces regulatory compliance (EASA Part-M, FAA AC 43-9C) and guarantees that any aircraft with an open CRITICAL incident is automatically blocked from dispatch.

---

## Features

- **Incident registration** — Log faults with tail number, fault type, severity, description, steps to reproduce, technician, supervisor, and up to 10 photo attachments.
- **Automatic dispatch block** — When a CRITICAL incident is created, the aircraft is immediately set to `GROUNDED` within the same database transaction. No partial states are allowed.
- **Incident lifecycle** — Incidents progress through `OPEN → IN_PROGRESS → RESOLVED → CLOSED`, with role-based controls on each transition.
- **Role-based access control** — Six roles: `TECHNICIAN`, `SUPERVISOR`, `MAINTENANCE_CONTROL`, `SAFETY_OFFICER`, `CHIEF_ENGINEER`, `ADMIN`. All API endpoints enforce JWT authentication and role permissions server-side.
- **Search and reporting** — Filter incidents by tail number, fault type, severity, status, date range, technician, and supervisor. Export results as PDF or CSV.
- **Audit trail** — Every create, update, and state transition is recorded in an append-only audit log, retained for 7 years per aviation record-keeping regulations. Records cannot be edited or deleted by any role.
- **Notifications** — CRITICAL incidents trigger automated alerts to the Shift Supervisor, Maintenance Control Center, and Safety Officer via email and in-app push.

---

## Architecture

The system follows a layered architecture (API → Service → Repository) with an event-driven model for side effects.

```
Client (Web / Mobile)
        │ HTTPS / REST + JWT
API Gateway / BFF Layer
  - JWT validation + role extraction
  - Request validation (schema, file size, image count)
        │
Incident Service (Core)
  - IncidentCommandService   (create, update, transition)
  - IncidentQueryService     (search, filter, export)
  - DispatchBlockService     (block / unblock aircraft)
  - NotificationService      (CRITICAL alert dispatch)
  - AuditService             (append-only event log)
        │
  ┌─────┴──────┐   ┌──────────────────┐   ┌─────────────┐   ┌──────────────┐
  Incident DB  │   Aircraft Registry  │   Object Storage│   Message Broker
  (PostgreSQL) │   (PostgreSQL)       │   (S3 / MinIO)  │   (RabbitMQ/Kafka)
```

---

## Data Model Highlights

| Entity | Key Fields |
|--------|-----------|
| `Incident` | `id`, `incidentNumber` (INC-YYYY-NNNNNN), `aircraftTailNumber`, `faultType`, `severity`, `status`, `description`, `stepsToReproduce`, `technicianId`, `supervisorId`, `attachments[]` |
| `Attachment` | `id`, `incidentId`, `storageKey`, `filename`, `mimeType`, `sizeBytes` (max 10 MB) |
| `Aircraft` | `tailNumber`, `operationalStatus`, `groundedByIncidentId`, `groundedAt` |
| `AuditEvent` | `entityType`, `entityId`, `eventType`, `actorId`, `previousValue`, `newValue`, `occurredAt` |

**Enumerations:**
- `FaultType`: `MECHANICAL`, `ELECTRICAL`, `HYDRAULIC`, `AVIONICS`
- `Severity`: `LOW`, `MEDIUM`, `HIGH`, `CRITICAL`
- `IncidentStatus`: `OPEN`, `IN_PROGRESS`, `RESOLVED`, `CLOSED`, `VOIDED`
- `AircraftStatus`: `AVAILABLE`, `IN_SERVICE`, `GROUNDED`, `MAINTENANCE`

---

## API Overview

### Incidents

| Method | Path | Min Role |
|--------|------|----------|
| `POST` | `/api/v1/incidents` | TECHNICIAN |
| `GET` | `/api/v1/incidents` | TECHNICIAN |
| `GET` | `/api/v1/incidents/:id` | TECHNICIAN |
| `PATCH` | `/api/v1/incidents/:id` | TECHNICIAN |
| `POST` | `/api/v1/incidents/:id/transition` | SUPERVISOR |
| `POST` | `/api/v1/incidents/:id/void` | SUPERVISOR |
| `GET` | `/api/v1/incidents/:id/audit` | MAINTENANCE_CONTROL |
| `GET` | `/api/v1/incidents/export` | MAINTENANCE_CONTROL |

### Attachments

| Method | Path | Min Role |
|--------|------|----------|
| `POST` | `/api/v1/incidents/:id/attachments` | TECHNICIAN |
| `GET` | `/api/v1/incidents/:id/attachments/:attachmentId` | TECHNICIAN |
| `DELETE` | `/api/v1/incidents/:id/attachments/:attachmentId` | TECHNICIAN |

### Aircraft Dispatch Control

| Method | Path | Min Role |
|--------|------|----------|
| `POST` | `/api/v1/aircraft/:tailNumber/unground` | SAFETY_OFFICER |
| `GET` | `/api/v1/aircraft/:tailNumber/status` | TECHNICIAN |

---

## Critical Flow — CRITICAL Incident → Dispatch Block

When a CRITICAL incident is submitted, the following steps execute atomically:

1. Validate all input fields, aircraft tail number, technician, and supervisor.
2. Validate and pre-check attachments before opening the transaction.
3. **BEGIN TRANSACTION**
   - Insert incident record (`status = OPEN`).
   - Update aircraft `operationalStatus = GROUNDED`, set `groundedByIncidentId`.
   - Insert audit event for the grounding.
   - Insert audit event for incident creation.
4. **COMMIT** — if any step fails, the entire operation rolls back.
5. Upload attachments to object storage (post-commit).
6. Publish `IncidentCreatedEvent` to the message broker → notifications sent asynchronously.

---

## Non-Functional Requirements

- **Atomicity** — The CRITICAL dispatch block is enforced in a single DB transaction spanning both `incidents` and `aircraft` tables.
- **Idempotency** — Incident creation accepts a client-supplied `Idempotency-Key` header to prevent duplicate submissions on retry.
- **Audit immutability** — A PostgreSQL trigger blocks `UPDATE` and `DELETE` on the `audit_events` table at the database level.
- **Retention** — Incident and audit records are retained for 7 years; records older than 2 years are archived to cold storage but remain queryable.
- **Notifications** — Delivered asynchronously via message broker with exponential backoff retry (max 3 attempts). Delivery failure does not roll back the incident.

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
