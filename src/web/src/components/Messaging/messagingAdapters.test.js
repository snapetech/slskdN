import * as chat from '../../lib/chat';
import * as pods from '../../lib/pods';
import * as rooms from '../../lib/rooms';
import {
  __test__,
  createChatAdapter,
  createPodAdapter,
  createRoomAdapter,
} from './messagingAdapters';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../../lib/chat', () => ({
  acknowledge: vi.fn(() => Promise.resolve()),
  get: vi.fn(),
  send: vi.fn(),
}));

vi.mock('../../lib/pods', () => ({
  getMembers: vi.fn(),
  getMessages: vi.fn(),
  sendMessage: vi.fn(),
}));

vi.mock('../../lib/rooms', () => ({
  getMessages: vi.fn(),
  getUsers: vi.fn(),
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

const privateMessage = ({ body, id, timestamp }) => ({
  direction: id % 2 === 0 ? 'Out' : 'In',
  id,
  message: body,
  timestamp,
  username: 'friend',
});

describe('timestamp normalization', () => {
  it('parses ASP.NET ISO timestamps and preserves numeric milliseconds', () => {
    expect(__test__.normalizeTimestamp('2026-07-15T12:00:00.123Z')).toBe(
      Date.parse('2026-07-15T12:00:00.123Z'),
    );
    expect(__test__.normalizeTimestamp(1_234)).toBe(1_234);
    expect(__test__.normalizeTimestamp('invalid')).toBe(0);
  });
});

describe('createChatAdapter', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('uses an overlapping ISO timestamp cursor and merges stable message IDs', async () => {
    const first = privateMessage({
      body: 'first',
      id: 1,
      timestamp: '2026-07-15T12:00:00.100Z',
    });
    const second = privateMessage({
      body: 'second',
      id: 2,
      timestamp: '2026-07-15T12:00:00.200Z',
    });
    const third = privateMessage({
      body: 'third',
      id: 3,
      timestamp: '2026-07-15T12:00:00.300Z',
    });
    chat.get
      .mockResolvedValueOnce({ messages: [first, second] })
      .mockResolvedValueOnce({ messages: [second, third] })
      .mockRejectedValueOnce(new Error('temporary failure'));
    const adapter = createChatAdapter({
      currentUser: 'local-peer',
      username: 'friend',
    });

    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ body: 'first', ts: Date.parse(first.timestamp) }),
        expect.objectContaining({ body: 'second', ts: Date.parse(second.timestamp) }),
      ],
      unread: 0,
    });
    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ body: 'first' }),
        expect.objectContaining({ body: 'second' }),
        expect.objectContaining({ body: 'third' }),
      ],
      unread: 0,
    });
    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ body: 'first' }),
        expect.objectContaining({ body: 'second' }),
        expect.objectContaining({ body: 'third' }),
      ],
      unread: 0,
    });

    expect(chat.get).toHaveBeenNthCalledWith(1, {
      since: null,
      username: 'friend',
    });
    expect(chat.get).toHaveBeenNthCalledWith(2, {
      since: Date.parse(second.timestamp) - 1,
      username: 'friend',
    });
    expect(chat.get).toHaveBeenNthCalledWith(3, {
      since: Date.parse(third.timestamp) - 1,
      username: 'friend',
    });
  });
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

describe('createRoomAdapter', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('uses an overlapping cursor and merges stable room-message IDs', async () => {
    const first = {
      id: 'room-message-1',
      message: 'first',
      timestamp: '2026-07-15T12:00:00.100Z',
      username: 'friend',
    };
    const second = {
      id: 'room-message-2',
      message: 'second',
      timestamp: '2026-07-15T12:00:00.200Z',
      username: 'me',
    };
    const third = {
      id: 'room-message-3',
      message: 'third',
      timestamp: '2026-07-15T12:00:00.300Z',
      username: 'friend',
    };
    rooms.getMessages
      .mockResolvedValueOnce([first, second])
      .mockResolvedValueOnce([second, third])
      .mockRejectedValueOnce(new Error('temporary failure'));
    const adapter = createRoomAdapter({
      currentUser: 'me',
      roomName: 'ambient',
    });

    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ body: 'first', id: first.id }),
        expect.objectContaining({ body: 'second', id: second.id, isSelf: true }),
      ],
    });
    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ id: first.id }),
        expect.objectContaining({ id: second.id }),
        expect.objectContaining({ body: 'third', id: third.id }),
      ],
    });
    await expect(adapter.list()).resolves.toEqual({
      messages: [
        expect.objectContaining({ id: first.id }),
        expect.objectContaining({ id: second.id }),
        expect.objectContaining({ id: third.id }),
      ],
    });

    expect(rooms.getMessages).toHaveBeenNthCalledWith(1, {
      roomName: 'ambient',
      since: null,
    });
    expect(rooms.getMessages).toHaveBeenNthCalledWith(2, {
      roomName: 'ambient',
      since: Date.parse(second.timestamp) - 1,
    });
    expect(rooms.getMessages).toHaveBeenNthCalledWith(3, {
      roomName: 'ambient',
      since: Date.parse(third.timestamp) - 1,
    });
  });

  it('bounds the initial room-message snapshot', async () => {
    rooms.getMessages.mockResolvedValue(
      Array.from({ length: 101 }, (_, index) => ({
        id: `room-message-${index}`,
        message: `message ${index}`,
        timestamp: index,
        username: 'friend',
      })),
    );
    const adapter = createRoomAdapter({ roomName: 'ambient' });

    const result = await adapter.list();

    expect(result.messages).toHaveLength(100);
    expect(result.messages[0].id).toBe('room-message-1');
    expect(result.messages[99].id).toBe('room-message-100');
  });
});
