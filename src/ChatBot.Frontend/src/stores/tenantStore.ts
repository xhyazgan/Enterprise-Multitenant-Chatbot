import { create } from 'zustand';
import type { TenantInfo } from '../types';
import { getTenants } from '../services/tenantService';

interface TenantState {
  tenants: TenantInfo[];
  loading: boolean;
  loaded: boolean;
  fetchTenants: () => Promise<void>;
  getTenantById: (id: string) => TenantInfo | undefined;
  reset: () => void;
}

export const useTenantStore = create<TenantState>((set, get) => ({
  tenants: [],
  loading: false,
  loaded: false,

  fetchTenants: async () => {
    if (get().loaded) return;
    set({ loading: true });
    try {
      const data = await getTenants();
      const tenants: TenantInfo[] = data.map(t => ({
        id: t.tenantId,
        name: t.displayName,
        realm: t.tenantId,
        color: t.color,
        description: t.description,
        aiModel: t.aiModel,
      }));
      set({ tenants, loading: false, loaded: true });
    } catch (error) {
      console.error('Failed to fetch tenants:', error);
      set({ loading: false });
    }
  },

  getTenantById: (id: string) => get().tenants.find(t => t.id === id),

  reset: () => set({ tenants: [], loaded: false, loading: false }),
}));
