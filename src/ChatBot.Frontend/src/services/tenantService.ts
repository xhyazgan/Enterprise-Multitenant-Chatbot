import axios from 'axios';

// Separate axios instance WITHOUT auth interceptor (public endpoint)
const publicApi = axios.create({ baseURL: '/api' });

export interface TenantApiResponse {
  tenantId: string;
  displayName: string;
  color: string;
  description: string;
  aiModel: string;
}

export interface CreateTenantRequest {
  tenantId: string;
  displayName: string;
  aiProvider: string;
  aiModel: string;
  systemPrompt?: string;
  color: string;
  description: string;
}

export async function getTenants(): Promise<TenantApiResponse[]> {
  const { data } = await publicApi.get<TenantApiResponse[]>('/tenants');
  return data;
}

export async function createTenant(tenant: CreateTenantRequest, adminKey: string): Promise<TenantApiResponse> {
  const { data } = await publicApi.post<TenantApiResponse>('/admin/tenants', tenant, {
    headers: { 'X-Admin-Key': adminKey }
  });
  return data;
}
