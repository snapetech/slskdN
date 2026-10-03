import '@testing-library/jest-dom';
import Logs from './index';
import React from 'react';
import { createLogsHubConnection } from '../../../lib/hubFactory';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../../lib/hubFactory', () => ({
  createLogsHubConnection: vi.fn(),
}));

const createHub = ({ start } = {}) => {
  const handlers = new Map();
  const lifecycle = {};

  return {
    emit: (event, ...args) => handlers.get(event)?.(...args),
    on: vi.fn((event, handler) => handlers.set(event, handler)),
    onclose: vi.fn((handler) => { lifecycle.close = handler; }),
    onreconnected: vi.fn((handler) => { lifecycle.reconnected = handler; }),
    onreconnecting: vi.fn((handler) => { lifecycle.reconnecting = handler; }),
    start: start || vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    trigger: (event, ...args) => lifecycle[event]?.(...args),
  };
};

const records = [
  {
    level: 'Information',
    message: 'An information record',
    timestamp: '2026-10-03T00:00:01Z',
  },
  {
    level: 'Warning',
    message: 'A warning record',
    timestamp: '2026-10-03T00:00:02Z',
  },
];

describe('System Logs', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the server buffer newest-first and filters by severity', async () => {
    const hub = createHub();
    createLogsHubConnection.mockReturnValue(hub);
    const user = userEvent.setup();

    render(<Logs />);
    await screen.findByText('Showing 0 of 0 logs');
    act(() => hub.emit('buffer', records));

    expect(await screen.findByText('A warning record')).toBeInTheDocument();
    const rows = screen.getAllByRole('row');
    expect(rows[1]).toHaveTextContent('A warning record');
    expect(rows[2]).toHaveTextContent('An information record');

    const warningFilter = screen.getByRole('button', { name: 'Show warning logs' });
    expect(warningFilter).toHaveAttribute('aria-pressed', 'false');
    await user.click(warningFilter);

    expect(warningFilter).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByText('A warning record')).toBeInTheDocument();
    expect(screen.queryByText('An information record')).not.toBeInTheDocument();
    expect(screen.getByText('Showing 1 of 2 logs')).toBeInTheDocument();
  });

  it('explains the severity filter on keyboard focus', async () => {
    const hub = createHub();
    createLogsHubConnection.mockReturnValue(hub);
    render(<Logs />);

    const allFilter = screen.getByRole('button', { name: 'Show all severities' });
    allFilter.focus();

    expect(await screen.findByText(
      'Show every severity so you can inspect the full live log feed.',
    )).toBeVisible();
  });

  it('shows a retry action after startup fails and reconnects on activation', async () => {
    const firstHub = createHub({
      start: vi.fn().mockRejectedValueOnce(new Error('logs unavailable')),
    });
    const secondHub = createHub();
    createLogsHubConnection
      .mockReturnValueOnce(firstHub)
      .mockReturnValueOnce(secondHub);
    const user = userEvent.setup();

    render(<Logs />);

    expect(await screen.findByText('logs unavailable')).toBeInTheDocument();
    const retry = screen.getByRole('button', { name: 'Retry live log connection' });
    expect(retry).toBeVisible();
    await user.click(retry);

    await waitFor(() => expect(createLogsHubConnection).toHaveBeenCalledTimes(2));
    expect(firstHub.stop).toHaveBeenCalledOnce();
    expect(await screen.findByText('Showing 0 of 0 logs')).toBeInTheDocument();
  });

  it('keeps buffered entries available while reconnecting', async () => {
    const hub = createHub();
    createLogsHubConnection.mockReturnValue(hub);
    render(<Logs />);
    await screen.findByText('Showing 0 of 0 logs');
    act(() => hub.emit('buffer', records));

    act(() => hub.trigger('reconnecting', new Error('network changed')));

    expect(screen.getByText('A warning record')).toBeInTheDocument();
    expect(screen.getByText('Reconnecting to live logs', { exact: true })).toBeInTheDocument();
    expect(screen.getByText(/Buffered entries remain available/u)).toBeInTheDocument();
  });

  it('stops the live hub and ignores later records when the page unmounts', async () => {
    const hub = createHub();
    createLogsHubConnection.mockReturnValue(hub);
    const { unmount } = render(<Logs />);

    unmount();

    await waitFor(() => expect(hub.stop).toHaveBeenCalledOnce());
    expect(() => hub.emit('log', records[0])).not.toThrow();
  });
});
