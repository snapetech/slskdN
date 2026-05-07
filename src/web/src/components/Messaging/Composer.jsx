import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';

const ME_PREFIX = '/me ';

const matchSuggestions = (draft, commands) => {
  if (!draft.startsWith('/') || draft.startsWith('/ ')) return [];
  const space = draft.indexOf(' ');
  if (space !== -1) return [];
  const prefix = draft.slice(1).toLowerCase();
  if (prefix.length === 0) return commands;
  return commands.filter((command) => {
    if (command.name.toLowerCase().startsWith(prefix)) return true;
    return (command.aliases || []).some((alias) =>
      alias.toLowerCase().startsWith(prefix),
    );
  });
};

const Composer = ({ adapter, commands = [], disabled, label, onCommand, placeholder }) => {
  const [draft, setDraft] = useState('');
  const [busy, setBusy] = useState(false);
  const [cursor, setCursor] = useState(0);
  const inputRef = useRef(null);

  useEffect(() => {
    setDraft('');
    setCursor(0);
  }, [adapter]);

  const suggestions = useMemo(
    () => matchSuggestions(draft, commands),
    [commands, draft],
  );

  useEffect(() => {
    if (cursor >= suggestions.length) setCursor(0);
  }, [cursor, suggestions.length]);

  const submit = useCallback(async () => {
    const trimmed = draft.trim();
    if (!trimmed || busy || !adapter) return;

    if (trimmed.startsWith('/') && !trimmed.startsWith(ME_PREFIX)) {
      const [head, ...rest] = trimmed.slice(1).split(/\s+/);
      const handled = onCommand?.({
        argv: rest,
        name: head.toLowerCase(),
        raw: trimmed,
      });
      if (handled) {
        setDraft('');
        return;
      }
    }

    setBusy(true);
    try {
      await adapter.send(trimmed);
      setDraft('');
    } catch (error) {
      console.error('Composer send failed:', error);
    } finally {
      setBusy(false);
    }
  }, [adapter, busy, draft, onCommand]);

  const completeSuggestion = useCallback(() => {
    const choice = suggestions[cursor];
    if (!choice) return false;
    setDraft(`/${choice.name} `);
    setCursor(0);
    return true;
  }, [cursor, suggestions]);

  const handleKeyDown = useCallback(
    (event) => {
      if (suggestions.length > 0) {
        if (event.key === 'ArrowDown') {
          event.preventDefault();
          setCursor((value) => Math.min(suggestions.length - 1, value + 1));
          return;
        }
        if (event.key === 'ArrowUp') {
          event.preventDefault();
          setCursor((value) => Math.max(0, value - 1));
          return;
        }
        if (event.key === 'Tab') {
          event.preventDefault();
          completeSuggestion();
          return;
        }
      }
      if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        submit();
      }
    },
    [completeSuggestion, submit, suggestions.length],
  );

  const isDisabled = disabled || !adapter;

  return (
    <div className="msgv2-composer-wrap">
      {suggestions.length > 0 && (
        <div
          aria-label="Command suggestions"
          className="msgv2-composer-suggestions"
          role="listbox"
        >
          {suggestions.map((command, index) => (
            <button
              aria-selected={index === cursor}
              className={`msgv2-composer-suggestion ${index === cursor ? 'is-active' : ''}`}
              key={command.name}
              onClick={() => {
                setCursor(index);
                setDraft(`/${command.name} `);
                inputRef.current?.focus();
              }}
              onMouseEnter={() => setCursor(index)}
              role="option"
              type="button"
            >
              <span className="msgv2-composer-suggestion-syntax">
                {command.syntax || `/${command.name}`}
              </span>
              <span className="msgv2-composer-suggestion-desc">
                {command.description}
              </span>
            </button>
          ))}
        </div>
      )}
      <div className="msgv2-composer">
        <textarea
          aria-label={label || 'Message composer'}
          className="msgv2-composer-input"
          disabled={isDisabled}
          onChange={(event) => setDraft(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={
            isDisabled
              ? 'Open a tab to start typing'
              : placeholder || 'Type a message — Enter sends, Shift+Enter newline'
          }
          ref={inputRef}
          rows={1}
          value={draft}
        />
        <button
          aria-label="Send"
          className="msgv2-composer-send"
          disabled={isDisabled || draft.trim().length === 0 || busy}
          onClick={submit}
          title="Send (Enter)"
          type="button"
        >
          {busy ? '…' : '▶'}
        </button>
      </div>
    </div>
  );
};

export default Composer;
export { matchSuggestions };
