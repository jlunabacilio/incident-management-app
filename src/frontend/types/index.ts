// ─── Enums ────────────────────────────────────────────────────────────────────

export type FaultType = 'Mechanical' | 'Electrical' | 'Hydraulic' | 'Avionics';
export type Severity = 'Low' | 'Medium' | 'High' | 'Critical';
export type IncidentStatus = 'Open' | 'InProgress' | 'PendingReview' | 'Resolved' | 'Closed' | 'Voided';
export type AircraftStatus = 'Available' | 'InService' | 'Grounded' | 'Maintenance';
export type UserRole = 'Technician' | 'Supervisor' | 'MaintenanceControl' | 'SafetyOfficer' | 'ChiefEngineer' | 'Admin';

// ─── Domain DTOs ──────────────────────────────────────────────────────────────

export interface AttachmentDto {
  id: string;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  uploadedAt: string;
  uploadedBy: string;
  url: string | null;
}

export interface IncidentDto {
  id: string;
  incidentNumber: string;
  aircraftTailNumber: string;
  faultType: FaultType;
  severity: Severity;
  status: IncidentStatus;
  description: string;
  stepsToReproduce: string;
  technicianId: string;
  supervisorId: string;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  resolvedAt: string | null;
  closedAt: string | null;
  attachments: AttachmentDto[];
}

export interface IncidentSummaryDto {
  id: string;
  incidentNumber: string;
  aircraftTailNumber: string;
  faultType: FaultType;
  severity: Severity;
  status: IncidentStatus;
  technicianId: string;
  createdAt: string;
}

export interface DashboardSummaryDto {
  totalOpen: number;
  lowCount: number;
  mediumCount: number;
  highCount: number;
  criticalCount: number;
  groundedAircraft: string[];
}

export interface PagedResult<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface AuditEvent {
  id: string;
  entityType: string;
  entityId: string;
  eventType: string;
  actorId: string;
  actorName: string;
  previousValue: string | null;
  newValue: string;
  occurredAt: string;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export interface LoginResponse {
  token: string;
  userId: string;
  fullName: string;
  role: UserRole;
  expiresAt: string;
}

export interface AuthUser {
  token: string;
  userId: string;
  fullName: string;
  role: UserRole;
}

// ─── Form inputs ──────────────────────────────────────────────────────────────

export interface CreateIncidentInput {
  aircraftTailNumber: string;
  faultType: FaultType;
  severity: Severity;
  description: string;
  stepsToReproduce: string;
  technicianId: string;
  supervisorId: string;
  attachments: File[];
}

export interface IncidentFilters {
  tailNumber?: string;
  faultType?: FaultType;
  severity?: Severity;
  status?: IncidentStatus;
  dateFrom?: string;
  dateTo?: string;
  page: number;
  pageSize: number;
}
