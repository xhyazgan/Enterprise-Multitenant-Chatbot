import { useEffect } from 'react';
import { useChatStore } from '../stores/chatStore';
import { useTenantTheme } from '../context/TenantThemeContext';

export default function SessionList() {
  const {
    sessions,
    activeSessionId,
    loading,
    loadSessions,
    selectSession,
    deleteSession,
    clearActiveSession,
  } = useChatStore();
  const { accentColor } = useTenantTheme();

  useEffect(() => {
    loadSessions();
  }, [loadSessions]);

  return (
    <div className="flex flex-col h-full">
      <div className="p-3">
        <button
          onClick={clearActiveSession}
          className="w-full px-4 py-2.5 text-white text-sm rounded-xl font-medium
                     transition-all hover:opacity-90 active:scale-[0.98]"
          style={{ backgroundColor: accentColor }}
        >
          + New Chat
        </button>
      </div>

      <div className="flex-1 overflow-y-auto px-2">
        {loading && sessions.length === 0 ? (
          <div className="p-4 text-gray-500 text-center text-sm">Loading...</div>
        ) : sessions.length === 0 ? (
          <div className="p-4 text-gray-600 text-center text-xs">
            No conversations yet
          </div>
        ) : (
          <div className="space-y-0.5">
            {sessions.map((session) => {
              const isActive = activeSessionId === session.id;
              return (
                <div
                  key={session.id}
                  className={`group flex items-center px-3 py-2.5 cursor-pointer rounded-lg
                             transition-colors text-sm ${
                               isActive ? 'bg-gray-800/80' : 'hover:bg-gray-800/40'
                             }`}
                  style={isActive ? { borderLeft: `2px solid ${accentColor}` } : { borderLeft: '2px solid transparent' }}
                  onClick={() => selectSession(session.id)}
                >
                  <span className={`flex-1 truncate ${isActive ? 'text-gray-200' : 'text-gray-400'}`}>
                    {session.title}
                  </span>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      deleteSession(session.id);
                    }}
                    className="opacity-0 group-hover:opacity-100 text-gray-600 hover:text-red-400
                               transition-opacity ml-2 text-xs"
                    title="Delete"
                  >
                    &#x2715;
                  </button>
                </div>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
