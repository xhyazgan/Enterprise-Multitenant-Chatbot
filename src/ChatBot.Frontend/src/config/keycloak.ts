import Keycloak from 'keycloak-js';
import type { TenantInfo } from '../types';

export const tenants: TenantInfo[] = [
  {
    id: 'basiccorp',
    name: 'BasicCorp',
    realm: 'basiccorp',
    color: '#3B82F6',
    description: 'Enterprise — Strict security, short sessions, 4-tier roles',
    aiModel: 'Claude Haiku 4.5',
  },
  {
    id: 'ssohub',
    name: 'SSOHub',
    realm: 'ssohub',
    color: '#10B981',
    description: 'Tech Company — OTP for admins, long sessions, audit logging',
    aiModel: 'Claude Haiku 4.5',
  },
  {
    id: 'startupxyz',
    name: 'StartupXYZ',
    realm: 'startupxyz',
    color: '#F59E0B',
    description: 'Startup — Self-registration, relaxed policy, flat roles',
    aiModel: 'Claude Haiku 4.5',
  },
];

const keycloakUrl = import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8080';

export function createKeycloakInstance(realm: string): Keycloak {
  return new Keycloak({
    url: keycloakUrl,
    realm,
    clientId: 'chatbot-frontend',
  });
}

export function getTenantFromSubdomain(): TenantInfo | null {
  const hostname = window.location.hostname;
  const parts = hostname.split('.');
  if (parts.length >= 2 && parts[parts.length - 1] === 'localhost') {
    const subdomain = parts[0];
    return tenants.find((t) => t.id === subdomain) ?? null;
  }
  return null;
}

export function getBaseUrl(): string {
  const port = window.location.port;
  return `http://localhost${port ? `:${port}` : ''}`;
}

export function getTenantUrl(tenantId: string): string {
  const port = window.location.port;
  return `http://${tenantId}.localhost${port ? `:${port}` : ''}`;
}
