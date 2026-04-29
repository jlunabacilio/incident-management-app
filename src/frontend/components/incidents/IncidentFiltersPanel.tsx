'use client';

import { Select, Input, FormField } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import type { FaultType, IncidentFilters, IncidentStatus, Severity } from '@/types';

interface Props {
  filters: IncidentFilters;
  onChange: (partial: Partial<IncidentFilters>) => void;
  onReset: () => void;
}

export function IncidentFiltersPanel({ filters, onChange, onReset }: Props) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-4">
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
        <FormField label="Tail Number">
          <Input
            placeholder="PP-XKA"
            value={filters.tailNumber ?? ''}
            onChange={(e) => onChange({ tailNumber: e.target.value || undefined })}
          />
        </FormField>

        <FormField label="Fault Type">
          <Select value={filters.faultType ?? ''} onChange={(e) => onChange({ faultType: (e.target.value as FaultType) || undefined })}>
            <option value="">All</option>
            {(['Mechanical', 'Electrical', 'Hydraulic', 'Avionics'] as FaultType[]).map((f) => (
              <option key={f}>{f}</option>
            ))}
          </Select>
        </FormField>

        <FormField label="Severity">
          <Select value={filters.severity ?? ''} onChange={(e) => onChange({ severity: (e.target.value as Severity) || undefined })}>
            <option value="">All</option>
            {(['Low', 'Medium', 'High', 'Critical'] as Severity[]).map((s) => (
              <option key={s}>{s}</option>
            ))}
          </Select>
        </FormField>

        <FormField label="Status">
          <Select value={filters.status ?? ''} onChange={(e) => onChange({ status: (e.target.value as IncidentStatus) || undefined })}>
            <option value="">All</option>
            {(['Open', 'InProgress', 'PendingReview', 'Resolved', 'Closed', 'Voided'] as IncidentStatus[]).map((s) => (
              <option key={s} value={s}>{s.replace(/([A-Z])/g, ' $1').trim()}</option>
            ))}
          </Select>
        </FormField>

        <FormField label="From">
          <Input type="date" value={filters.dateFrom ?? ''} onChange={(e) => onChange({ dateFrom: e.target.value || undefined })} />
        </FormField>

        <FormField label="To">
          <Input type="date" value={filters.dateTo ?? ''} onChange={(e) => onChange({ dateTo: e.target.value || undefined })} />
        </FormField>
      </div>
      <div className="mt-3 flex justify-end">
        <Button variant="ghost" onClick={onReset} className="text-sm">Reset filters</Button>
      </div>
    </div>
  );
}
