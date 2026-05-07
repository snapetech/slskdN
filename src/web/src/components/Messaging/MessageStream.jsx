import './MessageStream.css';
import React, { useCallback, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';

const NICK_PALETTE = [
  '#e07b91',
  '#e09d3b',
  '#d6c93b',
  '#7bd66e',
  '#3bd6b5',
  '#5ac0d6',
  '#7b9be0',
  '#a87be0',
  '#d67bce',
  '#e07b7b',
];

const SAME_SENDER_WINDOW_MS = 60_000;

const nickColor = (name) => {
  if (!name) return NICK_PALETTE[0];
  let hash = 5381;
  for (let index = 0; index < name.length; index += 1) {
    hash = ((hash << 5) + hash + name.charCodeAt(index)) | 0;
  }
  return NICK_PALETTE[Math.abs(hash) % NICK_PALETTE.length];
};

const pad2 = (value) => `${value}`.padStart(2, '0');

const formatTime = (ts) => {
  if (!ts) return '--:--';
  const date = new Date(ts);
  return `${pad2(date.getHours())}:${pad2(date.getMinutes())}`;
};

const formatDayHeader = (ts) => {
  if (!ts) return '';
  const date = new Date(ts);
  return new Intl.DateTimeFormat(undefined, {
    day: 'numeric',
    month: 'short',
    weekday: 'short',
    year: undefined,
  }).format(date);
};

const sameDay = (a, b) => {
  if (!a || !b) return false;
  const da = new Date(a);
  const db = new Date(b);
  return (
    da.getFullYear() === db.getFullYear() &&
    da.getMonth() === db.getMonth() &&
    da.getDate() === db.getDate()
  );
};

const buildItems = (messages) => {
  const items = [];
  let previousTs = null;
  let previousSender = null;
  let previousKind = null;

  for (const message of messages) {
    if (!sameDay(previousTs, message.ts)) {
      items.push({ kind: 'day', ts: message.ts });
      previousSender = null;
    }
    const compactWithPrev =
      message.kind === 'text' &&
      previousKind === 'text' &&
      message.sender === previousSender &&
      previousTs &&
      message.ts - previousTs < SAME_SENDER_WINDOW_MS;
    items.push({
      kind: 'message',
      message,
      showSender: !compactWithPrev,
    });
    previousTs = message.ts;
    previousSender = message.sender;
    previousKind = message.kind;
  }

  return items;
};

const ListenAlongCard = ({ message }) => {
  const meta = message.meta || {};
  const action = (meta.action || '').toLowerCase();
  const glyph = action === 'stop' ? '■' : '▶';
  const title = meta.title || 'unknown track';
  const artist = meta.artist;
  const album = meta.album;

  return (
    <div className="msg-stream-listenalong">
      <span className="msg-stream-listenalong-glyph">{glyph}</span>
      <span className="msg-stream-listenalong-host">{message.sender}</span>
      <span className="msg-stream-listenalong-action">
        {action === 'stop' ? 'stopped listenalong' : 'is playing'}
      </span>
      <span className="msg-stream-listenalong-title">{title}</span>
      {artist && (
        <span className="msg-stream-listenalong-artist">— {artist}</span>
      )}
      {album && (
        <span className="msg-stream-listenalong-album">· {album}</span>
      )}
    </div>
  );
};

const MessageRow = ({ item, onSenderClick }) => {
  const { message, showSender } = item;
  const color = useMemo(() => nickColor(message.sender), [message.sender]);

  if (message.kind === 'listenalong') {
    return (
      <div
        className="msg-stream-row msg-stream-row-listenalong"
        data-self={message.isSelf ? 'true' : undefined}
      >
        <span className="msg-stream-time">{formatTime(message.ts)}</span>
        <ListenAlongCard message={message} />
      </div>
    );
  }

  if (message.kind === 'me') {
    return (
      <div
        className="msg-stream-row msg-stream-row-me"
        data-self={message.isSelf ? 'true' : undefined}
      >
        <span className="msg-stream-time">{formatTime(message.ts)}</span>
        <span className="msg-stream-me-glyph">*</span>
        <button
          className="msg-stream-me-sender"
          onClick={() => onSenderClick?.(message.sender)}
          style={{ color }}
          type="button"
        >
          {message.sender}
        </button>
        <span className="msg-stream-me-body">{message.body}</span>
      </div>
    );
  }

  return (
    <div
      className="msg-stream-row msg-stream-row-text"
      data-compact={!showSender ? 'true' : undefined}
      data-self={message.isSelf ? 'true' : undefined}
    >
      <span className="msg-stream-time">
        {showSender ? formatTime(message.ts) : ''}
      </span>
      {showSender ? (
        <button
          className="msg-stream-sender"
          onClick={() => onSenderClick?.(message.sender)}
          style={{ color }}
          title={message.sender}
          type="button"
        >
          {message.sender}
        </button>
      ) : (
        <span className="msg-stream-sender-spacer" aria-hidden="true" />
      )}
      <span className="msg-stream-body">{message.body}</span>
    </div>
  );
};

const DaySeparator = ({ ts }) => (
  <div className="msg-stream-day">
    <span className="msg-stream-day-line" aria-hidden="true" />
    <span className="msg-stream-day-label">{formatDayHeader(ts)}</span>
    <span className="msg-stream-day-line" aria-hidden="true" />
  </div>
);

const MessageStream = ({ adapter, emptyHint, onSenderClick }) => {
  const [messages, setMessages] = useState([]);
  const [error, setError] = useState(null);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const scrollRef = useRef(null);
  const stuckToBottomRef = useRef(true);

  const refresh = useCallback(async () => {
    if (!adapter) return;
    try {
      const result = await adapter.list();
      const next = Array.isArray(result?.messages) ? result.messages : [];
      setMessages(next);
      setError(null);
    } catch (caught) {
      console.error('MessageStream refresh failed:', caught);
      setError(caught);
    } finally {
      setIsInitialLoad(false);
    }
  }, [adapter]);

  useEffect(() => {
    setMessages([]);
    setIsInitialLoad(true);
    stuckToBottomRef.current = true;
    if (!adapter) return undefined;
    refresh();
    const interval = window.setInterval(() => {
      refresh();
    }, adapter.pollIntervalMs || 2_000);
    return () => window.clearInterval(interval);
  }, [adapter, refresh]);

  useLayoutEffect(() => {
    const node = scrollRef.current;
    if (!node) return;
    if (stuckToBottomRef.current) {
      node.scrollTop = node.scrollHeight;
    }
  }, [messages]);

  const handleScroll = useCallback((event) => {
    const node = event.currentTarget;
    const distanceFromBottom = node.scrollHeight - node.clientHeight - node.scrollTop;
    stuckToBottomRef.current = distanceFromBottom < 32;
  }, []);

  const items = useMemo(() => buildItems(messages), [messages]);

  return (
    <div
      className="msg-stream"
      onScroll={handleScroll}
      ref={scrollRef}
    >
      {isInitialLoad ? (
        <div className="msg-stream-loading">Loading…</div>
      ) : items.length === 0 ? (
        <div className="msg-stream-empty">
          {error ? 'Could not load messages.' : emptyHint || 'No messages yet.'}
        </div>
      ) : (
        items.map((item, index) =>
          item.kind === 'day' ? (
            <DaySeparator key={`day-${item.ts}-${index}`} ts={item.ts} />
          ) : (
            <MessageRow
              item={item}
              key={item.message.id || index}
              onSenderClick={onSenderClick}
            />
          ),
        )
      )}
    </div>
  );
};

export default MessageStream;
export { nickColor };
