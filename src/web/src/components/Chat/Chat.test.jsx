import * as chat from '../../lib/chat';
import Chat from './Chat';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../lib/chat', () => ({
  getAll: vi.fn(),
  remove: vi.fn(),
}));

vi.mock('./ChatSession', () => ({
  default: ({ username }) => (
    <div data-testid="chat-session">{username || 'empty'}</div>
  ),
}));

describe('Chat', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    vi.clearAllMocks();
    chat.getAll.mockResolvedValue([]);
  });

  it('opens a chat tab from a URL so user actions work in new tabs', async () => {
    render(
      <MemoryRouter initialEntries={['/chat?user=alice']}>
        <Chat />
      </MemoryRouter>,
    );

    expect((await screen.findAllByText('alice')).length).toBeGreaterThan(0);
    expect(
      screen
        .getAllByTestId('chat-session')
        .some((session) => session.textContent === 'alice'),
    ).toBe(true);
  });

  it('ignores corrupted persisted tab shapes instead of crashing', async () => {
    localStorage.setItem('slskd-chat-tabs', JSON.stringify({ tabs: {} }));

    render(
      <MemoryRouter initialEntries={['/chat']}>
        <Chat />
      </MemoryRouter>,
    );

    expect(await screen.findByText('New Chat')).toBeInTheDocument();
  });

  it('ignores malformed persisted tab entries and counters', async () => {
    localStorage.setItem(
      'slskd-chat-tabs',
      JSON.stringify({
        tabCounter: 'bad',
        tabs: [
          null,
          'bad',
          { key: 'chat-tab-7', label: [], username: { bad: true } },
        ],
      }),
    );

    render(
      <MemoryRouter initialEntries={['/chat']}>
        <Chat />
      </MemoryRouter>,
    );

    expect(await screen.findByText('New Chat')).toBeInTheDocument();
  });

  it('ignores malformed conversation list payloads while hydrating', async () => {
    chat.getAll.mockResolvedValue({ conversations: [{ username: 'alice' }] });

    render(
      <MemoryRouter initialEntries={['/chat']}>
        <Chat />
      </MemoryRouter>,
    );

    expect(await screen.findByText('New Chat')).toBeInTheDocument();
    expect(screen.queryByText('alice')).not.toBeInTheDocument();
  });

  it('ignores malformed conversation usernames while hydrating', async () => {
    chat.getAll.mockResolvedValue([
      { username: { bad: true } },
      { username: 'alice' },
    ]);

    render(
      <MemoryRouter initialEntries={['/chat']}>
        <Chat />
      </MemoryRouter>,
    );

    expect((await screen.findAllByText('alice')).length).toBeGreaterThan(0);
    expect(screen.queryByText('[object Object]')).not.toBeInTheDocument();
  });
});
