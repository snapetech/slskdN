# slskdN feature parity and gap plan

Date: 2026-05-04

This document records the current feature/function/network/UI assessment and the implementation plan for closing identified parity, UX, documentation, and validation gaps.

## Current baseline

- Backend build passes with zero warnings.
- Unit tests pass: 3836 tests.
- Web production build passes.
- Full frontend unit tests, backend integration tests, E2E browser tests, and live Soulseek/mesh network interop tests still need to be run after the current remediation series.

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

- Native Soulseek mesh rendezvous was API-only after the runtime update. It now needs explicit UI, privacy language, and tests.
- Discovery surfaces should consistently explain source trust and whether they trigger network calls.

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

- Mesh/DHT/QUIC/service fabric is the highest-complexity area and needs dedicated adverse-network tests.
- Public discovery paths must remain explicit and documented.
- Anonymous network endpoints need an allowlist and abuse-control review.

### Security/privacy

Status: broad controls, needs endpoint-level audit artifact.

Good current properties:

- Mutating controller files use `ValidateCsrfForCookiesOnly`.
- Auth policies and admin-only/JWT-only paths exist.
- Hardening validator exists for dangerous startup combinations.
- Port-scoped CSRF token cookies are used.
- Soulseek mesh rendezvous publication now has an explicit default-off option.

Risks:

- Anonymous endpoints need a documented allowlist.
- Mesh service fabric and HTTP gateway need a focused threat model.
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

Status: partly complete. Swarm visualization is intentionally tied to active swarm jobs rather than restored as a standalone route. Compatibility-only and experimental pages still need periodic visibility review when their owning feature areas change.

### G5. Advanced-feature UX productization

Problem: Several powerful features are admin/API-first.

Plan:

- Add guided “Network Health”, “Improve Downloads”, “Discover Music”, “Share Collection”, “Play Local Files”, and “Join Listening Party” flows.
- Reduce cognitive load in System by grouping experimental/admin panels.
- Add clear “experimental” labels where behavior is not mature.

Status: in progress. MediaCore pod workflow navigation, focus filtering, focused workflow labels, active-card highlighting, reset action, anchors, and per-workflow safety notices are implemented. Pod discovery now keeps read-only discovery actions first and groups public registry mutation controls behind advanced disclosure. Pod join/leave now keeps pending-request review first and groups signed membership event publishing behind advanced disclosure. Pod message signing now keeps verification/statistics first and groups private-key signing/key generation behind advanced disclosure. Pod channel management now keeps channel loading/review first and groups create/edit/delete controls behind advanced disclosure. Pod opinion management now keeps refresh/list/statistics/aggregation actions first and groups opinion publishing plus affinity recalculation behind advanced disclosure. Pod content linking now keeps content search/validation first and groups content-linked pod creation behind advanced disclosure after validation. Pod message storage and backfill now keep stats/search/timestamp review first and group local storage maintenance plus backfill sync behind advanced disclosure. PodCore DHT publishing now keeps metadata retrieval and publishing stats first and groups publish/unpublish controls behind advanced disclosure. Pod membership management now keeps get/verify/statistics first and groups publish, role/ban changes, and cleanup behind advanced disclosure. Pod message routing now keeps deduplication checks and routing stats first and groups send, mark-seen, and cleanup controls behind advanced disclosure. MediaCore descriptor publishing now keeps retrieval/statistics first and groups descriptor publish, batch publish, update, and republish controls behind advanced disclosure. MediaCore ContentID registry and metadata portability now keep resolve/validate/export/conflict-analysis first and group registration/import controls behind advanced disclosure. MediaCore retrieval/dashboard management now keeps stats loading first and groups cache clearing plus global stats reset behind advanced disclosure. Remaining work is simplifying additional advanced MediaCore forms into task-focused panels or progressive disclosure.

### G6. Full validation pass

Problem: Build/unit/web-build are clean, but full frontend, integration, E2E, and network interop are not yet validated in this pass.

Plan:

- Run full `npm test`.
- Run integration tests.
- Run E2E smoke for navigation, search, transfers, messages, player streaming, and System panels.
- Run Soulseek runtime interop contract tests.
- Run mesh/DHT adverse-network smoke tests.

Status: partly complete. Backend tests and lint passed in recent remediation work, and the combined remediation baseline exists. Release validation still needs the explicit web unit/build pass and any live/E2E checks required for the target release.

### G7. Security allowlist and threat model

Problem: Public/anonymous endpoints and mesh/service-fabric surfaces need a current operator-readable audit artifact.

Plan:

- Document all anonymous routes and why they are public.
- Document rate limits and payload limits per public route family.
- Threat-model mesh service fabric, relay, streaming tickets, source feeds, ActivityPub/WebFinger, Solid, and external visualizer.
- Add tests for expected auth/CSRF behavior.

Status: complete for baseline allowlist controls. Anonymous and non-versioned route allowlists are documented and checked by the remediation baseline. Deeper threat-model notes should still be updated when new externally visible mesh, federation, Solid, source-feed, or visualizer surfaces are added.

## Implementation order

1. Continue simplifying MediaCore pod workflow forms into task-focused panels or progressive disclosure.
2. Keep compatibility-only and experimental pages intentionally visible, hidden, or documented as admin-only in `docs/route-ui-parity-matrix.md`.
3. Run release-target validation: backend build/tests, web unit tests, web build, remediation baseline, and any required live/E2E checks.
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
- MediaCore pod workflow index, card-driven focus filtering, focused workflow label, active-card highlight, reset action, anchors, and per-workflow safety notices.

Remaining:

- Simplify individual MediaCore pod forms into task-focused panels or progressive disclosure.
- Add versioned aliases for any additional active legacy-only backend surfaces discovered during feature work.
- Decide whether compatibility-only or experimental pages should stay visible, move behind an experimental label, or be documented as admin-only.
- Run full validation before release: backend build/tests, web unit tests, web build, and the remediation baseline check.
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

Work left before release is validation, not feature remediation:

- Run backend build and unit tests.
- Run web unit tests and web build.
- Run `npm run check:remediation`.
- Review any failures from validation and fix them as discrete defects.

See `docs/REMEDIATION_COMPLETION_REPORT.md` for the consolidated completion status across feature parity, route parity, security hardening, UI safety, and remediation baseline checks.
