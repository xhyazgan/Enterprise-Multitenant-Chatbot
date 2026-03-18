import { useState } from 'react';
import { createTenant } from '../services/tenantService';
import { useTenantStore } from '../stores/tenantStore';

export default function AdminPanel() {
  const [tenantId, setTenantId] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [description, setDescription] = useState('');
  const [color, setColor] = useState('#8B5CF6');
  const [aiProvider, setAiProvider] = useState('Claude');
  const [aiModel, setAiModel] = useState('claude-haiku-4-5');
  const [systemPrompt, setSystemPrompt] = useState('');
  const [adminKey, setAdminKey] = useState('dev-admin-key-change-in-production');
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const handleCreate = async () => {
    setError(null);
    setSuccess(null);

    if (!tenantId || !displayName) {
      setError('Tenant ID and Display Name are required');
      return;
    }

    if (!/^[a-z][a-z0-9-]{1,30}$/.test(tenantId)) {
      setError('Tenant ID must start with a letter, contain only lowercase letters, numbers, and hyphens (2-31 chars)');
      return;
    }

    setCreating(true);
    try {
      await createTenant({
        tenantId,
        displayName,
        description,
        color,
        aiProvider,
        aiModel,
        systemPrompt: systemPrompt || undefined,
      }, adminKey);

      setSuccess(`Tenant "${displayName}" created successfully! Keycloak realm is ready.`);

      // Reset form
      setTenantId('');
      setDisplayName('');
      setDescription('');
      setSystemPrompt('');

      // Refresh tenant list
      const store = useTenantStore.getState();
      store.reset();
      await store.fetchTenants();
    } catch (err: any) {
      const msg = err?.response?.data?.error || err?.message || 'Failed to create tenant';
      setError(msg);
    } finally {
      setCreating(false);
    }
  };

  return (
    <div className="mt-6 bg-gray-800 rounded-lg p-6 border border-gray-700">
      <h2 className="text-lg font-semibold text-white mb-4">Create New Tenant</h2>

      <div className="space-y-3">
        {/* Tenant ID */}
        <div>
          <label className="block text-sm text-gray-400 mb-1">Tenant ID (slug)</label>
          <input
            type="text"
            value={tenantId}
            onChange={e => setTenantId(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
            placeholder="acme-corp"
            className="w-full px-3 py-2 bg-gray-900 text-white rounded border border-gray-600 focus:border-blue-500 focus:outline-none text-sm"
          />
        </div>

        {/* Display Name */}
        <div>
          <label className="block text-sm text-gray-400 mb-1">Display Name</label>
          <input
            type="text"
            value={displayName}
            onChange={e => setDisplayName(e.target.value)}
            placeholder="Acme Corporation"
            className="w-full px-3 py-2 bg-gray-900 text-white rounded border border-gray-600 focus:border-blue-500 focus:outline-none text-sm"
          />
        </div>

        {/* Description */}
        <div>
          <label className="block text-sm text-gray-400 mb-1">Description</label>
          <input
            type="text"
            value={description}
            onChange={e => setDescription(e.target.value)}
            placeholder="A brief description of the organization"
            className="w-full px-3 py-2 bg-gray-900 text-white rounded border border-gray-600 focus:border-blue-500 focus:outline-none text-sm"
          />
        </div>

        {/* Color + AI Provider row */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-sm text-gray-400 mb-1">Color</label>
            <div className="flex items-center gap-2">
              <input
                type="color"
                value={color}
                onChange={e => setColor(e.target.value)}
                className="w-8 h-8 rounded cursor-pointer border-0 bg-transparent"
              />
              <span className="text-sm text-gray-400 font-mono">{color}</span>
            </div>
          </div>
          <div>
            <label className="block text-sm text-gray-400 mb-1">AI Provider</label>
            <select
              value={aiProvider}
              onChange={e => setAiProvider(e.target.value)}
              className="w-full px-3 py-2 bg-gray-900 text-white rounded border border-gray-600 focus:border-blue-500 focus:outline-none text-sm"
            >
              <option value="Claude">Claude</option>
              <option value="OpenAI">OpenAI</option>
            </select>
          </div>
        </div>

        {/* AI Model */}
        <div>
          <label className="block text-sm text-gray-400 mb-1">AI Model</label>
          <input
            type="text"
            value={aiModel}
            onChange={e => setAiModel(e.target.value)}
            className="w-full px-3 py-2 bg-gray-900 text-white rounded border border-gray-600 focus:border-blue-500 focus:outline-none text-sm"
          />
        </div>

        {/* System Prompt */}
        <div>
          <label className="block text-sm text-gray-400 mb-1">System Prompt (optional)</label>
          <textarea
            value={systemPrompt}
            onChange={e => setSystemPrompt(e.target.value)}
            rows={2}
            placeholder="You are a helpful assistant for..."
            className="w-full px-3 py-2 bg-gray-900 text-white rounded border border-gray-600 focus:border-blue-500 focus:outline-none text-sm resize-none"
          />
        </div>

        {/* Error/Success messages */}
        {error && (
          <div className="text-red-400 text-sm bg-red-900/20 px-3 py-2 rounded">
            {error}
          </div>
        )}
        {success && (
          <div className="text-green-400 text-sm bg-green-900/20 px-3 py-2 rounded">
            {success}
          </div>
        )}

        {/* Create button */}
        <button
          onClick={handleCreate}
          disabled={creating}
          className="w-full py-2 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 text-white rounded text-sm font-medium transition-colors"
        >
          {creating ? 'Creating...' : 'Create Tenant'}
        </button>
      </div>
    </div>
  );
}
