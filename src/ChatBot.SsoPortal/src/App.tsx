import { useEffect, useState, useCallback, useRef } from 'react';
import Keycloak from 'keycloak-js';

interface TenantInfo {
  id: string;
  name: string;
  realm: string;
  color: string;
}

const tenants: TenantInfo[] = [
  { id: 'basiccorp', name: 'BasicCorp', realm: 'basiccorp', color: '#3B82F6' },
  { id: 'ssohub', name: 'SSOHub', realm: 'ssohub', color: '#10B981' },
  { id: 'startupxyz', name: 'StartupXYZ', realm: 'startupxyz', color: '#F59E0B' },
];

function getTenantFromSubdomain(): TenantInfo | null {
  const hostname = window.location.hostname;
  const parts = hostname.split('.');
  if (parts.length >= 2 && parts[parts.length - 1] === 'localhost') {
    const subdomain = parts[0];
    return tenants.find((t) => t.id === subdomain) ?? null;
  }
  return null;
}

function getTenantUrl(tenantId: string): string {
  const port = window.location.port;
  return `http://${tenantId}.localhost${port ? `:${port}` : ''}`;
}

const keycloakUrl = import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8080';

function decodeToken(token: string): Record<string, unknown> {
  try {
    const payload = token.split('.')[1];
    return JSON.parse(atob(payload));
  } catch {
    return {};
  }
}

