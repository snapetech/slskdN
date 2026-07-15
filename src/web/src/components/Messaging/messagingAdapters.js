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
const normalizeTimestamp = (value) => {
  if (typeof value === 'number') {
    return Number.isFinite(value) ? value : 0;
  }

  const numeric = Number(value);
  if (value !== '' && Number.isFinite(numeric)) {
    return numeric;
  }

  const parsed = Date.parse(value);
  return Number.isFinite(parsed) ? parsed : 0;
};

export const createChatAdapter = ({ username, currentUser }) => {
  let cachedMessages = [];
  let latestTimestamp = null;

  return {
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
        const since = latestTimestamp === null ? null : Math.max(0, latestTimestamp - 1);
        conversation = await chat.get({ since, username });
      } catch {
        return { messages: cachedMessages, unread: 0 };
      }

      const rawMessages = asArray(conversation?.messages).filter(
        (item) => item && typeof item === 'object' && !Array.isArray(item),
      );
      const mapped = rawMessages.map((message) => {
        const isSelf = message.direction === 'Out';
        const sender = isSelf ? currentUser || 'me' : message.username;
        const classified = classifyBody(message.message);
        const timestamp = normalizeTimestamp(message.timestamp);
        return {
          body: classified.body,
          id: messageId([
            message.id,
            message.timestamp,
            message.direction,
            message.username,
          ]),
          isSelf,
          kind: classified.kind,
          meta: classified.meta,
          sender,
          ts: timestamp,
        };
      });

      const byId = new Map(cachedMessages.map((message) => [message.id, message]));
      mapped.forEach((message) => byId.set(message.id, message));
      cachedMessages = Array.from(byId.values())
        .sort((left, right) => left.ts - right.ts || left.id.localeCompare(right.id))
        .slice(-100);
      mapped.forEach((message) => {
        if (latestTimestamp === null || message.ts > latestTimestamp) {
          latestTimestamp = message.ts;
        }
      });

      if (conversation?.hasUnAcknowledgedMessages) {
        chat.acknowledge({ username }).catch(() => {});
      }

      return { messages: cachedMessages, unread: 0 };
    },

    send(body) {
      if (!username) return Promise.resolve();
      return chat.send({ message: body, username });
    },
  };
};

export const createRoomAdapter = ({ roomName, currentUser }) => {
  let cachedMessages = [];
  let latestTimestamp = null;

  return {
    capabilities: { contextMenu: true },
    pollIntervalMs: POLL_INTERVAL_MS,
    topic: `#${roomName}`,
    type: 'room',

    async list() {
      if (!roomName) return { messages: [] };
      let raw;
      try {
        const since =
          latestTimestamp === null ? null : Math.max(0, latestTimestamp - 1);
        raw = await rooms.getMessages({ roomName, since });
      } catch {
        return { messages: cachedMessages };
      }
      const mapped = asArray(raw)
        .filter(
          (item) => item && typeof item === 'object' && !Array.isArray(item),
        )
        .map((message) => {
          const sender = message.username || 'unknown';
          const isSelf = currentUser && sender === currentUser;
          const classified = classifyBody(message.message);
          const timestamp = normalizeTimestamp(message.timestamp);
          return {
            body: classified.body,
            id:
              message.id ||
              messageId([
                message.timestamp,
                sender,
                message.message,
              ]),
            isSelf: Boolean(isSelf),
            kind: classified.kind,
            meta: classified.meta,
            sender,
            ts: timestamp,
          };
        });

      if (latestTimestamp === null) {
        cachedMessages = mapped.slice(-100);
      } else if (mapped.length > 0) {
        const byId = new Map(
          cachedMessages.map((message) => [message.id, message]),
        );
        mapped.forEach((message) => byId.set(message.id, message));
        cachedMessages = Array.from(byId.values()).slice(-100);
      }

      mapped.forEach((message) => {
        if (latestTimestamp === null || message.ts > latestTimestamp) {
          latestTimestamp = message.ts;
        }
      });

      return { messages: cachedMessages };
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
  };
};

export const createPodAdapter = ({ channel, currentUser }) => {
  let cachedMessages = [];
  let latestTimestamp = null;

  return {
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
        const since = latestTimestamp === null ? null : Math.max(0, latestTimestamp - 1);
        raw = await pods.getMessages(channel.podId, channel.channelId, since);
      } catch {
        return { messages: cachedMessages };
      }

      const received = asArray(raw)
        .filter((item) => item && typeof item === 'object' && !Array.isArray(item));
      const mapped = received.map((m) => {
        const sender = m.senderPeerId || 'unknown';
        const isSelf = currentUser && sender === currentUser;
        const classified = classifyBody(m.body);
        const timestamp =
          normalizeTimestamp(m.timestampUnixMs);
        return {
          body: classified.body,
          id: m.messageId || messageId([timestamp, sender, m.body, m.signature]),
          isSelf: Boolean(isSelf),
          kind: classified.kind,
          meta: classified.meta,
          sender,
          ts: timestamp,
        };
      });

      if (latestTimestamp === null) {
        cachedMessages = mapped;
      } else if (mapped.length > 0) {
        const byId = new Map(cachedMessages.map((message) => [message.id, message]));
        mapped.forEach((message) => byId.set(message.id, message));
        cachedMessages = Array.from(byId.values())
          .sort((a, b) => a.ts - b.ts || a.id.localeCompare(b.id))
          .slice(-100);
      }

      received.forEach((message) => {
        const timestamp = Number(message.timestampUnixMs) || 0;
        if (latestTimestamp === null || timestamp > latestTimestamp) {
          latestTimestamp = timestamp;
        }
      });

      return { messages: cachedMessages };
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
  };
};

export const __test__ = { classifyBody, detectListenAlong, normalizeTimestamp };
