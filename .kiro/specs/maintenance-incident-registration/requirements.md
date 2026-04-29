# Requirements — Maintenance Incident Registration

## Introduction

This feature enables aviation maintenance personnel to register, track, and manage maintenance incidents against aircraft. It enforces safety-critical rules — most importantly, the automatic blocking of aircraft dispatch when a CRITICAL severity incident is open. The system must be auditable, traceable, and compliant with aviation maintenance regulations (e.g., EASA Part-M, FAA AC 43-9C).

---

## Requirements

### REQ-001 — Incident Creation

**User Story:** As a maintenance technician, I want to register a new maintenance incident so that faults are formally documented and tracked.

#### Acceptance Criteria

- [ ] 1.1 The system shall require the aircraft tail number (registration) as a mandatory field, validated against the aircraft registry.
- [ ] 1.2 The system shall require the fault type, selected from a fixed enumeration: `MECHANICAL`, `ELECTRICAL`, `HYDRAULIC`, `AVIONICS`.
- [ ] 1.3 The system shall require a severity level selected from: `LOW`, `MEDIUM`, `HIGH`, `CRITICAL`.
- [ ] 1.4 The system shall require a detailed description (minimum 20 characters, maximum 4000 characters).
- [ ] 1.5 The system shall require a "steps to reproduce / observation sequence" field (minimum 10 characters, maximum 2000 characters).
- [ ] 1.6 The system shall require the responsible technician (selected from active personnel registry).
- [ ] 1.7 The system shall require the shift supervisor (selected from active personnel registry, role = SUPERVISOR).
- [ ] 1.8 The system shall allow attachment of up to 10 images per incident (accepted formats: JPEG, PNG, WEBP; max 10 MB per file).
- [ ] 1.9 The system shall reject submissions that exceed 10 attached images with a clear validation error.
- [ ] 1.10 The system shall auto-populate the incident creation timestamp (UTC) and the authenticated user who submitted the form.

---

### REQ-002 — CRITICAL Severity — Automatic Aircraft Dispatch Block

**User Story:** As a safety officer, I want the system to automatically block an aircraft from dispatch when a CRITICAL incident is registered, so that no unsafe aircraft is released to operations.

#### Acceptance Criteria

- [ ] 2.1 When an incident is saved with severity = `CRITICAL`, the system shall immediately set the aircraft's operational status to `GROUNDED` in the aircraft registry.
- [ ] 2.2 The dispatch block shall be applied **within the same transaction** as the incident creation — partial states (incident saved but aircraft not blocked) are not permitted.
- [ ] 2.3 The system shall send an automated notification to the following roles upon a CRITICAL incident: Shift Supervisor, Maintenance Control Center (MCC), and Safety Officer.
- [ ] 2.4 The aircraft shall remain `GROUNDED` until the incident is explicitly resolved and the unblock is approved by a user with the `SAFETY_OFFICER` or `CHIEF_ENGINEER` role.
- [ ] 2.5 Any attempt to schedule or dispatch a `GROUNDED` aircraft shall be rejected by the system with a clear error referencing the blocking incident ID.
- [ ] 2.6 The system shall log every dispatch-block and dispatch-unblock event in an immutable audit trail with actor, timestamp, and incident reference.

---

### REQ-003 — Incident Lifecycle Management

**User Story:** As a shift supervisor, I want to update and close incidents so that the maintenance record reflects the current state of the aircraft.

#### Acceptance Criteria

- [ ] 3.1 An incident shall progress through the following states: `OPEN` → `IN_PROGRESS` → `RESOLVED` → `CLOSED`.
- [ ] 3.2 Only users with role `TECHNICIAN` or higher may transition an incident from `OPEN` to `IN_PROGRESS`.
- [ ] 3.3 Only users with role `SUPERVISOR` or higher may transition an incident to `RESOLVED`.
- [ ] 3.4 Only users with role `SAFETY_OFFICER` or `CHIEF_ENGINEER` may close a CRITICAL incident and unblock the aircraft.
- [ ] 3.5 Each state transition shall be recorded in the incident audit log with actor, previous state, new state, and timestamp.
- [ ] 3.6 The system shall prevent deletion of any incident record; incidents may only be closed or voided with a mandatory justification.

---

### REQ-004 — Search and Reporting

**User Story:** As a maintenance control manager, I want to search and filter incidents so that I can monitor fleet health and compliance.

#### Acceptance Criteria

- [ ] 4.1 The system shall allow filtering incidents by: tail number, fault type, severity, status, date range, technician, and supervisor.
- [ ] 4.2 The system shall display a dashboard summary showing open incidents grouped by severity.
- [ ] 4.3 The system shall provide an exportable report (PDF and CSV) of incidents filtered by any combination of the criteria in 4.1.
- [ ] 4.4 The system shall highlight aircraft with active CRITICAL incidents prominently in the fleet view.

---

### REQ-005 — Access Control

**User Story:** As a system administrator, I want role-based access control so that only authorized personnel can perform sensitive operations.

#### Acceptance Criteria

- [ ] 5.1 The system shall enforce the following roles: `TECHNICIAN`, `SUPERVISOR`, `MAINTENANCE_CONTROL`, `SAFETY_OFFICER`, `CHIEF_ENGINEER`, `ADMIN`.
- [ ] 5.2 Only authenticated users shall be able to create, view, or modify incidents.
- [ ] 5.3 Technicians shall not be able to resolve or close incidents.
- [ ] 5.4 Only `SAFETY_OFFICER` and `CHIEF_ENGINEER` roles shall be able to unblock a GROUNDED aircraft.
- [ ] 5.5 All API endpoints shall validate the JWT token and enforce role permissions server-side.

---

### REQ-006 — Audit and Traceability

**User Story:** As a compliance auditor, I want a complete, tamper-evident history of every incident and aircraft status change so that I can demonstrate regulatory compliance.

#### Acceptance Criteria

- [ ] 6.1 Every create, update, and state-transition event on an incident shall be recorded in an append-only audit log.
- [ ] 6.2 The audit log shall capture: event type, actor (user ID + name), timestamp (UTC), entity ID, previous value, and new value.
- [ ] 6.3 Audit log records shall not be editable or deletable by any user role, including `ADMIN`.
- [ ] 6.4 The system shall retain audit logs for a minimum of 7 years in compliance with aviation record-keeping regulations.
