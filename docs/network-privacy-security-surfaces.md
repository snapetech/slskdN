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
