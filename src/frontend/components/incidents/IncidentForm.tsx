'use client';

import { useState } from 'react';
import { AlertTriangle, Upload, X } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { FormField, Input, Select, Textarea } from '@/components/ui/Input';
import { createIncident } from '@/lib/api';
import { formatBytes } from '@/lib/utils';
import type { CreateIncidentInput, FaultType, Severity } from '@/types';

const FAULT_TYPES: FaultType[] = ['Mechanical', 'Electrical', 'Hydraulic', 'Avionics'];
const SEVERITIES: Severity[] = ['Low', 'Medium', 'High', 'Critical'];

interface Props {
  onSuccess: () => void;
  onCancel: () => void;
}

export function IncidentForm({ onSuccess, onCancel }: Props) {
  const [form, setForm] = useState<Omit<CreateIncidentInput, 'attachments'>>({
    aircraftTailNumber: '',
    faultType: 'Mechanical',
    severity: 'Low',
    description: '',
    stepsToReproduce: '',
    technicianId: '',
    supervisorId: '',
  });
  const [files, setFiles] = useState<File[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);

  function set(field: string, value: string) {
    setForm((f) => ({ ...f, [field]: value }));
    setErrors((e) => ({ ...e, [field]: '' }));
  }

  function validate(): boolean {
    const e: Record<string, string> = {};
    if (!form.aircraftTailNumber.trim()) e.aircraftTailNumber = 'Tail number is required.';
    if (form.description.length < 20) e.description = 'Description must be at least 20 characters.';
    if (form.stepsToReproduce.length < 10) e.stepsToReproduce = 'Steps must be at least 10 characters.';
    if (!form.technicianId.trim()) e.technicianId = 'Technician ID is required.';
    if (!form.supervisorId.trim()) e.supervisorId = 'Supervisor ID is required.';
    setErrors(e);
    return Object.keys(e).length === 0;
  }

  function handleFiles(incoming: FileList | null) {
    if (!incoming) return;
    const all = [...files, ...Array.from(incoming)].slice(0, 10);
    setFiles(all);
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!validate()) return;
    setLoading(true);
    try {
      await createIncident({ ...form, attachments: files });
      setSubmitted(true);
    } catch (err) {
      setErrors({ submit: err instanceof Error ? err.message : 'Submission failed.' });
    } finally {
      setLoading(false);
    }
  }

  if (submitted) {
    return (
      <div className="text-center py-8">
        <div className="text-green-600 text-5xl mb-4">✓</div>
        <h3 className="text-lg font-semibold text-gray-900 mb-2">Incident registered</h3>
        {form.severity === 'Critical' && (
          <div className="mt-4 p-4 bg-red-50 border border-red-200 rounded-lg text-red-800 text-sm">
            ⚠ Aircraft <strong>{form.aircraftTailNumber}</strong> has been grounded. Dispatch is blocked until this incident is resolved.
          </div>
        )}
        <Button className="mt-6" onClick={onSuccess}>View incidents</Button>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      {form.severity === 'Critical' && (
        <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-300 rounded-lg text-red-800 text-sm">
          <AlertTriangle size={18} className="mt-0.5 shrink-0" />
          <span>Selecting <strong>CRITICAL</strong> will immediately ground this aircraft and block dispatch.</span>
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField label="Aircraft Tail Number *" error={errors.aircraftTailNumber}>
          <Input
            value={form.aircraftTailNumber}
            onChange={(e) => set('aircraftTailNumber', e.target.value.toUpperCase())}
            placeholder="e.g. PP-XKA"
          />
        </FormField>

        <FormField label="Fault Type *">
          <Select value={form.faultType} onChange={(e) => set('faultType', e.target.value)}>
            {FAULT_TYPES.map((ft) => <option key={ft}>{ft}</option>)}
          </Select>
        </FormField>

        <FormField label="Severity *">
          <Select value={form.severity} onChange={(e) => set('severity', e.target.value)}>
            {SEVERITIES.map((s) => <option key={s}>{s}</option>)}
          </Select>
        </FormField>

        <FormField label="Technician ID *" error={errors.technicianId}>
          <Input value={form.technicianId} onChange={(e) => set('technicianId', e.target.value)} placeholder="usr_tech_001" />
        </FormField>

        <FormField label="Supervisor ID *" error={errors.supervisorId}>
          <Input value={form.supervisorId} onChange={(e) => set('supervisorId', e.target.value)} placeholder="usr_sup_001" />
        </FormField>
      </div>

      <FormField label="Description * (min 20 chars)" error={errors.description}>
        <Textarea
          rows={4}
          value={form.description}
          onChange={(e) => set('description', e.target.value)}
          placeholder="Describe the fault in detail..."
        />
        <span className="text-xs text-gray-400 text-right">{form.description.length}/4000</span>
      </FormField>

      <FormField label="Steps to Reproduce * (min 10 chars)" error={errors.stepsToReproduce}>
        <Textarea
          rows={3}
          value={form.stepsToReproduce}
          onChange={(e) => set('stepsToReproduce', e.target.value)}
          placeholder="1. Power on system. 2. ..."
        />
      </FormField>

      {/* Attachments */}
      <div>
        <label className="text-sm font-medium text-gray-700">Photos (max 10, 10 MB each)</label>
        <label className="mt-2 flex flex-col items-center justify-center border-2 border-dashed border-gray-300 rounded-lg p-6 cursor-pointer hover:border-blue-400 transition-colors">
          <Upload size={24} className="text-gray-400 mb-2" />
          <span className="text-sm text-gray-500">Click or drag images here</span>
          <input type="file" multiple accept="image/jpeg,image/png,image/webp" className="hidden"
            onChange={(e) => handleFiles(e.target.files)} />
        </label>
        {files.length > 0 && (
          <ul className="mt-2 space-y-1">
            {files.map((f, i) => (
              <li key={i} className="flex items-center justify-between text-sm bg-gray-50 rounded px-3 py-1.5">
                <span className="truncate">{f.name} <span className="text-gray-400">({formatBytes(f.size)})</span></span>
                <button type="button" onClick={() => setFiles(files.filter((_, j) => j !== i))} className="text-gray-400 hover:text-red-500 ml-2">
                  <X size={14} />
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {errors.submit && <p className="text-sm text-red-600">{errors.submit}</p>}

      <div className="flex justify-end gap-3 pt-2">
        <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
        <Button type="submit" loading={loading}
          className={form.severity === 'Critical' ? 'bg-red-600 hover:bg-red-700' : ''}>
          {form.severity === 'Critical' ? '⚠ Submit & Ground Aircraft' : 'Submit Incident'}
        </Button>
      </div>
    </form>
  );
}
