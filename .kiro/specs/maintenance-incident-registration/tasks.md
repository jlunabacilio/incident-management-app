# Implementation Tasks — Maintenance Incident Registration

## Overview

Tasks are ordered by dependency. Each task references the requirements (REQ-XXX) and design sections it implements. A task should not begin until all tasks it depends on are marked complete.

---

## Task 1 — Database Schema and Migrations

**Depends on:** —
**Implements:** REQ-001, REQ-002, REQ-003, REQ-006

- [ ] 1.1 Create migration: `incidents` table with all fields defined in the data model (id, incidentNumber, aircraftTailNumber, faultType, severity, status, description, stepsToReproduce, technicianId, supervisorId, createdBy, createdAt, updatedAt, resolvedAt, closedAt).
- [ ] 1.2 Create migration: `incident_attachments` table (id, incidentId FK, storageKey, filename, mimeType, sizeBytes, uploadedAt, uploadedBy).
- [ ] 1.3 Create migration: add `operationalStatus`, `groundedByIncidentId`, `groundedAt`, `groundedBy` columns to the existing `aircraft` table.
- [ ] 1.4 Create migration: `audit_events` table (id, entityType, entityId, eventType, actorId, actorName, previousValue JSONB, newValue JSONB, occurredAt).
- [ ] 1.5 Add PostgreSQL trigger on `audit_events` to block UPDATE and DELETE at the DB level (immutability enforcement).
- [ ] 1.6 Add DB-level CHECK constraint on `incidents.faultType` (MECHANICAL, ELECTRICAL, HYDRAULIC, AVIONICS).
- [ ] 1.7 Add DB-level CHECK constraint on `incidents.severity` (LOW, MEDIUM, HIGH, CRITICAL).
- [ ] 1.8 Add DB-level CHECK constraint on `incidents.status` (OPEN, IN_PROGRESS, RESOLVED, CLOSED, VOIDED).
- [ ] 1.9 Add indexes: `incidents(aircraftTailNumber)`, `incidents(status)`, `incidents(severity)`, `incidents(createdAt)`, `audit_events(entityId, entityType)`.
- [ ] 1.10 Seed reference data: fault types and severity levels in lookup tables (if using normalized approach).

---

## Task 2 — Domain Models and Enumerations

**Depends on:** Task 1
**Implements:** REQ-001, REQ-002, REQ-003

- [ ] 2.1 Define `FaultType` enum: `MECHANICAL | ELECTRICAL | HYDRAULIC | AVIONICS`.
- [ ] 2.2 Define `Severity` enum: `LOW | MEDIUM | HIGH | CRITICAL`.
- [ ] 2.3 Define `IncidentStatus` enum: `OPEN | IN_PROGRESS | RESOLVED | CLOSED | VOIDED`.
- [ ] 2.4 Define `AircraftStatus` enum: `AVAILABLE | IN_SERVICE | GROUNDED | MAINTENANCE`.
- [ ] 2.5 Implement `Incident` domain entity with all fields and validation rules (field lengths, required fields).
- [ ] 2.6 Implement `Attachment` value object with mime-type and size validation.
- [ ] 2.7 Implement `AuditEvent` entity (immutable after construction — no setters).
- [ ] 2.8 Implement `IncidentStateMachine`: define valid transitions and which roles may trigger each transition.

---

## Task 3 — Repository Layer

**Depends on:** Task 1, Task 2
**Implements:** REQ-001, REQ-003, REQ-004, REQ-006

- [ ] 3.1 Implement `IncidentRepository`: `save()`, `findById()`, `findAll(filters, pagination)`, `update()`.
- [ ] 3.2 Implement `AttachmentRepository`: `save()`, `findByIncidentId()`, `deleteById()`, `countByIncidentId()`.
- [ ] 3.3 Implement `AircraftRepository`: `findByTailNumber()`, `updateOperationalStatus()` — must execute within a caller-supplied transaction context.
- [ ] 3.4 Implement `AuditRepository`: `append()` only — no update or delete methods exposed.
- [ ] 3.5 Implement `PersonnelRepository`: `findById()`, `findSupervisors()` — used for technician/supervisor validation.
- [ ] 3.6 Write repository integration tests against a test database (use Docker / testcontainers).

