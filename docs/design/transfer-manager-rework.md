# Transfer Manager Rework — Single Pane of Glass

Status: Phase 1 + 2 + 3 + 4 complete (incl. auto-replace REMOVED follow-up)
Owner: Keith
Date: 2026-05-18

## Progress

- **Phase 1 — done.** Flat `GET /transfers?direction=&includeCompleted=&includeRemoved=`
  endpoint; `Id`/`PlaceInQueue` added to `TransferActivity` (+ `FromTransferProgress`);
  `PROGRESS`/`REMOVED` hub methods + extensions; throttled (~1 Hz, sampled)
  `Client_TransferProgressUpdated`; record-resolution on state change; `REMOVED`
  emitted from cancel-with-remove and clear-completed. Main project builds clean
  (0 errors). New unit tests in `TransfersControllerTests.cs` (flat endpoint
  direction filter, predicate translatability, `REMOVED` emission) — now
  compiling and **passing** (see "Pre-existing unit-test breakage — fixed").
- **Phase 2 — done.** `lib/transferStore.js` (composite-keyed `direction|username|filename`
  store, `useSyncExternalStore`-compatible, seed/applyActivity/applyProgress/
  applyRemoved/removeByKey); `lib/transfers.js` `getFlat()`; `TransferManager.jsx`
  (Downloads/Uploads tabs, SignalR activity/progress/removed wiring, 15s reconcile,
  ported bulk-op queue, action parity, reused `TransfersHeader`, live/reconnecting
  indicator); `TransferTable.jsx` (flat sortable table, `React.memo` rows keyed by
  composite key, selection, per-row + bulk actions). Routes `/downloads` and
  `/uploads` now render `TransferManager`. eslint clean, `vite build` green,
  existing Transfers tests still pass, new `transferStore.test.js` (7 tests) green —
  incl. the explicit "single row survives an id-changing auto-retry" guarantee.
  Old `Transfers.jsx`/`TransferGroup`/`TransferList` left in place for Phase 3.
- **Phase 3 — done.** `react-window@^1.8.11` added to `package.json` +
  `package-lock.json`. `TransferTable` rebuilt as a virtualized CSS-grid list
  (`FixedSizeList`): sortable headers; columns Name, Peer (+ per-row Browse-user
  button, `transfer-browse-user-<peer>` testid preserved for the
  browse-transfer-handoff e2e), Size, Progress (lightweight CSS bar), Speed, ETA,
  State, actions. Filter bar in `TransferManager`: status chips
  (All/Active/Queued/Completed/Failed with live counts) + name/peer search.
  Responsive: horizontal-scroll wrapper, min-width grid, sticky dark-aware header.
  Retired `Transfers.jsx` / `TransferGroup.jsx` / `TransferList.jsx` and their
  tests; `App.test.jsx` mock repointed to `TransferManager`. eslint clean,
  `vite build` green (`react-window` bundled into the TransferManager chunk),
  41 unit/component tests green.

- **Phase 4 — done.** UI polish: sticky header with separation shadow that
  stays aligned on both axes (header + virtual rows share one
  horizontally-scrolled `.transfer-grid`; the list scrolls vertically inside
  itself so the header never detaches); index-keyed zebra striping (not
  `:nth-child`, which would shimmer with react-window node recycling); row
  hover + selected accent bar; full a11y pass (`role=grid/row/columnheader/
  gridcell/rowgroup` via a `RowGroup` `innerElementType`, `aria-sort`,
  keyboard-sortable headers, focus-visible rings, labelled checkboxes,
  `aria-selected`, click/Space/Enter row selection that ignores interactive
  children); retry affordance — an attempts badge (`↻N`) and next-attempt
  clock surfaced from the existing `attempts` / `nextAttemptAt` DTO fields.
  eslint clean, `vite build` green, 41 tests green.

## Retry / auto-retry / peer-cycling — how rows are anchored

Rows are keyed by `direction|username|filename` (`transferKey`). Behaviour by
case:

- **Manual retry** (`download({ files:[{filename,size}], username })`) — same
  peer, same remote filename → key unchanged → the existing row patches in
  place; only `state`/`attempts` cycle (Errored → Queued → InProgress). One
  row. The peer does **not** cycle here.
- **Same-source auto-retry** (`DownloadAutoRetryService`) — same peer/filename
  → identical to manual retry. One stable row; `attempts`/`nextAttemptAt`
  badge communicates the retry.
