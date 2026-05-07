import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import UserPopover from './UserPopover';
import { describe, expect, it, vi } from 'vitest';

const baseProps = () => ({
  anchor: { x: 100, y: 100 },
  onBrowse: vi.fn(),
  onClose: vi.fn(),
  onMessage: vi.fn(),
  onProfile: vi.fn(),
  open: true,
  username: 'alice',
});

describe('UserPopover', () => {
  it('renders username header and three actions', () => {
    render(<UserPopover {...baseProps()} />);
    expect(screen.getByText('alice')).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /open profile/i })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /browse shares/i })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: /send dm/i })).toBeInTheDocument();
  });

  it('invokes the right callback for each action', () => {
    const props = baseProps();
    render(<UserPopover {...props} />);

    fireEvent.click(screen.getByRole('menuitem', { name: /open profile/i }));
    expect(props.onProfile).toHaveBeenCalledWith('alice');

    fireEvent.click(screen.getByRole('menuitem', { name: /browse shares/i }));
    expect(props.onBrowse).toHaveBeenCalledWith('alice');

    fireEvent.click(screen.getByRole('menuitem', { name: /send dm/i }));
    expect(props.onMessage).toHaveBeenCalledWith('alice');
  });

  it('closes on Escape', () => {
    const props = baseProps();
    render(<UserPopover {...props} />);
    fireEvent.keyDown(window, { key: 'Escape' });
    expect(props.onClose).toHaveBeenCalled();
  });

  it('closes on outside mousedown', () => {
    const props = baseProps();
    render(
      <div>
        <button data-testid="outside" type="button">outside</button>
        <UserPopover {...props} />
      </div>,
    );
    fireEvent.mouseDown(screen.getByTestId('outside'));
    expect(props.onClose).toHaveBeenCalled();
  });

  it('does not render when closed or username missing', () => {
    const props = { ...baseProps(), open: false };
    const { rerender, container } = render(<UserPopover {...props} />);
    expect(container.firstChild).toBeNull();

    rerender(<UserPopover {...baseProps()} username={null} />);
    expect(container.firstChild).toBeNull();
  });
});
