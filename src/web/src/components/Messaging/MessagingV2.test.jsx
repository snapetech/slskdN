import '@testing-library/jest-dom';
import * as chat from '../../lib/chat';
import * as pods from '../../lib/pods';
import * as rooms from '../../lib/rooms';
import MessagingV2 from './MessagingV2';
import React from 'react';
import { act, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/chat', () => ({
  getAll: vi.fn(),
  remove: vi.fn(),
}));

vi.mock('../../lib/pods', () => ({
  create: vi.fn(),
  discoverAll: vi.fn(),
  get: vi.fn(),
  leave: vi.fn(),
  list: vi.fn(),
}));

vi.mock('../../lib/rooms', () => ({
  getAvailable: vi.fn(),
  getJoined: vi.fn(),
  join: vi.fn(),
  leave: vi.fn(),
}));

vi.mock('../../lib/humanChallengeAutoResponse', () => ({
  applyHumanChallengeAutoResponse: vi.fn(),
  canApplyHumanChallengeAutoResponse: vi.fn(() => false),
  getHumanChallengeAutoResponseEnabled: vi.fn(() => false),
  readStoredHumanChallengeAutoResponse: vi.fn(() => false),
  writeStoredHumanChallengeAutoResponse: vi.fn(),
}));

vi.mock('./CommandHelp', () => ({ default: () => null }));
vi.mock('./Composer', () => ({ default: () => null }));
vi.mock('./MessageStream', () => ({ default: () => null }));
vi.mock('./QuickSwitcher', () => ({ default: () => null }));
vi.mock('./UserPopover', () => ({ default: () => null }));

const savedPods = Array.from({ length: 10 }, (_, index) => ({
  channels: [
    {
      channelId: 'general',
      kind: 'General',
      name: 'General',
    },
  ],
  name: `Pod ${index + 1}`,
  podId: `pod:${String(index + 1).padStart(32, '0')}`,
}));

const setDocumentHidden = (hidden) => {
  Object.defineProperty(document, 'hidden', {
    configurable: true,
    value: hidden,
  });
};

const flushPromises = async () => {
  await act(async () => {
    await Promise.resolve();
    await Promise.resolve();
  });
};

const renderMessaging = () =>
  render(
    <MemoryRouter>
      <MessagingV2 state={{ user: { username: 'local-user' } }} />
    </MemoryRouter>,
  );

describe('MessagingV2 hydration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.localStorage.clear();
    setDocumentHidden(false);
    chat.getAll.mockResolvedValue([]);
    rooms.getAvailable.mockResolvedValue(['ambient']);
    rooms.getJoined.mockResolvedValue([]);
    pods.discoverAll.mockResolvedValue([]);
    pods.list.mockResolvedValue(savedPods);
  });

  afterEach(() => {
    vi.useRealTimers();
    setDocumentHidden(false);
  });

  it('uses channel details from the pod list without per-pod detail requests', async () => {
    renderMessaging();

    await waitFor(() => {
      expect(screen.getByText('Pod 1 / General')).toBeInTheDocument();
    });

    expect(chat.getAll).toHaveBeenCalledTimes(1);
    expect(rooms.getJoined).toHaveBeenCalledTimes(1);
    expect(pods.list).toHaveBeenCalledTimes(1);
    expect(pods.discoverAll).toHaveBeenCalledTimes(1);
    expect(pods.get).not.toHaveBeenCalled();
  });

  it('polls messaging every ten seconds and pod metadata every sixty seconds', async () => {
    vi.useFakeTimers();
    renderMessaging();
    await flushPromises();

    expect(chat.getAll).toHaveBeenCalledTimes(1);
    expect(pods.list).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(59_999);
    });

    expect(chat.getAll).toHaveBeenCalledTimes(6);
    expect(rooms.getJoined).toHaveBeenCalledTimes(6);
    expect(pods.list).toHaveBeenCalledTimes(1);
    expect(pods.discoverAll).toHaveBeenCalledTimes(1);

    await act(async () => {
      await vi.advanceTimersByTimeAsync(1);
    });

    expect(chat.getAll).toHaveBeenCalledTimes(7);
    expect(rooms.getJoined).toHaveBeenCalledTimes(7);
    expect(pods.list).toHaveBeenCalledTimes(2);
    expect(pods.discoverAll).toHaveBeenCalledTimes(2);
  });

  it('does not overlap slow messaging hydration', async () => {
    vi.useFakeTimers();
    let resolveConversations;
    chat.getAll.mockReturnValue(new Promise((resolve) => {
      resolveConversations = resolve;
    }));

    renderMessaging();
    await flushPromises();

    await act(async () => {
      await vi.advanceTimersByTimeAsync(30_000);
    });

    expect(chat.getAll).toHaveBeenCalledTimes(1);
    expect(rooms.getJoined).toHaveBeenCalledTimes(1);

    await act(async () => {
      resolveConversations([]);
      await Promise.resolve();
      await Promise.resolve();
      await vi.advanceTimersByTimeAsync(10_000);
    });

    expect(chat.getAll).toHaveBeenCalledTimes(2);
    expect(rooms.getJoined).toHaveBeenCalledTimes(2);
  });

  it('starts on visibility and suspends both polling cadences while hidden', async () => {
    vi.useFakeTimers();
    setDocumentHidden(true);
    renderMessaging();
    await flushPromises();

    expect(chat.getAll).not.toHaveBeenCalled();
    expect(pods.list).not.toHaveBeenCalled();

    await act(async () => {
      setDocumentHidden(false);
      document.dispatchEvent(new Event('visibilitychange'));
      await Promise.resolve();
      await Promise.resolve();
    });

    expect(chat.getAll).toHaveBeenCalledTimes(1);
    expect(pods.list).toHaveBeenCalledTimes(1);

    await act(async () => {
      setDocumentHidden(true);
      document.dispatchEvent(new Event('visibilitychange'));
      await vi.advanceTimersByTimeAsync(60_000);
    });

    expect(chat.getAll).toHaveBeenCalledTimes(1);
    expect(rooms.getJoined).toHaveBeenCalledTimes(1);
    expect(pods.list).toHaveBeenCalledTimes(1);
    expect(pods.discoverAll).toHaveBeenCalledTimes(1);
  });
});
