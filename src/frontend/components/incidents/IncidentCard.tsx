import Link from 'next/link';
import { Clock } from 'lucide-react';
import { SeverityBadge, StatusBadge } from '@/components/ui/Badge';
import { formatDate } from '@/lib/utils';
import type { IncidentSummaryDto } from '@/types';

interface Props {
  incident: IncidentSummaryDto;
}

export function IncidentCard({ incident }: Props) {
  const isCritical = incident.severity === 'Critical';

  return (
    <Link href={`/incidents/${incident.id}`}>
      <div className={`bg-white rounded-xl border p-4 hover:shadow-md transition-shadow cursor-pointer
        ${isCritical ? 'border-red-300 bg-red-50' : 'border-gray-200'}`}>
        <div className="flex items-start justify-between gap-2 mb-3">
          <div>
            <p className="text-xs text-gray-500 font-mono">{incident.incidentNumber}</p>
            <p className="font-semibold text-gray-900 mt-0.5">{incident.aircraftTailNumber}</p>
          </div>
          <div className="flex flex-col items-end gap-1">
            <SeverityBadge severity={incident.severity} />
            <StatusBadge status={incident.status} />
          </div>
        </div>
        <p className="text-sm text-gray-600">{incident.faultType}</p>
        <div className="flex items-center gap-1 mt-3 text-xs text-gray-400">
          <Clock size={12} />
          <span>{formatDate(incident.createdAt)}</span>
        </div>
      </div>
    </Link>
  );
}
