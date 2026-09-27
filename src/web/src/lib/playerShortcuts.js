const interactiveSelector =
  'a, button, input, select, textarea, [contenteditable="true"], [role="button"], [role="slider"]';

export const isEditableShortcutTarget = (target) => {
  if (!target) return false;
  return Boolean(target.isContentEditable || target.closest?.(interactiveSelector));
};

export const getPlayerShortcutAction = (event = {}) => {
  if (
    event.defaultPrevented ||
    event.altKey ||
    event.ctrlKey ||
    event.metaKey ||
    isEditableShortcutTarget(event.target)
  ) {
    return null;
  }

  switch (event.key) {
    case ' ':
    case 'Spacebar':
    case 'k':
    case 'K':
      return 'togglePlayback';
    case 'ArrowLeft':
      return event.shiftKey ? 'previous' : 'seekBackward';
    case 'ArrowRight':
      return event.shiftKey ? 'next' : 'seekForward';
    case 'm':
    case 'M':
      return 'toggleMute';
    case 'e':
    case 'E':
      return 'toggleEqualizer';
    case 'l':
    case 'L':
      return 'toggleLyrics';
    case 'v':
    case 'V':
      return 'toggleVisualizer';
    default:
      return null;
  }
};
