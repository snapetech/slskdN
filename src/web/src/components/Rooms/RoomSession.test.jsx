import * as rooms from '../../lib/rooms';
import RoomSession from './RoomSession';
import { render, screen } from '@testing-library/react';
import React from 'react';

vi.mock('../../lib/rooms', () => ({
  getMessages: vi.fn(),
  getUsers: vi.fn(),
  sendMessage: vi.fn(),
}));

vi.mock('../Shared/UserCard', () => ({
  default: ({ children }) => <span>{children}</span>,
}));

describe('RoomSession', () => {
  beforeEach(() => {
    vi.clearAllMocks();
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
});
