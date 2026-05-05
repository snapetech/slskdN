# Network, Privacy, and Security Surface Notes

This document records remediation rules for features that can publish data, discover peers, or expose local node state. It complements the route/UI parity matrix and should be updated whenever a new external network surface is added.

## Baseline rules

| Rule | Requirement |
| --- | --- |
| Explicit opt-in | Features that publish externally identifiable state must default off and require an operator action. |
| Visible privacy copy | The UI must describe what identifier or interest is published before enabling the action. |
| Server-side gate | UI disablement is not sufficient; the API must enforce the same feature flag and relay-agent restrictions. |
| Rate limiting | Soulseek-originated discovery and browse/search style calls must pass through `ISoulseekSafetyLimiter`. |
| Versioned route preference | New web-consumed APIs should use `/api/v0/*` unless intentionally native/internal and documented. |
| Minimal data display | Candidate peer and mesh diagnostics should show enough to operate the feature without exposing unnecessary topology. |
| No upstream code copy | Upstream behavior can be matched, but remediation should use local implementation structure and comments. |

## Current externally relevant surfaces

| Surface | Data leaving node | Default | Enforcement | Notes |
| --- | --- | --- | --- | --- |
| Soulseek recommendations/interests | Interest strings and account identity through Soulseek protocol | Existing Soulseek behavior | Auth, relay-agent gate, safety limiter on reads/mutations | UI is search-adjacent and user initiated. |
| Soulseek mesh rendezvous | Recognizable `slskdN` mesh interest tag on the Soulseek account | Disabled | `mesh.enableSoulseekRendezvous`, auth, relay-agent gate, safety limiter | System > Mesh shows warning and opt-in controls. |
| Mesh transport/DHT | Peer descriptor and mesh transport metadata | Controlled by mesh config | Mesh options and transport services | Keep diagnostics operational, not topology-dumping. |
| Federation/ActivityPub | Federated profile/activity data | Experimental/configured | Mesh/federation options | Needs admin diagnostics before broader UI exposure. |
| Pods/native sharing | Pod metadata and membership state | Experimental/configured | Mesh options, controller policy, and federation diagnostics posture | MediaCore exposes extensive pod tooling; next step is stale route cleanup and UX consolidation. |
| Swarm jobs | Source/job status inside local web UI | Local view | Authenticated APIs | Modal is tied to active jobs; no standalone exposure. |
| Web player | Local browser playback state and file access | User initiated | Existing auth/file APIs | Browser-local queue state should stay local unless explicitly synced. |

## Remediation checklist for new parity work

1. Identify whether the route publishes state, only reads local state, or bridges to Soulseek/mesh/federation.
2. If it publishes state, add a config default and controller-side gate before adding UI controls.
3. Add user-visible copy describing exactly what leaves the node.
4. Add tests at the lowest useful level: client route, component behavior, and controller policy.
5. Update `docs/route-ui-parity-matrix.md` with the UI entry point and status.

## Web client transport hardening notes

| Area | Remediation |
| --- | --- |
| Direct fetch wrappers | Mutating direct-fetch wrappers must opt into `session.authHeaders({ csrf: true })` so cookie-auth requests carry `X-CSRF-TOKEN`. |
| Shared axios client | Library modules should pass relative paths such as `/podcore/messages` or `/library/health/scans`; do not embed `/api` or `/api/v0` in shared-client calls. |
| MediaCore pod helpers | Pod DHT, membership, discovery, routing, signing, verification, message storage, backfill, opinions, channels, and content helpers now target `/podcore/*` through the versioned client. |

## Pod UX consolidation notes

| Area | Remediation |
| --- | --- |
| MediaCore pod workflows | System > MediaCore now starts with a Pod Workflow Index that separates read-only diagnostics from publishing, signing, storage, routing, and key-material workflows. |
| Operator safety | Pod controls remain available, but the UI now warns that the page mixes local diagnostics with operations that publish metadata, membership records, messages, opinions, or key material. |

| MediaCore per-workflow notices | Each pod workflow card now labels whether the workflow is read-only, publishes metadata or membership state, mutates local storage, routes messages, changes pod structure, publishes opinions, or handles key material. |

| Web API path lint | `scripts/check-web-api-paths.sh` checks that shared axios calls do not embed `/api` or `/api/v0`, preventing double-prefix routing bugs for security-sensitive admin surfaces. |

| MediaCore workflow focus | System > MediaCore now includes a Workflow focus selector that lets operators narrow the page to one pod workflow while retaining the full surface behind "Show all pod workflows". |

| MediaCore workflow card focus | Selecting a pod workflow card now applies the focus filter immediately, and focused mode includes a reset action to restore the full pod surface. |

| MediaCore focused workflow label | Focused mode now names the selected pod workflow and visually highlights its index card, reducing accidental work in the wrong high-risk section. |

| Direct fetch CSRF lint | `scripts/check-web-fetch-csrf.sh` checks mutating direct `fetch` calls for `session.authHeaders({ csrf: true })`, covering wrappers that bypass axios interceptors. |

| Controller CSRF baseline | `scripts/check-controller-csrf.sh` fails when a mutating controller lacks `ValidateCsrfForCookiesOnly`. |
| Anonymous endpoint allowlist | `scripts/check-anonymous-endpoints.sh` fails when a controller with `[AllowAnonymous]` is not documented in `docs/ANONYMOUS_ENDPOINT_ALLOWLIST.md`. |

| Non-versioned route allowlist | `scripts/check-non-versioned-routes.sh` requires non-versioned-only controllers to be documented in `docs/NON_VERSIONED_ROUTE_ALLOWLIST.md`, keeping compatibility/protocol exceptions explicit. |

| Allowlist drift check | `scripts/check-allowlist-drift.sh` fails stale anonymous/non-versioned allowlist entries when controllers are removed or migrated. |

| Sensitive placeholder check | `scripts/check-sensitive-placeholders.sh` scans for high-confidence API token and private key patterns outside vendored/build artifacts. |

| Remediation script registry | `scripts/check-remediation-script-registry.sh` prevents focused check scripts from being added without executable bits or baseline wiring. |