- **Auto-replace / alternate source** (`AutoReplaceService.ReplaceDownloadAsync`)
  — the backend **cancels + removes** the original record and **enqueues a new
  download from a different peer** with a different remote path. It is, by
  design, a genuinely different transfer (Soulseek filenames are peer-specific
  paths, so there is no shared natural key across sources, and the persisted
  `Id` is a fresh `Guid`). So the peer does not "cycle within a row" — the row
  is *replaced*. The cancel emits an `ACTIVITY` (→ row shows `Cancelled`);
  service-layer `Remove` emits no event today, so the stale row lingers until
  the ≤15 s reconcile prunes it, while the new-source row appears immediately.

### Follow-up — done (user-approved)

`AutoReplaceService.ReplaceDownloadAsync` now captures the original record
(`Find(t => t.Id == originalGuid)`) before cancel/remove and emits `REMOVED`
via an injected `IHubContext<TransfersHub>`, so the stale row drops the instant
the replacement is enqueued. `transfersHub` is an **optional** ctor param
(`= null`, null-guarded emit): MS DI injects the registered hub in production,
and existing `new AutoReplaceService(...)` call sites (incl. the
pre-existing-broken `AutoReplaceServiceTests`) compile unchanged — no
DI-registration edit required. Backend builds clean (0 errors). True cross-peer
"file anchoring" beyond this would still require a new backend correlation id
(a logical download-intent id carried across replacement) — larger, separate,
not pursued.

## Installer / packaging note

`react-window` is a **build-time bundled** asset, not a per-platform runtime
dependency. Every release channel (Docker, Copr, release-linux, PPA,
build-on-tag; Chocolatey/Winget package the build-on-tag output) builds the web
via `bin/build` → `npm ci --legacy-peer-deps` and copies `src/web/build/*` into
`wwwroot`. The single source of truth is `package.json` + `package-lock.json`;
both are updated and committed, so strict `npm ci` resolves on every channel and
the dependency is bundled everywhere automatically. No per-installer manifest,
spec file, or native-dependency change is required.

## Pre-existing unit-test breakage — fixed

The `slskd.Tests.Unit` project no longer compiled because two test doubles were
not updated for the `ISearchService`/`IWishlistService` signature changes in
commit `60039cee8` (unrelated to this work). Fixed by implementing the missing
members and updating stale Moq setups to the new 7-arg `StartAsync`
(added `Guid? wishlistItemId`):

- `LidarrSyncServiceTests.FakeWishlistService`: added
  `GetSearchesForItemAsync` / `MarkViewedAsync` / `MarkAllViewedAsync`.
- `AutoReplaceServiceTests.RateLimitedSearchService`: added the 7-arg
  `StartAsync` overload, `CleanupAsync`, `GetByWishlistItemIdAsync`; updated 2
  Moq `.Setup/.ReturnsAsync` arities.
- `WishlistControllerTests`: updated 3 stale `StartAsync` Moq
  `.Setup/.Callback/.ReturnsAsync` arities.

Result: **full suite green — 4205 passed, 0 failed**, including the new
`TransfersControllerTests` (flat endpoint direction filter, predicate
translatability, `REMOVED` emission) which now actually run.

## browse-transfer-handoff e2e — real break found and fixed

Inspecting the spec (rather than assuming) revealed Phase 3 **would have broken
it**: it mocked the legacy nested `/api/v0/transfers/downloads`, which
`TransferManager` no longer calls — it seeds from the flat `/api/v0/transfers`.
Added an explicit flat-endpoint route returning a correctly-shaped flat record
(`direction: 'Download'`, `username: fixturePeer`, `state: 'Completed,
Errored'`) so the row and the `transfer-browse-user-fixturePeer` button render.
Spec now typechecks and is discovered by `playwright test --list`. A live run
still needs the multi-node harness (build slskd + web + fixtures + chromium) and
remains a CI job — not executed in this sandbox — but the spec is no longer
silently wrong against the new endpoint.

## Goal

Replace the nested card/accordion uploads & downloads pages with a single
qBittorrent/Deluge-style "transfer manager": one page, a Downloads/Uploads tab
toggle, flat sortable columns, realtime-ish updates, and no full-tree redraw on
poll or auto-retry. Targets: responsive browser UI, low steady-state cost,
efficient incremental updates.

## Decisions (confirmed with user)

- **Layout:** One page, two tabs (Downloads / Uploads). Both tabs share one
  table/row engine and one realtime store. Each tab is a flat, sortable,
  virtualized table.
