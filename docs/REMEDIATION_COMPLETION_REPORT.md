# Remediation Completion Report

This report consolidates the remediation work completed across feature parity, route parity, security posture, UI safety, and review baseline enforcement.

## Completed feature and UI remediation

- Mesh rendezvous backend, API client, System > Mesh UI, privacy warning, disabled-by-default gate, and tests.
- Federation diagnostics backend, API client, System > Integrations UI card, warning surfacing, and tests.
- MediaCore pod workflow index, card-driven focus filtering, reset action, section anchors, active workflow highlighting, and per-workflow risk notices.
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

The remaining work is validation and defect correction, not planned feature remediation:

- Run `npm run check:remediation`.
- Run backend build and unit tests.
- Run web unit tests and web build.
- Fix any concrete validation failures as targeted defects.
