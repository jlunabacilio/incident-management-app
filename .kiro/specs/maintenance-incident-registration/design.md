# Design — Maintenance Incident Registration

## Overview

The Maintenance Incident Registration system is a safety-critical module within the Aviation Maintenance Management Platform. It follows a layered architecture (API → Service → Repository) with an event-driven side-effect model for the CRITICAL dispatch-block rule. All state mutations that touch both the incident and the aircraft registry are wrapped in a distributed transaction to guarantee atomicity.

---

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Client (Web / Mobile)                    │
└───────────────────────────────┬─────────────────────────────────┘
                                │ HTTPS / REST + JWT
┌───────────────────────────────▼─────────────────────────────────┐
│                     API Gateway / BFF Layer                     │
│  - Auth middleware (JWT validation + role extraction)           │
│  - Request validation (schema, file size, image count)          │
│  - Rate limiting                                                │
└───────────────────────────────┬─────────────────────────────────┘
                                │
┌───────────────────────────────▼─────────────────────────────────┐
│                    Incident Service (Core)                      │
│  - IncidentCommandService   (create, update, transition)        │
│  - IncidentQueryService     (search, filter, export)            │
│  - DispatchBlockService     (block / unblock aircraft)          │
│  - NotificationService      (CRITICAL alert dispatch)           │
│  - AuditService             (append-only event log)             │
└──────┬──────────────────────────────────────┬───────────────────┘
       │                                      │
┌──────▼──────────┐                  ┌────────▼────────────────┐
│  Incident DB    │                  │  Aircraft Registry DB   │
│  (PostgreSQL)   │                  │  (PostgreSQL)           │
└─────────────────┘                  └─────────────────────────┘
       │
┌──────▼──────────┐
│  Object Storage │  ← Incident photo attachments (S3-compatible)
│  (S3 / MinIO)   │
└─────────────────┘
       │
┌──────▼──────────┐
│  Message Broker │  ← CRITICAL incident events → Notification consumers
│  (RabbitMQ /    │
│   Kafka)        │
└─────────────────┘
```

---

## Data Models

### Incident

```typescript
interface Incident {
  id: string;                        // UUID v4
  incidentNumber: string;            // Human-readable: INC-YYYY-NNNNNN
  aircraftTailNumber: string;        // FK → Aircraft registry
  faultType: FaultType;              // MECHANICAL | ELECTRICAL | HYDRAULIC | AVIONICS
  severity: Severity;                // LOW | MEDIUM | HIGH | CRITICAL
  status: IncidentStatus;            // OPEN | IN_PROGRESS | RESOLVED | CLOSED | VOIDED
  description: string;               // 20–4000 chars
  stepsToReproduce: string;          // 10–2000 chars
  technicianId: string;              // FK → Personnel registry
  supervisorId: string;              // FK → Personnel registry (role = SUPERVISOR)
  attachments: Attachment[];         // max 10
  createdBy: string;                 // FK → User (authenticated submitter)
  createdAt: Date;                   // UTC
  updatedAt: Date;                   // UTC
  resolvedAt?: Date;
  closedAt?: Date;
}

enum FaultType {
  MECHANICAL = 'MECHANICAL',
  ELECTRICAL = 'ELECTRICAL',
  HYDRAULIC  = 'HYDRAULIC',
  AVIONICS   = 'AVIONICS',
}

enum Severity {
  LOW      = 'LOW',
  MEDIUM   = 'MEDIUM',
  HIGH     = 'HIGH',
  CRITICAL = 'CRITICAL',
}

enum IncidentStatus {
  OPEN        = 'OPEN',
  IN_PROGRESS = 'IN_PROGRESS',
  RESOLVED    = 'RESOLVED',
  CLOSED      = 'CLOSED',
  VOIDED      = 'VOIDED',
}
```

### Attachment

```typescript
interface Attachment {
  id: string;           // UUID v4
  incidentId: string;   // FK → Incident
  storageKey: string;   // Object storage path
  filename: string;
  mimeType: string;     // image/jpeg | image/png | image/webp
  sizeBytes: number;    // max 10_485_760 (10 MB)
  uploadedAt: Date;
  uploadedBy: string;
}
```

### Aircraft (relevant fields)

```typescript
interface Aircraft {
  tailNumber: string;           // PK
  operationalStatus: AircraftStatus;
  groundedByIncidentId?: string; // FK → Incident (set on CRITICAL block)
  groundedAt?: Date;
  groundedBy?: string;          // User ID
}

enum AircraftStatus {
  AVAILABLE = 'AVAILABLE',
  IN_SERVICE = 'IN_SERVICE',
  GROUNDED  = 'GROUNDED',
  MAINTENANCE = 'MAINTENANCE',
}
```

### AuditEvent

```typescript
interface AuditEvent {
  id: string;           // UUID v4
  entityType: string;   // 'INCIDENT' | 'AIRCRAFT'
  entityId: string;
  eventType: string;    // 'CREATED' | 'UPDATED' | 'STATUS_CHANGED' | 'GROUNDED' | 'UNGROUNDED'
  actorId: string;
  actorName: string;
  previousValue?: Record<string, unknown>;
  newValue: Record<string, unknown>;
  occurredAt: Date;     // UTC, immutable
}
```

---

## Critical Flow — CRITICAL Incident → Dispatch Block

This is the most safety-sensitive flow. It must be atomic.

```
Client
  │
  ├─ POST /api/v1/incidents  { severity: "CRITICAL", ... }
  │
  ▼
