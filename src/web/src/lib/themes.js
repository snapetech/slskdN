/**
 * Theme palettes and application logic ported from seerrng.
 *
 * Each palette defines a surface, primary, and secondary colour scale plus
 * a few swatch colours used in the picker UI.
 *
 * applyPalette() writes CSS custom properties on document.documentElement so
 * the UI responds immediately without a page reload.  When no palette is
 * active the existing :root.dark CSS takes over.
 */

/* ------------------------------------------------------------------ */
/*  Tailwind-style colour scales (RGB space-separated triples)         */
/* ------------------------------------------------------------------ */

const themeScales = {
  slate: [
    '248 250 252', '241 245 249', '226 232 240', '203 213 225',
    '148 163 184', '100 116 139', '71 85 105', '51 65 85',
    '30 41 59', '15 23 42', '2 6 23',
  ],
  red: [
    '254 242 242', '254 226 226', '254 202 202', '252 165 165',
    '248 113 113', '239 68 68', '220 38 38', '185 28 28',
    '153 27 27', '127 29 29', '69 10 10',
  ],
  orange: [
    '255 247 237', '255 237 213', '254 215 170', '253 186 116',
    '251 146 60', '249 115 22', '234 88 12', '194 65 12',
    '154 52 18', '124 45 18', '67 20 7',
  ],
  amber: [
    '255 251 235', '254 243 199', '253 230 138', '252 211 77',
    '251 191 36', '245 158 11', '217 119 6', '180 83 9',
    '146 64 14', '120 53 15', '69 26 3',
  ],
  yellow: [
    '254 252 232', '254 249 195', '254 240 138', '253 224 71',
    '250 204 21', '234 179 8', '202 138 4', '161 98 7',
    '133 77 14', '113 63 18', '66 32 6',
  ],
  lime: [
    '247 254 231', '236 252 203', '217 249 157', '190 242 100',
    '163 230 53', '132 204 22', '101 163 13', '77 124 15',
    '63 98 18', '54 83 20', '26 46 5',
  ],
  green: [
    '240 253 244', '220 252 231', '187 247 208', '134 239 172',
    '74 222 128', '34 197 94', '22 163 74', '21 128 61',
    '22 101 52', '20 83 45', '5 46 22',
  ],
  emerald: [
    '236 253 245', '209 250 229', '167 243 208', '110 231 183',
    '52 211 153', '16 185 129', '5 150 105', '4 120 87',
    '6 95 70', '6 78 59', '2 44 34',
  ],
  teal: [
    '240 253 250', '204 251 241', '153 246 228', '94 234 212',
    '45 212 191', '20 184 166', '13 148 136', '15 118 110',
    '17 94 89', '19 78 74', '4 47 46',
  ],
  cyan: [
    '236 254 255', '207 250 254', '165 243 252', '103 232 249',
    '34 211 238', '6 182 212', '8 145 178', '14 116 144',
    '21 94 117', '22 78 99', '8 51 68',
  ],
  sky: [
    '240 249 255', '224 242 254', '186 230 253', '125 211 252',
    '56 189 248', '14 165 233', '2 132 199', '3 105 161',
    '7 89 133', '12 74 110', '8 47 73',
  ],
  blue: [
    '239 246 255', '219 234 254', '191 219 254', '147 197 253',
    '96 165 250', '59 130 246', '37 99 235', '29 78 216',
    '30 64 175', '30 58 138', '23 37 84',
  ],
  indigo: [
    '238 242 255', '224 231 255', '199 210 254', '165 180 252',
    '129 140 248', '99 102 241', '79 70 229', '67 56 202',
    '55 48 163', '49 46 129', '30 27 75',
  ],
  violet: [
    '245 243 255', '237 233 254', '221 214 254', '196 181 253',
    '167 139 250', '139 92 246', '124 58 237', '109 40 217',
    '91 33 182', '76 29 149', '46 16 101',
  ],
  purple: [
    '250 245 255', '243 232 255', '233 213 255', '216 180 254',
    '192 132 252', '168 85 247', '147 51 234', '126 34 206',
    '107 33 168', '88 28 135', '59 7 100',
  ],
  fuchsia: [
    '253 244 255', '250 232 255', '245 208 254', '240 171 252',
    '232 121 249', '217 70 239', '192 38 211', '162 28 175',
    '134 25 143', '112 26 117', '74 4 78',
  ],
  pink: [
    '253 242 248', '252 231 243', '251 207 232', '249 168 212',
    '244 114 182', '236 72 153', '219 39 119', '190 24 93',
    '157 23 77', '131 24 67', '80 7 36',
  ],
  rose: [
    '255 241 242', '255 228 230', '254 205 211', '253 164 175',
    '251 113 133', '244 63 94', '225 29 72', '190 18 60',
    '159 18 57', '136 19 55', '76 5 25',
  ],
  sietchNeon: [
    '250 246 255', '240 232 255', '222 207 255', '199 171 255',
    '174 128 255', '143 92 255', '124 58 237', '104 39 196',
    '79 30 142', '55 25 94', '31 18 46',
  ],
  sietchSpice: [
    '251 247 239', '242 232 217', '222 203 178', '198 166 128',
    '170 128 83', '142 96 54', '116 75 43', '91 62 45',
    '67 53 46', '58 45 32', '35 27 20',
  ],
};

