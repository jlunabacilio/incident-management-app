'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/hooks/useAuth';
import { Button } from '@/components/ui/Button';
import { FormField, Input } from '@/components/ui/Input';

export default function LoginPage() {
  const { login } = useAuth();
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(email, password);
      router.push('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-sm bg-white rounded-2xl shadow-lg p-8">
        <div className="text-center mb-8">
          <div className="text-4xl mb-3">✈</div>
          <h1 className="text-xl font-bold text-gray-900">Incident Management</h1>
          <p className="text-sm text-gray-500 mt-1">Aviation Maintenance System</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <FormField label="Email">
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)}
              placeholder="tech@demo.com" autoComplete="email" required />
          </FormField>
          <FormField label="Password">
            <Input type="password" value={password} onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••" autoComplete="current-password" required />
          </FormField>

          {error && <p className="text-sm text-red-600 text-center">{error}</p>}

          <Button type="submit" loading={loading} className="w-full mt-2">Sign in</Button>
        </form>

        <div className="mt-6 p-3 bg-gray-50 rounded-lg text-xs text-gray-500 space-y-1">
          <p className="font-medium text-gray-600">Demo accounts:</p>
          <p>tech@demo.com / Password123!</p>
          <p>supervisor@demo.com / Password123!</p>
          <p>safety@demo.com / Password123!</p>
        </div>
      </div>
    </div>
  );
}
