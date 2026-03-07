import { create } from 'zustand';
import type { AxiosError } from 'axios';
import type { ChatMessage, ChatSession } from '../types';
import * as chatService from '../services/chatService';

interface ChatState {
  sessions: ChatSession[];
  activeSessionId: string | null;
  messages: ChatMessage[];
  loading: boolean;
  sending: boolean;
  error: string | null;

  loadSessions: () => Promise<void>;
  selectSession: (sessionId: string) => Promise<void>;
  sendMessage: (message: string) => Promise<void>;
  deleteSession: (sessionId: string) => Promise<void>;
  clearActiveSession: () => void;
  clearError: () => void;
}

function extractErrorMessage(err: unknown): string {
  const axiosErr = err as AxiosError<{ error?: string }>;
  return axiosErr?.response?.data?.error ?? 'Something went wrong. Please try again.';
}

export const useChatStore = create<ChatState>((set, get) => ({
  sessions: [],
  activeSessionId: null,
  messages: [],
  loading: false,
  sending: false,
  error: null,

  loadSessions: async () => {
    set({ loading: true });
    try {
      const sessions = await chatService.getSessions();
      set({ sessions, loading: false });
    } catch (error) {
      console.error('Failed to load sessions:', error);
      set({ loading: false });
    }
  },

  selectSession: async (sessionId: string) => {
    set({ activeSessionId: sessionId, loading: true });
    try {
      const session = await chatService.getSessionMessages(sessionId);
      set({ messages: session.messages, loading: false });
    } catch (error) {
      console.error('Failed to load messages:', error);
      set({ loading: false });
    }
  },

  sendMessage: async (message: string) => {
    const { activeSessionId, messages } = get();
    // Optimistic UI: add user message immediately
    const optimisticMsg: ChatMessage = {
      id: crypto.randomUUID(),
      role: 'user',
      content: message,
      createdAt: new Date().toISOString(),
    };
    set({ messages: [...messages, optimisticMsg], sending: true, error: null });

    try {
      const response = await chatService.sendMessage(
        message,
        activeSessionId || undefined
      );

      // If this was a new session, update session list and active session
      if (!activeSessionId) {
        set({ activeSessionId: response.sessionId });
        await get().loadSessions();
      }

      // Replace optimistic messages with real data
      const currentMessages = get().messages;
      set({
        messages: [
          ...currentMessages,
          response.message,
        ],
        sending: false,
      });
    } catch (err) {
      console.error('Failed to send message:', err);
      set({
        messages: get().messages.filter((m) => m.id !== optimisticMsg.id),
        sending: false,
        error: extractErrorMessage(err),
      });
    }
  },

  deleteSession: async (sessionId: string) => {
    try {
      await chatService.deleteSession(sessionId);
      const { activeSessionId } = get();
      set({
        sessions: get().sessions.filter((s) => s.id !== sessionId),
        ...(activeSessionId === sessionId
          ? { activeSessionId: null, messages: [] }
          : {}),
      });
    } catch (error) {
      console.error('Failed to delete session:', error);
    }
  },

  clearActiveSession: () => {
    set({ activeSessionId: null, messages: [], error: null });
  },

  clearError: () => {
    set({ error: null });
  },
}));
