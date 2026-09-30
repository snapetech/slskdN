import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import usePrefersReducedMotion from './usePrefersReducedMotion';

const originalMatchMedia = Object.getOwnPropertyDescriptor(window, 'matchMedia');

afterEach(() => {
  if (originalMatchMedia) {
    Object.defineProperty(window, 'matchMedia', originalMatchMedia);
  } else {
    delete window.matchMedia;
  }
});

describe('usePrefersReducedMotion', () => {
  it('reads and subscribes to the system motion preference', () => {
    let matches = false;
    const listeners = new Set();
    const mediaQuery = {
      addEventListener: vi.fn((_type, listener) => listeners.add(listener)),
      matches,
      media: '(prefers-reduced-motion: reduce)',
      removeEventListener: vi.fn((_type, listener) => listeners.delete(listener)),
    };
    window.matchMedia = vi.fn(() => ({ ...mediaQuery, get matches() { return matches; } }));

    const Preference = () => {
      const prefersReducedMotion = usePrefersReducedMotion();
      return <div data-testid="motion-preference">{prefersReducedMotion ? 'reduce' : 'full'}</div>;
    };

    render(<Preference />);

    expect(window.matchMedia).toHaveBeenCalledWith('(prefers-reduced-motion: reduce)');
    expect(screen.getByTestId('motion-preference')).toHaveTextContent('full');

    act(() => {
      matches = true;
      listeners.forEach((listener) => listener({ matches, media: mediaQuery.media }));
    });

    expect(screen.getByTestId('motion-preference')).toHaveTextContent('reduce');
  });

  it('defaults to standard motion when matchMedia is unavailable', () => {
    delete window.matchMedia;

    const Preference = () => {
      const prefersReducedMotion = usePrefersReducedMotion();
      return <div data-testid="motion-preference">{prefersReducedMotion ? 'reduce' : 'full'}</div>;
    };

    render(<Preference />);

    expect(screen.getByTestId('motion-preference')).toHaveTextContent('full');
  });
});
