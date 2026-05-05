import ChatSession from './ChatSession';
import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';
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

describe('ChatSession', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
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
});
