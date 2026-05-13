# slskdN feature parity and gap plan

Date: 2026-05-04

This document records the current feature/function/network/UI assessment and the implementation plan for closing identified parity, UX, documentation, and validation gaps.

## Current baseline

- Full backend `dotnet test --no-restore` passed on 2026-05-13: `67/67`
  smoke, `4056/4056` unit, and `276/276` integration tests.
- Full frontend unit tests passed on 2026-05-13: `131/131` files and
  `716/716` tests.
- Web production build passed on 2026-05-13.
- `./bin/lint` passed on 2026-05-13.
- The remediation baseline passed all substantive checks on 2026-05-13 and
  stopped only at the release branch sync guard because local `main` was ahead
  of `origin/main`.

## Product parity summary

slskdN is not a minimal slskd fork. It contains the base slskd Soulseek daemon surface plus a large set of native subsystems: mesh/DHT, VirtualSoulfind, SongID, MediaCore, Discovery Graph, collections/sharing, integrated player, social/federated surfaces, Solid, telemetry, automation, and source integrations.

Core slskd parity appears strong:

- Search, browse, downloads, uploads, shares, users, rooms, private messages, options, server state, logs, events, sessions, and compatibility APIs are present.
- Dedicated compatibility controllers exist for downloads, library, rooms, search, server, and users.
- Web UI exposes the common slskd flows as first-class navigation.

slskdN-native breadth is high, but UX parity is uneven:

- Backend/API coverage is broader than first-class UI coverage.
- Advanced features are often available through System panels or APIs rather than guided product flows.
- Some older UI modules appear superseded or orphaned and should be cleaned up or intentionally restored.

## Feature/function inventory

### Core Soulseek

Status: strong.

Implemented surfaces:

- Search API/UI and search SignalR hub.
- Browse API/UI with tabbed browsing.
- Transfer API/UI for downloads and uploads.
- Shares API/UI and share scan state.
- Rooms, private conversations, and unified Messages workspace.
- Users, notes, user cards, user stats, and reputation badges.
- Options/session/server/logs/events.

Risks:

- Real protocol compatibility still needs live/stub Soulseek interop testing after the vendored runtime sync.
- Compatibility APIs should be route-inventoried and compared against downstream client expectations.

### Transfers and acquisition

Status: strong for common flows, advanced for specialist flows.

Implemented:

- Downloads/uploads.
- Auto-replace.
- Accelerated/multisource downloads.
- Rescue mode.
- Source ranking.
- Fairness, tracing, playback-aware scheduling, swarm analytics.

Risks:

- Rescue and multisource behavior should get targeted integration tests with degraded peers.
- Advanced APIs are not all surfaced as user-friendly workflows.

### Search/discovery

Status: strong.

Implemented:

- Search filters and ranking.
- Wishlist/background search.
- SongID.
- MusicBrainz panels.
- Discovery Graph.
- Soulseek native discovery.
- Federated taste recommendations.

Risks:

- Native Soulseek mesh rendezvous now has System UI, privacy language, a
  disabled-by-default gate, and tests; keep live behavior behind explicit
  operator opt-in.
- Discovery surfaces should consistently explain source trust and whether they
  trigger network calls.

### Player/media UX

Status: strong but complex.

Implemented:

- Footer-safe player drawer.
- Queue, transport controls, media session support.
- Local streaming through `/api/v0/streams/{contentId}`.
- Equalizer, lyrics, spectrum/scope analyzer.
- Butterchurn/MilkDrop visualizer and experimental native WebGL2/WebGPU engines.
- External visualizer launcher.
- ListenBrainz now-playing/scrobble with browser-local token.
- Listening history, ratings, discovery shelf, radio, crossfade, karaoke/vocal reduction, and Document Picture-in-Picture.

Risks:

- External visualizer is a host process launch surface and must stay opt-in/configured.
- Browser-local tokens are convenient but not strong secret storage.
- Streaming and visualizer flows need browser/E2E testing across supported browsers.
- Native MilkDrop engines should remain labeled experimental until broader device parity is proven.

### Network/mesh

Status: broad and high-risk.

Implemented:

