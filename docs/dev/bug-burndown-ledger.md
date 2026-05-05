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
| BUG-20260505-011 | Frontend/Workflow | Frontend URL intent lost at route boundaries | High | Likely | `src/web/src/components/Search/Searches.jsx` reads `q` from `URLSearchParams`, then applies `decodeURIComponent` again. | Direct `/searches?q=100%25` can throw `URIError`; literal `%2520` intent can be lost. | web | Add tests for `/searches?q=100%25`, malformed percent input, and literal encoded percent preservation; remove second decode if reproduced. | New |
| BUG-20260505-012 | Frontend/Workflow | UI action wiring/state persistence bugs | Medium | Likely | Browse, Chat, and Rooms load `parsed.tabs || []` and later call `tabs.map`. | Valid JSON with wrong shape, such as `{"tabs":{}}`, can crash core tabbed journeys on load. | web | Add corrupted-shape localStorage tests for Browse, Chat, and Rooms; validate `Array.isArray(parsed.tabs)`. | New |
| BUG-20260505-013 | Frontend/Workflow | Workflow semantic drift across manual vs automated paths | Medium | Likely | `RoomCreateModal` passes `isPrivate`, but `Rooms.createRoom(roomName, isPrivate)` delegates to `joinRoom(roomName)`. | User can select Private Room, but the UI performs the same join/create behavior. | web | Add test selecting private and assert either disabled/unsupported copy or a real private-room API call. | New |
| BUG-20260505-014 | Frontend/Workflow | Web API helper contract drift | Low | Likely | Room API helpers warn and return `undefined` on non-array payloads. | Future callers can crash or render stale state if they do not add their own `|| []`. | web | Add helper unit tests for malformed room API payloads expecting `[]`. | New |
| BUG-20260505-015 | Frontend/Workflow | UI action wiring/state persistence bugs | Low | Likely | Chat and Room send `Input.action` controls and room context buttons lack `Popup`, `title`, or accessible labels. | Violates the repo tooltip rule and makes icon actions harder to discover/test. | web | Add UI tests asserting accessible names/tooltips for send/context actions. | New |
| BUG-20260505-016 | Network Health | Soulseek network-health regressions | High | Likely | `MultiSourceDownloadService.FindVerifiedSourcesAsync` calls `_client.SearchAsync` directly rather than `SearchService`/safety limiter. | Remediation/swarm verification can bypass search safety budgets and create bursty network searches. | backend | Unit test with exhausted `ISoulseekSafetyLimiter` proves no Soulseek search is issued; callers should use request/shutdown cancellation. | New |
| BUG-20260505-017 | Network Health | Async/concurrency/lifecycle hazards | High | Likely | `BackfillFileAsync` waits on a semaphore with cancellation, then unconditionally decrements/releases in `finally`. | Cancellation while waiting can over-release concurrency and make active backfill accounting negative. | backend | Cancellation regression where a blocked waiter is cancelled leaves semaphore count and `ActiveBackfillCount` unchanged. | New |
| BUG-20260505-018 | Network Health | Protocol trust/signature fail-open | High | Likely | `ParseFlacHeader` returns `string.Empty` on invalid data; caller checks only `hash != null`. | Failed probes can be recorded as successful empty hashes and propagated to HashDb/mesh. | backend | Invalid/non-FLAC header test asserts failed status and no hash update/publish calls. | New |
| BUG-20260505-019 | Release/Ops | Release/tag/package/version drift | High | Likely | `.github/workflows/release-linux.yml` rewrites AUR `sha256sums` with too few entries after `slskd.tmpfiles` was added. | AUR update path can generate invalid PKGBUILDs. | packaging | Add workflow validation that runs AUR hash validation after generated PKGBUILD edits. | New |
| BUG-20260505-020 | Release/Ops | Release/tag/package/version drift | High | Likely | `.github/workflows/release-linux.yml` runs `bash ../packaging/scripts/validate-aur-pkgbuild-hashes.sh` after `cd packaging/aur`. | AUR update job may fail before pushing metadata. | packaging | Add workflow embedded-path dry run or shellcheck-style validation. | New |
| BUG-20260505-021 | Release/Ops | Installer/systemd/container permission bugs | High | Likely | `.github/workflows/release-packages.yml` Debian/RPM paths copy sysusers but not `slskd.tmpfiles`; RPM spec declares `Source4`. | Package build can fail or omit ownership convergence metadata. | packaging | Package smoke asserts `/usr/lib/tmpfiles.d/slskd.conf` is present in DEB/RPM artifacts. | New |
| BUG-20260505-022 | Backend/Security | Secret/log/error leakage | Medium | Likely | `MeshGatewayConfigValidator` logs generated CSRF token/header value. | Logs can replay localhost mesh gateway CSRF header for that session. | backend | Unit/log assertion that generated token value is never emitted, only configured status or fingerprint. | New |
| BUG-20260505-023 | Backend/Security | Secret/log/error leakage | Medium | Likely | `RelayService` logs relay auth challenge and share upload tokens. | Debug logs can replay short-lived relay workflows during token TTL. | backend | Log scan/test requiring hashed relay token IDs for all relay token logs. | New |
| BUG-20260505-024 | Backend/Security | SSRF/redirect/outbound fetch bypass | Medium | Needs Repro | Token-bearing notification/Spotify HTTP requests appear to use default redirect-following clients. | Redirects may leak notification/access tokens or message bodies to redirected hosts. | backend | Handler test with 30x to blocked host; assert no redirect follow and guard before send if active. | New |
| BUG-20260505-025 | Tests/Tooling | Weak or misleading test coverage | High | Likely | `scripts/check-non-versioned-routes.sh` skips a controller if any route is versioned. | Mixed legacy+versioned controllers can hide undocumented legacy routes. | tests | Require every non-versioned route to be allowlisted even when a controller has a versioned alias. | New |
| BUG-20260505-026 | Tests/Tooling | Weak or misleading test coverage | Medium | Likely | `scripts/check-controller-csrf.sh` and anonymous checks are marker-only regex scans. | Comments or unrelated attributes can satisfy endpoint-level checks. | tests | Replace marker-only checks with stricter attribute association or parser-backed scans. | New |

## Expert Council Notes

Use five read-only review roles for broad sweeps before fixing:

- Backend/Security: controllers, auth, SSRF, path, async, protocol trust.
- Frontend/Workflow: routing, API helpers, buttons, persisted state, e2e journeys.
- Release/Ops: workflows, package metadata, installers, systemd/container behavior.
- Network Health: Soulseek search/browse/download/probe automation, limiter use, cancellation.
- Adversarial Reviewer: duplicate detection, false-positive pressure, repro demands.

Add expert findings as `New`, then move only validated rows to `Accepted`.
