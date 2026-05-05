import RoomCreateModal from './RoomCreateModal';
import { fireEvent, render, screen } from '@testing-library/react';
import React from 'react';

describe('RoomCreateModal', () => {
  it('disables private room creation when the server API does not support it', () => {
    render(<RoomCreateModal onCreateRoom={vi.fn()} />);

    fireEvent.click(screen.getByRole('button', { name: /create room/i }));

    expect(screen.getByText('Not supported by this server API')).toBeInTheDocument();
    expect(document.querySelector('input[value="private"]')).toBeDisabled();
  });
});
