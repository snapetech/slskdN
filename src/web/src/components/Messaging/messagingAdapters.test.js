import * as pods from '../../lib/pods';
import { createPodAdapter } from './messagingAdapters';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/pods', () => ({
  getMembers: vi.fn(),
  getMessages: vi.fn(),
  sendMessage: vi.fn(),
}));

const podMessage = ({ body, id, timestamp }) => ({
  body,
  channelId: 'general',
  messageId: id,
  podId: 'pod:00000000000000000000000000000001',
  senderPeerId: 'peer-one',
  timestampUnixMs: timestamp,
});

describe('createPodAdapter', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('uses an overlapping timestamp cursor and merges stable message IDs', async () => {
    const first = podMessage({ body: 'first', id: 'm1', timestamp: 1_000 });
    const second = podMessage({ body: 'second', id: 'm2', timestamp: 2_000 });
    const third = podMessage({ body: 'third', id: 'm3', timestamp: 3_000 });
    pods.getMessages
      .mockResolvedValueOnce([first, second])
      .mockResolvedValueOnce([second, third])
      .mockRejectedValueOnce(new Error('temporary failure'));
    const adapter = createPodAdapter({
      channel: {
        channelId: 'general',
        channelName: 'General',
        podId: first.podId,
        podName: 'Test Pod',
      },
      currentUser: 'local-peer',
    });

    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ body: 'first', id: 'm1' }),
        expect.objectContaining({ body: 'second', id: 'm2' }),
      ],
    });
    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ body: 'first', id: 'm1' }),
        expect.objectContaining({ body: 'second', id: 'm2' }),
        expect.objectContaining({ body: 'third', id: 'm3' }),
      ],
    });
    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ body: 'first', id: 'm1' }),
        expect.objectContaining({ body: 'second', id: 'm2' }),
        expect.objectContaining({ body: 'third', id: 'm3' }),
      ],
    });

    expect(pods.getMessages).toHaveBeenNthCalledWith(1, first.podId, 'general', null);
    expect(pods.getMessages).toHaveBeenNthCalledWith(2, first.podId, 'general', 1_999);
    expect(pods.getMessages).toHaveBeenNthCalledWith(3, first.podId, 'general', 2_999);
  });
});
