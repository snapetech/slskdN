import React from 'react';
import { cleanup, render, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import SpectrumAnalyzer from './SpectrumAnalyzer';
import { getFrequencyBars, getScopePoints } from './SpectrumAnalyzer';

const audioGraph = {
  analyser: { fftSize: 1024, frequencyBinCount: 512 },
  ctx: { state: 'running' },
  visualizerInput: {},
};

vi.mock('./audioGraph', () => ({
  getOrCreateAudioGraph: vi.fn(() => audioGraph),
  resumeAudioGraph: vi.fn(() => Promise.resolve(audioGraph)),
  suspendAudioGraph: vi.fn(() => Promise.resolve(audioGraph)),
}));

import { getOrCreateAudioGraph, resumeAudioGraph } from './audioGraph';

const originalMatchMediaDescriptor = Object.getOwnPropertyDescriptor(window, 'matchMedia');

const stubReducedMotion = (matches) => {
  const listeners = new Set();
  const mediaQuery = {
    addEventListener: (_type, listener) => listeners.add(listener),
    matches,
    media: '(prefers-reduced-motion: reduce)',
    removeEventListener: (_type, listener) => listeners.delete(listener),
  };
  window.matchMedia = vi.fn(() => mediaQuery);
};

const createAudioElement = () => {
  const audioElement = document.createElement('audio');
  Object.defineProperty(audioElement, 'paused', { configurable: true, value: false });
  Object.defineProperty(audioElement, 'readyState', {
    configurable: true,
    value: HTMLMediaElement.HAVE_ENOUGH_DATA,
  });
  return audioElement;
};

beforeEach(() => {
  window.requestAnimationFrame = vi.fn(() => 1);
  window.cancelAnimationFrame = vi.fn();
  getOrCreateAudioGraph.mockClear();
  resumeAudioGraph.mockClear();
});

afterEach(() => {
  cleanup();
  if (originalMatchMediaDescriptor) {
    Object.defineProperty(window, 'matchMedia', originalMatchMediaDescriptor);
  } else {
    delete window.matchMedia;
  }
});

describe('SpectrumAnalyzer helpers', () => {
  it('aggregates frequency bins into logarithmic bars', () => {
    const data = new Uint8Array(1024);
    data[2] = 120;
    data[3] = 120;
    data[900] = 240;

    const bars = getFrequencyBars(data, 16);

    expect(bars).toHaveLength(16);
    expect(bars.some((value) => value > 0)).toBe(true);
    expect(bars[0]).toBeGreaterThan(0);
    expect(bars[15]).toBeGreaterThan(0);
  });

  it('centers and expands quiet scope samples without exceeding the canvas', () => {
    const data = new Uint8Array([126, 128, 130, 129]);
    const points = getScopePoints(data, 120, 80);

    expect(points).toHaveLength(4);
    expect(points[1].y).toBe(40);
    expect(Math.min(...points.map((point) => point.y))).toBeGreaterThanOrEqual(0);
    expect(Math.max(...points.map((point) => point.y))).toBeLessThanOrEqual(80);
    expect(Math.abs(points[0].y - 40)).toBeGreaterThan(0.5);
  });
});

describe('SpectrumAnalyzer motion preference', () => {
  it('does not create a graph or schedule frames when reduced motion is enabled', () => {
    stubReducedMotion(true);
    render(<SpectrumAnalyzer audioElement={createAudioElement()} mode="spectrum" />);

    expect(document.querySelector('[data-testid="player-spectrum"]'))
      .toHaveAttribute('data-motion-suppressed', 'true');
    expect(getOrCreateAudioGraph).not.toHaveBeenCalled();
    expect(resumeAudioGraph).not.toHaveBeenCalled();
    expect(window.requestAnimationFrame).not.toHaveBeenCalled();
  });

  it('allows an explicit session override under reduced motion', async () => {
    stubReducedMotion(true);
    render(
      <SpectrumAnalyzer
        allowReducedMotionAnimation
        audioElement={createAudioElement()}
        mode="spectrum"
      />,
    );

    await waitFor(() => expect(window.requestAnimationFrame).toHaveBeenCalledTimes(1));
    expect(document.querySelector('[data-testid="player-spectrum"]'))
      .not.toHaveAttribute('data-motion-suppressed');
    expect(resumeAudioGraph).toHaveBeenCalledTimes(1);
  });
});
