import '@testing-library/jest-dom';
import QuickSwitcher from './QuickSwitcher';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const items = [
  {
    accent: 'slsk',
    id: 'chat:alice',
    label: 'alice',
    prefix: '@',
    sublabel: 'Soulseek DM',
    target: 'alice',
    type: 'chat',
  },
  {
    accent: 'slsk',
    id: 'chat:bob',
    label: 'bob',
    prefix: '@',
    sublabel: 'Soulseek DM',
    target: 'bob',
    type: 'chat',
  },
  {
    accent: 'mesh',
    id: 'pod:gold',
    label: 'Gold Star Club / general',
    prefix: '&',
    sublabel: 'Mesh channel',
    target: 'pod:gold|general',
    type: 'pod',
    tabLabel: 'Gold Star Club / general',
  },
];

describe('QuickSwitcher', () => {
  it('renders items, filters on substring, and picks via Enter', () => {
    const onPick = vi.fn();
    const onClose = vi.fn();
    render(
      <QuickSwitcher
        items={items}
        onClose={onClose}
        onPick={onPick}
        open
      />,
    );

    expect(screen.getByText('alice')).toBeInTheDocument();
    expect(screen.getByText('bob')).toBeInTheDocument();
    expect(screen.getByText('Gold Star Club / general')).toBeInTheDocument();

    const input = screen.getByLabelText('Quick switcher search');
    fireEvent.change(input, { target: { value: 'gold' } });

    expect(screen.queryByText('alice')).not.toBeInTheDocument();
    expect(screen.getByText('Gold Star Club / general')).toBeInTheDocument();

    fireEvent.keyDown(input, { key: 'Enter' });
    expect(onPick).toHaveBeenCalledWith(expect.objectContaining({ id: 'pod:gold' }));
  });

  it('navigates with arrow keys and shows empty state when nothing matches', () => {
    const onPick = vi.fn();
    render(
      <QuickSwitcher
        items={items}
        onClose={vi.fn()}
        onPick={onPick}
        open
      />,
    );

    const input = screen.getByLabelText('Quick switcher search');
    fireEvent.keyDown(input, { key: 'ArrowDown' });
    fireEvent.keyDown(input, { key: 'Enter' });
    expect(onPick).toHaveBeenCalledWith(expect.objectContaining({ id: 'chat:bob' }));

    fireEvent.change(input, { target: { value: 'nope' } });
    expect(screen.getByText('No matches')).toBeInTheDocument();
  });

  it('calls onClose on Escape', () => {
    const onClose = vi.fn();
    render(
      <QuickSwitcher
        items={items}
        onClose={onClose}
        onPick={vi.fn()}
        open
      />,
    );
    const input = screen.getByLabelText('Quick switcher search');
    fireEvent.keyDown(input, { key: 'Escape' });
    expect(onClose).toHaveBeenCalled();
  });

  it('renders nothing when open is false', () => {
    const { container } = render(
      <QuickSwitcher
        items={items}
        onClose={vi.fn()}
        onPick={vi.fn()}
        open={false}
      />,
    );
    expect(container.firstChild).toBeNull();
  });
});