- **Realtime:** SignalR push + slow REST reconcile. Subscribe to the existing
  transfers hub for state changes, add throttled progress/speed push on the
  backend, patch only changed rows in a client store, REST reconcile (~15s) as
  a safety net.

## What's wrong today

**Backend** (`TransfersController`, `Application.cs`, `TransfersHub`)
- `GET /transfers/downloads|uploads` return a deeply nested `User → Directory →
  Files[]` grouping; controller TODO admits it "returns the world".
- `TransfersHub` already broadcasts an `ACTIVITY` event on every state change
  (`Client_TransferStateChanged`) but the transfers page never subscribes; only
  `TrafficTicker` does. Progress is never pushed
  (`Client_TransferProgressUpdated` is a deliberate no-op).
- `TransferActivity` carries no transfer id → rows can't be matched cheaply.

**Frontend** (`Transfers.jsx` + class components)
- Polls `getAll` every 2s (active) + 15s (full), re-normalizes a brand-new
  object graph, replaces whole state → entire tree re-renders/re-sorts.
- Auto-retry can mint a new transfer id, causing row churn, card re-mount, lost
  fold/selection state; elaborate "optimistic hide by (username,filename)"
  workarounds exist to mask this.
- Nested cards/accordions, not a scannable grid. No Peer/ETA/sortable columns.
  Two pages duplicate header + bulk-op logic.

## Target architecture

### Backend

1. **Flat snapshot endpoint** `GET /transfers?direction=&includeCompleted=`
   returning flat `Transfer[]` (no User/Directory nesting). Old endpoints kept
   for compatibility during migration.
2. **Stable id + queue pos in realtime events**: add `Id` (persisted `Guid`)
   and `PlaceInQueue` to `TransferActivity` so the client patches by id.
3. **Throttled progress push**: implement `Client_TransferProgressUpdated` to
   emit a `PROGRESS` hub event, coalesced per transfer at ~1 Hz (dictionary +
   timer in `Application.cs`, dropping intermediate ticks).
4. **Removal event**: emit `REMOVED` (id) on cancel-with-remove and
   clear-completed so the client drops rows immediately.

### Frontend

5. **Realtime store** `lib/transferStore.js`: `Map<id, transfer>` with
   subscribe/snapshot. Seeded by REST snapshot; patched by hub
   `ACTIVITY`/`PROGRESS`/`REMOVED`; ~15s REST reconcile corrects missed events
   and prunes stale rows. No full-tree rebuild.
6. **New components** under `components/Transfers/`:
   - `TransferManager.jsx` — container, tab state, hub wiring, store
     subscription, bulk-op queue (port `enqueueBulkOperations`).
   - `TransferTable.jsx` — sortable header + virtualized body (`react-window`).
   - `TransferRow.jsx` — `React.memo`, keyed by stable id; re-renders only when
     its transfer object changes.
   - `TransferFilters` / `TransferToolbar` — search, state filter chips, bulk
     retry/cancel/remove, speed summary from `/transfers/speeds`.
   - Columns: Name, Peer, Size, Progress (bar+%), Speed, ETA, Queue #, State,
     Added/Elapsed — sortable client-side.
7. **Routing**: `/downloads` and `/uploads` render `TransferManager` with the
   matching tab preselected; optionally add `/transfers`.
8. Reuse `lib/transfers.js` helpers; retire
   `TransferGroup`/`TransferList`/old `Transfers.jsx`; update tests.

### Auto-retry fix

Rows live in a keyed-by-id store; only changed rows patch. An auto-retry that
flips `Errored → Queued → InProgress` updates one row in place — no list
rebuild, no lost scroll/selection. Stable server-side id + reconcile keep an
id-minting retry to a single row swap. Removes most "optimistic hide" code.

## Phasing

- **Phase 1 — Backend.** Flat endpoint; `Id`/`PlaceInQueue` on
  `TransferActivity`; throttled `PROGRESS`; `REMOVED`. Tests.
- **Phase 2 — Frontend core.** Store + hub wiring + `TransferManager` with a
  basic table at parity with today's actions, behind existing routes.
- **Phase 3 — Polish.** Columns, sorting, virtualization, filters, responsive;
  remove old components; update tests.

## Risks

- Progress throttling must be coalesced server-side or SignalR floods on many
  concurrent transfers — main backend correctness point.
- `react-window` is a new (small, ubiquitous) dependency; hand-rolled windowing
  is the fallback.
- Reconcile interval (15s) is the staleness upper bound if the socket drops;
  SignalR auto-reconnect already configured in `hubFactory`.
