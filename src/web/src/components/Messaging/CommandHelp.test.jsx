import '@testing-library/jest-dom';
import CommandHelp from './CommandHelp';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const COMMANDS = [
  {
    aliases: ['part', 'leave'],
    description: 'Close the tab.',
    name: 'close',
    syntax: '/close',
  },
  { description: 'Italic action.', name: 'me', syntax: '/me <action>' },
];

describe('CommandHelp', () => {
  it('renders commands with syntax, description, and aliases', () => {
    render(
      <CommandHelp
        commands={COMMANDS}
        onClose={vi.fn()}
        open
      />,
    );
    expect(screen.getByText('/close')).toBeInTheDocument();
    expect(screen.getByText('Close the tab.')).toBeInTheDocument();
    expect(screen.getByText(/aliases: \/part, \/leave/)).toBeInTheDocument();
    expect(screen.getByText('/me <action>')).toBeInTheDocument();
  });

  it('calls onClose when Esc is pressed', () => {
    const onClose = vi.fn();
    render(
      <CommandHelp
        commands={COMMANDS}
        onClose={onClose}
        open
      />,
    );
    fireEvent.keyDown(window, { key: 'Escape' });
    expect(onClose).toHaveBeenCalled();
  });

  it('calls onClose on backdrop click but not modal click', () => {
    const onClose = vi.fn();
    render(
      <CommandHelp
        commands={COMMANDS}
        onClose={onClose}
        open
      />,
    );

    fireEvent.click(screen.getByText('/me <action>'));
    expect(onClose).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('dialog'));
    expect(onClose).toHaveBeenCalled();
  });

  it('renders nothing when open is false', () => {
    const { container } = render(
      <CommandHelp
        commands={COMMANDS}
        onClose={vi.fn()}
        open={false}
      />,
    );
    expect(container.firstChild).toBeNull();
  });
});
