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

const buildItems = (messages, newSinceIndex) => {
  const items = [];
  let previousTs = null;
  let previousSender = null;
  let previousKind = null;

  for (let index = 0; index < messages.length; index += 1) {
    if (newSinceIndex !== null && index === newSinceIndex && index > 0) {
      items.push({ kind: 'newmarker' });
      previousSender = null;
    }
    const message = messages[index];
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
      messageIndex: index,
      showSender: !compactWithPrev,
    });
    previousTs = message.ts;
    previousSender = message.sender;
    previousKind = message.kind;
  }

  return items;
};

const URL_REGEX = /\b(?:https?|ftp):\/\/[^\s<>"'()]+[^\s<>"'(),.!?:;]/g;

const autolink = (text) => {
  if (typeof text !== 'string' || text.length === 0) return [text];
  const parts = [];
  let lastIndex = 0;
  URL_REGEX.lastIndex = 0;
  let match = URL_REGEX.exec(text);
  while (match !== null) {
    if (match.index > lastIndex) {
      parts.push(text.slice(lastIndex, match.index));
    }
    parts.push({ url: match[0] });
    lastIndex = match.index + match[0].length;
    match = URL_REGEX.exec(text);
  }
  if (lastIndex < text.length) {
    parts.push(text.slice(lastIndex));
  }
  return parts;
};

const highlightText = (text, query) => {
  if (!query || typeof text !== 'string') return [text];
  const lower = text.toLowerCase();
  const needle = query.toLowerCase();
  const parts = [];
  let start = 0;
  let index = lower.indexOf(needle);

  while (index !== -1) {
    if (index > start) parts.push(text.slice(start, index));
    parts.push({ highlight: text.slice(index, index + needle.length) });
    start = index + needle.length;
    index = lower.indexOf(needle, start);
  }

  if (start < text.length) parts.push(text.slice(start));
  return parts;
};

const renderBody = (text, searchQuery) =>
  autolink(text).map((part, index) =>
    typeof part === 'string' ? (
      <React.Fragment key={index}>
        {highlightText(part, searchQuery).map((piece, pieceIndex) =>
          typeof piece === 'string' ? (
            <React.Fragment key={pieceIndex}>{piece}</React.Fragment>
          ) : (
            <mark
              className="msg-stream-search-hit"
              key={pieceIndex}
            >
              {piece.highlight}
            </mark>
          ),
        )}
      </React.Fragment>
    ) : (
      <a
        className="msg-stream-link"
        href={part.url}
        key={index}
        rel="noopener noreferrer"
        target="_blank"
      >
        {part.url}
      </a>
    ),
  );

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

const MessageActions = ({ message, onCopy, onQuote }) => {
  if (!onCopy && !onQuote) return null;
  return (
    <div
      aria-label="Message actions"
      className="msg-stream-actions"
      role="toolbar"
    >
      {onQuote && (
        <button
          aria-label="Quote message"
          className="msg-stream-action"
          onClick={() => onQuote(message)}
          title="Quote in composer"
          type="button"
        >
          ❝
        </button>
      )}
      {onCopy && (
        <button
          aria-label="Copy message"
          className="msg-stream-action"
          onClick={() => onCopy(message)}
          title="Copy text"
          type="button"
        >
          ⎘
        </button>
      )}
    </div>
  );
};

const messageMatches = (message, query) => {
  if (!query) return false;
  const needle = query.toLowerCase();
  return [message.sender, message.body, message.meta?.title, message.meta?.artist]
    .filter(Boolean)
    .some((value) => `${value}`.toLowerCase().includes(needle));
};

const MessageRow = ({
  activeMatch,
  item,
  onCopy,
  onQuote,
  onSenderClick,
  searchQuery,
}) => {
  const { message, showSender } = item;
  const color = useMemo(() => nickColor(message.sender), [message.sender]);
  const rowRef = useRef(null);

  useEffect(() => {
    if (activeMatch) {
      rowRef.current?.scrollIntoView?.({ block: 'center' });
    }
  }, [activeMatch]);

  if (message.kind === 'listenalong') {
    return (
      <div
        className="msg-stream-row msg-stream-row-listenalong"
        data-search-match={messageMatches(message, searchQuery) ? 'true' : undefined}
        data-search-active={activeMatch ? 'true' : undefined}
        data-self={message.isSelf ? 'true' : undefined}
        ref={rowRef}
      >
        <span className="msg-stream-time">{formatTime(message.ts)}</span>
        <ListenAlongCard message={message} />
        <MessageActions message={message} onCopy={onCopy} onQuote={onQuote} />
      </div>
    );
  }

  if (message.kind === 'me') {
    return (
      <div
        className="msg-stream-row msg-stream-row-me"
        data-search-match={messageMatches(message, searchQuery) ? 'true' : undefined}
        data-search-active={activeMatch ? 'true' : undefined}
        data-self={message.isSelf ? 'true' : undefined}
        ref={rowRef}
      >
        <span className="msg-stream-time">{formatTime(message.ts)}</span>
        <span className="msg-stream-me-glyph">*</span>
        <button
          className="msg-stream-me-sender"
          onClick={(event) => onSenderClick?.(message.sender, event)}
          style={{ color }}
          type="button"
        >
          {message.sender}
        </button>
        <span className="msg-stream-me-body">{renderBody(message.body, searchQuery)}</span>
        <MessageActions message={message} onCopy={onCopy} onQuote={onQuote} />
      </div>
    );
  }

  return (
    <div
      className="msg-stream-row msg-stream-row-text"
      data-compact={!showSender ? 'true' : undefined}
      data-search-match={messageMatches(message, searchQuery) ? 'true' : undefined}
      data-search-active={activeMatch ? 'true' : undefined}
      data-self={message.isSelf ? 'true' : undefined}
      ref={rowRef}
    >
      <span className="msg-stream-time">
        {showSender ? formatTime(message.ts) : ''}
      </span>
      {showSender ? (
        <button
          className="msg-stream-sender"
          onClick={(event) => onSenderClick?.(message.sender, event)}
          style={{ color }}
          title={message.sender}
          type="button"
        >
          {message.sender}
        </button>
      ) : (
        <span className="msg-stream-sender-spacer" aria-hidden="true" />
      )}
      <span className="msg-stream-body">{renderBody(message.body, searchQuery)}</span>
      <MessageActions message={message} onCopy={onCopy} onQuote={onQuote} />
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

const MessageStream = ({ adapter, emptyHint, onCopy, onQuote, onSenderClick }) => {
  const [messages, setMessages] = useState([]);
  const [error, setError] = useState(null);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const [lastSeenCount, setLastSeenCount] = useState(0);
  const [searchCursor, setSearchCursor] = useState(0);
  const [searchDraft, setSearchDraft] = useState('');
  const [stuck, setStuck] = useState(true);
  const scrollRef = useRef(null);
  const stuckRef = useRef(true);

  const setStuckBoth = useCallback((value) => {
    stuckRef.current = value;
    setStuck(value);
  }, []);

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
    setSearchCursor(0);
    setSearchDraft('');
    setStuckBoth(true);
    setLastSeenCount(0);
    if (!adapter) return undefined;
    refresh();
    const interval = window.setInterval(() => {
      refresh();
    }, adapter.pollIntervalMs || 2_000);
    return () => window.clearInterval(interval);
  }, [adapter, refresh, setStuckBoth]);

  useLayoutEffect(() => {
    const node = scrollRef.current;
    if (!node) return;
    if (stuckRef.current) {
      node.scrollTop = node.scrollHeight;
      setLastSeenCount(messages.length);
    }
  }, [messages]);

  const handleScroll = useCallback(
    (event) => {
      const node = event.currentTarget;
      const distanceFromBottom = node.scrollHeight - node.clientHeight - node.scrollTop;
      const atBottom = distanceFromBottom < 32;
      if (atBottom !== stuckRef.current) setStuckBoth(atBottom);
      if (atBottom) setLastSeenCount(messages.length);
    },
    [messages.length, setStuckBoth],
  );

  const jumpToLatest = useCallback(() => {
    const node = scrollRef.current;
    if (!node) return;
    node.scrollTop = node.scrollHeight;
    setStuckBoth(true);
    setLastSeenCount(messages.length);
  }, [messages.length, setStuckBoth]);

  const newSinceIndex =
    !stuck && lastSeenCount > 0 && lastSeenCount < messages.length
      ? lastSeenCount
      : null;

  const items = useMemo(
    () => buildItems(messages, newSinceIndex),
    [messages, newSinceIndex],
  );

  const newCount = newSinceIndex !== null ? messages.length - newSinceIndex : 0;
  const searchQuery = searchDraft.trim();
  const matchingIndexes = useMemo(
    () =>
      searchQuery
        ? messages
            .map((message, index) => (messageMatches(message, searchQuery) ? index : null))
            .filter((index) => index !== null)
        : [],
    [messages, searchQuery],
  );
  const activeMatchIndex = matchingIndexes[searchCursor] ?? null;

  useEffect(() => {
    if (searchCursor >= matchingIndexes.length) {
      setSearchCursor(0);
    }
  }, [matchingIndexes.length, searchCursor]);

  const moveSearch = useCallback(
    (delta) => {
      if (matchingIndexes.length === 0) return;
      setSearchCursor((current) =>
        (current + delta + matchingIndexes.length) % matchingIndexes.length,
      );
    },
    [matchingIndexes.length],
  );

  return (
    <div className="msg-stream-wrap">
      <div className="msg-stream-search">
        <input
          aria-label="Search active conversation"
          className="msg-stream-search-input"
          onChange={(event) => {
            setSearchDraft(event.target.value);
            setSearchCursor(0);
          }}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              event.preventDefault();
              moveSearch(event.shiftKey ? -1 : 1);
            } else if (event.key === 'Escape') {
              setSearchDraft('');
              setSearchCursor(0);
            }
          }}
          placeholder="Search in conversation"
          type="search"
          value={searchDraft}
        />
        <span className="msg-stream-search-count">
          {searchQuery
            ? `${matchingIndexes.length ? searchCursor + 1 : 0}/${matchingIndexes.length}`
            : ''}
        </span>
        <button
          aria-label="Previous search match"
          className="msg-stream-search-button"
          disabled={matchingIndexes.length === 0}
          onClick={() => moveSearch(-1)}
          title="Previous search match"
          type="button"
        >
          ↑
        </button>
        <button
          aria-label="Next search match"
          className="msg-stream-search-button"
          disabled={matchingIndexes.length === 0}
          onClick={() => moveSearch(1)}
          title="Next search match"
          type="button"
        >
          ↓
        </button>
      </div>
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
          items.map((item, index) => {
            if (item.kind === 'day') {
              return <DaySeparator key={`day-${item.ts}-${index}`} ts={item.ts} />;
            }
            if (item.kind === 'newmarker') {
              return <NewMessagesMarker key={`new-${index}`} />;
            }
            return (
              <MessageRow
                activeMatch={item.messageIndex === activeMatchIndex}
                item={item}
                key={item.message.id || index}
                onCopy={onCopy}
                onQuote={onQuote}
                onSenderClick={onSenderClick}
                searchQuery={searchQuery}
              />
            );
          })
        )}
      </div>
      {!stuck && (
        <button
          aria-label="Jump to latest"
          className="msg-stream-jump"
          onClick={jumpToLatest}
          type="button"
        >
          {newCount > 0
            ? `↓ ${newCount} new ${newCount === 1 ? 'message' : 'messages'}`
            : '↓ Jump to latest'}
        </button>
      )}
    </div>
  );
};

const NewMessagesMarker = () => (
  <div
    aria-label="New messages below"
    className="msg-stream-newmarker"
    role="separator"
  >
    <span className="msg-stream-newmarker-line" aria-hidden="true" />
    <span className="msg-stream-newmarker-label">new</span>
    <span className="msg-stream-newmarker-line" aria-hidden="true" />
  </div>
);

export default MessageStream;
export { autolink, nickColor };
