'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/hooks/useAuth';
import { useDashboard } from '@/hooks/useIncidents';
import { AlertTriangle, Plane, ClipboardList, TrendingUp } from 'lucide-react';
import Link from 'next/link';
import { Navbar } from '@/components/ui/Navbar';

function StatCard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className={`bg-white rounded-xl border p-5 ${color}`}>
      <p className="text-sm text-gray-500">{label}</p>
      <p className="text-3xl font-bold mt-1">{value}</p>
    </div>
  );
}

export default function DashboardPage() {
  const { user, loading: authLoading } = useAuth();
  const { data, loading, fetch } = useDashboard();
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && !user) router.push('/login');
  }, [user, authLoading, router]);

  useEffect(() => {
    if (user) fetch();
  }, [user, fetch]);

  if (authLoading || !user) return null;

  return (
    <div className="min-h-screen bg-gray-50">
      <Navbar />

      <main className="max-w-6xl mx-auto px-6 py-8">
        <h1 className="text-2xl font-bold text-gray-900 mb-6">Fleet Dashboard</h1>

        {loading ? (
          <p className="text-gray-500">Loading...</p>
        ) : data ? (
          <>
            {/* Grounded aircraft alert */}
            {data.groundedAircraft.length > 0 && (
              <div className="mb-6 p-4 bg-red-50 border border-red-300 rounded-xl flex items-start gap-3">
                <AlertTriangle className="text-red-600 mt-0.5 shrink-0" size={20} />
                <div>
                  <p className="font-semibold text-red-800">Grounded Aircraft</p>
                  <p className="text-sm text-red-700 mt-1">
                    {data.groundedAircraft.join(', ')} — dispatch blocked due to CRITICAL incidents.
                  </p>
                </div>
              </div>
            )}

            {/* Stats */}
            <div className="grid grid-cols-2 sm:grid-cols-5 gap-4 mb-8">
              <StatCard label="Total Open" value={data.totalOpen} color="border-blue-200" />
              <StatCard label="Low" value={data.lowCount} color="border-green-200" />
              <StatCard label="Medium" value={data.mediumCount} color="border-yellow-200" />
              <StatCard label="High" value={data.highCount} color="border-orange-200" />
              <StatCard label="Critical" value={data.criticalCount} color="border-red-300" />
            </div>

            {/* Quick links */}
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              <Link href="/incidents" className="bg-white rounded-xl border border-gray-200 p-6 hover:shadow-md transition-shadow flex items-center gap-4">
                <ClipboardList className="text-blue-600" size={28} />
                <div>
                  <p className="font-semibold">All Incidents</p>
                  <p className="text-sm text-gray-500">Search and filter</p>
                </div>
              </Link>
              <Link href="/incidents/new" className="bg-white rounded-xl border border-gray-200 p-6 hover:shadow-md transition-shadow flex items-center gap-4">
                <Plane className="text-green-600" size={28} />
                <div>
                  <p className="font-semibold">Register Incident</p>
                  <p className="text-sm text-gray-500">New fault report</p>
                </div>
              </Link>
              <div className="bg-white rounded-xl border border-gray-200 p-6 flex items-center gap-4 opacity-60">
                <TrendingUp className="text-purple-600" size={28} />
                <div>
                  <p className="font-semibold">Reports</p>
                  <p className="text-sm text-gray-500">Export PDF / CSV</p>
                </div>
              </div>
            </div>
          </>
        ) : null}
      </main>
    </div>
  );
}
