import RoomCreateModal from './RoomCreateModal';
import userEvent from '@testing-library/user-event';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import React from 'react';

describe('RoomCreateModal', () => {
  it('disables private room creation when the server API does not support it', () => {
    render(<RoomCreateModal onCreateRoom={vi.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: /create room/i }));

    expect(screen.getByText('Not supported by this server API')).toBeInTheDocument();
    expect(document.querySelector('input[value="private"]')).toBeDisabled();
  });

  it('explains create and cancel actions before they change room membership', async () => {
    const user = userEvent.setup();
    const onCreateRoom = vi.fn();
    render(<RoomCreateModal onCreateRoom={onCreateRoom} />);

    await user.click(screen.getByRole('button', { name: /create room/i }));

    const modalActions = within(document.querySelector('.ui.modal'));
    const cancel = modalActions.getByRole('button', { name: 'Cancel' });
    await user.hover(cancel);
    await waitFor(() => {
      expect(screen.getByText('Close this dialog without creating or joining a room.'))
        .toBeInTheDocument();
    });

    const create = modalActions.getByRole('button', { name: /create room/i });
    expect(create).toBeDisabled();
    await user.hover(create.parentElement);
    await waitFor(() => {
      expect(screen.getByText(
        'Create a public Soulseek room with this name and join it. Other users can see the room and its participants.',
      )).toBeInTheDocument();
    });
    expect(onCreateRoom).not.toHaveBeenCalled();
  });
});
