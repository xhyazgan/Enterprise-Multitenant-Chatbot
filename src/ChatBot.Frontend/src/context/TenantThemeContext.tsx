import { createContext, useContext } from 'react';
import type { TenantInfo } from '../types';

interface TenantTheme {
  tenant: TenantInfo;
  accentColor: string;
}

const TenantThemeContext = createContext<TenantTheme | null>(null);

export function TenantThemeProvider({
  tenant,
  children,
}: {
  tenant: TenantInfo;
  children: React.ReactNode;
}) {
  return (
    <TenantThemeContext.Provider value={{ tenant, accentColor: tenant.color }}>
      {children}
    </TenantThemeContext.Provider>
  );
}

export function useTenantTheme(): TenantTheme {
  const ctx = useContext(TenantThemeContext);
  if (!ctx) throw new Error('useTenantTheme must be used within TenantThemeProvider');
  return ctx;
}
