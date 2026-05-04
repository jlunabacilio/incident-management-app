'use client';

import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { LogOut } from 'lucide-react';
import { useAuth } from '@/hooks/useAuth';

export function Navbar() {
  const { user, logout } = useAuth();
  const router = useRouter();

  function handleLogout() {
    logout();
    router.push('/login');
  }

  if (!user) return null;

  return (
    <nav className="bg-white border-b border-gray-200 px-6 py-4 flex items-center justify-between">
      <div className="flex items-center gap-3">
        <Link href="/" className="flex items-center gap-3">
          <span className="text-2xl">✈</span>
          <span className="font-semibold text-gray-900">Incident Management</span>
        </Link>
      </div>
      <div className="flex items-center gap-4">
        <span className="text-sm text-gray-600">
          {user.fullName} <span className="text-gray-400">({user.role})</span>
        </span>
        <Link href="/incidents" className="text-sm text-blue-600 hover:underline">
          Incidents
        </Link>
        <button
          onClick={handleLogout}
          className="flex items-center gap-1 text-sm text-red-600 hover:text-red-800 transition-colors"
          aria-label="Log out"
        >
          <LogOut size={16} />
          Logout
        </button>
      </div>
    </nav>
  );
}
