import type { IncidentStatus, UserRole } from '@/types';

type Transition = { from: IncidentStatus; to: IncidentStatus; minRole: UserRole };

const ROLE_ORDER: UserRole[] = [
  'Technician', 'Supervisor', 'MaintenanceControl', 'SafetyOfficer', 'ChiefEngineer', 'Admin',
];

const TRANSITIONS: Transition[] = [
  { from: 'Open',          to: 'InProgress',    minRole: 'Technician' },
  { from: 'InProgress',    to: 'PendingReview', minRole: 'Technician' },
  { from: 'PendingReview', to: 'Resolved',      minRole: 'Supervisor' },
  { from: 'Resolved',      to: 'Closed',        minRole: 'SafetyOfficer' },
];

function roleIndex(role: UserRole): number {
  return ROLE_ORDER.indexOf(role);
}

export const IncidentStateMachine = {
  getAllowedTransitions(current: IncidentStatus, role: UserRole): IncidentStatus[] {
    return TRANSITIONS
      .filter((t) => t.from === current && roleIndex(role) >= roleIndex(t.minRole))
      .map((t) => t.to);
  },
};
