import { useState } from 'react';
import { useChatStore } from '../stores/chatStore';
import { useTenantTheme } from '../context/TenantThemeContext';

export default function MessageInput() {
  const [input, setInput] = useState('');
  const { sendMessage, sending } = useChatStore();
  const { accentColor } = useTenantTheme();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const message = input.trim();
    if (!message || sending) return;

    setInput('');
    await sendMessage(message);
  };

  return (
    <form onSubmit={handleSubmit} className="px-6 py-4 border-t border-gray-800/50">
      <div className="flex gap-3 max-w-3xl mx-auto">
        <input
          type="text"
          value={input}
          onChange={(e) => setInput(e.target.value)}
          placeholder={sending ? 'Waiting for response...' : 'Type a message...'}
          disabled={sending}
          className="flex-1 px-4 py-3 bg-gray-800/50 text-white text-sm rounded-xl border border-gray-700/50
                     focus:outline-none placeholder-gray-500
                     disabled:opacity-40 transition-colors"
          style={{
            borderColor: input.trim() ? accentColor + '60' : undefined,
          }}
        />
        <button
          type="submit"
          disabled={sending || !input.trim()}
          className="px-5 py-3 text-white text-sm rounded-xl font-medium
                     transition-all disabled:opacity-30 disabled:cursor-not-allowed
                     hover:opacity-90 active:scale-95"
          style={{ backgroundColor: accentColor }}
        >
          <svg
            className="w-4 h-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            strokeWidth={2}
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M6 12 3.269 3.125A59.769 59.769 0 0 1 21.485 12 59.768 59.768 0 0 1 3.27 20.875L5.999 12Zm0 0h7.5"
            />
          </svg>
        </button>
      </div>
    </form>
  );
}
