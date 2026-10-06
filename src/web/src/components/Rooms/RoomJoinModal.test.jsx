import * as rooms from '../../lib/rooms';
import RoomJoinModal from './RoomJoinModal';
import React from 'react';
import userEvent from '@testing-library/user-event';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { vi } from 'vitest';

vi.mock('../../lib/rooms', () => ({ getAvailable: vi.fn() }));

describe('RoomJoinModal', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    rooms.getAvailable.mockResolvedValue([{ name: 'public-lobby', userCount: 4 }]);
  });

  it('explains room joining and canceling without joining on hover', async () => {
    const user = userEvent.setup();
    const joinRoom = vi.fn();
    render(<RoomJoinModal joinRoom={joinRoom} />);

    await user.click(screen.getByRole('button', { name: 'Join Room' }));
    await waitFor(() => expect(rooms.getAvailable).toHaveBeenCalledOnce());
    expect(await screen.findByText('Type to search rooms')).toBeInTheDocument();

    const join = screen.getByRole('button', { name: 'Join' });
    expect(join).toBeDisabled();
    fireEvent.mouseEnter(join.parentElement);
    expect(await screen.findByText(
      'Join the selected Soulseek room; its members will see this account in the participant list.',
    )).toBeInTheDocument();
    expect(joinRoom).not.toHaveBeenCalled();

    const filter = screen.getByPlaceholderText('Room Filter');
    await user.type(filter, 'public-lobby');
    await user.click(await screen.findByText('public-lobby'));
    await waitFor(() => {
      expect(screen.getByRole('button', { name: 'Join' })).toBeEnabled();
    });

    await user.hover(screen.getByRole('button', { name: 'Join' }));
    expect(await screen.findByText(
      'Join the selected Soulseek room; its members will see this account in the participant list.',
    )).toBeInTheDocument();
    await user.hover(screen.getByRole('button', { name: 'Cancel' }));
    expect(await screen.findByText('Close the dialog without joining a Soulseek room.'))
      .toBeInTheDocument();
    expect(joinRoom).not.toHaveBeenCalled();
  });

  it('sorts the available room list with keyboard-accessible explained controls', async () => {
    const user = userEvent.setup();
    rooms.getAvailable.mockResolvedValue([
      { name: 'gamma-room', userCount: 3 },
      { name: 'alpha-room', userCount: 1 },
      { name: 'beta-room', userCount: 2 },
    ]);
    render(<RoomJoinModal joinRoom={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: 'Join Room' }));
    await waitFor(() => expect(rooms.getAvailable).toHaveBeenCalledOnce());
    await user.type(screen.getByPlaceholderText('Room Filter'), '-room');

    const sortByName = screen.getByRole('button', {
      name: 'Sort available rooms by name',
    });
    const nameHeader = sortByName.closest('th');
    const orderedRoomNames = () => screen.getAllByRole('row')
      .slice(1)
      .map((row) => row.querySelector('td').textContent.trim());
    const orderedUserCounts = () => screen.getAllByRole('row')
      .slice(1)
      .map((row) => Number(row.querySelector('td:nth-child(2)').textContent.trim()));
    expect(nameHeader).toHaveAttribute('aria-sort', 'descending');
    expect(orderedRoomNames()).toEqual(['gamma-room', 'beta-room', 'alpha-room']);
    await user.hover(sortByName);
    expect(await screen.findByText(/Sort room names ascending/))
      .toBeInTheDocument();
    await user.unhover(sortByName);
    await user.click(sortByName);
    expect(nameHeader).toHaveAttribute('aria-sort', 'ascending');
    expect(orderedRoomNames()).toEqual(['alpha-room', 'beta-room', 'gamma-room']);

    const sortByUserCount = screen.getByRole('button', {
      name: 'Sort available rooms by user count',
    });
    await user.click(sortByUserCount);
    expect(orderedUserCounts()).toEqual([1, 2, 3]);
    await user.click(sortByUserCount);
    expect(orderedUserCounts()).toEqual([3, 2, 1]);
    expect(rooms.getAvailable).toHaveBeenCalledOnce();
  });
});
