import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createButterchurnEngine } from './butterchurnEngine';

const { createVisualizer, getPresets, visualizer } = vi.hoisted(() => ({
  createVisualizer: vi.fn(),
  getPresets: vi.fn(),
  visualizer: {
    connectAudio: vi.fn(),
    disconnectAudio: vi.fn(),
    loadPreset: vi.fn(),
    render: vi.fn(),
    setRendererSize: vi.fn(),
  },
}));

vi.mock('butterchurn', () => ({
  createVisualizer,
  default: { createVisualizer },
}));
vi.mock('butterchurn-presets', () => ({ default: { getPresets } }));

describe('createButterchurnEngine', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    createVisualizer.mockReturnValue(visualizer);
    getPresets.mockReturnValue({ fixture: { name: 'Fixture preset' } });
  });

  it('disconnects audio and loses the WebGL context once on dispose', async () => {
    const loseContext = vi.fn();
    const context = { getExtension: vi.fn(() => ({ loseContext })) };
    const canvas = { getContext: vi.fn(() => context) };
    const audioNode = {};
    const engine = await createButterchurnEngine({
      audioContext: {},
      audioNode,
      canvas,
      pixelRatio: 1,
    });

    engine.dispose();
    engine.dispose();

    expect(visualizer.disconnectAudio).toHaveBeenCalledExactlyOnceWith(audioNode);
    expect(canvas.getContext).toHaveBeenCalledExactlyOnceWith('webgl2', {
      alpha: false,
      antialias: false,
      depth: false,
      premultipliedAlpha: false,
      stencil: false,
    });
    expect(context.getExtension).toHaveBeenCalledWith('WEBGL_lose_context');
    expect(loseContext).toHaveBeenCalledOnce();
  });

  it('loses a partially created WebGL context when engine setup fails', async () => {
    const loseContext = vi.fn();
    const canvas = {
      getContext: vi.fn(() => ({ getExtension: vi.fn(() => ({ loseContext })) })),
    };
    createVisualizer.mockImplementation(() => {
      throw new Error('Visualizer setup failed.');
    });

    await expect(createButterchurnEngine({
      audioContext: {},
      audioNode: {},
      canvas,
      pixelRatio: 1,
    })).rejects.toThrow('Visualizer setup failed.');

    expect(loseContext).toHaveBeenCalledOnce();
  });

  it('disconnects audio and loses the context when the initial preset fails', async () => {
    const loseContext = vi.fn();
    const context = { getExtension: vi.fn(() => ({ loseContext })) };
    const canvas = { getContext: vi.fn(() => context) };
    const audioNode = {};
    visualizer.loadPreset.mockImplementation(() => {
      throw new Error('Initial preset failed.');
    });

    await expect(createButterchurnEngine({
      audioContext: {},
      audioNode,
      canvas,
      pixelRatio: 1,
    })).rejects.toThrow('Initial preset failed.');

    expect(visualizer.disconnectAudio).toHaveBeenCalledExactlyOnceWith(audioNode);
    expect(loseContext).toHaveBeenCalledOnce();
  });

  it('loses the WebGL context even when disconnecting audio throws', async () => {
    const loseContext = vi.fn();
    const canvas = {
      getContext: vi.fn(() => ({ getExtension: vi.fn(() => ({ loseContext })) })),
    };
    visualizer.disconnectAudio.mockImplementation(() => {
      throw new Error('Audio disconnect failed.');
    });
    const engine = await createButterchurnEngine({
      audioContext: {},
      audioNode: {},
      canvas,
      pixelRatio: 1,
    });

    expect(() => engine.dispose()).toThrow('Audio disconnect failed.');
    expect(loseContext).toHaveBeenCalledOnce();
  });
});
