# Transfer Manager Rework — Single Pane of Glass

Status: Phase 1 complete (backend); Phase 2 (frontend) not started
Owner: Keith
Date: 2026-05-18

## Progress

- **Phase 1 — done.** Flat `GET /transfers?direction=&includeCompleted=&includeRemoved=`
  endpoint; `Id`/`PlaceInQueue` added to `TransferActivity` (+ `FromTransferProgress`);
  `PROGRESS`/`REMOVED` hub methods + extensions; throttled (~1 Hz, sampled)
  `Client_TransferProgressUpdated`; record-resolution on state change; `REMOVED`
  emitted from cancel-with-remove and clear-completed. Main project builds clean
  (0 errors). New unit tests written in `TransfersControllerTests.cs` but the
  unit-test project currently fails to compile due to **pre-existing, unrelated**
  breakage (`LidarrSyncServiceTests`, `AutoReplaceServiceTests` fakes missing
  interface members added by commit `60039cee8`); not introduced by this work.
- **Phase 2 / 3 — pending.**

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
