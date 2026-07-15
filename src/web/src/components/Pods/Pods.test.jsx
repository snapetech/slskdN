import * as podsApi from '../../lib/pods';
import { Pods } from './Pods';
import { act, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/pods', () => ({
  create: vi.fn(),
  discoverAll: vi.fn(),
  discoverByName: vi.fn(),
  get: vi.fn(),
  getMembers: vi.fn(),
  getMessages: vi.fn(),
  leave: vi.fn(),
  list: vi.fn(),
  sendMessage: vi.fn(),
}));

vi.mock('../Player/PodListenAlongPanel', () => ({
  default: () => <div>Listen Along</div>,
}));

vi.mock('./PortForwarding', () => ({
  default: () => <div>Port Forwarding</div>,
}));

vi.mock('./VpnGatewayConfig', () => ({
  default: () => <div>VPN Gateway</div>,
}));

const pod = {
  channels: [
    {
      channelId: 'general',
      kind: 'General',
      name: 'General',
    },
  ],
  description: 'Test pod',
  podId: 'pod:00000000000000000000000000000001',
  tags: ['ambient'],
  visibility: 'Unlisted',
  name: 'Ambient Pod',
};

const firstMessage = {
  body: 'first',
  channelId: 'general',
  messageId: 'm1',
  podId: pod.podId,
  senderPeerId: 'peer-one',
  signature: 'sig-1',
  timestampUnixMs: 1_000,
};

const secondMessage = {
  ...firstMessage,
  body: 'second',
  messageId: 'm2',
  signature: 'sig-2',
  timestampUnixMs: 2_000,
};

const setDocumentHidden = (hidden) => {
  Object.defineProperty(document, 'hidden', {
    configurable: true,
    value: hidden,
  });
};

const renderPods = (params = {}) => {
  const navigate = vi.fn();
  render(
    <Pods
      location={{ pathname: '/pods' }}
      navigate={navigate}
      params={params}
      state={{ user: { username: 'local-peer' } }}
    />,
  );
  return navigate;
};

const flushPromises = async () => {
  await Promise.resolve();
  await Promise.resolve();
  await Promise.resolve();
  await Promise.resolve();
};

describe('Pods', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    setDocumentHidden(false);
    podsApi.list.mockResolvedValue([pod]);
    podsApi.get.mockResolvedValue(pod);
    podsApi.getMembers.mockResolvedValue([
      { peerId: 'local-peer', role: 'member' },
    ]);
    podsApi.getMessages.mockResolvedValue([]);
  });

  afterEach(() => {
    vi.useRealTimers();
    setDocumentHidden(false);
  });

  it('hydrates a direct channel route from list metadata without a detail request', async () => {
    renderPods({ channelId: 'general', podId: pod.podId });

    expect(await screen.findByRole('heading', { name: 'Ambient Pod' })).toBeInTheDocument();
    await waitFor(() =>
      expect(podsApi.getMessages).toHaveBeenCalledWith(
        pod.podId,
        'general',
        null,
      ),
    );

    expect(podsApi.get).not.toHaveBeenCalled();
    expect(podsApi.getMembers).toHaveBeenCalledWith(pod.podId);
  });

  it('uses a sixty-second metadata cadence and incremental message cursor', async () => {
    vi.useFakeTimers();
    podsApi.getMessages
      .mockResolvedValueOnce([firstMessage])
      .mockResolvedValueOnce([firstMessage, secondMessage])
      .mockResolvedValue([]);
    renderPods({ channelId: 'general', podId: pod.podId });

    await act(async () => {
      await flushPromises();
    });
    expect(podsApi.getMessages).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(60_000);
    });

    expect(podsApi.list).toHaveBeenCalledTimes(2);
    expect(podsApi.getMessages).toHaveBeenCalledTimes(31);
    expect(podsApi.getMessages).toHaveBeenNthCalledWith(
      2,
      pod.podId,
      'general',
      999,
    );
    expect(podsApi.getMessages).toHaveBeenNthCalledWith(
      3,
      pod.podId,
      'general',
      1_999,
    );
    expect(screen.getByText('first')).toBeInTheDocument();
    expect(screen.getByText('second')).toBeInTheDocument();
  });

  it('does not hydrate or poll while hidden and refreshes immediately when visible', async () => {
    vi.useFakeTimers();
    setDocumentHidden(true);
    renderPods({ channelId: 'general', podId: pod.podId });

    expect(podsApi.list).not.toHaveBeenCalled();
    expect(podsApi.getMessages).not.toHaveBeenCalled();

    await act(async () => {
      setDocumentHidden(false);
      document.dispatchEvent(new Event('visibilitychange'));
      await flushPromises();
    });
    expect(podsApi.list).toHaveBeenCalledTimes(1);
    expect(podsApi.getMessages).toHaveBeenCalledTimes(1);

    await act(async () => {
      setDocumentHidden(true);
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(120_000);
    });
    expect(podsApi.list).toHaveBeenCalledTimes(1);
    expect(podsApi.getMessages).toHaveBeenCalledTimes(1);
  });

  it('does not overlap slow message polling requests', async () => {
    vi.useFakeTimers();
    renderPods({ channelId: 'general', podId: pod.podId });
    await act(async () => {
      await flushPromises();
    });

    let resolveMessages;
    podsApi.getMessages.mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveMessages = resolve;
        }),
    );
    await act(async () => {
      await vi.advanceTimersByTimeAsync(10_000);
    });
    expect(podsApi.getMessages).toHaveBeenCalledTimes(2);

    await act(async () => {
      resolveMessages([]);
      await flushPromises();
      await vi.advanceTimersByTimeAsync(2_000);
    });
    expect(podsApi.getMessages).toHaveBeenCalledTimes(3);
  });
});