/** @typedef {'slate'|'red'|'orange'|'amber'|'yellow'|'lime'|'green'|'emerald'|'teal'|'cyan'|'sky'|'blue'|'indigo'|'violet'|'purple'|'fuchsia'|'pink'|'rose'|'sietchNeon'|'sietchSpice'} ScaleName */

/**
 * @typedef {Object} PaletteDefinition
 * @property {string} id
 * @property {string} name
 * @property {string[]} swatches – hex colours shown in the picker
 * @property {ScaleName} surface – scale name used for backgrounds / surfaces
 * @property {ScaleName} primary – scale name used for primary accents
 * @property {ScaleName} secondary – scale name used for secondary accents
 */

/** @type {PaletteDefinition[]} */
export const THEME_PALETTES = [
  { id: 'aurora',    name: 'Aurora',    swatches: ['#4f46e5', '#a855f7', '#14b8a6', '#6366f1'], surface: 'indigo', primary: 'indigo', secondary: 'purple' },
  { id: 'ember',     name: 'Ember',     swatches: ['#dc2626', '#f97316', '#f59e0b', '#fbbf24'], surface: 'orange', primary: 'red', secondary: 'orange' },
  { id: 'lagoon',    name: 'Lagoon',    swatches: ['#0f766e', '#0891b2', '#2563eb', '#06b6d4'], surface: 'teal', primary: 'teal', secondary: 'cyan' },
  { id: 'orchid',    name: 'Orchid',    swatches: ['#7c3aed', '#d946ef', '#ec4899', '#a855f7'], surface: 'fuchsia', primary: 'violet', secondary: 'fuchsia' },
  { id: 'forest',    name: 'Forest',    swatches: ['#15803d', '#65a30d', '#0f766e', '#22c55e'], surface: 'green', primary: 'green', secondary: 'lime' },
  { id: 'sapphire',  name: 'Sapphire',  swatches: ['#1d4ed8', '#0284c7', '#6366f1', '#3b82f6'], surface: 'blue', primary: 'blue', secondary: 'sky' },
  { id: 'rosewood',  name: 'Rosewood',  swatches: ['#be123c', '#db2777', '#7c2d12', '#fb7185'], surface: 'rose', primary: 'rose', secondary: 'pink' },
  { id: 'citrus',    name: 'Citrus',    swatches: ['#ca8a04', '#84cc16', '#f97316', '#facc15'], surface: 'yellow', primary: 'yellow', secondary: 'lime' },
  { id: 'arctic',    name: 'Arctic',    swatches: ['#0284c7', '#64748b', '#22d3ee', '#38bdf8'], surface: 'slate', primary: 'sky', secondary: 'slate' },
  { id: 'grape',     name: 'Grape',     swatches: ['#6d28d9', '#9333ea', '#4f46e5', '#a78bfa'], surface: 'purple', primary: 'purple', secondary: 'violet' },
  { id: 'coral',     name: 'Coral',     swatches: ['#e11d48', '#fb7185', '#f97316', '#f43f5e'], surface: 'orange', primary: 'rose', secondary: 'orange' },
  { id: 'mint',      name: 'Mint',      swatches: ['#059669', '#10b981', '#06b6d4', '#34d399'], surface: 'emerald', primary: 'emerald', secondary: 'teal' },
  { id: 'steel',     name: 'Steel',     swatches: ['#475569', '#2563eb', '#0f766e', '#64748b'], surface: 'slate', primary: 'slate', secondary: 'blue' },
  { id: 'gold',      name: 'Gold',      swatches: ['#b45309', '#eab308', '#ea580c', '#f59e0b'], surface: 'amber', primary: 'amber', secondary: 'yellow' },
  { id: 'plum',      name: 'Plum',      swatches: ['#86198f', '#be185d', '#7c3aed', '#d946ef'], surface: 'pink', primary: 'fuchsia', secondary: 'pink' },
  { id: 'skyline',   name: 'Skyline',   swatches: ['#0369a1', '#4f46e5', '#06b6d4', '#0284c7'], surface: 'sky', primary: 'sky', secondary: 'indigo' },
  { id: 'moss',      name: 'Moss',      swatches: ['#4d7c0f', '#16a34a', '#ca8a04', '#65a30d'], surface: 'lime', primary: 'lime', secondary: 'green' },
  { id: 'flame',     name: 'Flame',     swatches: ['#c2410c', '#dc2626', '#f59e0b', '#f97316'], surface: 'red', primary: 'orange', secondary: 'red' },
  { id: 'violet',    name: 'Violet',    swatches: ['#5b21b6', '#7e22ce', '#2563eb', '#8b5cf6'], surface: 'violet', primary: 'violet', secondary: 'blue' },
  { id: 'ocean',     name: 'Ocean',     swatches: ['#075985', '#0d9488', '#1d4ed8', '#0891b2'], surface: 'cyan', primary: 'cyan', secondary: 'blue' },
  { id: 'sietch-neon', name: 'Sietch',  swatches: ['#8e6036', '#43352e', '#8f5cff', '#d7ff3f'], surface: 'sietchSpice', primary: 'sietchSpice', secondary: 'sietchNeon' },
];

