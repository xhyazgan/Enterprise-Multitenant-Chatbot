import { tenants, getTenantUrl } from '../config/keycloak';

export default function TenantSelector() {
  const handleSelect = (tenantId: string) => {
    window.location.href = getTenantUrl(tenantId);
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-900">
      <div className="max-w-md w-full mx-4">
        <h1 className="text-3xl font-bold text-white text-center mb-2">
          Enterprise Chatbot
        </h1>
        <p className="text-gray-400 text-center mb-8">
          Select your organization to continue
        </p>
        <div className="space-y-3">
          {tenants.map((tenant) => (
            <button
              key={tenant.id}
              onClick={() => handleSelect(tenant.id)}
              className="w-full px-6 py-4 bg-gray-800 hover:bg-gray-700 text-white rounded-lg
                         border-l-4 hover:scale-[1.02] transition-all text-left"
              style={{ borderLeftColor: tenant.color }}
            >
              <div className="flex items-center gap-3">
                <div
                  className="w-3 h-3 rounded-full flex-shrink-0"
                  style={{ backgroundColor: tenant.color }}
                />
                <div>
                  <div className="text-lg font-semibold">{tenant.name}</div>
                  <div className="text-sm text-gray-400 mt-0.5">{tenant.description}</div>
                </div>
              </div>
              <div className="mt-2 ml-6 text-xs text-gray-500 font-mono">
                {tenant.id}.localhost
              </div>
            </button>
          ))}
        </div>
        <p className="text-gray-600 text-center text-xs mt-6">
          Each organization runs on its own subdomain with isolated sessions
        </p>
      </div>
    </div>
  );
}
