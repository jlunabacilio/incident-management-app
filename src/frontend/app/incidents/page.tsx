'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Plus, RefreshCw } from 'lucide-react';
import Link from 'next/link';
import { useAuth } from '@/hooks/useAuth';
import { useIncidents } from '@/hooks/useIncidents';
import { IncidentCard } from '@/components/incidents/IncidentCard';
import { IncidentFiltersPanel } from '@/components/incidents/IncidentFiltersPanel';
import { Button } from '@/components/ui/Button';

const DEFAULT_FILTERS = { page: 1, pageSize: 20 };

export default function IncidentsPage() {
  const { user, loading: authLoading } = useAuth();
  const router = useRouter();
  const { result, loading, error, filters, fetch, updateFilters } = useIncidents(DEFAULT_FILTERS);

  useEffect(() => {
    if (!authLoading && !user) router.push('/login');
  }, [user, authLoading, router]);

  useEffect(() => {
    if (user) fetch();
  }, [user]);

  if (authLoading || !user) return null;

  const totalPages = result ? Math.ceil(result.total / result.pageSize) : 0;

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Link href="/" className="text-2xl">✈</Link>
          <span className="font-semibold text-gray-900">Incidents</span>
        </div>
        <div className="flex items-center gap-3">
          <Button variant="ghost" onClick={() => fetch()} className="gap-2">
            <RefreshCw size={16} /> Refresh
          </Button>
          <Link href="/incidents/new">
            <Button className="gap-2"><Plus size={16} /> New Incident</Button>
          </Link>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto px-6 py-6 space-y-4">
        <IncidentFiltersPanel
          filters={filters}
          onChange={updateFilters}
          onReset={() => updateFilters(DEFAULT_FILTERS)}
        />

        {error && <p className="text-red-600 text-sm">{error}</p>}

        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {Array.from({ length: 6 }).map((_, i) => (
              <div key={i} className="bg-white rounded-xl border border-gray-200 p-4 h-32 animate-pulse" />
            ))}
          </div>
        ) : result?.items.length === 0 ? (
          <div className="text-center py-16 text-gray-500">
            <p className="text-lg">No incidents found.</p>
            <Link href="/incidents/new" className="mt-4 inline-block text-blue-600 hover:underline text-sm">Register the first one →</Link>
          </div>
        ) : (
          <>
            <p className="text-sm text-gray-500">{result?.total} incident{result?.total !== 1 ? 's' : ''} found</p>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
              {result?.items.map((inc) => <IncidentCard key={inc.id} incident={inc} />)}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-center gap-2 pt-4">
                <Button variant="secondary" disabled={filters.page <= 1}
                  onClick={() => updateFilters({ page: filters.page - 1 })}>Previous</Button>
                <span className="text-sm text-gray-600">Page {filters.page} of {totalPages}</span>
                <Button variant="secondary" disabled={filters.page >= totalPages}
                  onClick={() => updateFilters({ page: filters.page + 1 })}>Next</Button>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}
