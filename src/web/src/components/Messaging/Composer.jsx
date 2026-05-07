import React, { useCallback, useEffect, useRef, useState } from 'react';

const ME_PREFIX = '/me ';

const Composer = ({ adapter, disabled, label, onCommand, placeholder }) => {
  const [draft, setDraft] = useState('');
  const [busy, setBusy] = useState(false);
  const inputRef = useRef(null);

  useEffect(() => {
    setDraft('');
  }, [adapter]);

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

  const handleKeyDown = useCallback(
    (event) => {
      if (event.key === 'Enter' && !event.shiftKey) {
        event.preventDefault();
        submit();
      }
    },
    [submit],
  );

  const isDisabled = disabled || !adapter;

  return (
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
  );
};

export default Composer;
