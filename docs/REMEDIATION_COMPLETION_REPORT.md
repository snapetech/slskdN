# Remediation Completion Report

This report consolidates the remediation work completed across feature parity, route parity, security posture, UI safety, and review baseline enforcement.

## Completed feature and UI remediation

- Mesh rendezvous backend, API client, System > Mesh UI, privacy warning, disabled-by-default gate, and tests.
- Federation diagnostics backend, API client, System > Integrations UI card, warning surfacing, and tests.
- MediaCore pod workflow index, card-driven focus filtering, reset action, section anchors, active workflow highlighting, and per-workflow risk notices.
- MediaCore read-only-first advanced controls for pod workflows, descriptor
  publishing/retrieval, ContentID registry, metadata portability, retrieval
  management, perceptual hashing, and fuzzy matching.
- System tab labels for admin and experimental panels.
- Library Health web client route fix from double-prefixed `/api/v0/api/...` paths to shared-client relative paths.
- MediaCore pod helper route fix from incorrect `/mediacore/podcore/*` and absolute `apiBaseUrl` usage to shared-client `/podcore/*` paths.
- Direct fetch CSRF opt-in added for mutating pod and port-forwarding wrappers.

## Completed backend route remediation

- Added versioned aliases while preserving compatibility routes for:
  - Native slskdN capabilities, library health, and warm cache routes.
  - VirtualSoulfind canonical, disaster-mode, and shadow-index routes.
  - Audio analyzer migration, canonical, and dedupe routes.
  - Discography and label-crate job routes.
  - Library Health API route family.
- Added route alias tests covering active legacy route aliases.
- Regenerated API route inventory after route alias work.

## Completed remediation baseline checks

The combined baseline is `npm run check:remediation` and includes:

- Route inventory freshness check.
- Mutating controller CSRF marker check.
- Anonymous endpoint allowlist check.
- Non-versioned route allowlist check.
- Allowlist drift check.
- Sensitive placeholder/token pattern check.
- Shared web API path check.
- Direct fetch CSRF opt-in check.
- MediaCore pod route regression check.
- Remediation script registry check.
- Remediation docs command-reference check.

## Intentional remaining non-versioned surfaces

Remaining non-versioned surfaces are documented in `docs/NON_VERSIONED_ROUTE_ALLOWLIST.md` and fall into these categories:

- slskd compatibility shims.
- ActivityPub and WebFinger protocol-required routes.
- Mesh transport protocol routes.
- OAuth callback compatibility routes.
- Legacy compatibility routes retained alongside versioned aliases.

## Intentional anonymous surfaces

Anonymous controller usage is documented in `docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md` and is limited to login/bootstrap, protocol discovery, OAuth callback, public verification/discovery, or explicit ticket/token streaming flows.

## Work left before release

The remaining work is release coordination and target-specific live/E2E checks,
not planned feature remediation:

- Push local commits before cutting a release tag so the remediation sync guard
  can pass.
- Run any live/E2E checks required for the target release.
- Fix any concrete failures as targeted defects.

## Validation status

Validated on 2026-05-13:

- Full frontend unit tests: `131/131` files and `716/716` tests passed.
- Frontend production build passed.
- Full backend `dotnet test --no-restore`: `67/67` smoke, `4056/4056`
  unit, and `276/276` integration tests passed.
- `./bin/lint` passed.
- `npm run check:remediation` passed all substantive checks and stopped only at
  the release branch sync guard because local `main` was ahead of `origin/main`.
