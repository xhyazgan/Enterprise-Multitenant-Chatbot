import { useAuthStore } from '../stores/authStore';
import { TenantThemeProvider } from '../context/TenantThemeContext';
import SessionList from './SessionList';
import MessagePanel from './MessagePanel';

export default function ChatLayout() {
  const { logout, switchTenant, currentTenant, user } = useAuthStore();

  if (!currentTenant) return null;

  const accentColor = currentTenant.color;
  const tenant = currentTenant;

  return (
    <TenantThemeProvider tenant={tenant}>
      <div className="flex h-screen bg-gray-900 text-white">
        {/* Sidebar */}
        <div className="w-64 bg-gray-950 flex flex-col border-r border-gray-800/50">
          {/* Tenant header */}
          <div className="p-4">
            <div className="flex items-center gap-2.5">
              <div
                className="w-8 h-8 rounded-lg flex items-center justify-center text-sm font-bold"
                style={{ backgroundColor: accentColor + '20', color: accentColor }}
              >
                {tenant.name.charAt(0)}
              </div>
              <div>
                <div className="text-sm font-semibold text-white">{tenant.name}</div>
                <div className="text-[11px] text-gray-500 leading-tight">{tenant.description}</div>
              </div>
            </div>
          </div>

          {/* User info */}
          <div className="px-4 pb-3 border-b border-gray-800/50">
            <div className="flex items-center gap-2">
              <div
                className="w-6 h-6 rounded-full flex items-center justify-center text-[10px] font-semibold"
                style={{ backgroundColor: accentColor + '30', color: accentColor }}
              >
                {user?.name?.charAt(0)?.toUpperCase() ?? 'U'}
              </div>
              <div className="flex-1 min-w-0">
                <div className="text-xs text-gray-300 truncate">{user?.name}</div>
              </div>
            </div>
            {user?.roles && user.roles.length > 0 && (
              <div className="flex flex-wrap gap-1 mt-2 ml-8">
                {user.roles
                  .filter((r) => !r.startsWith('default-roles-'))
                  .map((role) => (
                    <span
                      key={role}
                      className="px-1.5 py-0.5 rounded text-[10px] font-medium"
                      style={{ backgroundColor: accentColor + '15', color: accentColor }}
                    >
                      {role}
                    </span>
                  ))}
              </div>
            )}
          </div>

          {/* Sessions */}
          <div className="flex-1 overflow-hidden">
            <SessionList />
          </div>

          {/* Actions */}
          <div className="p-3 border-t border-gray-800/50 space-y-1.5">
            <button
              onClick={() => {
                const portalUrl = `http://${tenant.id}.localhost:5174`;
                window.open(portalUrl, '_blank');
              }}
              className="w-full px-3 py-2 text-xs rounded-lg transition-colors flex items-center justify-center gap-1.5"
              style={{ backgroundColor: accentColor + '15', color: accentColor }}
              onMouseEnter={(e) => { e.currentTarget.style.backgroundColor = accentColor + '25'; }}
              onMouseLeave={(e) => { e.currentTarget.style.backgroundColor = accentColor + '15'; }}
            >
              <span>&#x2197;</span> Open Admin Portal (SSO Demo)
            </button>
            <button
              onClick={switchTenant}
              className="w-full px-3 py-2 bg-gray-800/50 hover:bg-gray-800 text-gray-400 hover:text-gray-300
                         rounded-lg transition-colors text-xs"
            >
              Switch Organization
            </button>
            <button
              onClick={logout}
              className="w-full px-3 py-2 hover:bg-gray-800/50 text-gray-500 hover:text-gray-400
                         rounded-lg transition-colors text-xs"
            >
              Logout
            </button>
          </div>
        </div>

        {/* Main chat area */}
        <div className="flex-1 flex flex-col bg-gray-900">
          <MessagePanel />
        </div>
      </div>
    </TenantThemeProvider>
  );
}
