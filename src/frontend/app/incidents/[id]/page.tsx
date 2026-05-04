'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import Link from 'next/link';
import { ArrowLeft, AlertTriangle, Shield } from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';
import { getIncident, getAuditTrail, transitionIncident, voidIncident, ungroundAircraft } from '@/lib/api';
import { SeverityBadge, StatusBadge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { AuditTimeline } from '@/components/incidents/AuditTimeline';
import { Modal } from '@/components/ui/Modal';
import { Textarea, FormField } from '@/components/ui/Input';
import { formatDate, formatBytes } from '@/lib/utils';
import { hasRole } from '@/lib/auth';
import type { AuditEvent, IncidentDto, IncidentStatus } from '@/types';
import { IncidentStateMachine } from '@/lib/stateMachine';
import { Navbar } from '@/components/ui/Navbar';

export default function IncidentDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user, loading: authLoading } = useAuth();
  const router = useRouter();

  const [incident, setIncident] = useState<IncidentDto | null>(null);
  const [audit, setAudit] = useState<AuditEvent[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [voidOpen, setVoidOpen] = useState(false);
  const [voidJustification, setVoidJustification] = useState('');
  const [actionLoading, setActionLoading] = useState(false);

  useEffect(() => {
    if (!authLoading && !user) router.push('/login');
  }, [user, authLoading, router]);

  useEffect(() => {
    if (!user || !id) return;
    setLoading(true);
    getIncident(id)
      .then((inc) => setIncident(inc))
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));

    // Audit trail is role-restricted — load independently and fail gracefully
    getAuditTrail(id)
      .then((aud) => setAudit(aud))
      .catch(() => setAudit([]));
  }, [user, id]);

  async function handleTransition(newStatus: IncidentStatus) {
    if (!incident) return;
    setActionLoading(true);
    try {
      const updated = await transitionIncident(incident.id, newStatus);
      setIncident(updated);
      const aud = await getAuditTrail(incident.id);
      setAudit(aud);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Transition failed.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleVoid() {
    if (!incident || !voidJustification.trim()) return;
    setActionLoading(true);
    try {
      const updated = await voidIncident(incident.id, voidJustification);
      setIncident(updated);
      setVoidOpen(false);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Void failed.');
    } finally {
      setActionLoading(false);
    }
  }

  async function handleUnground() {
    if (!incident) return;
    setActionLoading(true);
    try {
      await ungroundAircraft(incident.aircraftTailNumber);
      const updated = await getIncident(incident.id);
      setIncident(updated);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Unground failed.');
    } finally {
      setActionLoading(false);
    }
  }

  if (authLoading || !user) return null;
  if (loading) return <div className="min-h-screen flex items-center justify-center text-gray-500">Loading...</div>;
  if (!incident) return <div className="min-h-screen flex items-center justify-center text-red-600">{error || 'Incident not found.'}</div>;

  const allowedTransitions = IncidentStateMachine.getAllowedTransitions(incident.status, user.role);
  const canVoid = hasRole(user, 'Supervisor', 'MaintenanceControl', 'SafetyOfficer', 'ChiefEngineer', 'Admin')
    && (incident.status === 'Open' || incident.status === 'InProgress');
  const canUnground = hasRole(user, 'SafetyOfficer', 'ChiefEngineer') && incident.severity === 'Critical';

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />
      <nav className="bg-white border-b border-gray-200 px-6 py-3 flex items-center gap-4">
        <Link href="/incidents" className="text-gray-500 hover:text-gray-800">
          <ArrowLeft size={20} />
        </Link>
        <span className="font-semibold text-gray-700">{incident.incidentNumber}</span>
        <div className="ml-auto flex items-center gap-2">
          <SeverityBadge severity={incident.severity} />
          <StatusBadge status={incident.status} />
        </div>
      </nav>

      <main className="max-w-4xl mx-auto px-6 py-8 space-y-6">
        {/* CRITICAL grounded banner */}
        {incident.severity === 'Critical' && incident.status !== 'Closed' && (
          <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-300 rounded-xl text-red-800">
            <AlertTriangle size={20} className="mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="font-semibold">AIRCRAFT GROUNDED — {incident.aircraftTailNumber}</p>
              <p className="text-sm mt-0.5">Dispatch is blocked until this CRITICAL incident is resolved and closed.</p>
            </div>
            {canUnground && (
              <Button variant="danger" loading={actionLoading} onClick={handleUnground} className="shrink-0">
                <Shield size={14} /> Unground
              </Button>
            )}
          </div>
        )}

        {error && <p className="text-red-600 text-sm">{error}</p>}

        {/* Details */}
        <div className="bg-white rounded-xl border border-gray-200 p-6 space-y-4">
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
            <div><p className="text-gray-500">Aircraft</p><p className="font-semibold">{incident.aircraftTailNumber}</p></div>
            <div><p className="text-gray-500">Fault Type</p><p className="font-semibold">{incident.faultType}</p></div>
            <div><p className="text-gray-500">Created</p><p className="font-semibold">{formatDate(incident.createdAt)}</p></div>
            <div><p className="text-gray-500">Updated</p><p className="font-semibold">{formatDate(incident.updatedAt)}</p></div>
          </div>

          <div>
            <p className="text-sm text-gray-500 mb-1">Description</p>
            <p className="text-sm text-gray-800 whitespace-pre-wrap">{incident.description}</p>
          </div>

          <div>
            <p className="text-sm text-gray-500 mb-1">Steps to Reproduce</p>
            <p className="text-sm text-gray-800 whitespace-pre-wrap">{incident.stepsToReproduce}</p>
          </div>
        </div>

        {/* Attachments */}
        {incident.attachments.length > 0 && (
          <div className="bg-white rounded-xl border border-gray-200 p-6">
            <h3 className="font-semibold mb-4">Attachments ({incident.attachments.length})</h3>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              {incident.attachments.map((a) => (
                <a key={a.id} href={a.url ?? '#'} target="_blank" rel="noopener noreferrer"
                  className="block rounded-lg border border-gray-200 overflow-hidden hover:shadow-md transition-shadow">
                  {a.url && <img src={a.url} alt={a.fileName} className="w-full h-24 object-cover" />}
                  <div className="p-2">
                    <p className="text-xs text-gray-600 truncate">{a.fileName}</p>
                    <p className="text-xs text-gray-400">{formatBytes(a.sizeBytes)}</p>
                  </div>
                </a>
              ))}
            </div>
          </div>
        )}

        {/* Actions */}
        {(allowedTransitions.length > 0 || canVoid) && (
          <div className="bg-white rounded-xl border border-gray-200 p-6">
            <h3 className="font-semibold mb-4">Actions</h3>
            <div className="flex flex-wrap gap-3">
              {allowedTransitions.map((status) => (
                <Button key={status} loading={actionLoading} onClick={() => handleTransition(status)}>
                  Move to {status.replace(/([A-Z])/g, ' $1').trim()}
                </Button>
              ))}
              {canVoid && (
                <Button variant="danger" onClick={() => setVoidOpen(true)}>Void Incident</Button>
              )}
            </div>
          </div>
        )}

        {/* Audit trail */}
        <div className="bg-white rounded-xl border border-gray-200 p-6">
          <h3 className="font-semibold mb-4">Audit Trail</h3>
          {audit.length > 0 ? (
            <AuditTimeline events={audit} />
          ) : (
            <p className="text-sm text-gray-500">
              {hasRole(user, 'MaintenanceControl', 'SafetyOfficer', 'ChiefEngineer', 'Admin')
                ? 'No audit events recorded yet.'
                : 'You do not have permission to view the audit trail.'}
            </p>
          )}
        </div>
      </main>

      {/* Void modal */}
      <Modal open={voidOpen} onClose={() => setVoidOpen(false)} title="Void Incident">
        <div className="space-y-4">
          <p className="text-sm text-gray-600">Provide a mandatory justification for voiding this incident.</p>
          <FormField label="Justification *">
            <Textarea rows={4} value={voidJustification} onChange={(e) => setVoidJustification(e.target.value)}
              placeholder="Reason for voiding..." />
          </FormField>
          <div className="flex justify-end gap-3">
            <Button variant="secondary" onClick={() => setVoidOpen(false)}>Cancel</Button>
            <Button variant="danger" loading={actionLoading} onClick={handleVoid}
              disabled={!voidJustification.trim()}>Confirm Void</Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
