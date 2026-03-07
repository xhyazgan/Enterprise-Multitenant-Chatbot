export interface ChatSession {
  id: string;
  title: string;
  createdAt: string;
}

export interface ChatMessage {
  id: string;
  role: 'user' | 'assistant';
  content: string;
  createdAt: string;
}

export interface SessionWithMessages extends ChatSession {
  messages: ChatMessage[];
}

export interface SendMessageResponse {
  sessionId: string;
  message: ChatMessage;
}

export interface TenantInfo {
  id: string;
  name: string;
  realm: string;
  color: string;
  description: string;
  aiModel: string;
}
