# Iris & Oxide: the slskdN / slskr color identities

Two disciplined, distinct color identities replacing the old base theme
(competing violet + amber accents in slskdN, byte-identical to slskr's).
Full audit and rationale: see the "Signal & Noise" design review this pair
of identities came out of.

## The rule

One accent hue per product. Four semantic colors — success, warning,
danger, info — shared by both products, used only for real state, never
for branding or categorization. Everything else is neutral (`Label basic`,
no `color` prop).

| Token | Iris (slskdN) | Oxide (slskr) |
| --- | --- | --- |
| `bg` | `#17151a` | `#12161b` |
| `surface` | `#1f1c24` | `#181d24` |
| `border` | `#35303d` | `#2c343d` |
| `text` | `#edeaf2` | `#e9edf0` |
| `text-muted` | `#a79fb0` | `#93a0ac` |
| `accent` | `#9c7cf0` (violet) | `#d46a3b` (rust/oxide) |
| `accent-hover` | `#b79bf6` | `#e8804f` |

Shared semantic colors (identical in both products):

| State | Hex |
| --- | --- |
| success | `#4fb477` |
| warning | `#d9a441` |
| danger | `#e1594f` |
| info | `#4c9fe0` |

## Why these two

- **Iris** (slskdN): the existing mark is a quill feather over an ink well.
  Keeps the one accent already worth keeping — violet — and gives it sole
  ownership of "brand," dropping the amber it used to compete with. Neutrals
  are warmed a few degrees toward the ink-well rather than cold slate.
- **Oxide** (slskr): the Rust rewrite. The accent is a literal rust/oxide
  orange — unrelated to slskdN's violet on purpose — on a cooler steel-slate
  neutral scale, so the two products never get mistaken for each other in a
  screenshot even though the codebases share a lot of history.

## Where this lives in code

Both products define the same token names with a per-product prefix
(`--slskdn-*` / `--slskr-*`) in `components/App.css` under `:root.dark`.
`--slskdn-accent-warm` / `--slskr-accent-warm` (and their `-hover`/`-muted`
variants) are kept as aliases of the single accent — some older component
CSS still references the "warm" name, and aliasing it is safer than a full
rename across every consumer. slskdN's `lib/themes.js` runtime palette
picker follows the same rule when a user selects a palette: a palette's
`secondary` scale only tints backgrounds (see `createSurfaceScale`), never
supplies a second foreground/accent hue.

## The other rules from the same pass

Not color-specific, but part of the same fix:

- The player collapses by default (`slskdn.player.collapsed` /
  `slskr.player.collapsed` in `localStorage`) until the user explicitly
  expands it once.
- Top nav groups low-traffic destinations under `Discover` / `Network` /
  `Sharing` dropdowns instead of one flat 15-item row.
- System settings groups its 22 tabs into six named sections (Overview,
  Network & Mesh, Security & Trust, Automation & Jobs, Diagnostics,
  Advanced) instead of one flat, horizontally-scrolling strip.
- The footer shows only attribution when logged out — no donation badges,
  build/version badge, or live telemetry before someone has signed in.
- A colored pill/badge/chip has to represent one of the four semantic
  states above to earn a color. A pure category or count gets a neutral
  `basic` label instead (Wishlist's summary row, Playlist Intake's summary
  row, System → Network's health-score legend, Source Providers'
  "Registered" pill).