---

## Task 4 — Attachment Service

**Depends on:** Task 2, Task 3
**Implements:** REQ-001 (1.8, 1.9)

- [ ] 4.1 Implement `AttachmentService.validateFiles(files[])`: enforce max 10 images, accepted MIME types (JPEG, PNG, WEBP), max 10 MB per file.
- [ ] 4.2 Implement `AttachmentService.upload(incidentId, files[])`: generate object storage keys, upload to S3-compatible storage, persist metadata via `AttachmentRepository`.
- [ ] 4.3 Implement `AttachmentService.generateDownloadUrl(attachmentId)`: return a time-limited pre-signed URL.
- [ ] 4.4 Implement `AttachmentService.delete(attachmentId, actorId)`: only allowed when incident status is `OPEN`; write audit event.
- [ ] 4.5 Write unit tests for file validation logic (boundary cases: exactly 10 files, 11 files, oversized file, wrong MIME type).

---

## Task 5 — Dispatch Block Service

**Depends on:** Task 2, Task 3
**Implements:** REQ-002

- [ ] 5.1 Implement `DispatchBlockService.blockAircraft(tailNumber, incidentId, actorId, txContext)`: updates aircraft status to `GROUNDED` within the provided transaction context; writes audit event.
- [ ] 5.2 Implement `DispatchBlockService.unblockAircraft(tailNumber, actorId)`: validates actor has `SAFETY_OFFICER` or `CHIEF_ENGINEER` role; sets aircraft status back to `AVAILABLE`; writes audit event; requires the blocking incident to be in `RESOLVED` or `CLOSED` state.
- [ ] 5.3 Implement `DispatchBlockService.isBlocked(tailNumber)`: returns boolean + blocking incident reference.
- [ ] 5.4 Write unit tests: block on CRITICAL, no block on HIGH/MEDIUM/LOW, unblock role enforcement, unblock rejected when incident still OPEN.

---

## Task 6 — Incident Command Service

**Depends on:** Task 2, Task 3, Task 4, Task 5
**Implements:** REQ-001, REQ-002, REQ-003

- [ ] 6.1 Implement `IncidentCommandService.create(dto, actorId)`:
  - Validate all input fields.
  - Validate `aircraftTailNumber` exists in aircraft registry.
  - Validate `technicianId` and `supervisorId` exist and have correct roles.
  - Call `AttachmentService.validateFiles()` before starting the transaction.
  - Open DB transaction.
  - Persist incident (status = `OPEN`).
  - If `severity == CRITICAL`, call `DispatchBlockService.blockAircraft()` within the same transaction.
  - Write `INCIDENT_CREATED` audit event within the same transaction.
  - Commit transaction.
  - Upload attachments (post-commit; on failure, log and alert — do not roll back incident).
  - Publish `IncidentCreatedEvent` to message broker.
  - Return created incident DTO.
- [ ] 6.2 Implement `IncidentCommandService.update(id, dto, actorId)`: allow updating description, stepsToReproduce, technicianId, supervisorId while status is `OPEN` or `IN_PROGRESS`; write audit event.
- [ ] 6.3 Implement `IncidentCommandService.transition(id, newStatus, actorId)`: validate state machine rules and role permissions; write audit event.
- [ ] 6.4 Implement `IncidentCommandService.void(id, justification, actorId)`: requires `SUPERVISOR` role and mandatory justification; write audit event.
- [ ] 6.5 Write unit tests for the CRITICAL transaction atomicity: simulate DB failure after incident insert — verify aircraft status is NOT changed.
- [ ] 6.6 Write unit tests for state machine transitions: valid paths, invalid paths, role violations.

---

## Task 7 — Incident Query Service

**Depends on:** Task 3
**Implements:** REQ-004

