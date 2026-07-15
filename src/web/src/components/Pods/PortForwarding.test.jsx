// <copyright file="PortForwarding.test.jsx" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import '@testing-library/jest-dom';
import * as pods from '../../lib/pods';
import * as portForwarding from '../../lib/portForwarding';
import PortForwarding from './PortForwarding';
import React from 'react';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/pods', () => ({
  get: vi.fn(),
  getMembers: vi.fn(),
  list: vi.fn(),
}));

vi.mock('../../lib/portForwarding', () => ({
  getAvailablePorts: vi.fn(),
  getForwardingStatus: vi.fn(),
  startForwarding: vi.fn(),
  stopForwarding: vi.fn(),
}));

const setDocumentHidden = (hidden) => {
  Object.defineProperty(document, 'hidden', {
    configurable: true,
    value: hidden,
  });
};

const forwarding = {
  activeConnections: 3,
  bytesForwarded: 12_288,
  destinationHost: 'service.internal',
  destinationPort: 443,
  isActive: true,
  localPort: 8_080,
  performance: {
    averageBytesPerConnection: 4_096,
    isHighThroughput: true,
  },
  podId: 'pod:test',
  serviceName: 'service',
  streamMappingEnabled: true,
};

describe('Pod port forwarding', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setDocumentHidden(false);
    pods.getMembers.mockResolvedValue([
      { peerId: 'peer-one' },
      { peerId: 'peer-two' },
      { peerId: 'peer-three' },
    ]);
    pods.list.mockResolvedValue([
      {
        capabilities: ['PrivateServiceGateway'],
        name: 'Test Pod',
        podId: 'pod:test',
        privateServicePolicy: { enabled: true },
      },
    ]);
    portForwarding.getAvailablePorts.mockResolvedValue({
      availablePortCount: 64_512,
      availablePorts: [1_024, 1_025],
      usedPortCount: 0,
    });
    portForwarding.getForwardingStatus.mockResolvedValue([forwarding]);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.restoreAllMocks();
    setDocumentHidden(false);
  });

  it('lazy-loads bounded secondary-tab data and renders only the active pane', async () => {
    render(<PortForwarding />);

    await waitFor(() => {
      expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(1);
      expect(pods.list).toHaveBeenCalledTimes(1);
    });
    expect(portForwarding.getAvailablePorts).not.toHaveBeenCalled();
    expect(pods.getMembers).not.toHaveBeenCalled();
    expect(screen.queryByText('Available ports for forwarding (1024-65535):'))
      .not.toBeInTheDocument();
    expect(screen.queryByText('Total Connections')).not.toBeInTheDocument();

    fireEvent.click(screen.getByText('Available Ports'));

    await waitFor(() => {
      expect(portForwarding.getAvailablePorts).toHaveBeenCalledWith(
        1_024,
        65_535,
        100,
      );
    });
    expect(screen.getByText('64512')).toBeInTheDocument();
    expect(screen.getByText(/\(\+64510 more\)/)).toBeInTheDocument();

    fireEvent.click(screen.getByText('VPN Pods'));

    await waitFor(() => {
      expect(pods.getMembers).toHaveBeenCalledWith('pod:test');
    });
    expect(screen.getByText('Total Members').previousElementSibling)
      .toHaveTextContent('3');
  });

  it('renders real forwarding statistics without a synthetic stats timer', async () => {
    render(<PortForwarding />);
    await waitFor(() => {
      expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(1);
    });

    fireEvent.click(screen.getByText('Tunnel Statistics'));

    expect(screen.getByText('Total Connections')).toBeInTheDocument();
    expect(screen.getByText('12.0 KB')).toBeInTheDocument();
    expect(screen.getByText('4.0 KB')).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
  });

  it('defers initial hydration until a hidden document becomes visible', async () => {
    vi.useFakeTimers();
    setDocumentHidden(true);

    render(<PortForwarding />);
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(pods.list).not.toHaveBeenCalled();
    expect(portForwarding.getForwardingStatus).not.toHaveBeenCalled();

    await act(async () => {
      setDocumentHidden(false);
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(pods.list).toHaveBeenCalledTimes(1);
    expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(1);
  });

  it('retains the last successful status when a poll fails', async () => {
    vi.useFakeTimers();
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    portForwarding.getForwardingStatus
      .mockResolvedValueOnce([forwarding])
      .mockRejectedValueOnce(new Error('temporary failure'));

    render(<PortForwarding />);
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(screen.getByText('localhost:8080')).toBeInTheDocument();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });
    expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(2);
    expect(screen.getByText('localhost:8080')).toBeInTheDocument();
    consoleError.mockRestore();
  });

  it('rejects overlapping status polls and suspends them while hidden', async () => {
    vi.useFakeTimers();
    let resolveStatus;
    portForwarding.getForwardingStatus.mockReturnValue(
      new Promise((resolve) => {
        resolveStatus = resolve;
      }),
    );

    render(<PortForwarding />);
    await act(async () => {
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolveStatus([]);
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(10_000);
    });
    expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(2);

    await act(async () => {
      setDocumentHidden(true);
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(30_000);
    });
    expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(2);

    await act(async () => {
      setDocumentHidden(false);
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
      await Promise.resolve();
    });
    expect(portForwarding.getForwardingStatus).toHaveBeenCalledTimes(3);
  });
});
