// <copyright file="audioGraph.test.js" company="slskdN Team">
// Copyright (c) slskdN Team. All rights reserved.
// </copyright>
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { getOrCreateAudioGraph, releaseAudioGraph, resumeAudioGraph, suspendAudioGraph } from './audioGraph';

const deferred = () => {
  let resolve;
  const promise = new Promise((done) => { resolve = done; });
  return { promise, resolve };
};

const makeNode = () => ({
  connect: vi.fn(),
  disconnect: vi.fn(),
  frequency: { value: 0 },
  gain: { cancelScheduledValues: vi.fn(), linearRampToValueAtTime: vi.fn(), setValueAtTime: vi.fn(), value: 0 },
  Q: { value: 0 },
});

const createHarness = ({ initialState = 'suspended', onResume, onSuspend } = {}) => {
  const contexts = [];
  class TestAudioContext {
    constructor() {
      this.state = initialState;
      this.destination = makeNode();
      this.resume = vi.fn(() => (onResume
        ? onResume(this)
        : Promise.resolve().then(() => { this.state = 'running'; })));
      this.suspend = vi.fn(() => (onSuspend
        ? onSuspend(this)
        : Promise.resolve().then(() => { this.state = 'suspended'; })));
      contexts.push(this);
    }

    close() {
      this.state = 'closed';
      return Promise.resolve();
    }

    createAnalyser() {
      return makeNode();
    }

    createBiquadFilter() {
      return makeNode();
    }

    createGain() {
      return makeNode();
    }

    createMediaElementSource() {
      return makeNode();
    }

  }

  vi.stubGlobal('AudioContext', TestAudioContext);
  return { audio: document.createElement('audio'), contexts };
};

describe('audio graph playback intent ordering', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('suspends after a pending resume when Pause is the latest intent', async () => {
    const resumeGate = deferred();
    const { audio } = createHarness({
      onResume: (context) => resumeGate.promise.then(() => { context.state = 'running'; }),
    });
    const graph = getOrCreateAudioGraph(audio);

    const start = resumeAudioGraph(audio, false);
    await vi.waitFor(() => expect(graph.ctx.resume).toHaveBeenCalledOnce());
    const pause = suspendAudioGraph(audio);

    expect(graph.ctx.suspend).not.toHaveBeenCalled();
    resumeGate.resolve();
    await Promise.all([start, pause]);

    expect(graph.ctx.resume).toHaveBeenCalledOnce();
    expect(graph.ctx.suspend).toHaveBeenCalledOnce();
    expect(graph.ctx.state).toBe('suspended');
    releaseAudioGraph(audio);
  });

  it('does not let a late Pause suspend playback after newer Play wins', async () => {
    const resumeGate = deferred();
    const { audio } = createHarness({
      onResume: (context) => resumeGate.promise.then(() => { context.state = 'running'; }),
    });
    const graph = getOrCreateAudioGraph(audio);

    const start = resumeAudioGraph(audio, false);
    await vi.waitFor(() => expect(graph.ctx.resume).toHaveBeenCalledOnce());
    const pause = suspendAudioGraph(audio);
    const replay = resumeAudioGraph(audio, false);

    resumeGate.resolve();
    await Promise.all([start, pause, replay]);

    expect(graph.ctx.suspend).not.toHaveBeenCalled();
    expect(graph.ctx.state).toBe('running');
    releaseAudioGraph(audio);
  });

  it('resumes only after an older suspend finishes', async () => {
    const suspendGate = deferred();
    const { audio } = createHarness({
      initialState: 'running',
      onSuspend: (context) => suspendGate.promise.then(() => { context.state = 'suspended'; }),
    });
    const graph = getOrCreateAudioGraph(audio);

    const pause = suspendAudioGraph(audio);
    await vi.waitFor(() => expect(graph.ctx.suspend).toHaveBeenCalledOnce());
    const replay = resumeAudioGraph(audio, false);

    expect(graph.ctx.resume).not.toHaveBeenCalled();
    suspendGate.resolve();
    await Promise.all([pause, replay]);

    expect(graph.ctx.resume).toHaveBeenCalledOnce();
    expect(graph.ctx.state).toBe('running');
    releaseAudioGraph(audio);
  });

  it('rechecks playback intent arriving just after a transition settles', async () => {
    const { audio } = createHarness({ initialState: 'running' });
    const graph = getOrCreateAudioGraph(audio);
    let currentChange = null;
    let injectPlay = true;
    let replay;
    Object.defineProperty(graph, 'stateChange', {
      configurable: true,
      get: () => currentChange,
      set: (value) => {
        currentChange = value;
        if (value && injectPlay) {
          injectPlay = false;
          Promise.resolve(value).then(() => { replay = resumeAudioGraph(audio, false); });
        }
      },
    });

    const pause = suspendAudioGraph(audio);
    await vi.waitFor(() => expect(replay).toBeDefined());
    await Promise.all([pause, replay]);

    expect(graph.ctx.suspend).toHaveBeenCalledOnce();
    expect(graph.ctx.resume).toHaveBeenCalledOnce();
    expect(graph.ctx.state).toBe('running');
    releaseAudioGraph(audio);
  });
});
