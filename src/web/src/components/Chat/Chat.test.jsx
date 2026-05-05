import * as chat from '../../lib/chat';
import Chat from './Chat';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../lib/chat', () => ({
  getAll: vi.fn(),
  remove: vi.fn(),
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

    expect(await screen.findByText('alice')).toBeInTheDocument();
  });
});
