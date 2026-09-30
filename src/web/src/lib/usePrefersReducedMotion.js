import { useSyncExternalStore } from 'react';

const reducedMotionQuery = '(prefers-reduced-motion: reduce)';

const getMediaQuery = () => {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return null;
  return window.matchMedia(reducedMotionQuery);
};

const getSnapshot = () => Boolean(getMediaQuery()?.matches);

const subscribe = (onChange) => {
  const mediaQuery = getMediaQuery();
  if (!mediaQuery) return () => {};

  if (typeof mediaQuery.addEventListener === 'function') {
    mediaQuery.addEventListener('change', onChange);
    return () => mediaQuery.removeEventListener('change', onChange);
  }

  if (typeof mediaQuery.addListener === 'function') {
    mediaQuery.addListener(onChange);
    return () => mediaQuery.removeListener(onChange);
  }

  return () => {};
};

const usePrefersReducedMotion = () =>
  useSyncExternalStore(subscribe, getSnapshot, () => false);

export default usePrefersReducedMotion;