- Soulseek listener and peer/file transfer paths.
- Web HTTP/HTTPS/UDS.
- SignalR hubs.
- DHT rendezvous.
- Shared UDP listener.
- UDP/QUIC mesh overlay.
- Relay controller/agent mode.
- Mesh service fabric.
- NAT detection and network health surfaces.

Risks:

- Mesh/DHT/QUIC/service fabric is the highest-complexity area and needs
  dedicated adverse-network tests.
- Public discovery paths must remain explicit and documented.
- Anonymous network endpoints are allowlisted and checked; keep abuse-control
  rationale current when public surfaces change.

### Security/privacy

Status: baseline controls and route audit artifacts are in place; continue
focused threat-model updates for new externally visible surfaces.

Good current properties:

- Mutating controller files use `ValidateCsrfForCookiesOnly`.
- Auth policies and admin-only/JWT-only paths exist.
- Hardening validator exists for dangerous startup combinations.
- Port-scoped CSRF token cookies are used.
- Soulseek mesh rendezvous publication now has an explicit default-off option.

Risks:

- Anonymous endpoints need the documented allowlist to stay synchronized with
  route changes.
- Mesh service fabric and HTTP gateway threat-model notes should be kept current
  when externally visible behavior changes.
- Source integrations and MediaCore retrieval paths need SSRF/path traversal verification.
- Logs, diagnostics, reports, metrics, and route debugging must avoid leaking sensitive environment details.

## Identified gaps

### G1. Mesh rendezvous UX

Problem: Backend/client functions existed for Soulseek interest-based mesh rendezvous, but there was no obvious UI for status, privacy warning, add/remove interest, or discovered users.

Plan:

- Add backend status endpoint.
- Add web client helper.
- Add System -> Mesh panel card with privacy warning, status, add/remove controls, and user list.
- Add tests.

Status: complete. Backend status endpoint, System -> Mesh UI controls, privacy framing, disabled-by-default gate, and tests are implemented.

### G2. Generated route inventory

Problem: The backend has more than 100 controller files. Manual route review is fragile.

Plan:

- Add a script or build target that emits route, method, auth, CSRF, anonymous, and versioning metadata.
- Commit generated `docs/system-surfaces-current.md`.
- Mark each route as UI-called, external/API-only, or anonymous-public.

Status: complete. `scripts/check-route-inventory.sh` generates and verifies `docs/system-surfaces-current.md`, and the remediation baseline keeps the generated route inventory fresh.

### G3. API version consistency

Problem: Most APIs are under `/api/v0`, but some are plain `/api/...`.

Plan:

- Inventory non-versioned APIs.
- Classify as legacy compatibility or inconsistency.
- Add versioned aliases for inconsistent routes.
- Preserve old routes where compatibility matters.

Status: complete for the active remediation tranche. Versioned aliases were added for active legacy-only native slskdN, VirtualSoulfind, Audio, Discography, Label Crate, and Library Health route families while preserving compatibility routes.

### G4. Stale/orphan UI modules

Problem: Some components appear superseded or disconnected, including older pod/chat/room menu modules and SwarmVisualization.

Plan:

- Generate import graph.
- Remove truly orphaned code.
- Reconnect intentionally hidden surfaces.
- Add route smoke tests for all top-level routes.

Status: partly complete. Swarm visualization is intentionally tied to active
swarm jobs rather than restored as a standalone route. Top-level route smoke
coverage is now in `src/web/src/components/App.test.jsx`. Compatibility-only
and experimental pages still need periodic visibility review when their owning
feature areas change.

### G5. Advanced-feature UX productization

Problem: Several powerful features are admin/API-first.

Plan:

- Add guided “Network Health”, “Improve Downloads”, “Discover Music”, “Share Collection”, “Play Local Files”, and “Join Listening Party” flows.
- Reduce cognitive load in System by grouping experimental/admin panels.
- Add clear “experimental” labels where behavior is not mature.

