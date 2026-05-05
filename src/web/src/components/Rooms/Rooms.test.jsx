import * as rooms from '../../lib/rooms';
import Rooms from './Rooms';
import { render, screen } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../lib/rooms', () => ({
  getAvailable: vi.fn(),
  getJoined: vi.fn(),
  join: vi.fn(),
  leave: vi.fn(),
}));

vi.mock('./RoomSession', () => ({
  default: ({ roomName }) => (
    <div data-testid="room-session">{roomName || 'empty'}</div>
  ),
}));

describe('Rooms', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    rooms.getJoined.mockResolvedValue([]);
  });

  it('ignores corrupted persisted tab shapes instead of crashing', async () => {
    localStorage.setItem('slskd-room-tabs', JSON.stringify({ tabs: {} }));

    render(
      <MemoryRouter initialEntries={['/rooms']}>
        <Rooms />
      </MemoryRouter>,
    );

    expect(await screen.findByText('New Room Tab')).toBeInTheDocument();
  });

  it('ignores malformed joined room list payloads while hydrating', async () => {
    rooms.getJoined.mockResolvedValue({ rooms: ['chill'] });

    render(
      <MemoryRouter initialEntries={['/rooms']}>
        <Rooms />
      </MemoryRouter>,
    );

    expect(await screen.findByText('New Room Tab')).toBeInTheDocument();
    expect(screen.queryByText('chill')).not.toBeInTheDocument();
  });
});