function formatDate(epoch: number): string {
  return new Date(epoch * 1000).toLocaleTimeString('tr-TR', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

function App() {
  const [keycloak, setKeycloak] = useState<Keycloak | null>(null);
  const [authenticated, setAuthenticated] = useState(false);
  const [initialized, setInitialized] = useState(false);
  const [ssoDetected, setSsoDetected] = useState(false);
  const initStartTime = useRef(0);
  const initCalled = useRef(false);

  const tenant = getTenantFromSubdomain();

  const init = useCallback(async () => {
    if (initCalled.current) return;
    initCalled.current = true;

    if (!tenant) {
      setInitialized(true);
      return;
    }

    const kc = new Keycloak({
      url: keycloakUrl,
      realm: tenant.realm,
      clientId: 'sso-portal',
    });

    initStartTime.current = Date.now();

    try {
      const auth = await kc.init({
        onLoad: 'login-required',
        pkceMethod: 'S256',
      });

      setKeycloak(kc);
      setAuthenticated(auth);
      setInitialized(true);

      // If login completed in under 3 seconds, SSO was used (no manual password entry)
      const elapsed = Date.now() - initStartTime.current;
      if (auth && elapsed < 3000) {
        setSsoDetected(true);
      }
    } catch (error) {
      console.error('Keycloak init failed:', error);
      setInitialized(true);
    }
  }, [tenant]);

  useEffect(() => {
    if (!initialized) {
      init();
    }
  }, [initialized, init]);

  // Tenant selection screen
  if (!tenant) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center">
        <div className="max-w-md w-full mx-4">
          <div className="text-center mb-8">
            <div className="text-3xl font-bold text-white mb-2">SSO Admin Portal</div>
            <p className="text-gray-400">Select your organization to continue</p>
          </div>
          <div className="space-y-3">
            {tenants.map((t) => (
              <button
                key={t.id}
                onClick={() => { window.location.href = getTenantUrl(t.id); }}
                className="w-full p-4 rounded-xl border border-gray-800 hover:border-gray-600 bg-gray-900 hover:bg-gray-800/80 transition-all text-left flex items-center gap-3"
              >
                <div
                  className="w-10 h-10 rounded-lg flex items-center justify-center text-lg font-bold"
                  style={{ backgroundColor: t.color + '20', color: t.color }}
                >
                  {t.name.charAt(0)}
                </div>
                <div>
                  <div className="text-white font-semibold">{t.name}</div>
                  <div className="text-gray-500 text-sm">{t.id}.localhost{window.location.port ? `:${window.location.port}` : ''}</div>
                </div>
              </button>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (!initialized) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center">
        <div className="text-white text-xl">Connecting to Keycloak...</div>
      </div>
    );
  }

  if (!authenticated || !keycloak?.token) {
    return (
      <div className="min-h-screen bg-gray-950 flex items-center justify-center">
        <div className="text-white text-xl">Redirecting to login...</div>
      </div>
    );
  }

  const parsed = keycloak.tokenParsed as Record<string, unknown> | undefined;
  const username = (parsed?.['preferred_username'] as string) ?? 'User';
  const email = (parsed?.['email'] as string) ?? '';
  const firstName = (parsed?.['given_name'] as string) ?? '';
  const realmRoles = (parsed?.['realm_access'] as { roles?: string[] })?.roles ?? [];
  const roles = realmRoles.filter((r) => !r.startsWith('default-roles-'));
  const tenantId = (parsed?.['tenant_id'] as string) ?? tenant.id;
  const iat = parsed?.['iat'] as number | undefined;
  const exp = parsed?.['exp'] as number | undefined;
  const iss = parsed?.['iss'] as string | undefined;
  const azp = parsed?.['azp'] as string | undefined;

  const rawDecoded = decodeToken(keycloak.token);

  const chatbotPort = '5173';
  const chatbotUrl = `http://${tenant.id}.localhost:${chatbotPort}`;

  const accentColor = tenant.color;

  return (
    <div className="min-h-screen bg-gray-950 text-white">
      {/* Header */}
      <header className="border-b border-gray-800/50">
        <div className="max-w-5xl mx-auto px-6 py-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div
              className="w-9 h-9 rounded-lg flex items-center justify-center text-sm font-bold"
              style={{ backgroundColor: accentColor + '20', color: accentColor }}
            >
              {tenant.name.charAt(0)}
            </div>
            <div>
              <div className="text-lg font-semibold">{tenant.name} Admin Portal</div>
              <div className="text-xs text-gray-500">SSO Demo Application</div>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <a
              href={chatbotUrl}
              className="px-3 py-1.5 text-xs rounded-lg border border-gray-700 hover:border-gray-500 text-gray-400 hover:text-white transition-colors"
            >
              Open Chatbot
            </a>
            <button
              onClick={() => keycloak.logout({ redirectUri: window.location.origin })}
              className="px-3 py-1.5 text-xs rounded-lg bg-gray-800 hover:bg-gray-700 text-gray-300 transition-colors"
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      <main className="max-w-5xl mx-auto px-6 py-8 space-y-6">
        {/* SSO Status Banner */}
        {ssoDetected ? (
          <div
            className="rounded-xl p-5 border"
            style={{
              backgroundColor: '#10B98115',
              borderColor: '#10B98140',
            }}
          >
            <div className="flex items-center gap-3">
              <div className="text-2xl">&#x2713;</div>
              <div>
                <div className="text-green-400 font-semibold text-lg">
                  SSO Login Successful!
                </div>
                <div className="text-green-400/70 text-sm">
                  Welcome {firstName || username}! You were automatically logged in via Single Sign-On.
                  No password was required because you already have an active Keycloak session.
                </div>
              </div>
            </div>
          </div>
        ) : (
          <div
            className="rounded-xl p-5 border"
            style={{
              backgroundColor: accentColor + '10',
              borderColor: accentColor + '30',
            }}
          >
            <div className="flex items-center gap-3">
              <div className="text-2xl">&#x1F511;</div>
              <div>
                <div style={{ color: accentColor }} className="font-semibold text-lg">
                  Welcome, {firstName || username}!
                </div>
                <div className="text-gray-400 text-sm">
                  You are logged into the Admin Portal. To see SSO in action, first log into the
                  Chatbot app, then open this portal — you'll be logged in automatically!
                </div>
              </div>
            </div>
          </div>
        )}

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {/* User Info Card */}
          <div className="bg-gray-900 rounded-xl border border-gray-800/50 p-6">
            <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">
              User Information
            </h2>
            <div className="space-y-3">
              <InfoRow label="Username" value={username} />
              <InfoRow label="Email" value={email} />
              <InfoRow label="First Name" value={firstName} />
              <InfoRow label="Tenant" value={tenantId} />
              <div>
                <span className="text-xs text-gray-500">Roles</span>
                <div className="flex flex-wrap gap-1.5 mt-1">
                  {roles.map((role) => (
                    <span
                      key={role}
                      className="px-2 py-0.5 rounded-md text-xs font-medium"
                      style={{ backgroundColor: accentColor + '20', color: accentColor }}
                    >
                      {role}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Token Info Card */}
          <div className="bg-gray-900 rounded-xl border border-gray-800/50 p-6">
            <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">
              Token Details
            </h2>
            <div className="space-y-3">
              <InfoRow label="Client ID" value={azp ?? 'sso-portal'} />
              <InfoRow label="Issuer" value={iss ?? ''} />
              <InfoRow label="Issued At" value={iat ? formatDate(iat) : '-'} />
              <InfoRow label="Expires At" value={exp ? formatDate(exp) : '-'} />
              <InfoRow
                label="Token Lifespan"
                value={iat && exp ? `${Math.round((exp - iat) / 60)} minutes` : '-'}
              />
            </div>
          </div>

          {/* SSO Explanation Card */}
          <div className="bg-gray-900 rounded-xl border border-gray-800/50 p-6 md:col-span-2">
            <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">
              How SSO Works
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
              <StepCard
                step="1"
                title="Login to App 1"
                desc="User logs into Chatbot with username & password"
                color={accentColor}
              />
              <StepCard
                step="2"
                title="Keycloak Session"
                desc="Keycloak creates a session cookie on its domain"
                color={accentColor}
              />
              <StepCard
                step="3"
                title="Open App 2"
                desc="User opens Admin Portal (same realm, different client)"
                color={accentColor}
              />
              <StepCard
                step="4"
                title="Auto Login!"
                desc="Keycloak sees the cookie → issues token without password"
                color="#10B981"
              />
            </div>
          </div>

          {/* Raw Token Card */}
          <div className="bg-gray-900 rounded-xl border border-gray-800/50 p-6 md:col-span-2">
            <h2 className="text-sm font-semibold text-gray-400 uppercase tracking-wider mb-4">
              Decoded JWT Token
            </h2>
            <pre className="bg-gray-950 rounded-lg p-4 text-xs text-gray-300 overflow-x-auto max-h-64 overflow-y-auto">
              {JSON.stringify(rawDecoded, null, 2)}
            </pre>
          </div>
        </div>
      </main>
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <span className="text-xs text-gray-500">{label}</span>
      <div className="text-sm text-white truncate">{value}</div>
    </div>
  );
}

function StepCard({
  step,
  title,
  desc,
  color,
}: {
  step: string;
  title: string;
  desc: string;
  color: string;
}) {
  return (
    <div className="bg-gray-950 rounded-lg p-4 relative">
      <div
        className="w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold mb-3"
        style={{ backgroundColor: color + '20', color }}
      >
        {step}
      </div>
      <div className="text-sm font-medium text-white mb-1">{title}</div>
      <div className="text-xs text-gray-400">{desc}</div>
    </div>
  );
}
export default App;

