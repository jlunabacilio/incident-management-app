import type {
  AuditEvent,
  AuthUser,
  CreateIncidentInput,
  DashboardSummaryDto,
  IncidentDto,
  IncidentFilters,
  IncidentSummaryDto,
  IncidentStatus,
  LoginResponse,
  PagedResult,
} from '@/types';

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000';

// ─── HTTP helpers ─────────────────────────────────────────────────────────────

function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem('auth_token');
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = getToken();
  const headers: Record<string, string> = {
    ...(init.headers as Record<string, string>),
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (!(init.body instanceof FormData)) {
    headers['Content-Type'] = 'application/json';
  }

  const res = await fetch(`${BASE_URL}${path}`, { ...init, headers });

  if (!res.ok) {
    const body = await res.json().catch(() => ({ error: res.statusText }));
    throw new Error(body?.error ?? `HTTP ${res.status}`);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// ─── Auth ─────────────────────────────────────────────────────────────────────

export async function login(email: string, password: string): Promise<LoginResponse> {
  return request<LoginResponse>('/api/v1/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
}

// ─── Incidents ────────────────────────────────────────────────────────────────

export async function getIncidents(filters: IncidentFilters): Promise<PagedResult<IncidentSummaryDto>> {
  const params = new URLSearchParams();
  if (filters.tailNumber) params.set('tailNumber', filters.tailNumber);
  if (filters.faultType) params.set('faultType', filters.faultType);
  if (filters.severity) params.set('severity', filters.severity);
  if (filters.status) params.set('status', filters.status);
  if (filters.dateFrom) params.set('dateFrom', filters.dateFrom);
  if (filters.dateTo) params.set('dateTo', filters.dateTo);
  params.set('page', String(filters.page));
  params.set('pageSize', String(filters.pageSize));
  return request<PagedResult<IncidentSummaryDto>>(`/api/v1/incidents?${params}`);
}

export async function getIncident(id: string): Promise<IncidentDto> {
  return request<IncidentDto>(`/api/v1/incidents/${id}`);
}

export async function createIncident(input: CreateIncidentInput): Promise<IncidentDto> {
  const form = new FormData();
  form.append('aircraftTailNumber', input.aircraftTailNumber);
  form.append('faultType', input.faultType);
  form.append('severity', input.severity);
  form.append('description', input.description);
  form.append('stepsToReproduce', input.stepsToReproduce);
  form.append('technicianId', input.technicianId);
  form.append('supervisorId', input.supervisorId);
  input.attachments.forEach((f) => form.append('attachments', f));
  return request<IncidentDto>('/api/v1/incidents', { method: 'POST', body: form });
}

export async function updateIncident(
  id: string,
  data: { description: string; stepsToReproduce: string; technicianId: string; supervisorId: string }
): Promise<IncidentDto> {
  return request<IncidentDto>(`/api/v1/incidents/${id}`, {
    method: 'PATCH',
    body: JSON.stringify(data),
  });
}

export async function transitionIncident(id: string, newStatus: IncidentStatus): Promise<IncidentDto> {
  return request<IncidentDto>(`/api/v1/incidents/${id}/transition`, {
    method: 'POST',
    body: JSON.stringify({ newStatus }),
  });
}

export async function voidIncident(id: string, justification: string): Promise<IncidentDto> {
  return request<IncidentDto>(`/api/v1/incidents/${id}/void`, {
    method: 'POST',
    body: JSON.stringify({ justification }),
  });
}

export async function getAuditTrail(incidentId: string): Promise<AuditEvent[]> {
  return request<AuditEvent[]>(`/api/v1/incidents/${incidentId}/audit`);
}

export async function getDashboard(): Promise<DashboardSummaryDto> {
  return request<DashboardSummaryDto>('/api/v1/incidents/dashboard');
}

// ─── Aircraft ─────────────────────────────────────────────────────────────────

export async function getAircraftStatus(tailNumber: string) {
  return request<{ tailNumber: string; status: string; groundedByIncidentId: string | null; groundedAt: string | null }>(
    `/api/v1/aircraft/${tailNumber}/status`
  );
}

export async function ungroundAircraft(tailNumber: string) {
  return request<{ message: string }>(`/api/v1/aircraft/${tailNumber}/unground`, { method: 'POST' });
}
