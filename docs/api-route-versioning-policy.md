# API Route Versioning Policy

This project now treats route versioning as a parity and remediation control, not just an API style preference.

## Rule

New web-consumed JSON APIs should be exposed under `/api/v{version:apiVersion}/...` and currently target API version `0` unless the route belongs to an explicitly documented exception category.

## Exception categories

| Category | Examples | Rationale | Requirement |
| --- | --- | --- | --- |
| Protocol routes | `/actors/*`, `/.well-known/webfinger` | ActivityPub and WebFinger clients expect protocol-defined paths. | Keep protocol-specific auth, payload, and privacy tests. |
| Mesh transport routes | `/mesh/http/*` | Transport protocol endpoint, not a browser JSON API. | Keep outside general UI client usage. |
| Compatibility shims | `/api/*` routes retained for upstream/slskd compatibility | Existing clients may rely on legacy route shape. | New UI code should prefer versioned aliases when available. |
| OAuth callbacks | integration callback routes | External providers require stable redirect paths. | Callback endpoints must stay scoped and CSRF/auth reviewed. |

## Remediation guidance

1. Add versioned aliases before removing or changing legacy routes.
2. Update web clients to call versioned aliases first.
3. Keep compatibility shims read/write equivalent until a deliberate deprecation decision is made.
4. Every non-versioned controller should appear in `docs/system-surfaces-current.md` under `Non-versioned or protocol routes`.
5. Route additions that publish data externally must also update `docs/network-privacy-security-surfaces.md`.

## Current known compatibility-heavy families

| Family | Current state | Direction |
| --- | --- | --- |
| slskd compatibility controllers | Legacy `/api` routes | Preserve for compatibility; avoid new UI dependencies. |
| Jobs/source-provider/source-feed routes | Some have both legacy and versioned routes; some remain legacy-only | Add versioned aliases before UI rewiring. |
| VirtualSoulfind bridge routes | Some have both legacy and versioned routes | Prefer `/api/v0/bridge*` for new clients. |
| Audio and library health routes | Several legacy-only native routes | Add versioned aliases if actively consumed by web UI. |
| ActivityPub/WebFinger | Protocol routes | Keep non-versioned by design. |

## Implemented alias tranche

| Date | Controllers | Client impact |
| --- | --- | --- |
| 2026-05-05 | `LibraryHealthController`, `DiscographyJobsController`, `LabelCrateJobsController` | Library Health web client now uses `/library/health/*` through the shared `/api/v0` base; job-specific controllers now expose versioned aliases while preserving legacy `/api/jobs/*` routes. |
