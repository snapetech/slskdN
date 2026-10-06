import * as chat from '../../lib/chat';
import Chat from './Chat';
import { render, screen } from '@testing-library/react';
import React from 'react';
import userEvent from '@testing-library/user-event';
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

  it('explains closing a chat tab and keeps the saved conversation', async () => {
    const user = userEvent.setup();
    localStorage.setItem('slskd-chat-tabs', JSON.stringify({
      activeIndex: 0,
      tabCounter: 2,
      tabs: [
        { key: 'chat-tab-1', label: 'alice', username: 'alice' },
        { key: 'chat-tab-2', label: 'bob', username: 'bob' },
      ],
    }));
    render(
      <MemoryRouter initialEntries={['/chat']}>
        <Chat />
      </MemoryRouter>,
    );

    const close = screen.getByRole('button', { name: 'Close alice chat tab' });
    await user.hover(close);
    expect(await screen.findByText(/Its saved conversation remains available/))
      .toBeInTheDocument();
    expect(chat.remove).not.toHaveBeenCalled();

    await user.click(close);
    expect(screen.queryByRole('button', { name: 'Close alice chat tab' }))
      .not.toBeInTheDocument();
    expect(chat.remove).not.toHaveBeenCalled();
  });
});
