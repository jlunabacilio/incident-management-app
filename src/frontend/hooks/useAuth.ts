'use client';

import { useEffect, useState } from 'react';
import { getAuth, saveAuth, clearAuth } from '@/lib/auth';
import { login as apiLogin } from '@/lib/api';
import type { AuthUser } from '@/types';

export function useAuth() {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setUser(getAuth());
    setLoading(false);
  }, []);

  async function login(email: string, password: string): Promise<void> {
    const res = await apiLogin(email, password);
    const authUser: AuthUser = {
      token: res.token,
      userId: res.userId,
      fullName: res.fullName,
      role: res.role,
    };
    saveAuth(authUser);
    setUser(authUser);
  }

  function logout(): void {
    clearAuth();
    setUser(null);
  }

  return { user, loading, login, logout };
}