/**
 * @param {string} paletteId
 * @returns {PaletteDefinition|null}
 */
export const getThemePalette = (paletteId) =>
  THEME_PALETTES.find((p) => p.id === paletteId) ?? null;

/* ------------------------------------------------------------------ */
/*  Utility                                                            */
/* ------------------------------------------------------------------ */

const parseRgb = (value) => value.split(' ').map(Number);

const mixRgb = (from, to, amount) => {
  const fromRgb = parseRgb(from);
  const toRgb = parseRgb(to);
  return fromRgb
    .map((channel, i) => Math.round(channel * (1 - amount) + toRgb[i] * amount))
    .join(' ');
};

/**
 * Create a surface scale appropriate for the current mode.
 */
const createSurfaceScale = (surfaceScale, accentScale, secondaryScale, mode) => {
  if (mode === 'dark') {
    const slateMix = [0.03, 0.04, 0.06, 0.08, 0.12, 0.16, 0.22, 0.28, 0.34, 0.38, 0.42];
    const accentMix = [0.08, 0.1, 0.12, 0.16, 0.2, 0.24, 0.3, 0.36, 0.42, 0.48, 0.52];
    return surfaceScale.map((surface, i) =>
      mixRgb(mixRgb(surface, themeScales.slate[i], slateMix[i]), accentScale[i], accentMix[i]),
    );
  }

  // light mode
  const reversedSurfaceScale = [...surfaceScale].reverse();
  const reversedSecondaryScale = [...secondaryScale].reverse();
  const slateMix = [0.1, 0.12, 0.14, 0.18, 0.22, 0.24, 0.2, 0.16, 0.12, 0.08, 0.04];
  const accentMix = [0.08, 0.1, 0.12, 0.15, 0.18, 0.2, 0.18, 0.16, 0.14, 0.12, 0.1];
  return reversedSurfaceScale.map((surface, i) =>
    mixRgb(mixRgb(surface, themeScales.slate[i], slateMix[i]), reversedSecondaryScale[i], accentMix[i]),
  );
};

