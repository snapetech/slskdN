# Plan: full implementation of Bas's feedback + re-key

Five workstreams. Phases ordered so each ships value standalone and the architectural change lands last on a hardened foundation.

---

## Phase 1 — Wishlist hit count includes locked (small)

**Goal:** the number on the wishlist page reflects everything the search saw, not just visible.

**Data already there:** `WishlistItem.LastVisibleHitCount`, `LastHiddenLockedHitCount`, `LastFilteredOutHitCount` (`src/slskd/Wishlist/Types/WishlistItem.cs:64-74`). They're populated in `WishlistService.cs:596`. Frontend just isn't showing the locked portion.

**Changes:**
- `src/web/src/lib/wishlist.js` / `src/web/src/components/Wishlist/Wishlist.jsx`: render hit count as `visible (+N locked)`, with tooltip explaining what locked means. Add a small "Show locked" toggle on the wishlist item detail view that re-renders results including `LockedFiles` from the cached search response (read-only — these are unreachable).
- `WishlistController` already returns the full item; no API change needed unless the detail view doesn't include locked files yet — verify and add if missing.

**Risk:** zero. Pure display.

---

## Phase 2 — Wishlist sort & filter (small)

**Goal:** alphabetical sort, plus a "has new results" filter.

**Definition of "new":** `LastSearchedAt > LastViewedAt && LastVisibleHitCount > 0`. `LastViewedAt` already exists (`WishlistItem.cs:105`).

**Changes:**
- `src/web/src/components/Wishlist/Wishlist.jsx`:
  - Sort dropdown: Created (default), Name (A→Z / Z→A), Last searched, Hit count.
  - Filter chips: All / Enabled / Has new results / Auto-download. Multi-select OK.
  - Persist sort+filter selection in localStorage.
- No backend changes — sort/filter client-side over the already-returned list.

**Risk:** zero. Pure frontend.

---

## Phase 3 — Configurable download path template (medium)

**Goal:** stop the "sometimes folder, sometimes hash" inconsistency. Let users pick `<uploader>/<folder>/<files>` like N+, or define their own template.

**Status:** `Options.GlobalDownloadOptions.CompletedLayout` (`Options.cs`) is an enum-string with four values (`flat`, `uploader_folder`, `remote_folder`, `batch_id`). It now defaults to `remote_folder`, preserving the source folder/file path instead of putting completed downloads under the transfer `batch_id`. `ResolveCompletedDestinationDirectory` (`DownloadService.cs`) switches on this.

**Changes:**

1. **Add a real template option alongside the enum** (keep enum for back-compat, deprecate `batch_id` default in favor of source folder/file names):
   - New option: `Global.Download.CompletedPathTemplate` (string). Tokens: `{uploader}`, `{remote_folder}`, `{remote_parent}`, `{remote_filename}` (no extension), `{batch_id}`, `{request_name}` (added in Phase 5; renders empty pre-Phase-5), `{search_text}` (for wishlist-originated downloads), `{date:yyyy-MM-dd}` (RequestedAt). All segments sanitized via `ReplaceInvalidFileNameCharacters`. Path-separator literal `/` is honored.
   - If `CompletedPathTemplate` is set and non-empty, it wins over `CompletedLayout`. Otherwise existing behavior.
   - Fresh installs now default to `remote_folder`; operators can set `completed_path_template: '{uploader}/{remote_folder}'` when they want uploader-anchored paths.

