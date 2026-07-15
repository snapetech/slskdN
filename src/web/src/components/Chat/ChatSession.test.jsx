import ChatSession from './ChatSession';
import React from 'react';
import { act, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import * as chat from '../../lib/chat';

vi.mock('../../lib/chat', () => ({
  acknowledge: vi.fn(() => Promise.resolve()),
  get: vi.fn(),
  remove: vi.fn(() => Promise.resolve()),
  send: vi.fn(() => Promise.resolve()),
}));

vi.mock('../Shared/UserCard', () => ({
  default: ({ children }) => <span>{children}</span>,
}));

const setDocumentHidden = (hidden) => {
  Object.defineProperty(document, 'hidden', {
    configurable: true,
    value: hidden,
  });
};

const conversation = (messages = []) => ({
  hasUnAcknowledgedMessages: false,
  isActive: true,
  messages,
  unAcknowledgedMessageCount: 0,
  username: 'alice',
});

const privateMessage = ({ body, id, timestamp }) => ({
  direction: id % 2 === 0 ? 'Out' : 'In',
  id,
  isAcknowledged: true,
  message: body,
  timestamp,
  username: 'alice',
});

describe('ChatSession', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setDocumentHidden(false);
    chat.get.mockResolvedValue(conversation());
  });

  afterEach(() => {
    vi.useRealTimers();
    setDocumentHidden(false);
    vi.restoreAllMocks();
  });

  it('ignores malformed conversation message lists', async () => {
    chat.get.mockResolvedValueOnce({
      hasUnAcknowledgedMessages: true,
      messages: { 0: { message: 'bad' }, length: 1 },
    });

    render(
      <ChatSession
        active
        user={{ username: 'me' }}
        username="alice"
      />,
    );

    expect(await screen.findByText('alice')).toBeInTheDocument();
    await waitFor(() => {
      expect(chat.acknowledge).toHaveBeenCalledWith({ username: 'alice' });
    });
    expect(screen.queryByText('bad')).not.toBeInTheDocument();
  });

  it('renders inactive chats as a lightweight shell without polling messages', () => {
    render(
      <ChatSession
        active={false}
        user={{ username: 'me' }}
        username="alice"
      />,
    );

    expect(screen.getByText('alice')).toBeInTheDocument();
    expect(chat.get).not.toHaveBeenCalled();
    expect(chat.acknowledge).not.toHaveBeenCalled();
  });

  it('uses an overlapping ISO timestamp cursor and merges message deltas', async () => {
    vi.useFakeTimers();
    const first = privateMessage({
      body: 'first',
      id: 1,
      timestamp: '2026-07-15T12:00:00.100Z',
    });
    const second = privateMessage({
      body: 'second',
      id: 2,
      timestamp: '2026-07-15T12:00:00.200Z',
    });
    chat.get
      .mockResolvedValueOnce(conversation([first]))
      .mockResolvedValueOnce(conversation([first, second]))
      .mockResolvedValue(conversation([]));

    render(
      <ChatSession
        active
        user={{ username: 'me' }}
        username="alice"
      />,
    );
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });

    expect(chat.get).toHaveBeenNthCalledWith(1, {
      since: null,
      username: 'alice',
    });
    expect(chat.get).toHaveBeenNthCalledWith(2, {
      since: Date.parse(first.timestamp) - 1,
      username: 'alice',
    });
    expect(chat.get).toHaveBeenNthCalledWith(3, {
      since: Date.parse(second.timestamp) - 1,
      username: 'alice',
    });
    expect(screen.getByText('first')).toBeInTheDocument();
    expect(screen.getByText('second')).toBeInTheDocument();
  });

  it('does not poll while hidden and refreshes immediately when visible', async () => {
    vi.useFakeTimers();
    setDocumentHidden(true);
    render(
      <ChatSession
        active
        user={{ username: 'me' }}
        username="alice"
      />,
    );

    expect(chat.get).not.toHaveBeenCalled();

    await act(async () => {
      setDocumentHidden(false);
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(chat.get).toHaveBeenCalledTimes(1);

    await act(async () => {
      setDocumentHidden(true);
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(20_000);
    });
    expect(chat.get).toHaveBeenCalledTimes(1);
  });

  it('does not overlap slow conversation requests', async () => {
    vi.useFakeTimers();
    let resolveConversation;
    chat.get.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveConversation = resolve;
        }),
    );
    render(
      <ChatSession
        active
        user={{ username: 'me' }}
        username="alice"
      />,
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(15_000);
    });
    expect(chat.get).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolveConversation(conversation());
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(5_000);
    });
    expect(chat.get).toHaveBeenCalledTimes(2);
  });
});
