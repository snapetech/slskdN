import { describe, it, expect } from 'vitest';
import { THEME_PALETTES, getThemePalette, getThemeTokens } from './themes';

describe('THEME_PALETTES', () => {
  it('includes all 21 palettes', () => {
    expect(THEME_PALETTES).toHaveLength(21);
  });

  it('includes the Sietch palette displayed by the theme picker', () => {
    const ids = THEME_PALETTES.map((p) => p.id);
    expect(ids.slice(-3)).toEqual(['violet', 'ocean', 'sietch-neon']);

    const sietch = getThemePalette('sietch-neon');
    expect(sietch?.name).toBe('Sietch');
    expect(sietch?.swatches).toEqual(['#8e6036', '#43352e', '#8f5cff', '#d7ff3f']);
    expect(sietch?.surface).toBe('sietchSpice');
    expect(sietch?.primary).toBe('sietchSpice');
    expect(sietch?.secondary).toBe('sietchNeon');
  });

  it('has unique IDs for every palette', () => {
    const ids = THEME_PALETTES.map((p) => p.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it('has exactly 4 swatches per palette', () => {
    for (const p of THEME_PALETTES) {
      expect(p.swatches).toHaveLength(4);
    }
  });

  it('has valid scale names for surface/primary/secondary', () => {
    const validScales = [
      'slate', 'red', 'orange', 'amber', 'yellow', 'lime', 'green',
      'emerald', 'teal', 'cyan', 'sky', 'blue', 'indigo', 'violet',
      'purple', 'fuchsia', 'pink', 'rose', 'sietchNeon', 'sietchSpice',
    ];
    for (const p of THEME_PALETTES) {
      expect(validScales).toContain(p.surface);
      expect(validScales).toContain(p.primary);
      expect(validScales).toContain(p.secondary);
    }
  });
});

describe('getThemeTokens', () => {
  it('returns null for unknown palette', () => {
    const tokens = getThemeTokens('dark', 'nonexistent');
    expect(tokens).toBeNull();
  });

  it('returns tokens for every palette in dark mode', () => {
    for (const p of THEME_PALETTES) {
      const tokens = getThemeTokens('dark', p.id);
      expect(tokens).not.toBeNull();
      expect(tokens?.activePaletteId).toBe(p.id);
      expect(tokens?.surfaceScale).toHaveLength(11);
      expect(tokens?.primaryScale).toHaveLength(11);
      expect(tokens?.secondaryScale).toHaveLength(11);
      expect(tokens?.pageBg).toBeTruthy();
    }
  });

  it('gives every palette distinct page and sidebar chrome in dark mode', () => {
    const signatures = THEME_PALETTES.map((p) => {
      const t = getThemeTokens('dark', p.id);
      return [t?.pageBg, t?.pageGlowStart, t?.sidebarStart, t?.sidebarEnd].join('|');
    });
    expect(new Set(signatures).size).toBe(THEME_PALETTES.length);
  });

  it('gives every palette distinct page and sidebar chrome in light mode', () => {
    const signatures = THEME_PALETTES.map((p) => {
      const t = getThemeTokens('light', p.id);
      return [t?.pageBg, t?.pageGlowStart, t?.sidebarStart, t?.sidebarEnd].join('|');
    });
    expect(new Set(signatures).size).toBe(THEME_PALETTES.length);
  });

  it('keeps Sietch spice-led with neon secondary accent (dark mode)', () => {
    const t = getThemeTokens('dark', 'sietch-neon');
    expect(t).not.toBeNull();
    const [pr, pg, pb] = t.pageBg.split(' ').map(Number);
    const [sr, sg, sb] = t.sidebarBorder.split(' ').map(Number);

    // Page background should stay warm (red > blue, green > blue)
    expect(pr).toBeGreaterThanOrEqual(pb);
    expect(pg).toBeGreaterThanOrEqual(pb);

    // Secondary accents should stay neon purple (blue > red, blue > green)
    expect(sb).toBeGreaterThan(sr);
    expect(sb).toBeGreaterThan(sg);
  });
});

describe('applyPalette', () => {
  it('is a function', async () => {
    const { applyPalette } = await import('./themes');
    expect(typeof applyPalette).toBe('function');
  });
});
