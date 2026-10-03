import '@testing-library/jest-dom';
import * as relayAPI from '../lib/relay';
import * as serverAPI from '../lib/server';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import { Menu } from 'semantic-ui-react';
import { vi } from 'vitest';
import ConnectionStatusMenuItem from './ConnectionStatusMenuItem';

vi.mock('../lib/relay', () => ({
  connect: vi.fn(),
  disconnect: vi.fn(),
}));

vi.mock('../lib/server', () => ({
  connect: vi.fn(),
  disconnect: vi.fn(),
}));

vi.mock('semantic-ui-react', async (importOriginal) => {
  const ReactModule = await import('react');
  const actual = await importOriginal();

  return {
    ...actual,
    Popup: ({ content, trigger }) =>
      ReactModule.cloneElement(trigger, { 'data-tooltip': content }),
  };
});

const renderMenuItem = (props) => render(
  <Menu>
    <ConnectionStatusMenuItem {...props} />
  </Menu>,
);

describe('ConnectionStatusMenuItem', () => {
  afterEach(() => {
    vi.clearAllMocks();
  });

  it('explains and connects an idle relay controller', () => {
    renderMenuItem({
      controller: { state: 'Disconnected' },
      mode: 'Agent',
    });

    const action = screen.getByTitle('Connect to the relay controller.');
    expect(action).toHaveAttribute(
      'aria-label',
      'Connect to the relay controller.',
    );
    expect(action).toHaveAttribute(
      'data-tooltip',
      'Connect to the relay controller.',
    );
    expect(action).toHaveTextContent('Controller Disconnected');
    fireEvent.click(action);
    expect(relayAPI.connect).toHaveBeenCalledOnce();
  });

  it('explains and disconnects a connected relay controller', () => {
    renderMenuItem({
      controller: { state: 'Connected' },
      mode: 'Agent',
    });

    const action = screen.getByTitle('Disconnect from the relay controller.');
    expect(action).toHaveAttribute(
      'aria-label',
      'Disconnect from the relay controller.',
    );
    expect(action).toHaveAttribute(
      'data-tooltip',
      'Disconnect from the relay controller.',
    );
    fireEvent.click(action);
    expect(relayAPI.disconnect).toHaveBeenCalledOnce();
  });

  it('explains and connects a disconnected Soulseek server', () => {
    renderMenuItem({
      mode: 'Server',
      server: { isConnected: false },
    });

    const action = screen.getByTitle('Connect to the Soulseek server.');
    expect(action).toHaveAttribute(
      'aria-label',
      'Connect to the Soulseek server.',
    );
    expect(action).toHaveAttribute(
      'data-tooltip',
      'Connect to the Soulseek server.',
    );
    expect(action).toHaveTextContent('Disconnected');
    fireEvent.click(action);
    expect(serverAPI.connect).toHaveBeenCalledOnce();
  });

  it('explains and disconnects a connected Soulseek server', () => {
    renderMenuItem({
      mode: 'Server',
      pendingReconnect: false,
      server: { isConnected: true },
      user: { privileges: { isPrivileged: true } },
    });

    const action = screen.getByTitle('Disconnect from the Soulseek server.');
    expect(action).toHaveAttribute(
      'aria-label',
      'Disconnect from the Soulseek server.',
    );
    expect(action).toHaveAttribute(
      'data-tooltip',
      'Disconnect from the Soulseek server.',
    );
    expect(action).toHaveTextContent('Connected');
    fireEvent.click(action);
    expect(serverAPI.disconnect).toHaveBeenCalledOnce();
  });
});
