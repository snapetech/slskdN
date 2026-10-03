import '@testing-library/jest-dom';
import React from 'react';
import SystemOverview from './SystemOverview';
import { render, screen } from '@testing-library/react';

describe('SystemOverview', () => {
  it('summarizes the connected account, installed version, and clear state', () => {
    render(
      <SystemOverview
        state={{
          server: { isConnected: true },
          user: { username: 'listener' },
          version: { current: '1.2.3' },
        }}
      />,
    );

    expect(screen.getByTestId('system-overview')).toHaveTextContent('Connected');
    expect(screen.getByTestId('system-overview')).toHaveTextContent('listener');
    expect(screen.getByTestId('system-overview')).toHaveTextContent('1.2.3');
    expect(screen.getByText('None')).toBeInTheDocument();
  });

  it('shows each pending operation and makes missing connection state explicit', () => {
    render(
      <SystemOverview
        state={{
          pendingReconnect: true,
          pendingRestart: true,
          server: { isConnected: false },
          shares: { scanPending: true },
          version: { current: '1.2.3', isUpdateAvailable: true, latest: '1.2.4' },
        }}
      />,
    );

    expect(screen.getByTestId('system-overview')).toHaveTextContent('Disconnected');
    expect(screen.getByTestId('system-overview')).toHaveTextContent('Restart required');
    expect(screen.getByTestId('system-overview')).toHaveTextContent('Reconnect required');
    expect(screen.getByTestId('system-overview')).toHaveTextContent('Share scan pending');
    expect(screen.getByTestId('system-overview')).toHaveTextContent('Version 1.2.4 is available');
  });

  it('does not report disconnected while the Soulseek session is still connecting', () => {
    render(<SystemOverview state={{ server: { IsLoggingIn: true } }} />);

    expect(screen.getByTestId('system-overview')).toHaveTextContent('Connecting');
  });
});
