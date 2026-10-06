import * as podsApi from '../../lib/pods';
import { Pods } from './Pods';
import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
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
    podsApi.discoverAll.mockResolvedValue([]);
    podsApi.discoverByName.mockResolvedValue([
      { name: 'New Pod', podId: 'pod:new', tags: ['ambient'] },
    ]);
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

  it('names icon controls and explains Pod creation, discovery, membership, and messaging', async () => {
    renderPods({ channelId: 'general', podId: pod.podId });
    expect(await screen.findByRole('heading', { name: 'Ambient Pod' })).toBeInTheDocument();

    const createPod = screen.getByRole('button', { name: 'Create a pod' });
    fireEvent.mouseEnter(createPod);
    expect(
      await screen.findByText(
        'Create a durable pod with a default channel. It is saved by the daemon and restored after restart.',
      ),
    ).toBeInTheDocument();
    expect(podsApi.create).not.toHaveBeenCalled();
    fireEvent.click(createPod);
    expect(await screen.findByText('Create Pod')).toBeInTheDocument();
    const create = screen.getByRole('button', { name: 'Create' });
    expect(create).toBeDisabled();
    fireEvent.mouseEnter(create.parentElement);
    expect(
      await screen.findByText(
        'Create a pod with a General channel and save it on the server so it remains available after restarts.',
      ),
    ).toBeInTheDocument();
    const cancel = screen.getByRole('button', { name: 'Cancel' });
    fireEvent.mouseEnter(cancel);
    expect(
      await screen.findByText(
        'Close this form without creating a pod; the entered details are discarded.',
      ),
    ).toBeInTheDocument();
    fireEvent.click(cancel);
    expect(podsApi.create).not.toHaveBeenCalled();

    const discover = screen.getByRole('button', {
      name: 'Search the pod discovery index',
    });
    fireEvent.mouseEnter(discover);
    expect(
      await screen.findByText(
        'Search the pod discovery index for listed pods. This sends your search term to the discovery service so you can find pods to save locally.',
      ),
    ).toBeInTheDocument();
    expect(podsApi.discoverByName).not.toHaveBeenCalled();
    fireEvent.change(screen.getByPlaceholderText('Find pods...'), {
      target: { value: 'ambient' },
    });
    fireEvent.click(discover);
    expect(await screen.findByText('New Pod')).toBeInTheDocument();
    await waitFor(() => {
      expect(podsApi.discoverByName).toHaveBeenCalledWith('ambient');
    });

    const saveDiscoveredPod = screen.getByRole('button', {
      name: 'Save discovered pod New Pod locally',
    });
    fireEvent.mouseEnter(saveDiscoveredPod);
    expect(
      await screen.findByText(
        "Save this pod's details in your local pod list so it is available after restarts. This does not join the pod.",
      ),
    ).toBeInTheDocument();
    expect(podsApi.create).not.toHaveBeenCalled();

    const leavePod = screen.getByRole('button', { name: `Leave pod ${pod.name}` });
    fireEvent.mouseEnter(leavePod);
    expect(
      await screen.findByText(
        'Remove the current peer from this pod and stop participating in its channels. Leave when you no longer want this peer to participate.',
      ),
    ).toBeInTheDocument();
    expect(podsApi.leave).not.toHaveBeenCalled();

    const channel = screen.getByRole('button', { name: 'Open General channel' });
    fireEvent.mouseEnter(channel);
    expect(
      await screen.findByText(
        'Open General to read its messages and compose updates for the channel participants.',
      ),
    ).toBeInTheDocument();
    const send = screen.getByRole('button', {
      name: 'Send a message to the active pod channel',
    });
    fireEvent.mouseEnter(send);
    expect(
      await screen.findByText(
        "Send this text to the active pod channel's participants. Use this when you want to share an update with them.",
      ),
    ).toBeInTheDocument();
    expect(podsApi.sendMessage).not.toHaveBeenCalled();
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
