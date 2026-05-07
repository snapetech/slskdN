import React, { useEffect } from 'react';

const CommandHelp = ({ commands, onClose, open }) => {
  useEffect(() => {
    if (!open) return;
    const handleKeyDown = (event) => {
      if (event.key === 'Escape') {
        event.preventDefault();
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onClose, open]);

  if (!open) return null;

  return (
    <div
      aria-modal="true"
      className="msgv2-help-overlay"
      onClick={onClose}
      role="dialog"
    >
      <div
        className="msgv2-help-modal"
        onClick={(event) => event.stopPropagation()}
      >
        <header className="msgv2-help-header">
          <span className="msgv2-help-title">Commands</span>
          <button
            aria-label="Close"
            className="msgv2-help-close"
            onClick={onClose}
            type="button"
          >
            ×
          </button>
        </header>
        <ul className="msgv2-help-list">
          {commands.map((command) => (
            <li
              className="msgv2-help-item"
              key={command.name}
            >
              <code className="msgv2-help-syntax">
                {command.syntax || `/${command.name}`}
              </code>
              <span className="msgv2-help-desc">{command.description}</span>
              {command.aliases && command.aliases.length > 0 && (
                <span className="msgv2-help-aliases">
                  aliases: {command.aliases.map((alias) => `/${alias}`).join(', ')}
                </span>
              )}
            </li>
          ))}
        </ul>
        <footer className="msgv2-help-footer">
          <span>
            <kbd>Tab</kbd> autocompletes <kbd>↑</kbd>
            <kbd>↓</kbd> navigates suggestions
          </span>
          <span>
            <kbd>Esc</kbd> closes
          </span>
        </footer>
      </div>
    </div>
  );
};

export default CommandHelp;