Status: in progress. MediaCore pod workflow navigation, focus filtering, focused workflow labels, active-card highlighting, reset action, anchors, and per-workflow safety notices are implemented. Pod discovery now keeps read-only discovery actions first and groups public registry mutation controls behind advanced disclosure. Pod join/leave now keeps pending-request review first and groups signed membership event publishing behind advanced disclosure. Pod message signing now keeps verification/statistics first and groups private-key signing/key generation behind advanced disclosure. Pod channel management now keeps channel loading/review first and groups create/edit/delete controls behind advanced disclosure. Pod opinion management now keeps refresh/list/statistics/aggregation actions first and groups opinion publishing plus affinity recalculation behind advanced disclosure. Pod content linking now keeps content search/validation first and groups content-linked pod creation behind advanced disclosure after validation. Pod message storage and backfill now keep stats/search/timestamp review first and group local storage maintenance plus backfill sync behind advanced disclosure. PodCore DHT publishing now keeps metadata retrieval and publishing stats first and groups publish/unpublish controls behind advanced disclosure. Pod membership management now keeps get/verify/statistics first and groups publish, role/ban changes, and cleanup behind advanced disclosure. Pod message routing now keeps deduplication checks and routing stats first and groups send, mark-seen, and cleanup controls behind advanced disclosure. MediaCore descriptor publishing now keeps retrieval/statistics first and groups descriptor publish, batch publish, update, and republish controls behind advanced disclosure. MediaCore descriptor retrieval now keeps cached single lookup first and groups fresh DHT lookup plus batch retrieval behind advanced disclosure with network-impact guidance. MediaCore ContentID registry and metadata portability now keep resolve/validate/export/conflict-analysis first and group registration/import controls behind advanced disclosure; ContentID examples now populate read-first resolve/validation fields along with advanced registration fields. MediaCore retrieval/dashboard management now keeps stats loading first and groups cache clearing plus global stats reset behind advanced disclosure. MediaCore perceptual hashing now keeps similarity review/statistics as the default path and groups raw audio/image hash computation behind advanced disclosure. MediaCore fuzzy matching now keeps pairwise perceptual/text similarity first and groups candidate search behind advanced disclosure. System tabs now label admin and experimental panels directly in the navigation, and focused tests verify the expected panels carry those labels. Remaining work is broader G5 productization: guided flows rather than more obvious MediaCore mutation-form disclosure or System label coverage.

### G6. Full validation pass

Problem: Build/unit/web-build are clean, but full frontend, integration, E2E, and network interop are not yet validated in this pass.

Plan:

- Run full `npm test`.
- Run integration tests.
- Run E2E smoke for navigation, search, transfers, messages, player streaming, and System panels.
- Run Soulseek runtime interop contract tests.
- Run mesh/DHT adverse-network smoke tests.

Status: partly complete. Full frontend unit tests (`131/131` files, `716/716` tests), frontend production build, full backend `dotnet test --no-restore` (`67/67` smoke, `4056/4056` unit, `276/276` integration), and `./bin/lint` passed on 2026-05-13. The remediation baseline passed all substantive checks and stopped only at the release branch sync guard because local `main` is ahead of `origin/main`. Release validation still needs any live/E2E checks required for the target release after the branch is pushed.

### G7. Security allowlist and threat model

Problem: Public/anonymous endpoints and mesh/service-fabric surfaces need a current operator-readable audit artifact.

Plan:

- Document all anonymous routes and why they are public.
- Document rate limits and payload limits per public route family.
- Threat-model mesh service fabric, relay, streaming tickets, source feeds, ActivityPub/WebFinger, Solid, and external visualizer.
- Add tests for expected auth/CSRF behavior.

Status: complete for baseline allowlist controls. Anonymous and non-versioned route allowlists are documented and checked by the remediation baseline. Deeper threat-model notes should still be updated when new externally visible mesh, federation, Solid, source-feed, or visualizer surfaces are added.

## Implementation order

1. Continue broader G5 guided-flow productization when a concrete guided flow is prioritized.
2. Keep compatibility-only and experimental pages intentionally visible, hidden, or documented as admin-only in `docs/route-ui-parity-matrix.md`.
3. Run any required live/E2E checks for the target release after local commits are pushed.
4. Fix concrete validation failures as targeted defects.
5. Update `docs/network-privacy-security-surfaces.md` whenever new externally visible mesh, federation, Solid, source-feed, or visualizer surfaces are added.

## Completion criteria

