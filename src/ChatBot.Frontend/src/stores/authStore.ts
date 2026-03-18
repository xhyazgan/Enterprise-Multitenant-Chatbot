import { create } from 'zustand';
import Keycloak from 'keycloak-js';
import { createKeycloakInstance, getSubdomainId, getTenantUrl, getBaseUrl } from '../config/keycloak';
import type { TenantInfo } from '../types';

interface UserProfile {
  name: string;
  email: string;
  roles: string[];
}

interface AuthState {
  keycloak: Keycloak | null;
  token: string | null;
  authenticated: boolean;
  tenantId: string | null;
  loading: boolean;
  initialized: boolean;
  user: UserProfile | null;
  currentTenant: TenantInfo | null;
  login: (tenantId: string) => void;
  logout: () => Promise<void>;
  switchTenant: () => void;
  refreshToken: () => Promise<void>;
  restoreSession: () => Promise<void>;
}

function parseUserFromToken(kc: Keycloak): UserProfile {
  const parsed = kc.tokenParsed as Record<string, unknown> | undefined;
  const name = (parsed?.['preferred_username'] as string)
    ?? (parsed?.['name'] as string)
    ?? 'User';
  const email = (parsed?.['email'] as string) ?? '';
  const realmRoles = (parsed?.['realm_access'] as { roles?: string[] })?.roles ?? [];
  const directRoles = (parsed?.['roles'] as string[]) ?? [];
  const roles = realmRoles.length > 0 ? realmRoles : directRoles;
  return { name, email, roles };
}

function startTokenRefresh(kc: Keycloak, set: (state: Partial<AuthState>) => void, get: () => AuthState) {
  setInterval(async () => {
    try {
      const refreshed = await kc.updateToken(70);
      if (refreshed) {
        set({ token: kc.token || null, user: parseUserFromToken(kc) });
      }
    } catch {
      get().logout();
    }
  }, 60000);
}

function setAuthenticated(
  kc: Keycloak,
  tenant: TenantInfo,
  set: (state: Partial<AuthState>) => void,
  get: () => AuthState
) {
  sessionStorage.removeItem(`kc_init_${tenant.id}`);
  set({
    keycloak: kc,
    token: kc.token || null,
    authenticated: true,
    tenantId: tenant.id,
    currentTenant: tenant,
    loading: false,
    initialized: true,
    user: parseUserFromToken(kc),
  });
  startTokenRefresh(kc, set, get);
}

let _initPromise: Promise<void> | null = null;

export const useAuthStore = create<AuthState>((set, get) => ({
  keycloak: null,
  token: null,
  authenticated: false,
  tenantId: null,
  loading: false,
  initialized: false,
  user: null,
  currentTenant: null,

  login: (tenantId: string) => {
    window.location.href = getTenantUrl(tenantId);
  },

  restoreSession: async () => {
    if (_initPromise) return _initPromise;

    _initPromise = (async () => {
      const subdomain = getSubdomainId();

      if (!subdomain) {
        set({ initialized: true });
        return;
      }

      if (window.location.hash.includes('error=')) {
        console.warn('[auth] Keycloak returned error, clearing');
        window.history.replaceState(null, '', window.location.pathname);
        set({ initialized: true });
        return;
      }

      // Fetch tenant info from API
      const { useTenantStore } = await import('./tenantStore');
      const tenantStore = useTenantStore.getState();
      await tenantStore.fetchTenants();
      const tenant = tenantStore.getTenantById(subdomain);

      if (!tenant) {
        console.warn('[auth] Unknown tenant subdomain:', subdomain);
        window.location.href = getBaseUrl();
        return;
      }

      // Redirect loop protection: max 3 attempts per page session
      const loopKey = `kc_init_${tenant.id}`;
      const attempts = parseInt(sessionStorage.getItem(loopKey) || '0');
      if (attempts > 2) {
        console.error('[auth] Redirect loop detected, stopping after', attempts, 'attempts');
        sessionStorage.removeItem(loopKey);
        window.history.replaceState(null, '', window.location.pathname);
        set({ initialized: true });
        return;
      }
      sessionStorage.setItem(loopKey, String(attempts + 1));

      const hasAuthCode = window.location.hash.includes('code=');
      console.log('[auth] Init for', tenant.id, '| hasCode:', hasAuthCode, '| attempt:', attempts + 1);

      set({ loading: true, currentTenant: tenant, tenantId: tenant.id });

      try {
        const kc = createKeycloakInstance(tenant.realm);
        const authenticated = await kc.init({
          onLoad: 'login-required',
          pkceMethod: 'S256',
        });

        console.log('[auth] Keycloak init result:', authenticated);

        if (authenticated) {
          setAuthenticated(kc, tenant, set, get);
        } else {
          console.warn('[auth] Not authenticated after init');
          set({ loading: false, initialized: true });
        }
      } catch (error) {
        console.error('[auth] Keycloak init failed:', error);
        sessionStorage.removeItem(loopKey);
        set({ loading: false, initialized: true });
      }
    })();

    return _initPromise;
  },

  switchTenant: () => {
    const { keycloak } = get();
    const base = getBaseUrl();
    if (keycloak) {
      keycloak.logout({ redirectUri: base });
    } else {
      window.location.href = base;
    }
  },

  logout: async () => {
    const { keycloak } = get();
    const base = getBaseUrl();
    if (keycloak) {
      await keycloak.logout({ redirectUri: base });
    }
    set({
      keycloak: null,
      token: null,
      authenticated: false,
      tenantId: null,
      currentTenant: null,
      initialized: true,
      user: null,
    });
  },

  refreshToken: async () => {
    const { keycloak } = get();
    if (keycloak) {
      try {
        await keycloak.updateToken(30);
        set({ token: keycloak.token || null, user: parseUserFromToken(keycloak) });
      } catch {
        get().logout();
      }
    }
  },
}));