/* ------------------------------------------------------------------ */
/*  Public API                                                         */
/* ------------------------------------------------------------------ */

/**
 * Compute the full set of palette tokens for a given mode + palette.
 *
 * @param {'dark'|'light'} mode
 * @param {string} paletteId
 * @returns {{ activePaletteId: string, surfaceScale: string[], primaryScale: string[], secondaryScale: string[], pageBg: string, pageGlowStart: string, sidebarStart: string, sidebarEnd: string, sidebarBorder: string, sidebarHover: string } | null}
 */
export const getThemeTokens = (mode, paletteId) => {
  const active = getThemePalette(paletteId);
  if (!active) return null;

  const primaryScale = themeScales[active.primary];
  const secondaryScale = themeScales[active.secondary];
  const surfaceScale = createSurfaceScale(
    themeScales[active.surface], primaryScale, secondaryScale, mode,
  );

  return {
    activePaletteId: active.id,
    primaryScale,
    secondaryScale,
    surfaceScale,
    pageBg: surfaceScale[9],
    pageGlowStart: mode === 'dark'
      ? mixRgb(surfaceScale[8], primaryScale[7], 0.56)
      : mixRgb(surfaceScale[8], primaryScale[3], 0.44),
    sidebarStart: mode === 'dark'
      ? mixRgb(surfaceScale[8], primaryScale[8], 0.58)
      : mixRgb(primaryScale[7], surfaceScale[2], 0.24),
    sidebarEnd: mode === 'dark'
      ? mixRgb(surfaceScale[10], primaryScale[9], 0.52)
      : mixRgb(primaryScale[9], surfaceScale[1], 0.18),
    sidebarBorder: mode === 'dark'
      ? mixRgb(surfaceScale[7], secondaryScale[6], 0.48)
      : mixRgb(primaryScale[6], secondaryScale[6], 0.42),
    sidebarHover: mode === 'dark'
      ? mixRgb(surfaceScale[7], primaryScale[7], 0.52)
      : mixRgb(primaryScale[6], secondaryScale[5], 0.76),
  };
};

/* ------------------------------------------------------------------ */
/*  CSS custom property overrides for palettes                         */
/* ------------------------------------------------------------------ */

/**
 * Compute the full set of CSS custom property overrides for a palette.
 * Returns an object mapping CSS property names to their values.
 *
 * Palettes only apply in dark mode — light mode uses Semantic UI defaults
 * (all CSS variable refs in App.css are scoped under :root.dark selectors).
 *
 * @param {{ activePaletteId: string, surfaceScale: string[], primaryScale: string[], secondaryScale: string[] }} t
 * @returns {Record<string, string>}
 */
