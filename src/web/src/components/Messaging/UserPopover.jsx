import React, { useEffect, useLayoutEffect, useRef, useState } from 'react';

const POPOVER_OFFSET = 6;
const POPOVER_MARGIN = 8;

const UserPopover = ({ anchor, onBrowse, onClose, onMessage, onProfile, open, username }) => {
  const ref = useRef(null);
  const [position, setPosition] = useState({ left: 0, top: 0, ready: false });

  useLayoutEffect(() => {
    if (!open || !anchor || !ref.current) return;
    const node = ref.current;
    const rect = node.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    let left = anchor.x + POPOVER_OFFSET;
    let top = anchor.y + POPOVER_OFFSET;

    if (left + rect.width > viewportWidth - POPOVER_MARGIN) {
      left = Math.max(POPOVER_MARGIN, anchor.x - rect.width - POPOVER_OFFSET);
    }
    if (top + rect.height > viewportHeight - POPOVER_MARGIN) {
      top = Math.max(POPOVER_MARGIN, viewportHeight - rect.height - POPOVER_MARGIN);
    }

    setPosition({ left, ready: true, top });
  }, [anchor, open]);

  useEffect(() => {
    if (!open) return undefined;
    const handleKey = (event) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        onClose();
      }
    };
    const handleClick = (event) => {
      if (ref.current && !ref.current.contains(event.target)) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKey);
    window.addEventListener('mousedown', handleClick);
    return () => {
      window.removeEventListener('keydown', handleKey);
      window.removeEventListener('mousedown', handleClick);
    };
  }, [onClose, open]);

  if (!open || !username) return null;

  return (
    <div
      aria-label={`Actions for ${username}`}
      className="msgv2-userpop"
      ref={ref}
      role="menu"
      style={{
        left: position.left,
        top: position.top,
        visibility: position.ready ? 'visible' : 'hidden',
      }}
    >
      <header className="msgv2-userpop-header">
        <span className="msgv2-userpop-name">{username}</span>
      </header>
      <button
        className="msgv2-userpop-action"
        onClick={() => onProfile(username)}
        role="menuitem"
        type="button"
      >
        <span className="msgv2-userpop-glyph">i</span>
        Open profile
      </button>
      <button
        className="msgv2-userpop-action"
        onClick={() => onBrowse(username)}
        role="menuitem"
        type="button"
      >
        <span className="msgv2-userpop-glyph">⤓</span>
        Browse shares
      </button>
      <button
        className="msgv2-userpop-action"
        onClick={() => onMessage(username)}
        role="menuitem"
        type="button"
      >
        <span className="msgv2-userpop-glyph">@</span>
        Send DM
      </button>
    </div>
  );
};

export default UserPopover;