- [ ] 7.1 Implement `IncidentQueryService.findById(id)`: return full incident detail including attachments.
- [ ] 7.2 Implement `IncidentQueryService.search(filters, pagination)`: support filtering by tailNumber, faultType, severity, status, dateRange, technicianId, supervisorId.
- [ ] 7.3 Implement `IncidentQueryService.getDashboardSummary()`: return counts grouped by severity and status.
- [ ] 7.4 Implement `IncidentQueryService.exportCsv(filters)`: stream CSV output.
- [ ] 7.5 Implement `IncidentQueryService.exportPdf(filters)`: generate PDF report with incident table and summary.
- [ ] 7.6 Write unit tests for filter combinations and pagination edge cases.

---

## Task 8 — Notification Service

**Depends on:** Task 6 (event publishing)
**Implements:** REQ-002 (2.3)

- [ ] 8.1 Implement message broker consumer for `IncidentCreatedEvent`.
- [ ] 8.2 On receiving a CRITICAL incident event, dispatch notifications to: Shift Supervisor (from incident), all users with `SAFETY_OFFICER` role, all users with `MAINTENANCE_CONTROL` role.
- [ ] 8.3 Notification channels: email (primary) + in-app push notification.
- [ ] 8.4 Notification content must include: incident number, aircraft tail number, fault type, severity, description summary, link to incident detail.
- [ ] 8.5 Implement retry logic with exponential backoff for failed notification deliveries (max 3 retries).
- [ ] 8.6 Write unit tests: verify correct recipients are resolved for CRITICAL severity; verify non-CRITICAL incidents do not trigger the CRITICAL notification path.

---

## Task 9 — REST API Controllers

**Depends on:** Task 6, Task 7, Task 8
**Implements:** REQ-001 through REQ-005

- [ ] 9.1 Implement `POST /api/v1/incidents` controller: parse multipart form (fields + files), call `IncidentCommandService.create()`, return 201.
- [ ] 9.2 Implement `GET /api/v1/incidents` controller: parse query params, call `IncidentQueryService.search()`, return paginated list.
- [ ] 9.3 Implement `GET /api/v1/incidents/:id` controller: call `IncidentQueryService.findById()`, return detail.
- [ ] 9.4 Implement `PATCH /api/v1/incidents/:id` controller: call `IncidentCommandService.update()`.
- [ ] 9.5 Implement `POST /api/v1/incidents/:id/transition` controller: call `IncidentCommandService.transition()`.
- [ ] 9.6 Implement `POST /api/v1/incidents/:id/void` controller: call `IncidentCommandService.void()`.
- [ ] 9.7 Implement `GET /api/v1/incidents/:id/audit` controller: return audit trail for incident.
- [ ] 9.8 Implement `GET /api/v1/incidents/export` controller: stream CSV or PDF based on `Accept` header or `format` query param.
- [ ] 9.9 Implement `POST /api/v1/incidents/:id/attachments` controller: multipart upload, call `AttachmentService.upload()`.
- [ ] 9.10 Implement `DELETE /api/v1/incidents/:id/attachments/:attachmentId` controller.
- [ ] 9.11 Implement `POST /api/v1/aircraft/:tailNumber/unground` controller: call `DispatchBlockService.unblockAircraft()`.
- [ ] 9.12 Implement `GET /api/v1/aircraft/:tailNumber/status` controller.
- [ ] 9.13 Apply authentication middleware (JWT validation) to all routes.
- [ ] 9.14 Apply role-based authorization middleware per endpoint as defined in the design.
- [ ] 9.15 Apply idempotency key middleware to `POST /api/v1/incidents`.

---

## Task 10 — Input Validation Middleware

**Depends on:** Task 9
**Implements:** REQ-001, REQ-005

- [ ] 10.1 Define and apply JSON schema / DTO validation for `CreateIncidentDto`: all required fields, enum values, string length constraints.
- [ ] 10.2 Define and apply validation for `UpdateIncidentDto`: partial fields, same constraints.
- [ ] 10.3 Define and apply validation for `TransitionIncidentDto`: `newStatus` must be a valid `IncidentStatus`.
- [ ] 10.4 Validate multipart file uploads: MIME type whitelist, per-file size limit (10 MB), total file count limit (10).
- [ ] 10.5 Return structured `422 VALIDATION_ERROR` responses with per-field error details.

