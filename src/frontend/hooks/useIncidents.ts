'use client';

import { useState, useCallback } from 'react';
import { getIncidents, getDashboard } from '@/lib/api';
import type { IncidentFilters, IncidentSummaryDto, DashboardSummaryDto, PagedResult } from '@/types';

export function useIncidents(initialFilters: IncidentFilters) {
  const [filters, setFilters] = useState<IncidentFilters>(initialFilters);
  const [result, setResult] = useState<PagedResult<IncidentSummaryDto> | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetch = useCallback(async (f?: IncidentFilters) => {
    const active = f ?? filters;
    setLoading(true);
    setError(null);
    try {
      const data = await getIncidents(active);
      setResult(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load incidents');
    } finally {
      setLoading(false);
    }
  }, [filters]);

  function updateFilters(partial: Partial<IncidentFilters>) {
    const next = { ...filters, ...partial, page: 1 };
    setFilters(next);
    fetch(next);
  }

  return { result, loading, error, filters, fetch, updateFilters };
}

export function useDashboard() {
  const [data, setData] = useState<DashboardSummaryDto | null>(null);
  const [loading, setLoading] = useState(false);

  const fetch = useCallback(async () => {
    setLoading(true);
    try {
      setData(await getDashboard());
    } finally {
      setLoading(false);
    }
  }, []);

  return { data, loading, fetch };
}
