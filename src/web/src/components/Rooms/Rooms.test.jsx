import * as rooms from '../../lib/rooms';
import Rooms from './Rooms';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

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

const setDocumentHidden = (hidden) => {
  Object.defineProperty(document, 'hidden', {
    configurable: true,
    value: hidden,
  });
};

describe('Rooms', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    setDocumentHidden(false);
    rooms.getJoined.mockResolvedValue([]);
  });

  afterEach(() => {
    vi.useRealTimers();
    setDocumentHidden(false);
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

  it('ignores malformed persisted tab entries and counters', async () => {
    localStorage.setItem(
      'slskd-room-tabs',
      JSON.stringify({
        tabCounter: 'bad',
        tabs: [
          null,
          'bad',
          { key: 'room-tab-7', label: [], roomName: { bad: true } },
        ],
      }),
    );

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

  it('ignores malformed joined room names while hydrating', async () => {
    rooms.getJoined.mockResolvedValue([{ name: 'bad' }, 'chill']);

    render(
      <MemoryRouter initialEntries={['/rooms']}>
        <Rooms />
      </MemoryRouter>,
    );

    expect(await screen.findByText('chill')).toBeInTheDocument();
    expect(screen.queryByText('[object Object]')).not.toBeInTheDocument();
  });

  it('shows the explicit join room button and joins a selected available room', async () => {
    rooms.getAvailable.mockResolvedValue([
      null,
      { name: '' },
      { name: 'slskdn', userCount: 3 },
    ]);
    rooms.getJoined
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce(['slskdn']);

    render(
      <MemoryRouter initialEntries={['/rooms']}>
        <Rooms />
      </MemoryRouter>,
    );

    fireEvent.click(await screen.findByRole('button', { name: /join room/i }));
    fireEvent.change(await screen.findByPlaceholderText('Room Filter'), {
      target: { value: 'slskdn' },
    });

    expect(await screen.findByText('slskdn')).toBeInTheDocument();
    expect(screen.queryByText('[object Object]')).not.toBeInTheDocument();

    fireEvent.click(screen.getByText('slskdn'));
    fireEvent.click(screen.getByRole('button', { name: 'Join' }));

    await waitFor(() =>
      expect(rooms.join).toHaveBeenCalledWith({ roomName: 'slskdn' }),
    );
    expect(await screen.findByText('slskdn')).toBeInTheDocument();
  });

  it('pauses joined-room hydration while hidden and refreshes when visible', async () => {
    vi.useFakeTimers();
    setDocumentHidden(true);

    render(
      <MemoryRouter initialEntries={['/rooms']}>
        <Rooms />
      </MemoryRouter>,
    );

    expect(rooms.getJoined).not.toHaveBeenCalled();

    await act(async () => {
      setDocumentHidden(false);
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
    });
    expect(rooms.getJoined).toHaveBeenCalledTimes(1);

    await act(async () => {
      setDocumentHidden(true);
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(rooms.getJoined).toHaveBeenCalledTimes(1);
  });

  it('does not overlap slow joined-room hydration requests', async () => {
    vi.useFakeTimers();
    let resolveJoined;
    rooms.getJoined.mockImplementation(
      () => new Promise((resolve) => {
        resolveJoined = resolve;
      }),
    );

    render(
      <MemoryRouter initialEntries={['/rooms']}>
        <Rooms />
      </MemoryRouter>,
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(rooms.getJoined).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolveJoined([]);
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(10_000);
    });
    expect(rooms.getJoined).toHaveBeenCalledTimes(2);
  });
});