IncidentCommandService.create()
  │
  ├─ 1. Validate all fields (schema + business rules)
  ├─ 2. Validate aircraft tail number exists in registry
  ├─ 3. Validate technician and supervisor IDs
  ├─ 4. Upload attachments to object storage (pre-signed URLs or direct)
  │
  ├─ BEGIN TRANSACTION ──────────────────────────────────────────────┐
  │   5. INSERT incident record (status = OPEN)                      │
  │   6. IF severity == CRITICAL:                                    │
  │       a. UPDATE aircraft SET status = GROUNDED,                  │
  │              groundedByIncidentId = <incidentId>,                │
  │              groundedAt = NOW()                                  │
  │       b. INSERT audit_event (GROUNDED, actor, incidentId)        │
  │   7. INSERT audit_event (INCIDENT CREATED)                       │
  └─ COMMIT TRANSACTION ─────────────────────────────────────────────┘
  │
  ├─ 8. Publish IncidentCreatedEvent to message broker
  │       └─ NotificationConsumer → email/push to Supervisor, MCC, Safety Officer
  │
  └─ 9. Return 201 Created { incidentId, incidentNumber, aircraftStatus }
```

If the transaction fails at any point, the entire operation is rolled back — no partial state is persisted.

---

## API Endpoints

### Incidents

| Method | Path | Description | Min Role |
|--------|------|-------------|----------|
| `POST` | `/api/v1/incidents` | Create a new incident | TECHNICIAN |
| `GET` | `/api/v1/incidents` | List / search incidents | TECHNICIAN |
| `GET` | `/api/v1/incidents/:id` | Get incident detail | TECHNICIAN |
| `PATCH` | `/api/v1/incidents/:id` | Update incident fields | TECHNICIAN |
| `POST` | `/api/v1/incidents/:id/transition` | Change incident status | SUPERVISOR |
| `POST` | `/api/v1/incidents/:id/void` | Void an incident | SUPERVISOR |
| `GET` | `/api/v1/incidents/:id/audit` | Get audit trail | MAINTENANCE_CONTROL |
| `GET` | `/api/v1/incidents/export` | Export PDF / CSV | MAINTENANCE_CONTROL |

### Attachments

| Method | Path | Description | Min Role |
|--------|------|-------------|----------|
| `POST` | `/api/v1/incidents/:id/attachments` | Upload images (multipart) | TECHNICIAN |
| `GET` | `/api/v1/incidents/:id/attachments/:attachmentId` | Download image | TECHNICIAN |
| `DELETE` | `/api/v1/incidents/:id/attachments/:attachmentId` | Remove image (OPEN only) | TECHNICIAN |

### Aircraft Dispatch Control

| Method | Path | Description | Min Role |
|--------|------|-------------|----------|
| `POST` | `/api/v1/aircraft/:tailNumber/unground` | Unblock aircraft dispatch | SAFETY_OFFICER |
| `GET` | `/api/v1/aircraft/:tailNumber/status` | Get operational status | TECHNICIAN |

---

## Request / Response Schemas

### POST /api/v1/incidents — Request Body

```json
{
  "aircraftTailNumber": "PP-XKA",
  "faultType": "HYDRAULIC",
  "severity": "CRITICAL",
  "description": "Hydraulic pressure loss detected on main landing gear retraction cycle during pre-flight check. Pressure dropped from 3000 PSI to 800 PSI within 4 seconds.",
  "stepsToReproduce": "1. Power on hydraulic system. 2. Command gear retraction. 3. Observe pressure gauge on panel 4B.",
  "technicianId": "usr_tech_0042",
  "supervisorId": "usr_sup_0011"
}
```

### POST /api/v1/incidents — Response (201 Created)

```json
{
  "incidentId": "inc_01HZ9K2M3P",
  "incidentNumber": "INC-2026-004821",
  "aircraftTailNumber": "PP-XKA",
  "severity": "CRITICAL",
  "status": "OPEN",
  "aircraftStatus": "GROUNDED",
  "dispatchBlocked": true,
  "createdAt": "2026-04-29T14:32:00Z"
}
```

### POST /api/v1/incidents — Response (422 — Validation Error)

```json
{
  "error": "VALIDATION_ERROR",
  "details": [
    { "field": "attachments", "message": "Maximum 10 images allowed; received 12." },
    { "field": "severity",    "message": "Invalid value. Must be one of: LOW, MEDIUM, HIGH, CRITICAL." }
  ]
}
```

---

## Component Responsibilities

| Component | Responsibility |
|-----------|---------------|
| `IncidentCommandService` | Orchestrates creation, update, and state transitions. Owns the transactional boundary. |
| `IncidentQueryService` | Read-only queries, filtering, pagination, export generation. |
| `DispatchBlockService` | Applies and removes aircraft GROUNDED status. Called only by IncidentCommandService within a transaction. |
| `AttachmentService` | Validates image count/size/type, manages pre-signed upload URLs, stores metadata. |
| `NotificationService` | Consumes domain events from the broker and dispatches email/push notifications. |
| `AuditService` | Writes append-only audit events. Never updates or deletes records. |

---

## Non-Functional Considerations

- **Atomicity:** The CRITICAL block rule is enforced via a single DB transaction spanning both the `incidents` and `aircraft` tables. No eventual consistency is acceptable for this rule.
- **Idempotency:** Incident creation uses a client-supplied `idempotencyKey` header to prevent duplicate submissions on retry.
- **Storage:** Attachments are stored in object storage (not the DB). The DB holds only metadata and the storage key.
- **Notifications:** Sent asynchronously via message broker to avoid blocking the HTTP response. Delivery failure does not roll back the incident.
- **Audit immutability:** The audit table uses a PostgreSQL trigger to prevent UPDATE and DELETE operations at the database level, not just the application level.
- **Retention:** Audit records and incident records are retained for 7 years. A scheduled archival job moves records older than 2 years to cold storage while keeping them queryable.
