import * as chat from '../../lib/chat';
import * as pods from '../../lib/pods';
import * as rooms from '../../lib/rooms';

const POLL_INTERVAL_MS = 2_000;
const ME_PREFIX = '/me ';
const CTCP_ACTION = 'ACTION ';
const LISTEN_ALONG_KIND = 'slskdn.listenAlong.v1';

const asArray = (value) => (Array.isArray(value) ? value : []);

const detectListenAlong = (body) => {
  if (typeof body !== 'string' || body.length === 0 || body[0] !== '{') {
    return null;
  }
  try {
    const parsed = JSON.parse(body);
    if (parsed && parsed.kind === LISTEN_ALONG_KIND) {
      return parsed;
    }
  } catch {
    // not json, not our payload
  }
  return null;
};

const classifyBody = (rawBody) => {
  const body = typeof rawBody === 'string' ? rawBody : '';

  if (body.startsWith(ME_PREFIX)) {
    return { body: body.slice(ME_PREFIX.length), kind: 'me' };
  }
  if (body.startsWith(CTCP_ACTION) && body.endsWith('')) {
    return { body: body.slice(CTCP_ACTION.length, -1), kind: 'me' };
  }

  const listenAlong = detectListenAlong(body);
  if (listenAlong) {
    return { body, kind: 'listenalong', meta: listenAlong };
  }

  return { body, kind: 'text' };
};

const messageId = (parts) => parts.filter((part) => part != null).join('|');

export const createChatAdapter = ({ username, currentUser }) => ({
  capabilities: { batch: true },
  fetchOnce: false,
  members: null,
  pollIntervalMs: POLL_INTERVAL_MS,
  topic: `@${username}`,
  type: 'chat',

  async list() {
    if (!username) return { messages: [], unread: 0 };
    let conversation;
    try {
      conversation = await chat.get({ username });
    } catch {
      return { messages: [], unread: 0 };
    }

    const rawMessages = asArray(conversation?.messages).filter(
      (item) => item && typeof item === 'object' && !Array.isArray(item),
    );
    const messages = rawMessages.map((m, index) => {
      const isSelf = m.direction === 'Out';
      const sender = isSelf ? currentUser || 'me' : m.username;
      const classified = classifyBody(m.message);
      return {
        body: classified.body,
        id: messageId([m.timestamp, m.direction, index]),
        isSelf,
        kind: classified.kind,
        meta: classified.meta,
        sender,
        ts: typeof m.timestamp === 'number' ? m.timestamp : Number(m.timestamp) || 0,
      };
    });

    if (conversation?.hasUnAcknowledgedMessages) {
      chat.acknowledge({ username }).catch(() => {});
    }

    return { messages, unread: 0 };
  },

  send(body) {
    if (!username) return Promise.resolve();
    return chat.send({ message: body, username });
  },
});

export const createRoomAdapter = ({ roomName, currentUser }) => ({
  capabilities: { contextMenu: true },
  pollIntervalMs: POLL_INTERVAL_MS,
  topic: `#${roomName}`,
  type: 'room',

  async list() {
    if (!roomName) return { messages: [] };
    let raw;
    try {
      raw = await rooms.getMessages({ roomName });
    } catch {
      return { messages: [] };
    }
    const messages = asArray(raw)
      .filter((item) => item && typeof item === 'object' && !Array.isArray(item))
      .map((m, index) => {
        const sender = m.username || 'unknown';
        const isSelf = currentUser && sender === currentUser;
        const classified = classifyBody(m.message);
        return {
          body: classified.body,
          id: messageId([m.timestamp, sender, index]),
          isSelf: Boolean(isSelf),
          kind: classified.kind,
          meta: classified.meta,
          sender,
          ts: typeof m.timestamp === 'number' ? m.timestamp : Number(m.timestamp) || 0,
        };
      });
    return { messages };
  },

  send(body) {
    if (!roomName) return Promise.resolve();
    return rooms.sendMessage({ message: body, roomName });
  },

  async members() {
    try {
      const users = await rooms.getUsers({ roomName });
      return asArray(users).filter(Boolean);
    } catch {
      return [];
    }
  },
});

export const createPodAdapter = ({ channel, currentUser }) => ({
  capabilities: { listenAlong: true },
  pollIntervalMs: POLL_INTERVAL_MS,
  topic: channel?.podName
    ? `${channel.podName} / ${channel.channelName || channel.channelId}`
    : 'Pod channel',
  type: 'pod',

  async list() {
    if (!channel?.podId || !channel?.channelId) return { messages: [] };
    let raw;
    try {
      raw = await pods.getMessages(channel.podId, channel.channelId);
    } catch {
      return { messages: [] };
    }
    const messages = asArray(raw)
      .filter((item) => item && typeof item === 'object' && !Array.isArray(item))
      .map((m, index) => {
        const sender = m.senderPeerId || 'unknown';
        const isSelf = currentUser && sender === currentUser;
        const classified = classifyBody(m.body);
        return {
          body: classified.body,
          id: messageId([m.timestampUnixMs, sender, index]),
          isSelf: Boolean(isSelf),
          kind: classified.kind,
          meta: classified.meta,
          sender,
          ts:
            typeof m.timestampUnixMs === 'number'
              ? m.timestampUnixMs
              : Number(m.timestampUnixMs) || 0,
        };
      });
    return { messages };
  },

  send(body) {
    if (!channel?.podId || !channel?.channelId) return Promise.resolve();
    return pods.sendMessage(
      channel.podId,
      channel.channelId,
      body,
      currentUser || 'local-peer',
    );
  },

  async members() {
    if (!channel?.podId) return [];
    try {
      const raw = await pods.getMembers(channel.podId);
      return asArray(raw).filter(
        (item) => item && typeof item === 'object' && !Array.isArray(item),
      );
    } catch {
      return [];
    }
  },
});

export const __test__ = { classifyBody, detectListenAlong };
