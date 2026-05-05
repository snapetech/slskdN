# Remediation Review Checklist

Use this checklist when continuing parity, security, route, or UI remediation work.

## Baseline checks

Run these before reviewing route/API remediation changes:

```bash
npm run check:remediation
```

Equivalent direct scripts:

```bash
scripts/check-route-inventory.sh
scripts/check-web-api-paths.sh
```

## Route/API review

- New web-consumed JSON APIs should be under `/api/v{version:apiVersion}`.
- Shared frontend API modules should call relative paths such as `/soulseek/recommendations`, not `/api/v0/soulseek/recommendations`.
- Legacy routes can remain when they are compatibility shims, but new UI code should prefer versioned aliases.
- Protocol routes such as `/actors/*`, `/.well-known/webfinger`, and `/mesh/http/*` are allowed outside `/api/v0` when protocol-required.
- If controller route attributes change, regenerate `docs/system-surfaces-current.md`.

## Privacy/security review

- Any workflow that publishes metadata, memberships, opinions, messages, discovery tags, peer IDs, or key material must have local UI warning text.
- UI disablement is not sufficient for sensitive operations; controller-side feature gates must enforce the same policy.
- Direct `fetch` wrappers that mutate server state must use `session.authHeaders({ csrf: true })`.
- Shared axios calls get CSRF automatically for mutating methods.

## UI parity review

- Prefer adding navigation, safety framing, and task-specific simplification before removing experimental controls.
- Keep pod, mesh, federation, and Soulseek discovery controls visibly separated by risk level.
- Update `docs/route-ui-parity-matrix.md` whenever a backend surface gets a new UI entry point or an intentional no-UI decision.

## Documentation updates

Update these files when relevant:

- `docs/system-surfaces-current.md` for route inventory changes.
- `docs/api-route-versioning-policy.md` for versioning exceptions or alias tranches.
- `docs/network-privacy-security-surfaces.md` for externally visible data flows.
- `docs/route-ui-parity-matrix.md` for feature/UI parity status.
- `docs/FEATURE_PARITY_AND_GAP_PLAN.md` for plan-level status changes.

## Direct fetch review

- Prefer the shared axios `api` client for JSON API modules.
- If direct `fetch` is required, mutating methods must use `session.authHeaders({ csrf: true })`.
- Run `npm run check:web-fetch-csrf` or the full `npm run check:remediation` baseline after touching direct fetch wrappers.

## MediaCore pod route review

- MediaCore ContentID routes use `/mediacore/*`.
- PodCore routes use `/podcore/*`.
- Do not combine them as `/mediacore/podcore/*`.
- Run `npm run check:web-mediacore-routes` after touching `src/web/src/lib/mediacore.js`.

## Backend endpoint exposure review

- Mutating controllers should carry `ValidateCsrfForCookiesOnly`; run `npm run check:controller-csrf` after controller edits.
- Controllers with `[AllowAnonymous]` must be listed in `docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md` with a rationale; run `npm run check:anonymous-endpoints` after auth attribute changes.
- Anonymous protocol surfaces still need payload limits, rate limits, or protocol-specific validation where applicable.

## Non-versioned route review

- Controllers with only non-versioned routes must be protocol-required, compatibility shims, OAuth callbacks, or temporary legacy surfaces under remediation.
- Document allowed non-versioned controllers in `docs/NON_VERSIONED_ROUTE_ALLOWLIST.md`.
- Run `npm run check:non-versioned-routes` after controller route changes.

## Allowlist drift review

- Run `npm run check:allowlist-drift` after deleting, renaming, or migrating controllers.
- Remove allowlist entries when a controller no longer exists, no longer has `[AllowAnonymous]`, or no longer exposes non-versioned routes.

## Sensitive placeholder review

- Use obvious placeholders such as `example-token`, `redacted`, or `secret-key` in tests and docs.
- Run `npm run check:sensitive-placeholders` after adding diagnostics, examples, fixtures, or generated reports.
- This check is high-confidence pattern scanning; it does not replace a dedicated secret scanner for release workflows.

## Remediation script registry review

- Every `scripts/check-*.sh` file should be executable.
- Every focused check script should be referenced by `scripts/check-remediation-baseline.sh` unless it is the combined baseline script itself.
- Run `npm run check:remediation-registry` after adding or renaming remediation check scripts.
