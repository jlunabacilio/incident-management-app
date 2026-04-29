import type { Severity, IncidentStatus } from '@/types';

export function severityColor(severity: Severity): string {
  switch (severity) {
    case 'Critical': return 'bg-red-100 text-red-800 border-red-300';
    case 'High':     return 'bg-orange-100 text-orange-800 border-orange-300';
    case 'Medium':   return 'bg-yellow-100 text-yellow-800 border-yellow-300';
    case 'Low':      return 'bg-green-100 text-green-800 border-green-300';
  }
}

export function statusColor(status: IncidentStatus): string {
  switch (status) {
    case 'Open':          return 'bg-blue-100 text-blue-800';
    case 'InProgress':    return 'bg-purple-100 text-purple-800';
    case 'PendingReview': return 'bg-yellow-100 text-yellow-800';
    case 'Resolved':      return 'bg-teal-100 text-teal-800';
    case 'Closed':        return 'bg-gray-100 text-gray-700';
    case 'Voided':        return 'bg-red-100 text-red-700';
  }
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('en-US', {
    year: 'numeric', month: 'short', day: '2-digit',
    hour: '2-digit', minute: '2-digit',
  });
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function statusLabel(status: IncidentStatus): string {
  const map: Record<IncidentStatus, string> = {
    Open: 'Open',
    InProgress: 'In Progress',
    PendingReview: 'Pending Review',
    Resolved: 'Resolved',
    Closed: 'Closed',
    Voided: 'Voided',
  };
  return map[status];
}
