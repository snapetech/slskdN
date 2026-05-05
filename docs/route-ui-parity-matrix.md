# Route and UI Parity Matrix

This matrix tracks backend surface area against visible UI coverage. It is intentionally implementation-focused: each row identifies whether the surface is user-facing, admin-only, automation-only, or still needs UI work.

| Surface | API family | Current UI entry point | Status | Remediation |
| --- | --- | --- | --- | --- |
| Authentication/session | `/api/v0/session`, auth middleware | Login/session chrome | Covered | Keep aligned with cookie/CSRF tests. |
| Transfers | `/api/v0/transfers`, `/api/v0/downloads`, `/api/v0/uploads` | Transfers views | Covered | Recheck after any queue model changes. |
| Search | `/api/v0/searches`, wishlist search helpers | Search and Wishlist | Covered | Keep Soulseek safety limiter visible in failures. |
| Browse/user metadata | `/api/v0/users`, browse endpoints | Users, Browse, Search detail flows | Covered | Add regression tests around bad peer data and null responses. |
| Chat and rooms | `/api/v0/conversations`, `/api/v0/rooms` | Chat and Rooms | Covered | Message input behavior is manually ported; keep implementation distinct from upstream slskd. |
| Shares/library | `/api/v0/shares`, scan endpoints | Shares/config pages | Covered | Confirm scan status wording after backend changes. |
| Configuration | `/api/v0/options`, system config surfaces | Options/System pages | Mostly covered | Route policy, inventory generator, inventory freshness check, and first active legacy alias tranche are implemented. |
| Logs/diagnostics | `/api/v0/logs`, health/status routes | System pages | Mostly covered | Add consolidated diagnostics card for mesh/runtime warnings. |
| Player | local audio queue/state plus file endpoints | Persistent player bar | Covered locally | Document browser-only limitations; backend streaming parity remains bounded by available file APIs. |
| Mesh transport | `/api/v0/mesh/*`, `/api/v0/mesh/health/*` | System > Mesh | Covered | Continue unit coverage for transport preference display and error states. |
| Soulseek mesh rendezvous | `/api/v0/soulseek/mesh-rendezvous/*` | System > Mesh | Implemented | Default disabled; keep privacy warning and explicit opt-in. |
| Mesh evidence policy | Local UI policy storage | System > Mesh | Covered | Local-only by design; no backend sync without explicit privacy review. |
| Realm subject conflicts | Mesh subject index APIs | System > Mesh | Covered | Keep stale conflict cleanup visible. |
| DHT/bootstrap/NAT internals | Mesh services and health routes | Partial System visibility | Partial | Expose actionable NAT/bootstrap warnings without leaking internal topology. |
| Pods/native federation | `/api/v0/pods`, `/api/v0/podcore/*` | System > MediaCore pod workflow index | Partial | Workflow navigation, card-driven focus filtering, focused workflow label, active-card highlight, reset action, and per-workflow safety notices added; remaining work is deeper simplification of individual advanced forms. |
| Social federation/ActivityPub | federation routes plus `/api/v0/federation/diagnostics` | System > Integrations diagnostics | Covered for diagnostics | Keep public actor routes separate; do not add mutation UI without explicit privacy review. |
| VirtualSoulfind v2 | backend providers and native APIs | Search-adjacent only | Gap | Map provider capabilities into Search UI only after backend route inventory is stable. |
| Swarm visualization | swarm job status and trace routes | System > Jobs modal | Covered | Keep tied to active swarm jobs; no standalone route needed. |
| Runtime fork updates | vendored `slskNet.Runtime` | Not directly visible | Covered by build/tests | Keep patches security-oriented and avoid copying upstream slskd application code verbatim. |

## Current priority order

1. Finish automated coverage for mesh rendezvous UI/API.
2. Keep `docs/system-surfaces-current.md` current with `scripts/check-route-inventory.sh` during route remediation reviews.
3. Simplify individual MediaCore pod forms now that workflow navigation, focus filtering, and safety framing are in place.
4. Add versioned aliases for active legacy-only web client dependencies.
5. Add explicit privacy/security notes for every surface that publishes data outside the local node.