2. **Fix the "single-file share" hash fallback.** When `remote_folder` is empty (uploader shared the file from their root), template should fall through to a stable string — `_singles` or similar — not BatchId. Same for `UploaderFolder` enum case (already does this via `GetRemoteParentFolderName` returning `_`, but the BatchId default doesn't).

3. **Files:**
   - `src/slskd/Core/Options.cs:1253-1292` — add `CompletedPathTemplate`.
   - `src/slskd/Transfers/Downloads/DownloadService.cs:1990-2014` — new `RenderCompletedPathTemplate(transfer, template)` method; switch picks template path when set.
   - `config/slskd.example.yml`, `docs/config.md` — document tokens.
   - Tests covering each token, empty remote_folder fallback, path traversal sanitization (token values can't contain `..` or absolute paths).

**Risk:** medium. Files moving to a different layout on first run after upgrade. Mitigation: don't change *existing* installs' default — only set `{uploader}/{remote_folder}` as the example/docs default. Existing configs continue with whatever they had.

---

## Phase 4 — Richer per-row metadata on Downloads (small–medium)

**Goal:** show artist/album/title etc. on the transfer row when uploaders give garbage filenames.

**Data already there:** `Transfer.Artist/Album/Title/TrackNumber/Year/BitRate/SampleRate/BitDepth/Length` and the new migration (`Z05222026_TransferFileDetailsMigration.cs`) persists them. Population happens post-download via tag read (`DownloadService.cs:1986`).

**Gap:** these aren't exposed on the API DTO or rendered in the UI. Also no pre-download metadata (uploaders sometimes attach `bitRate`/`length`/`sampleRate`/`bitDepth` on the search response — Soulseek file attributes).

**Changes:**
- `src/slskd/Transfers/API/DTO/Transfer.cs` — include the new fields in the DTO and the mapping in `src/slskd/Transfers/Extensions.cs`.
- At enqueue time (`DownloadService.EnqueueAsync` ~line 512), pull search-result file attributes (bitrate, length, sampleRate, bitDepth) from the originating search response and seed them on the Transfer so the row shows them before download completes. (Need to plumb attributes through `EnqueueAsync`'s tuple; the call sites in `TransfersController` and `WishlistService` have the source `File` available.)
- `src/web/src/lib/transferColumns.js` — add Artist, Album, Title, Bitrate, Length columns (default hidden except Bitrate/Length); update sort comparators.
- `src/web/src/components/Transfers/TransferTable.jsx` — secondary line under the filename showing "Artist — Title" when known.

**Risk:** low. Additive.

---

## Phase 5 — DownloadRequest re-key (large)

**Goal:** introduce a stable user-facing entity that survives source swaps and rename.

### 5a. Data model

New entity `DownloadRequest`:
```
Id              Guid    PK
Name            string  display label, user-renamable; defaults to remote filename basename
OriginalFilename string the filename as first asked for (audit/debug)
Size            long?   first known size (the request, not the attempt)
BatchId         Guid?   inherited from current Transfer
DestinationDirectory string?
State           enum    Aggregate status across attempts: Active/Completed/Failed/Cancelled
CreatedAt       DateTime
CompletedAt     DateTime?
WishlistItemId  Guid?   nullable FK; when set, the originating wishlist item
SearchResponseId Guid? for traceability
// metadata cached on the request so it survives across attempts:
Artist/Album/Title/TrackNumber/Year/BitRate/SampleRate/BitDepth/Length
```

`Transfer` becomes an **attempt** under a request:
- Add `RequestId Guid` (FK to DownloadRequest). Required on all new transfers.
- Existing `Username`, `Filename`, `Id` stay as the attempt-level identity (still needed for Soulseek protocol exchange).
- Remove the supersede-via-`Removed` mechanism for the user-facing case: now a rescue/alt-source creates a new Transfer under the same RequestId, and the old Transfer is marked `State=Cancelled` or `Removed=true` for *attempt-level* bookkeeping only. The UI never sees Transfers directly.

### 5b. Migration (`Z05292026_DownloadRequestMigration.cs`)

1. `CREATE TABLE DownloadRequests (...)`.
2. `ALTER TABLE Transfers ADD COLUMN RequestId TEXT NULL`.
3. **Backfill:** for every existing non-Removed Transfer, create one DownloadRequest with `Name = basename(Filename)`, copy `BatchId`, `DestinationDirectory`, audio metadata, and set `RequestId`. For groups of Transfers sharing `(Filename, BatchId)` where some are Removed=true (the supersede pattern), collapse them into a single Request and link all attempts.
4. Index `Transfers(RequestId)`, `DownloadRequests(State)`, `DownloadRequests(WishlistItemId)`.
5. Leave `RequestId` nullable in schema for one release for safety, but make code require it.

### 5c. Service layer

- New `IDownloadRequestService` owning request lifecycle: `Create`, `Rename`, `Cancel`, `MarkCompleted`, `GetWithAttempts`. Lives in `src/slskd/Transfers/Downloads/`.
- `DownloadService.EnqueueAsync`:
  - First creates a `DownloadRequest` per file (or accepts a pre-existing one for the rescue/alt-source path).
  - Returns `(Request, Transfer)` pairs.
- Rescue alt-source path: instead of `Removed=true` + new Transfer with new GUID + UI churn, call `DownloadRequestService.AddAttempt(requestId, newUsername, newFilename)`. The current Transfer becomes Cancelled; a new Transfer is added under the same Request. UI keeps showing the Request row, just updates "currently attempting from X".
- `DownloadService.cs:336` and `:528-530` (supersede sites) become: "supersede attempts under the same Request when a duplicate enqueue arrives" — narrower scope, internal only.

### 5d. API + DTO

- New endpoint family `/api/v0/downloads/requests/{...}` returning DownloadRequest + current attempt + recent attempt history. Mirror the existing actions: cancel, retry, remove, rename.
- Existing `/api/v0/transfers/downloads/...` endpoints stay for one release marked deprecated, with a header redirect. Avoids breaking external scripts immediately.
- `Transfer` DTO gains `RequestId`. New `DownloadRequest` DTO includes embedded `CurrentAttempt` (Transfer DTO).

### 5e. Completed-path resolution

`ResolveCompletedDestinationDirectory` now keys off `DownloadRequest`, not Transfer, so the on-disk location:
- Doesn't change when the source swaps mid-download.
- Can use `{request_name}` token (Phase 3 token lands here for real).
- Uses cached metadata on the request rather than re-reading from each attempt's Transfer.

### 5f. Web

- `src/web/src/components/Transfers/TransferTable.jsx` and friends switch to fetching DownloadRequests. Each row shows: Name (editable inline → renames), current source ("from X"), progress, last error. Expand row shows attempt history with timestamps and reasons. SignalR group key changes from transfer id to request id.
- `src/web/src/lib/transferColumns.js` updated for Request-level columns; "Username" column becomes "Current source".
- Wishlist auto-download path: WishlistService creates the Request with `WishlistItemId` set, name defaulting to `"{search_text} — {filename}"` or just the search text — pick something readable.

### 5g. Risks & sequencing inside Phase 5

1. **In-flight transfers during deploy.** Migration backfill must run before the new code path activates. Existing in-flight transfers get a Request retroactively but their SignalR clients may need a refresh. Acceptable for a release boundary.
2. **External API consumers.** Keep the old `/transfers/downloads` endpoints alive one release.
3. **Removed=true semantics shift.** Audit all consumers of `Removed=true` (`grep Removed`) — it currently means both "user removed it" and "superseded by new attempt". After this change, the first becomes a Request state and the second becomes invisible plumbing. Some queries (`DownloadService.cs:331, :796, :828`) need to be re-examined.
4. **Order of attempt history.** Decide whether `Transfer.Removed` rows count as attempt history (yes — keep them for audit, just hidden by default from request-level queries).

### 5h. Cut-up into reviewable PRs

1. Schema + entity + backfill migration, no behavior change. New table populated; `RequestId` set on existing transfers; new code paths dark.
2. Service + API in parallel (old endpoints still primary).
3. Rescue path switched to add-attempt instead of supersede.
4. Web UI rewritten to consume request endpoints; old endpoints removed in the release *after* that.

---

## What to do this week vs. later

- This release: **Phases 1, 2, 4**. Pure wins for Bas, zero architectural risk.
- Next release: **Phase 3**. Path template lands, default kept conservative for upgrades.
- Release after that: **Phase 5**, in the four-PR sequence above.

Splitting it this way also means `{request_name}` in the path template gets defined before there's a Request entity to fill it — fine; render empty until 5 lands, then enable.