---

## Task 11 — Frontend — Incident Registration Form

**Depends on:** Task 9, Task 10
**Implements:** REQ-001, REQ-002

- [ ] 11.1 Build incident registration form with fields: tail number (autocomplete from aircraft registry), fault type (dropdown), severity (dropdown with color coding), description (textarea), steps to reproduce (textarea), technician (searchable select), supervisor (searchable select).
- [ ] 11.2 Implement image upload component: drag-and-drop + file picker, preview thumbnails, enforce max 10 images and 10 MB per file client-side, display validation errors.
- [ ] 11.3 Display a prominent warning banner when severity `CRITICAL` is selected: "Selecting CRITICAL will immediately ground this aircraft and block dispatch."
- [ ] 11.4 On successful submission with CRITICAL severity, display a confirmation modal showing the aircraft has been grounded and the incident number.
- [ ] 11.5 Implement form-level validation with inline error messages before submission.
- [ ] 11.6 Implement loading state and error handling for the submission API call.

---

## Task 12 — Frontend — Incident List and Dashboard

**Depends on:** Task 9
**Implements:** REQ-004

- [ ] 12.1 Build incident list view with columns: incident number, tail number, fault type, severity (color-coded badge), status, technician, created date.
- [ ] 12.2 Implement filter panel: tail number, fault type, severity (multi-select), status (multi-select), date range picker.
- [ ] 12.3 Implement dashboard summary cards: total open incidents, breakdown by severity (LOW / MEDIUM / HIGH / CRITICAL counts).
- [ ] 12.4 Highlight rows / aircraft with CRITICAL open incidents in red.
- [ ] 12.5 Implement export buttons (CSV, PDF) that call the export endpoint with current filters.
- [ ] 12.6 Implement pagination (page size: 20, 50, 100).

---

## Task 13 — Frontend — Incident Detail and Lifecycle

**Depends on:** Task 9
**Implements:** REQ-003, REQ-006

- [ ] 13.1 Build incident detail view showing all fields, attachments (thumbnail gallery with download), and current status.
- [ ] 13.2 Implement status transition controls: show only valid next states based on current status and authenticated user's role.
- [ ] 13.3 Implement audit trail timeline view at the bottom of the detail page.
- [ ] 13.4 For CRITICAL incidents, display a persistent "AIRCRAFT GROUNDED" alert banner with the grounding timestamp.
- [ ] 13.5 Implement the "Unground Aircraft" action (visible only to SAFETY_OFFICER / CHIEF_ENGINEER roles) with a confirmation dialog.

---

## Task 14 — End-to-End and Integration Tests

**Depends on:** Task 9 through Task 13
**Implements:** All REQs (verification)

- [ ] 14.1 E2E test: Create a CRITICAL incident → verify aircraft status is `GROUNDED` in the DB and API response.
- [ ] 14.2 E2E test: Attempt to dispatch a GROUNDED aircraft → verify rejection with correct error referencing incident ID.
- [ ] 14.3 E2E test: Resolve CRITICAL incident and unground aircraft as SAFETY_OFFICER → verify aircraft status returns to `AVAILABLE`.
- [ ] 14.4 E2E test: Attempt to unground aircraft as TECHNICIAN → verify 403 Forbidden.
- [ ] 14.5 E2E test: Upload 11 images → verify 422 validation error.
- [ ] 14.6 E2E test: Create incident with missing required fields → verify 422 with per-field errors.
- [ ] 14.7 Integration test: Simulate DB failure mid-transaction on CRITICAL incident → verify rollback (no GROUNDED status, no incident record).
- [ ] 14.8 Integration test: Duplicate submission with same idempotency key → verify only one incident is created.
- [ ] 14.9 Verify audit log is written for every state transition and cannot be deleted via any API endpoint.
