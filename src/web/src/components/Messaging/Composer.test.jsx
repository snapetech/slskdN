import '@testing-library/jest-dom';
import Composer, { matchSuggestions } from './Composer';
import React from 'react';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

const COMMANDS = [
  { description: 'Italic action.', name: 'me', syntax: '/me <action>' },
  { description: 'Open DM.', name: 'msg', syntax: '/msg <user>' },
  { description: 'Join room.', name: 'join', syntax: '/join <room>' },
  {
    aliases: ['part', 'leave'],
    description: 'Close tab.',
    name: 'close',
    syntax: '/close',
  },
  { description: 'Show help.', name: 'help', syntax: '/help' },
];

describe('matchSuggestions', () => {
  it('returns empty when input does not start with slash', () => {
    expect(matchSuggestions('hello', COMMANDS)).toEqual([]);
  });

  it('returns all commands when input is just /', () => {
    expect(matchSuggestions('/', COMMANDS)).toHaveLength(COMMANDS.length);
  });

  it('filters by prefix match on name', () => {
    const result = matchSuggestions('/m', COMMANDS);
    expect(result.map((c) => c.name)).toEqual(['me', 'msg']);
  });

  it('matches aliases too', () => {
    const result = matchSuggestions('/par', COMMANDS);
    expect(result.map((c) => c.name)).toEqual(['close']);
  });

  it('returns empty after a space (command already chosen)', () => {
    expect(matchSuggestions('/me waving', COMMANDS)).toEqual([]);
  });
});

const buildAdapter = () => ({
  list: () => Promise.resolve({ messages: [] }),
  send: vi.fn(() => Promise.resolve()),
});

describe('Composer', () => {
  it('shows suggestions when input starts with slash and Tab autocompletes', () => {
    const adapter = buildAdapter();
    render(<Composer adapter={adapter} commands={COMMANDS} />);
    const input = screen.getByLabelText('Message composer');

    fireEvent.change(input, { target: { value: '/m' } });
    expect(screen.getByText('/me <action>')).toBeInTheDocument();
    expect(screen.getByText('/msg <user>')).toBeInTheDocument();

    fireEvent.keyDown(input, { key: 'Tab' });
    expect(input.value).toBe('/me ');
  });

  it('routes /help to the onCommand handler instead of sending', async () => {
    const adapter = buildAdapter();
    const onCommand = vi.fn(({ name }) => name === 'help');
    render(
      <Composer
        adapter={adapter}
        commands={COMMANDS}
        onCommand={onCommand}
      />,
    );
    const input = screen.getByLabelText('Message composer');
    fireEvent.change(input, { target: { value: '/help' } });
    fireEvent.keyDown(input, { key: 'Enter' });

    await waitFor(() => {
      expect(onCommand).toHaveBeenCalledWith(
        expect.objectContaining({ name: 'help' }),
      );
    });
    expect(adapter.send).not.toHaveBeenCalled();
    expect(input.value).toBe('');
  });

  it('navigates suggestions with arrow keys', () => {
    const adapter = buildAdapter();
    render(<Composer adapter={adapter} commands={COMMANDS} />);
    const input = screen.getByLabelText('Message composer');

    fireEvent.change(input, { target: { value: '/' } });
    fireEvent.keyDown(input, { key: 'ArrowDown' });
    fireEvent.keyDown(input, { key: 'Tab' });
    expect(input.value).toBe('/msg ');
  });

  it('sends plain text via adapter.send when not a command', async () => {
    const adapter = buildAdapter();
    render(<Composer adapter={adapter} commands={COMMANDS} />);
    const input = screen.getByLabelText('Message composer');
    fireEvent.change(input, { target: { value: 'hello world' } });
    fireEvent.keyDown(input, { key: 'Enter' });

    await waitFor(() => {
      expect(adapter.send).toHaveBeenCalledWith('hello world');
    });
  });
});
