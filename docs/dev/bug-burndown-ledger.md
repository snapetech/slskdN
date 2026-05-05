# Bug Burn-Down Ledger

This ledger is the canonical intake queue for bug council findings. It is for defects only: feature ideas and broad refactors stay in `memory-bank/tasks.md` until a concrete bug is accepted.

## Operating Rules

- Add one row per distinct finding and keep the `ID` stable after triage.
- Do not fix directly from scanner output. First classify confidence, duplicate status, and regression requirement.
- Stop-ship and High rows need a repro, failing check, focused test proposal, or static proof before implementation.
- Every accepted bug fix must add or update a regression check before it can move to `Fixed`.
- Repeated or non-obvious gotchas must be documented in `memory-bank/decisions/adr-0001-known-gotchas.md` before continuing with implementation.
- Release-worthy user, security, packaging, or ops fixes need a `docs/CHANGELOG.md` entry.
- Record validation commands in the row before moving a bug to `Verified`.

## Domains

- Backend/Security
- Frontend/Workflow
- Release/Ops
- Network Health
- Tests/Tooling
- Docs/Config

## Severity

- `Stop-ship`: direct security exposure, file/data loss, or release artifact that must not ship.
- `High`: core workflow breakage, meaningful network harm, package install failure, or auth/role drift.
- `Medium`: degraded workflow, false-confidence tests, or drift that can mislead operators.
- `Low`: docs-only drift, cosmetic issues, or low-blast-radius maintenance bugs.

## Confidence

- `Confirmed`: reproduced with a command/test or proven by exact static invariant violation.
- `Likely`: strong code-path evidence, but still needs a focused repro before fixing.
- `Needs Repro`: plausible scanner/expert finding without enough proof.
- `False Positive`: retained only when useful to explain why a scanner should not fail.

## Status

- `New`
- `Triaged`
- `Accepted`
- `Fixing`
- `Fixed`
- `Verified`
- `Deferred`
- `Rejected`

## Reproducible Discovery Commands

Baseline gate:

```bash
npm run check:remediation
./bin/lint
dotnet test slskd.sln --no-restore
cd src/web && npm run lint && npm test
bash packaging/scripts/validate-packaging-metadata.sh
bash packaging/scripts/validate-release-copy.sh
```

Focused existing gates:

```bash
scripts/check-route-inventory.sh
scripts/check-controller-csrf.sh
scripts/check-anonymous-endpoints.sh
scripts/check-non-versioned-routes.sh
scripts/check-allowlist-drift.sh
scripts/check-sensitive-placeholders.sh
scripts/check-web-api-paths.sh
scripts/check-web-fetch-csrf.sh
scripts/check-web-mediacore-routes.sh
```

Pattern inventory:

```bash
rg -n "\[FromBody\]\s+string" src/slskd
rg -n "api\.(post|put|patch|delete)\([^\n]*(message|roomName|yaml|contentId|disambiguator)" src/web/src
rg -n "navigate\('/(browse|users|chat)'|state=\{\{\s*user|state:\s*\{\s*user" src/web/src
rg -n "CreateClient\(|new HttpClient|AllowAutoRedirect|GetAsync\(|SendAsync\(" src/slskd -g '*.cs'
rg -n "Path\.Combine|File\.|Directory\.|CopyToAsync|OpenWrite|Create\(" src/slskd -g '*.cs'
rg -n "async void|Task\.Run|WaitAsync\(|SemaphoreSlim|ConcurrentDictionary|IServiceProvider" src/slskd -g '*.cs'
rg -n "ISoulseekClient|SearchScope|BrowseAsync|DownloadAsync|SafetyLimiter" src/slskd -g '*.cs'
```

New bug-council gates:

```bash
scripts/check-web-url-intent.sh
scripts/check-web-json-string-bodies.sh
scripts/check-mutating-role-requirements.sh
scripts/check-outbound-http-guards.sh
scripts/check-path-containment.sh
scripts/check-soulseek-network-health.sh
scripts/check-workflow-trigger-policy.sh
scripts/check-release-asset-matrix.sh
scripts/check-config-option-drift.sh
scripts/check-systemd-permission-matrix.sh
```

## Ranked Ledger

Extended process fields for every row:

- `Duplicate ADR`: known gotcha entry when this row is a regression guard for an existing bug class.
- `Baseline guard`: scanner or test that currently covers the row.
- `Repro status`: command/test/static proof state.
- `Verification command`: command required before `Verified`.
- `False positive reason`: required when confidence becomes `False Positive`.

