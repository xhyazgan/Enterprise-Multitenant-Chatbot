import api from './api';
import type { ChatSession, SendMessageResponse, SessionWithMessages } from '../types';

export async function getSessions(): Promise<ChatSession[]> {
  const { data } = await api.get<ChatSession[]>('/chat/sessions');
  return data;
}

export async function getSessionMessages(sessionId: string): Promise<SessionWithMessages> {
  const { data } = await api.get<SessionWithMessages>(`/chat/sessions/${sessionId}/messages`);
  return data;
}

export async function sendMessage(
  message: string,
  sessionId?: string
): Promise<SendMessageResponse> {
  const { data } = await api.post<SendMessageResponse>('/chat/message', {
    sessionId,
    message,
  });
  return data;
}

export async function deleteSession(sessionId: string): Promise<void> {
  await api.delete(`/chat/sessions/${sessionId}`);
}
