import Keycloak from 'keycloak-js';

const keycloakUrl = import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8080';

export function createKeycloakInstance(realm: string): Keycloak {
  return new Keycloak({
    url: keycloakUrl,
    realm,
    clientId: 'chatbot-frontend',
  });
}

/**
 * Extract subdomain ID from the hostname.
 * e.g., "basiccorp.localhost:5173" → "basiccorp"
 * e.g., "localhost:5173" → null
 */
export function getSubdomainId(): string | null {
  const hostname = window.location.hostname;
  const parts = hostname.split('.');
  if (parts.length >= 2 && parts[parts.length - 1] === 'localhost') {
    return parts[0];
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