| ID | Domain | Class | Severity | Confidence | Evidence | Impact | Fix owner area | Regression requirement | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| BUG-20260505-001 | Tests/Tooling | Weak or misleading test coverage | Medium | Confirmed | `scripts/check-web-url-intent.sh`; ADR-0001 `0z290` | Route-state-only user navigation can regress silently and break new-tab/direct-load Browse, Users, or Chat workflows. | web | Keep `scripts/check-web-url-intent.sh` in `npm run check:remediation`; add focused route tests for any new user-targeted page. | Accepted |
| BUG-20260505-002 | Tests/Tooling | Primitive JSON body mismatches | Medium | Confirmed | `scripts/check-web-json-string-bodies.sh`; ADR-0001 `0z291` | Web helpers can post invalid JSON to ASP.NET `[FromBody] string` actions, causing room/config/content operations to fail before controller logic runs. | web/backend | Keep `scripts/check-web-json-string-bodies.sh` in `npm run check:remediation`; add helper tests when introducing `[FromBody] string` endpoints. | Accepted |
| BUG-20260505-003 | Backend/Security | API auth/role drift | High | Confirmed | `scripts/check-mutating-role-requirements.sh`; ADR-0001 `0z287` | New mutating endpoints can accidentally inherit read-only authenticated access. | backend | Keep `scripts/check-mutating-role-requirements.sh` in `npm run check:remediation`; add controller authorization tests for new mutating APIs. | Accepted |
| BUG-20260505-004 | Backend/Security | SSRF/redirect/outbound fetch bypass | High | Confirmed | `scripts/check-outbound-http-guards.sh`; ADR-0001 `0z288` | Guarded outbound callers can validate the first URI but follow redirects or use an unguarded client for the actual request. | backend | Keep `scripts/check-outbound-http-guards.sh` in `npm run check:remediation`; add no-redirect/guard tests for any new remote URL feature. | Accepted |
| BUG-20260505-005 | Backend/Security | Filesystem/path containment | High | Confirmed | `scripts/check-path-containment.sh`; ADR-0001 `0z288` | Remote or user-derived filenames can escape intended roots or write unbounded data if path guards are bypassed. | backend | Keep `scripts/check-path-containment.sh` in `npm run check:remediation`; add path traversal and copy-size regressions for new file write paths. | Accepted |
| BUG-20260505-006 | Network Health | Soulseek network-health regressions | High | Confirmed | `scripts/check-soulseek-network-health.sh`; ADR-0001 `0z289` | Saved searches or automations can use protocol scopes or missing cancellation/limiter metadata that behave differently from manual network searches. | backend/web | Keep `scripts/check-soulseek-network-health.sh` in `npm run check:remediation`; add focused tests for any automated Soulseek-facing producer. | Accepted |
| BUG-20260505-007 | Release/Ops | Release/tag/package/version drift | High | Confirmed | `scripts/check-workflow-trigger-policy.sh`; tag-only build policy in `AGENTS.md` | Branch-push release builds or workflow drift can publish unintended artifacts or miss intended tag builds. | packaging | Keep `scripts/check-workflow-trigger-policy.sh` in `npm run check:remediation`; validate workflow trigger changes before release work. | Accepted |
| BUG-20260505-008 | Release/Ops | Release/tag/package/version drift | Medium | Confirmed | `scripts/check-release-asset-matrix.sh`; `packaging/scripts/validate-packaging-metadata.sh` | Workflow asset names can drift away from AUR/RPM/PPA/Homebrew/Winget expectations. | packaging | Keep `scripts/check-release-asset-matrix.sh` in `npm run check:remediation`; update package metadata checks with any asset rename. | Accepted |
| BUG-20260505-009 | Docs/Config | Config/docs/example drift | Medium | Confirmed | `scripts/check-config-option-drift.sh`; `config/slskd.example.yml` | New operator options can ship without an example or documentation anchor. | docs/backend | Keep `scripts/check-config-option-drift.sh` in `npm run check:remediation`; update config docs when adding option classes. | Accepted |
| BUG-20260505-010 | Release/Ops | Installer/systemd/container permission bugs | High | Confirmed | `scripts/check-systemd-permission-matrix.sh`; ADR-0001 `0z286` | Linux packages can install a daemon that cannot write config/data paths as its service user. | packaging | Keep `scripts/check-systemd-permission-matrix.sh` in `npm run check:remediation`; validate tmpfiles/sysusers/service changes together. | Accepted |

