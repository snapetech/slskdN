// <copyright file="usePlayerDialog.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>

import { useCallback, useId, useLayoutEffect, useRef } from 'react';

const focusableSelector = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled]):not([type="hidden"])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[contenteditable="true"]',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

const getFocusableElements = (dialog) => {
  if (!dialog) return [];

  return Array.from(dialog.querySelectorAll(focusableSelector)).filter((element) =>
    element.tabIndex >= 0 &&
    !element.closest('[aria-hidden="true"]') &&
    element.getClientRects().length > 0 &&
    window.getComputedStyle(element).visibility !== 'hidden');
};

const usePlayerDialog = (open) => {
  const titleId = useId();
  const returnFocusRef = useRef(null);

  useLayoutEffect(() => {
    if (!open) return undefined;

    const activeElement = document.activeElement;
    returnFocusRef.current = activeElement instanceof HTMLElement ? activeElement : null;

    const dialog = document.getElementById(titleId)?.closest('[role="dialog"]');
    const focusTarget = getFocusableElements(dialog)[0] || dialog;
    focusTarget?.focus({ preventScroll: true });

    return () => {
      const returnFocus = returnFocusRef.current;
      returnFocusRef.current = null;
      if (returnFocus?.isConnected) returnFocus.focus({ preventScroll: true });
    };
  }, [open, titleId]);

  const handleKeyDown = useCallback((event) => {
    if (event.key !== 'Tab') return;

    const dialog = event.currentTarget;
    const focusableElements = getFocusableElements(dialog);
    if (focusableElements.length === 0) {
      event.preventDefault();
      dialog.focus();
      return;
    }

    const first = focusableElements[0];
    const last = focusableElements[focusableElements.length - 1];
    const current = document.activeElement;
    if (event.shiftKey && (current === first || current === dialog || !dialog.contains(current))) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && (current === last || current === dialog || !dialog.contains(current))) {
      event.preventDefault();
      first.focus();
    }
  }, []);

  return {
    dialogProps: {
      'aria-labelledby': titleId,
      'aria-modal': 'true',
      onKeyDown: handleKeyDown,
      role: 'dialog',
      tabIndex: -1,
    },
    titleProps: { id: titleId },
  };
};

export default usePlayerDialog;