const computeCssOverrides = (t) => {
  const pRgb = (i) => t.primaryScale[i];
  const sRgb = (i) => t.surfaceScale[i];
  const secRgb = (i) => t.secondaryScale[i];
  const s = (i) => `rgb(${t.surfaceScale[i]})`;
  const sec = (i) => `rgb(${t.secondaryScale[i]})`;
  const p = (i) => `rgb(${t.primaryScale[i]})`;

  return {
    '--slskd-primary-background': s(9),
    '--slskd-secondary-background': s(8),
    '--slskd-overlay-background': s(7),
    '--slskd-tertiary-background': s(6),
    '--slskd-tertiary-border-color': `rgba(${pRgb(8)}, 0.24)`,
    '--slskd-subtle-background': `rgba(${pRgb(4)}, 0.13)`,
    '--slskd-color': '#f1eadf',
    '--slskd-color-subtle': sec(3),
    '--slskd-hover-background': s(5),
    '--slskd-color-inset': s(10),
    '--slskd-emphasis-background': p(6),
    '--slskdn-accent-primary': p(5),
    '--slskdn-accent-primary-hover': p(4),
    '--slskdn-accent-primary-muted': `rgba(${pRgb(5)}, 0.18)`,
    '--slskdn-accent-primary-border': `rgba(${pRgb(5)}, 0.26)`,
    // Alias of the single accent — a palette's "secondary" scale only ever
    // tints backgrounds (see createSurfaceScale), never a second foreground hue.
    '--slskdn-accent-warm': p(5),
    '--slskdn-accent-warm-hover': p(4),
    '--slskdn-accent-warm-muted': `rgba(${pRgb(5)}, 0.18)`,
    '--slskdn-nav-background': s(9),
    '--slskdn-nav-border': `rgba(${pRgb(5)}, 0.24)`,
    '--slskdn-footer-background': s(9),
    '--slskdn-footer-shadow': `0 -1px 0 rgba(${pRgb(5)}, 0.14)`,
    '--slskdn-theme-trigger-background': s(6),
    '--slskdn-theme-trigger-hover-background': s(5),
    '--slskdn-theme-menu-background': s(7),
    '--slskdn-theme-menu-active-background': s(4),
    '--slskdn-theme-menu-shadow': `0 16px 38px rgba(0,0,0,0.5)`,
    '--slskdn-surface-shadow': `0 12px 28px rgba(0,0,0,0.3)`,
    '--slskdn-surface-shadow-soft': `0 6px 16px rgba(0,0,0,0.24)`,
    '--slskdn-focus-ring': `0 0 0 3px rgba(${pRgb(5)}, 0.42)`,
    '--slskdn-affordance-hover-background': `rgba(${pRgb(5)}, 0.14)`,
    '--slskdn-affordance-active-background': `rgba(${pRgb(5)}, 0.24)`,
    '--slskdn-affordance-outline': p(4),
  };
};

/** List of CSS custom properties managed by the palette system. */
const MANAGED_PROPS = [
  '--slskd-primary-background', '--slskd-secondary-background', '--slskd-overlay-background',
  '--slskd-tertiary-background', '--slskd-tertiary-border-color', '--slskd-subtle-background',
  '--slskd-color', '--slskd-color-subtle', '--slskd-hover-background', '--slskd-color-inset',
  '--slskd-emphasis-background',
  '--slskdn-accent-primary', '--slskdn-accent-primary-hover', '--slskdn-accent-primary-muted',
  '--slskdn-accent-primary-border',
  '--slskdn-accent-warm', '--slskdn-accent-warm-hover', '--slskdn-accent-warm-muted',
  '--slskdn-nav-background', '--slskdn-nav-border',
  '--slskdn-footer-background', '--slskdn-footer-shadow',
  '--slskdn-theme-trigger-background', '--slskdn-theme-trigger-hover-background',
  '--slskdn-theme-menu-background', '--slskdn-theme-menu-active-background', '--slskdn-theme-menu-shadow',
  '--slskdn-surface-shadow', '--slskdn-surface-shadow-soft',
  '--slskdn-focus-ring',
  '--slskdn-affordance-hover-background', '--slskdn-affordance-active-background',
  '--slskdn-affordance-outline',
];

/**
 * Apply a palette (and mode) to the document so CSS custom properties
 * are available immediately.  Pass `null` for `paletteId` to clear
 * palette overrides and revert to the CSS default theme.
 *
 * @param {'dark'|'light'} mode
 * @param {string|null} paletteId
 */
export const applyPalette = (mode, paletteId) => {
  if (typeof document === 'undefined') return;

  const root = document.documentElement;

  if (!paletteId) {
    // Clear palette overrides – let CSS defaults take over
    for (const prop of MANAGED_PROPS) {
      root.style.removeProperty(prop);
    }
    delete root.dataset.slskdnPalette;
    return;
  }

  const t = getThemeTokens(mode, paletteId);
  if (!t) {
    // Unknown palette, clear like null
    for (const prop of MANAGED_PROPS) {
      root.style.removeProperty(prop);
    }
    delete root.dataset.slskdnPalette;
    return;
  }

  const overrides = computeCssOverrides(t);
  for (const [prop, value] of Object.entries(overrides)) {
    root.style.setProperty(prop, value);
  }

  root.dataset.slskdnPalette = t.activePaletteId;
};