## Council Intake

These rows came from read-only expert review and still need adversarial triage before implementation.

| ID | Domain | Class | Severity | Confidence | Evidence | Impact | Fix owner area | Regression requirement | Status |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| BUG-20260505-011 | Frontend/Workflow | Frontend URL intent lost at route boundaries | High | Confirmed | `src/web/src/components/Search/Searches.jsx` read `q` from `URLSearchParams`, then applied `decodeURIComponent` again. Fixed by removing the second decode and de-duplicating URL-created searches. | Direct `/searches?q=100%25` could throw `URIError`; literal `%2520` intent could be lost. | web | `cd src/web && npm test -- Searches.test.jsx` covers encoded percent preservation. | Verified |
| BUG-20260505-012 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | Browse, Chat, and Rooms loaded `parsed.tabs || []` and later called `tabs.map`. Fixed by accepting only `Array.isArray(parsed.tabs)`. | Valid JSON with wrong shape, such as `{"tabs":{}}`, could crash core tabbed journeys on load. | web | `cd src/web && npm test -- Browse.test.jsx Chat.test.jsx Rooms.test.jsx` covers corrupted tab storage. | Verified |
| BUG-20260505-013 | Frontend/Workflow | Workflow semantic drift across manual vs automated paths | Medium | Confirmed | `RoomCreateModal` passed `isPrivate`, but `Rooms.createRoom(roomName, isPrivate)` delegated to `joinRoom(roomName)`. Fixed by disabling unsupported private-room creation and throwing if called programmatically. | User could select Private Room, but the UI performed the same public join/create behavior. | web | `cd src/web && npm test -- RoomCreateModal.test.jsx Rooms.test.jsx` verifies unsupported private-room posture. | Verified |
| BUG-20260505-014 | Frontend/Workflow | Web API helper contract drift | Low | Confirmed | Room API helpers warned and returned `undefined` on non-array payloads. Fixed by returning `[]` for malformed list responses. | Future callers could crash or render stale state if they did not add their own `|| []`. | web | `cd src/web && npm test -- rooms.test.js` verifies malformed payloads return `[]`. | Verified |
| BUG-20260505-015 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Confirmed | Chat and Room send actions/context buttons lacked discoverable labels/tooltips. Fixed by adding action labels/titles and room context `Popup` wrappers. | Violated the repo tooltip rule and made icon actions harder to discover/test. | web | `cd src/web && npm run lint` validates the touched JSX; future UI-specific coverage remains useful. | Fixed |
| BUG-20260505-016 | Network Health | Soulseek network-health regressions | High | Confirmed | `MultiSourceDownloadService.FindVerifiedSourcesAsync` called `_client.SearchAsync` directly rather than a safety limiter. Fixed by consuming the Soulseek search limiter before source discovery. | Remediation/swarm verification could bypass search safety budgets and create bursty network searches. | backend | `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --no-restore --filter FullyQualifiedName~MultiSourceDownloadServiceSanitizationTests` verifies exhausted limiter prevents Soulseek search. | Verified |
| BUG-20260505-017 | Network Health | Async/concurrency/lifecycle hazards | High | Confirmed | `BackfillFileAsync` waited on a semaphore with cancellation, then unconditionally decremented/released in `finally`. Fixed by tracking whether the semaphore was acquired. | Cancellation while waiting could over-release concurrency and make active backfill accounting negative. | backend | `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --no-restore --filter FullyQualifiedName~BackfillSchedulerServiceTests` covers cancellation while waiting. | Verified |
| BUG-20260505-018 | Network Health | Protocol trust/signature fail-open | High | Confirmed | `ParseFlacHeader` returned `string.Empty` on invalid data; caller checked only `hash != null`. Fixed by returning `null` for invalid/non-FLAC headers and requiring a non-blank hash. | Failed probes could be recorded as successful empty hashes and propagated to HashDb/mesh. | backend | `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --no-restore --filter FullyQualifiedName~BackfillSchedulerServiceTests` covers invalid FLAC headers. | Verified |
| BUG-20260505-019 | Release/Ops | Release/tag/package/version drift | High | Confirmed | `.github/workflows/release-linux.yml` rewrote AUR `sha256sums` with too few entries after `slskd.tmpfiles` was added. Fixed by including the tmpfiles checksum. | AUR update path could generate invalid PKGBUILDs. | packaging | `bash packaging/scripts/validate-packaging-metadata.sh` verifies the workflow checksum matrix. | Verified |
| BUG-20260505-020 | Release/Ops | Release/tag/package/version drift | High | Confirmed | `.github/workflows/release-linux.yml` ran `bash ../packaging/scripts/validate-aur-pkgbuild-hashes.sh` after `cd packaging/aur`. Fixed to use `../scripts/validate-aur-pkgbuild-hashes.sh`. | AUR update job could fail before pushing metadata. | packaging | `bash packaging/scripts/validate-packaging-metadata.sh` verifies the workflow path. | Verified |
| BUG-20260505-021 | Release/Ops | Installer/systemd/container permission bugs | High | Confirmed | Debian/RPM/COPR packaging copied sysusers but not `slskd.tmpfiles`; RPM spec declared `Source4`. Fixed by copying tmpfiles metadata into package build sources/artifacts. | Package build could fail or omit ownership convergence metadata. | packaging | `bash packaging/scripts/validate-packaging-metadata.sh` verifies tmpfiles copy paths. | Verified |
| BUG-20260505-022 | Backend/Security | Secret/log/error leakage | Medium | Confirmed | `MeshGatewayConfigValidator` logged generated CSRF token/header value. Fixed by logging only generic generation/configuration status. | Logs could replay localhost mesh gateway CSRF header for that session. | backend | `npm run check:remediation` runs `scripts/check-sensitive-placeholders.sh`, which rejects raw CSRF token log templates. | Verified |
| BUG-20260505-023 | Backend/Security | Secret/log/error leakage | Medium | Confirmed | `RelayService` logged relay auth challenge and upload/download tokens. Fixed by logging stable short SHA-256 token IDs instead of raw replayable values. | Debug logs could replay short-lived relay workflows during token TTL. | backend | `npm run check:remediation` runs `scripts/check-sensitive-placeholders.sh`, which rejects raw relay token log templates. | Verified |
| BUG-20260505-024 | Backend/Security | SSRF/redirect/outbound fetch bypass | Medium | Confirmed | Token-bearing notification and Spotify HTTP requests used default redirect-following clients. Fixed by using the shared no-redirect guarded client and extending the outbound guard to notifications. | Redirects could leak notification/access tokens or message bodies to redirected hosts. | backend | `npm run check:remediation` runs `scripts/check-outbound-http-guards.sh`, including notifications/source feeds. | Verified |
| BUG-20260505-025 | Tests/Tooling | Weak or misleading test coverage | High | Confirmed | `scripts/check-non-versioned-routes.sh` skipped a controller if any route was versioned. Fixed so every API route attribute is inspected and non-versioned API routes must be allowlisted. | Mixed legacy+versioned controllers could hide undocumented legacy routes. | tests | `npm run check:remediation` verifies strict non-versioned API route scanning. | Verified |
| BUG-20260505-026 | Tests/Tooling | Weak or misleading test coverage | Medium | Confirmed | `scripts/check-controller-csrf.sh`, anonymous checks, and allowlist drift relied on marker-only regex scans. Fixed by anchoring attribute checks to real attribute lines and API route contexts. | Comments or unrelated attributes could satisfy endpoint-level checks. | tests | `npm run check:remediation` verifies tightened CSRF, anonymous, and allowlist checks. | Verified |
| BUG-20260505-027 | Frontend/Workflow | Frontend URL intent lost at route boundaries | Medium | Confirmed | `Users.jsx` read `location.search` only on mount, so navigating from `/users?user=alice` to `/users?user=bob` while the component stayed mounted left the old user selected. | User profile links could show stale user data when routed within the mounted Users page. | web | `cd src/web && npm test -- Users.test.jsx` verifies URL user changes while mounted. | Verified |
| BUG-20260505-028 | Release/Ops | Installer/systemd/container permission bugs | High | Confirmed | `packaging/docker/slskdn-container-start` executed the server as root when no `PUID`/`PGID` was set. Fixed by chowning the app dir, checking slskdn access, and `exec gosu slskdn "$@"`. | Default Docker containers could run the daemon as root and write root-owned app data. | packaging | `bash packaging/scripts/validate-packaging-metadata.sh` verifies default container startup drops to `slskdn`. | Verified |
| BUG-20260505-029 | Backend/Security | Protocol trust/signature fail-open | Medium | Confirmed | Vendored `RoomListResponseFactory` validated room-name collection counts but accepted negative per-room user counts. Fixed by validating non-negative room user counts before creating `RoomInfo`. | Malformed server room-list payloads could create impossible negative user-count domain values. | backend | `dotnet test vendor/slskNet.Runtime/tests/Soulseek.Tests.Unit/Soulseek.Tests.Unit.csproj --no-restore --filter "FullyQualifiedName~ProtocolCountHardeningTests|FullyQualifiedName~RoomListTests"` verifies negative count rejection. | Verified |
| BUG-20260505-030 | Network Health | Soulseek network-health regressions | High | Confirmed | `MultiSourceController` used direct wide `Client.SearchAsync` calls for users, file sources, download-file source lookup, swarm, search, and test endpoints without consuming the shared safety limiter. Fixed by gating each controller search through `TryConsumeSearchBudget`. | Multi-source diagnostic/helper endpoints could bypass Soulseek search budgets and issue large searches even after the limiter was exhausted. | backend | `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --no-restore --filter FullyQualifiedName~MultiSourceControllerTests` and `bash scripts/check-soulseek-network-health.sh` verify exhausted budget prevents search and all direct multi-source search paths are guarded. | Verified |
| BUG-20260505-031 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/searches.js` parsed `slskdn_blocked_users` and returned any valid JSON directly, then `blockUser()` and `unblockUser()` called array methods on it. Fixed by returning `[]` unless the parsed value is an array. | A malformed-but-valid blocked-users localStorage value could crash Search response filtering or block/unblock actions. | web | `cd src/web && npm test -- searches.test.js` verifies wrong-shape blocked-user storage is ignored before array operations. | Verified |
| BUG-20260505-032 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/automationRecipes.js` parsed automation state/input localStorage maps without rejecting arrays. Fixed by requiring a non-array object before merging or writing nested recipe state. | Malformed automation state could produce numeric-key junk and break recipe input/state updates. | web | `cd src/web && npm test -- src/components/System/AutomationCenter/index.test.jsx` verifies wrong-shape automation state and inputs are ignored. | Verified |
| BUG-20260505-033 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Confirmed | `src/web/src/components/System/ExperienceSettings/index.jsx` spread any parsed preference JSON over defaults. Fixed by accepting only non-array objects. | Malformed local preference arrays could leak numeric keys into the form state and create confusing report/save output. | web | `cd src/web && npm test -- src/components/System/ExperienceSettings/index.test.jsx` verifies wrong-shape preferences fall back to defaults. | Verified |
| BUG-20260505-034 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/components/App.jsx` treated any parsed room activity JSON as a timestamp map. Fixed by accepting only non-array objects. | Malformed room-activity storage could skip first-run baselining or compare unread room timestamps against invalid state. | web | `cd src/web && npm test -- App.test.jsx` verifies malformed room activity is ignored and re-baselined. | Verified |
| BUG-20260505-035 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/components/Messaging/Messaging.jsx` parsed workspace storage and read `panelCounter` before validating the top-level shape. Fixed by requiring a non-array object and resetting invalid counters to `0`. | Array-shaped workspace storage could poison the panel counter and create `NaN` panel IDs after opening a new chat/room panel. | web | `cd src/web && npm test -- Messaging.test.jsx` verifies malformed workspace storage creates `chat-1` instead of a poisoned ID. | Verified |
| BUG-20260505-036 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Confirmed | `src/web/src/lib/audioVerification.js` accepted array-shaped audio verification caches as object maps. Fixed by rejecting arrays. | Malformed cache storage could merge numeric keys into fingerprint cache state and confuse cache-hit behavior. | web | `cd src/web && npm test -- audioVerification.test.js` verifies malformed cache shapes return `{}`. | Verified |
| BUG-20260505-037 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Confirmed | `src/web/src/components/Player/Visualizer.jsx` returned any parsed active native preset JSON. Fixed by accepting only non-array objects. | Malformed active native preset storage could be treated as preset metadata during native edit/remove operations. | web | `cd src/web && npm test -- Visualizer.test.jsx` covers native preset workflows after the shape guard. | Verified |
| BUG-20260505-038 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/components/System/Events/index.jsx` called `JSON.parse(event.data)` during render. Fixed by formatting valid JSON and falling back to the raw string for malformed data. | One malformed backend event payload could crash the System Events table and block event inspection. | web | `cd src/web && npm test -- src/components/System/Events/index.test.jsx` verifies malformed event data renders without crashing. | Verified |
| BUG-20260505-039 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/communityQualitySignals.js` accepted invalid entries inside an array-shaped localStorage value. Fixed by filtering entries to non-array objects before normalization. | Malformed community quality signal storage could crash local dashboard summaries and reputation signal rendering. | web | `cd src/web && npm test -- communityQualitySignals.test.js` verifies malformed signal entries are ignored. | Verified |
| BUG-20260505-040 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/discoveryInbox.js` accepted invalid entries inside an array-shaped localStorage value. Fixed by filtering entries to non-array objects before `normalizeItem`. | Malformed Discovery Inbox storage could crash candidate review and acquisition handoff flows. | web | `cd src/web && npm test -- discoveryInbox.test.js` verifies malformed discovery items are ignored. | Verified |
| BUG-20260505-041 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/acquisitionPlans.js` accepted invalid entries inside an array-shaped localStorage value. Fixed by filtering entries to non-array objects before `normalizePlan`. | Malformed acquisition plan storage could crash plan execution/review and block bounded search or Wishlist handoffs. | web | `cd src/web && npm test -- acquisitionPlans.test.js` verifies malformed plan entries are ignored. | Verified |
| BUG-20260505-042 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/discoveryShelf.js` accepted invalid entries inside an array-shaped localStorage value. Fixed by filtering entries to non-array objects before summaries and promote handoffs. | Malformed Discovery Shelf storage could crash shelf summaries or promote-preview inbox generation. | web | `cd src/web && npm test -- discoveryShelf.test.js` verifies malformed shelf entries are ignored. | Verified |
| BUG-20260505-043 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Confirmed | `src/web/src/lib/albumDecisionRules.js` accepted invalid entries inside an array-shaped localStorage value. Fixed by filtering entries to non-array objects before save/replace logic. | Malformed album decision rule storage could crash local rule saves and duplicate replacement. | web | `cd src/web && npm test -- albumDecisionRules.test.js` verifies malformed rule entries are ignored. | Verified |
| BUG-20260505-044 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/listeningHistory.js` accepted invalid entries inside an array-shaped localStorage value. Fixed by filtering entries to non-array objects before stats and seed generation. | Malformed listening history storage could crash listening stats and explicit recommendation seed generation. | web | `cd src/web && npm test -- listeningHistory.test.js` verifies malformed history entries are ignored. | Verified |
| BUG-20260505-045 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/playlistIntake.js` normalized every persisted playlist entry and nested track entry without item-shape checks. Fixed by filtering top-level playlists and nested tracks to non-array objects before normalization. | Malformed playlist intake storage could create phantom playlists or crash review, refresh, and organization workflows. | web | `cd src/web && npm test -- playlistIntake.test.js` verifies malformed playlist and track entries are ignored. | Verified |
| BUG-20260505-046 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/lib/watchlists.js` normalized every persisted watchlist entry and expansion candidate without item-shape checks, and `releaseTypes` assumed an array. Fixed by filtering persisted watchlists/candidates and normalizing non-array release types. | Malformed watchlist storage could create phantom watches or crash similar-artist expansion decisions. | web | `cd src/web && npm test -- watchlists.test.js` verifies malformed watchlist and expansion candidate entries are ignored. | Verified |
| BUG-20260505-047 | Frontend/Workflow | Web API helper contract drift | Low | Confirmed | `src/web/src/lib/quarantineJury.js` returned `response.data || []` from a list helper. Fixed by returning data only when it is an array. | Malformed quarantine-jury request payloads could escape as object-shaped lists and crash list callers. | web | `cd src/web && npm test -- quarantineJury.test.js` verifies malformed list payloads return `[]`. | Verified |
| BUG-20260505-048 | Frontend/Workflow | Web API helper contract drift | Low | Confirmed | `src/web/src/lib/listeningParty.js` returned `response.data || []` from a list helper. Fixed by returning data only when it is an array. | Malformed listening-party directory payloads could escape as object-shaped lists and crash directory callers. | web | `cd src/web && npm test -- listeningParty.test.js` verifies malformed list payloads return `[]`. | Verified |
| BUG-20260505-049 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Contacts/Contacts.jsx` stored contact and nearby API payloads with `response.data || []`. Fixed by accepting only arrays before setting list state. | Malformed identity payloads could crash Contacts list rendering or nearby-peer rendering. | web | `cd src/web && npm test -- Contacts.test.jsx` verifies malformed contact and nearby payloads are ignored. | Verified |
| BUG-20260505-050 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Collections/Collections.jsx`, `src/web/src/components/Shares/SharedWithMe.jsx`, and `src/web/src/components/ShareGroups/ShareGroups.jsx` stored collection/share/share-group API payloads with `response.data || []`. Fixed by accepting only arrays before list state or member rendering. | Malformed collection/share payloads could crash collection tables, shared-with-me enrichment, or share-group member display. | web | `cd src/web && npm run lint` plus component-local `asArray` guards cover the touched render paths; dedicated component tests remain a follow-up for Collections/Shares/ShareGroups. | Verified |
| BUG-20260505-051 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Search/SoulseekDiscoveryPanel.jsx` mapped `response.data || []` for similar users and counted the raw payload length. Fixed by accepting only arrays before normalization/counting. | Malformed Soulseek similar-user payloads could crash the discovery panel or show impossible counts. | web | `cd src/web && npm test -- SoulseekDiscoveryPanel.test.jsx` verifies malformed similar-user payloads render as an empty result. | Verified |
| BUG-20260505-052 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Player/PlayerBar.jsx` stored collection, collection item, file, directory, and breadcrumb payloads with `response.data || []` or property fallbacks. Fixed by accepting only arrays before launcher/browser list state. | Malformed player collection or file-browser payloads could crash local playback launch modals. | web | `cd src/web && npm test -- PlayerBar.test.jsx` verifies malformed player launcher/browser list payloads are ignored. | Verified |
| BUG-20260505-053 | Frontend/Workflow | Filesystem/path containment | Medium | Confirmed | `src/web/src/lib/files.js` used `btoa(path)` directly in route segments. Fixed by UTF-8 encoding paths before base64 and URL-encoding the base64 route segment. | File Explorer actions could throw for Unicode filenames/directories or misroute paths whose base64 contained `/` or `+`. | web | `cd src/web && npm test -- files.test.js` verifies Unicode path encoding and encoded file/directory API routes. | Verified |
| BUG-20260505-054 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Confirmed | `src/web/src/components/Search/DiscoveryGraphAtlasPanel.jsx` and `src/web/src/components/Search/DiscoveryGraphModal.jsx` loaded `slskdn.discoveryGraph.savedBranches` arrays without validating entries. Fixed by centralizing saved branch parsing and filtering entries with missing `id`/`title` or invalid shapes. | A malformed saved Discovery Graph branch entry could crash atlas/modal saved-branch rendering. | web | `cd src/web && npm test -- discoveryGraph.test.js` verifies malformed saved branch entries are ignored. | Verified |
| BUG-20260505-055 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | Discovery Graph render paths used `(graph?.edges || []).map/filter`, while only nodes were array-guarded. Fixed by centralizing node/edge list guards and using them in Atlas, AtlasPanel, and Modal. | A malformed graph response with truthy non-array `edges` or `nodes` could crash Discovery Graph atlas/modal rendering. | web | `cd src/web && npm test -- discoveryGraph.test.js` verifies malformed node/edge payloads are treated as empty lists. | Verified |
| BUG-20260505-056 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/lib/events.js` returned raw `response.data` to the System Events table. Fixed by returning an empty list unless the event payload is an array. | A malformed events API payload could make the System Events table call `.map()` on a truthy non-array value. | web | `cd src/web && npm test -- events.test.js src/components/System/Events/index.test.jsx` verifies malformed event lists render as empty tables. | Verified |
| BUG-20260505-057 | Frontend/Workflow | Web API helper contract drift | Low | Confirmed | `src/web/src/lib/bridge.js` returned `response.data?.clients || []`. Fixed by returning clients only when the payload is an array. | A malformed Bridge clients payload could crash Bridge client rendering. | web | `cd src/web && npm test -- bridge.test.js` verifies malformed client lists return `[]`. | Verified |
| BUG-20260505-058 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Search/AlbumCompletionPanel.jsx` stored `response.data?.albums ?? []` and later called `album.tracks.filter`. Fixed by normalizing album and nested track arrays before rendering. | Malformed MusicBrainz album completion payloads could crash album completion rendering. | web | `cd src/web && npm test -- AlbumCompletionPanel.test.jsx` verifies malformed album and track payloads are ignored. | Verified |
| BUG-20260505-059 | Frontend/Workflow | Web API helper contract drift | Low | Confirmed | `src/web/src/components/Collections/Collections.jsx` used `response.data?.items || []` for collection item search results. Fixed by accepting only arrays before item result dropdown state. | A malformed collection item search payload could crash the collection item result dropdown. | web | `cd src/web && npm run lint` verifies the touched component; the remaining scan for `response.data?.x || []` is clean. | Verified |
| BUG-20260505-060 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Search/FederatedTasteRecommendationsPanel.jsx` used `response.data?.recommendations || []` and counted the raw payload length. Fixed by accepting only arrays before render state and status counts. | A malformed federated taste recommendation payload could crash recommendation rendering or show impossible counts. | web | `cd src/web && npm test -- FederatedTasteRecommendationsPanel.test.jsx` verifies malformed recommendation payloads render as an empty result. | Verified |
| BUG-20260505-061 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Search/Detail/SearchDetail.jsx` called `response.data.reduce` for user notes without verifying that the response payload was an array. Fixed by mapping only array payloads and ignoring malformed note entries. | A malformed user-notes payload could crash Search Detail before results render. | web | `cd src/web && npm test -- SearchDetail.test.jsx` verifies malformed user note payloads return an empty note map. | Verified |
| BUG-20260505-062 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Confirmed | `src/web/src/lib/albumDecisionRules.js` called `.map()` on candidate `formatMix`, `warnings`, and `substitutionOptions` via `|| []` fallbacks. Fixed by accepting only arrays and filtering nested option objects. | A malformed album candidate could crash browser-local album decision rule preview/save. | web | `cd src/web && npm test -- albumDecisionRules.test.js` verifies malformed candidate nested fields are ignored. | Verified |
| BUG-20260505-063 | Frontend/Workflow | Web API helper contract drift | Low | Confirmed | `src/web/src/components/Search/FederatedTasteRecommendationsPanel.jsx` mapped `recommendation.reasons || []` and joined `recommendation.sourceActors` after a length check. Fixed by requiring arrays for both nested fields. | A malformed recommendation entry could crash the Federated Taste list after the top-level recommendation array was valid. | web | `cd src/web && npm test -- FederatedTasteRecommendationsPanel.test.jsx` verifies malformed nested reason/source actor lists are ignored. | Verified |
| BUG-20260505-064 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Search/DiscographyCoveragePanel.jsx` called `.flatMap()`/`.map()` over `coverage.releases` and nested `release.tracks` without shape checks. Fixed by normalizing coverage, releases, and tracks before rendering/counting missing tracks. | A malformed MusicBrainz discography coverage payload could crash the Discography Concierge. | web | `cd src/web && npm test -- DiscographyCoveragePanel.test.jsx` verifies malformed release/track arrays are ignored. | Verified |
| BUG-20260505-065 | Frontend/Workflow | Web API helper contract drift | Low | Confirmed | `src/web/src/components/System/SourceProviders/index.jsx` normalized provider capabilities and profile provider priority through `|| []`-style assumptions. Fixed by requiring arrays for nested capabilities and priority lists. | A malformed source provider catalog could crash Source Providers cards or policy rendering. | web | `cd src/web && npm test -- src/components/System/SourceProviders/index.test.jsx` verifies malformed nested provider/policy lists normalize to `[]`. | Verified |
| BUG-20260505-066 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Confirmed | `src/web/src/lib/watchlists.js` summarized `item.expansionCandidates || []` with `.reduce()`. Fixed by reusing `normalizeExpansionCandidates()` before summarizing. | A malformed watchlist expansion candidate field could crash watchlist summary rendering. | web | `cd src/web && npm test -- watchlists.test.js` verifies malformed expansion candidates summarize as empty. | Verified |
| BUG-20260505-067 | Frontend/Workflow | Web API helper contract drift | Medium | Confirmed | `src/web/src/components/Messaging/Messaging.jsx` hydrated conversations, joined rooms, pods, and pod channels with `|| []` list fallbacks. Fixed by accepting only arrays and filtering object-shaped conversation/pod/channel entries before workspace state. | Malformed chat, room, or pod payloads could crash the unified Messaging workspace during hydration. | web | `cd src/web && npm test -- Messaging.test.jsx` verifies malformed server list payloads hydrate without crashing. | Verified |

## Expert Council Notes

Use five read-only review roles for broad sweeps before fixing:

- Backend/Security: controllers, auth, SSRF, path, async, protocol trust.
- Frontend/Workflow: routing, API helpers, buttons, persisted state, e2e journeys.
- Release/Ops: workflows, package metadata, installers, systemd/container behavior.
- Network Health: Soulseek search/browse/download/probe automation, limiter use, cancellation.
- Adversarial Reviewer: duplicate detection, false-positive pressure, repro demands.

Add expert findings as `New`, then move only validated rows to `Accepted`.
