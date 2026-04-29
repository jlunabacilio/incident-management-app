import type { Severity, IncidentStatus } from '@/types';
import { severityColor, statusColor, statusLabel } from '@/lib/utils';

export function SeverityBadge({ severity }: { severity: Severity }) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold border ${severityColor(severity)}`}>
      {severity === 'Critical' && <span className="mr-1">⚠</span>}
      {severity}
    </span>
  );
}

export function StatusBadge({ status }: { status: IncidentStatus }) {
  return (
    <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${statusColor(status)}`}>
      {statusLabel(status)}
    </span>
  );
}
