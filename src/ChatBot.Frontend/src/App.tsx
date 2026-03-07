import { useEffect } from 'react';
import { useAuthStore } from './stores/authStore';
import { getTenantFromSubdomain } from './config/keycloak';
import TenantSelector from './components/TenantSelector';
import ChatLayout from './components/ChatLayout';

function App() {
  const { authenticated, initialized, loading, restoreSession } = useAuthStore();

  useEffect(() => {
    if (!initialized) {
      restoreSession();
    }
  }, [initialized, restoreSession]);

  if (!initialized || loading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-900">
        <div className="text-white text-xl">Loading...</div>
      </div>
    );
  }

  const isOnSubdomain = getTenantFromSubdomain() !== null;

  if (!authenticated && !isOnSubdomain) {
    return <TenantSelector />;
  }

  if (!authenticated && isOnSubdomain) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-gray-900">
        <div className="text-white text-xl">Redirecting to login...</div>
      </div>
    );
  }

  return <ChatLayout />;
}

export default App;
