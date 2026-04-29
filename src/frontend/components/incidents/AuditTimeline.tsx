import { formatDate } from '@/lib/utils';
import type { AuditEvent } from '@/types';

interface Props {
  events: AuditEvent[];
}

export function AuditTimeline({ events }: Props) {
  if (events.length === 0) {
    return <p className="text-sm text-gray-500 text-center py-6">No audit events yet.</p>;
  }

  return (
    <ol className="relative border-l border-gray-200 space-y-6 ml-3">
      {events.map((ev) => (
        <li key={ev.id} className="ml-6">
          <span className="absolute -left-3 flex h-6 w-6 items-center justify-center rounded-full bg-blue-100 ring-4 ring-white text-blue-600 text-xs font-bold">
            {ev.eventType[0]}
          </span>
          <div className="bg-gray-50 rounded-lg p-3 border border-gray-100">
            <div className="flex items-center justify-between mb-1">
              <span className="text-xs font-semibold text-gray-700 uppercase tracking-wide">{ev.eventType}</span>
              <span className="text-xs text-gray-400">{formatDate(ev.occurredAt)}</span>
            </div>
            <p className="text-sm text-gray-600">by <strong>{ev.actorName}</strong></p>
            {ev.previousValue && (
              <p className="text-xs text-gray-400 mt-1 font-mono truncate">Before: {ev.previousValue}</p>
            )}
            <p className="text-xs text-gray-500 mt-0.5 font-mono truncate">After: {ev.newValue}</p>
          </div>
        </li>
      ))}
    </ol>
  );
}
