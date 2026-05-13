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
| Configuration | `/api/v0/options`, system config surfaces | Options/System pages | Mostly covered | Route policy, inventory generator, inventory freshness check, baseline gates, and active legacy alias tranches are implemented. |
| Logs/diagnostics | `/api/v0/logs`, health/status routes | System pages | Mostly covered | Add consolidated diagnostics card for mesh/runtime warnings. |
| Player | local audio queue/state plus file endpoints | Persistent player bar | Covered locally | Document browser-only limitations; backend streaming parity remains bounded by available file APIs. |
| Mesh transport | `/api/v0/mesh/*`, `/api/v0/mesh/health/*` | System > Mesh | Covered | Continue unit coverage for transport preference display and error states. |
| Soulseek mesh rendezvous | `/api/v0/soulseek/mesh-rendezvous/*` | System > Mesh | Implemented | Default disabled; keep privacy warning and explicit opt-in. |
| Mesh evidence policy | Local UI policy storage | System > Mesh | Covered | Local-only by design; no backend sync without explicit privacy review. |
| Realm subject conflicts | Mesh subject index APIs | System > Mesh | Covered | Keep stale conflict cleanup visible. |
| DHT/bootstrap/NAT internals | Mesh services and health routes | System > Network and System > Mesh | Mostly covered | Network and Mesh panels expose DHT/LAN-only/connectivity posture; keep NAT/bootstrap warnings actionable without leaking internal topology. |
| Pods/native federation | `/api/v0/pods`, `/api/v0/podcore/*` | System > MediaCore pod workflow index | Mostly covered | Workflow navigation, card-driven focus filtering, focused workflow label, active-card highlight, reset action, per-workflow safety notices, and read-only-first advanced controls are added. Continue with broader guided-flow validation rather than more obvious form-disclosure work. |
| Social federation/ActivityPub | federation routes plus `/api/v0/federation/diagnostics` | System > Integrations diagnostics | Covered for diagnostics | Keep public actor routes separate; do not add mutation UI without explicit privacy review. |
| VirtualSoulfind v2 | backend providers and native APIs | System > Source Providers and Search provider gating | Mostly covered | Provider capability/risk/priority catalog is visible, and Search only sends Scene/Pod providers when backend capability advertises the bridge. Keep live execution behind explicit provider enablement. |
| Swarm visualization | swarm job status and trace routes | System > Jobs modal | Covered | Keep tied to active swarm jobs; no standalone route needed. |
| Runtime fork updates | vendored `slskNet.Runtime` | Not directly visible | Covered by build/tests | Keep patches security-oriented and avoid copying upstream slskd application code verbatim. |

## Current priority order

1. Keep `docs/system-surfaces-current.md` current with `scripts/check-route-inventory.sh` during route remediation reviews.
2. Keep System admin/experimental labels and route parity notes current when feature panels move or new panels are added.
3. Run validation and fix any concrete failures from the completed route alias tranche.
4. Add explicit privacy/security notes for every surface that publishes data outside the local node.
