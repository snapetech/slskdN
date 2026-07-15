import * as rooms from '../../lib/rooms';
import RoomSession from './RoomSession';
import { act, render, screen } from '@testing-library/react';
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/rooms', () => ({
  getMessages: vi.fn(),
  getUsers: vi.fn(),
  sendMessage: vi.fn(),
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

describe('RoomSession', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.useFakeTimers();
    setDocumentHidden(false);
    rooms.getMessages.mockResolvedValue([]);
    rooms.getUsers.mockResolvedValue([]);
  });

  afterEach(() => {
    vi.useRealTimers();
    setDocumentHidden(false);
  });

  it('renders inactive rooms as a lightweight shell without polling room data', () => {
    render(
      <RoomSession
        active={false}
        roomName="slskdn"
      />,
    );

    expect(screen.getByText('slskdn')).toBeInTheDocument();
    expect(rooms.getMessages).not.toHaveBeenCalled();
    expect(rooms.getUsers).not.toHaveBeenCalled();
  });

  it('polls messages and users on separate cadences', async () => {
    render(
      <RoomSession
        active
        roomName="slskdn"
      />,
    );

    await act(async () => {
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(10_000);
    });

    expect(rooms.getMessages).toHaveBeenCalledTimes(6);
    expect(rooms.getUsers).toHaveBeenCalledTimes(2);
  });

  it('stops all polling while hidden and refreshes immediately when visible', async () => {
    setDocumentHidden(true);
    render(
      <RoomSession
        active
        roomName="slskdn"
      />,
    );

    expect(rooms.getMessages).not.toHaveBeenCalled();
    expect(rooms.getUsers).not.toHaveBeenCalled();

    await act(async () => {
      setDocumentHidden(false);
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
    });

    expect(rooms.getMessages).toHaveBeenCalledTimes(1);
    expect(rooms.getUsers).toHaveBeenCalledTimes(1);

    await act(async () => {
      setDocumentHidden(true);
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(20_000);
    });

    expect(rooms.getMessages).toHaveBeenCalledTimes(1);
    expect(rooms.getUsers).toHaveBeenCalledTimes(1);
  });

  it('does not overlap slow message refreshes', async () => {
    let resolveMessages;
    rooms.getMessages.mockImplementation(
      () => new Promise((resolve) => {
        resolveMessages = resolve;
      }),
    );
    render(
      <RoomSession
        active
        roomName="slskdn"
      />,
    );

    await act(async () => {
      await vi.advanceTimersByTimeAsync(6_000);
    });
    expect(rooms.getMessages).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolveMessages([]);
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(2_000);
    });
    expect(rooms.getMessages).toHaveBeenCalledTimes(2);
  });
});