- Every top-level feature has backend/API status, web UI status, test status, and doc status.
- All public/anonymous routes are documented with abuse controls.
- All top-level web routes have smoke coverage.
- Core slskd compatibility routes are checked against downstream expectations.
- Mesh rendezvous is explicitly opt-in, visible, and reversible from the UI.
- Full validation suite results are documented.

## Implemented remediation baseline checks

- `scripts/check-route-inventory.sh` verifies that `docs/system-surfaces-current.md` matches the current controller route attributes, ignoring only the generated timestamp.
- `scripts/check-web-api-paths.sh` prevents shared web API client calls from embedding `/api` or `/api/v0` into paths that already use the `/api/v0` base URL.
- `scripts/check-web-fetch-csrf.sh` prevents mutating direct fetch calls from using `session.authHeaders()` without CSRF opt-in.
- `scripts/check-remediation-baseline.sh` runs both checks for route/API remediation reviews.

## Remediation review workflow

Use `docs/REMEDIATION_REVIEW_CHECKLIST.md` as the operator checklist for future remediation passes. The root package exposes `npm run check:remediation`, `npm run check:routes`, and `npm run check:web-api-paths` for the generated baseline checks.

## Current remediation status snapshot

Implemented:

- Mesh rendezvous backend, client, System UI, privacy gate, and tests.
- Federation diagnostics backend, client, System UI card, and tests.
- Route inventory generation, freshness check, and web API path linting.
- First active legacy route alias tranche for Library Health and job-specific controllers.
- Library Health web client double-prefix fix and tests.
- MediaCore pod route normalization and tests.
- Direct fetch CSRF opt-in for mutating pod and port-forwarding helpers.
- MediaCore pod workflow index, card-driven focus filtering, focused workflow label, active-card highlight, reset action, anchors, per-workflow safety notices, and read-only-first advanced controls.
- System admin and experimental panel labels.

Remaining:

- Add versioned aliases for any additional active legacy-only backend surfaces discovered during feature work.
- Keep compatibility-only and experimental pages visible, labeled, hidden, or documented as admin-only as feature ownership changes.
- Maintain the top-level route smoke table in `src/web/src/components/App.test.jsx`
  when adding or removing primary web routes.
- Push local commits before any release-tag validation so the remediation sync guard can pass.
- Run any required live/E2E checks for the target release.
- Add similar focused checks when a fixed regression is cheap to encode as a script.

## Additional backend hardening checks

- `scripts/check-controller-csrf.sh` gates mutating controller CSRF coverage.
- `scripts/check-anonymous-endpoints.sh` gates anonymous controller documentation through `docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md`.

- `scripts/check-non-versioned-routes.sh` gates undocumented non-versioned controller routes through `docs/NON_VERSIONED_ROUTE_ALLOWLIST.md`.

- `scripts/check-allowlist-drift.sh` keeps anonymous and non-versioned route allowlists synchronized with current controller files and attributes.

- `scripts/check-sensitive-placeholders.sh` scans remediation docs/tests for high-confidence secret and private-key patterns.

- `scripts/check-remediation-script-registry.sh` ensures focused remediation checks are executable and included in the combined baseline.

- `scripts/check-remediation-doc-commands.sh` verifies remediation docs reference existing check scripts and npm aliases.

## Completion pass status

Completed in the final remediation pass:

- Fixed combined remediation baseline script wiring so all focused checks run through the repository root.
- Added versioned aliases for the remaining active legacy-only native slskdN, VirtualSoulfind, and Audio controllers.
- Extended route alias tests to cover all active legacy route aliases.
- Regenerated `docs/system-surfaces-current.md` after the alias tranche.

Intentional remaining non-versioned routes are now compatibility shims, protocol-required surfaces, OAuth callback compatibility, or documented legacy compatibility routes retained alongside versioned aliases.

Work left before release is release coordination and any target-specific
live/E2E checks, not planned feature remediation:

- Push local commits before cutting a release tag so the remediation sync guard
  can pass.
- Run any required live/E2E checks for the target release.
- Review any failures from those checks and fix them as discrete defects.

See `docs/REMEDIATION_COMPLETION_REPORT.md` for the consolidated completion status across feature parity, route parity, security hardening, UI safety, and remediation baseline checks.
