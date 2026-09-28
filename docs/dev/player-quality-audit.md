# Player quality audit

Updated: 2026-09-28. The player overhaul remains active.

### Resource coverage correction — 2026-09-28

Earlier resource sections use “whole-browser” for totals from CDP's process
list. That label is too broad: a live Linux headless Chromium snapshot showed
seven OS browser processes versus five reported by CDP, omitting two zygotes
with about 18 MiB combined PSS at that moment. Retained numeric results measure
CDP-reported browser processes; zero missing readings and no churn refer to
that list. They do not establish full OS browser-tree totals. Preserve the
original values and workload distinctions. Full-tree measurement is follow-up
work; do not add the snapshot's omitted PSS to earlier windows. Confidence: high
in the observed enumeration difference. Gotcha 0z1171 records the correction.

## Quality target

Build a dependable, polished player with the responsiveness, intuitive controls
and low resource use expected from Winamp or VLC, adapted to slskdN's browser
and Soulseek environment.

Audit and improve complete playback, queue, seeking, recovery, accessibility,
listen-along, radio and cross-node workflows. Work in substantial batches,
verify real behavior, document fixes, and commit and push completed improvements.
Keep network impact conservative and the interface unobtrusive.

Completion requires the agreed workflows to pass direct validation, resource
use to meet documented targets, and no known actionable defects within scope.
Record hardware and infrastructure requirements explicitly; unverified behavior
remains unfinished. Green regression suites alone do not prove completion.

## Current evidence

The isolated Chromium suite in `src/web/e2e/player.spec.ts` uses generated PCM,
server-side AIFF decoding and a backend configured without remote peer connections.
It passes 16 workflows, including controlled HTTP radio failure/retry and a synthetic directory response. Browser metadata/action checks call the registered Media
Session handlers while retaining the browser implementation; they do not exercise
physical headset buttons. Output-switch regressions use simulated device APIs.

| Area | Evidence | Status / confidence |
| --- | --- | --- |
| Native transport | Actual PCM playback, Pause, absolute seeks, remount and queue advancement | Verified in Chromium / high |
| Server files | Indexed and unindexed downloads, direct switching and Play Next | Verified in Chromium / high |
| Decoding | Actual AIFF to MP3, absolute seeks while paused/playing | Verified in Chromium / high; other formats need runtime coverage |
| Queue and playlists | Backend save/load, repeated server entries and duplicate local files | Verified in Chromium / high |
| Recovery | Refresh retains latest server position without autoplay; Previous restarts replay at zero | Verified in Chromium / high |
| Browser media actions | Metadata, position, Play/Pause, seek actions, Previous/Next, Stop | Registered callbacks verified / high; physical controls unverified |
| Analyzer | Reads stop on Pause/Stop, resume on Play; existing contexts suspend | Verified in Chromium / high |
| Crossfade | Both streams play; Pause suspends both; Resume plays one; natural completion suspends outgoing context | Verified in Chromium / high |
| Picture-in-Picture | Actual spectrum rendering and Stop/hide closure; pending request cancellation covered by regression tests | Verified in headless Chromium / high; physical window sizing and focus unverified |
| Layout | Expanded/compact controls at 1440, 768, 390 and 320px; narrow primary controls meet 44px bounds | Chromium viewport checks / high; physical mobile unverified |
| Output routing | New playback waits for switch success/failure and uses selected/rolled-back sink | Simulated regression checks / high; physical routing unverified |
| Listed radio | Reachable picker, directory failure/manual refresh, metadata-only controls, actual HTTP audio failure/retry, temporary URL exclusion | Real two-backend Chromium discovery, decoded playback/seek, revocation, counters, reverse publication, refreshed host ticket and 15-minute live renewal soak verified / high; sustained throughput remains open |
| Listen-along recovery | Startup retry, closed/rejoin/refresh failure controls, disposed callbacks and live-event precedence | Actual Chromium PlayerBar follows real routed room events and catches the latest state after automatic transport recovery; two authenticated real SignalR clients on one backend verify leave, explicit disconnect/rejoin, snapshot recovery and live ban / high; cross-node state propagation unverified |

See the dated validation sections below for latest gate counts; earlier counts
record the source version validated at that time.

## Resource sample

Ten-second intervals, one isolated headless browser, native PCM, no audio graphs:

| State | CPU (% of one core) | Chromium PSS (MiB) | Renderer work (%) | JS heap (MiB) |
| --- | ---: | ---: | ---: | ---: |
| Idle | 4.80 | 300.44 | 1.01 | 9.39 |
| Playing | 2.80 | 315.18 | 0.62 | 9.61 |
| Paused | 2.50 | 309.18 | 0.46 | 8.07 |

Linux proportional memory was measured for all five surviving browser processes
with no unavailable readings. One process exited during the idle CPU interval;
its delta is excluded. Playing and paused CPU intervals retained all five
processes. Totals include Chromium and the application shell. The observed
playing memory increase over idle was 14.74 MiB. Confidence in these samples is
moderate; repeated warm-baseline and sustained-session measurements remain due.

## Live host renewal soak — 2026-09-28

An opt-in Playwright run (`pnpm test:player:radio-soak`) kept the actual PlayerBar
host and a listed-radio listener playing across two connected backend nodes for
15 wall-clock minutes. The host renewal timer returned three successful
responses and the remote directory exposed four distinct scoped tickets. Host
and listener playback advanced to 910 and 908 seconds. After the original
900-second ticket expired, a fresh range returned 206 and the old ticket
returned 401.

This verifies browser timer delivery, remote directory rotation and in-flight
playback over loopback. It is not a WAN-latency or throughput result; both stay
open for representative deployment measurements.

## Remaining completion work

- Verify repeated admissions, source replacement and sustained playback across realistic latency. The 15-minute loopback host/listener soak, three browser-driven renewals and post-expiry old/new ticket checks pass; WAN latency and sustained throughput remain due.
- Verify cross-node listen-along state propagation. Real Chromium PlayerBar follow now survives an automatic transport interruption, rejoins and catches the latest backend snapshot; authenticated same-node clients also cover explicit disconnect/rejoin and live membership revocation.
- Exercise supported browser engines and additional audio formats, including
  failures, decode cancellation and recovery.
- Verify physical mobile interactions, physical output routing/media buttons,
  and Picture-in-Picture window sizing/focus.
- Measure repeated warm idle/play/pause intervals and sustained queue,
  visualizer, output-switch and floating-window cycles for retained memory,
  stray timers and active contexts.
- Inspect accessibility with keyboard-only and assistive-technology workflows.
- Resolve newly discovered defects and update this audit with direct evidence.


## Mesh transport prerequisites — 2026-09-28

A negative regression reproduced truncation of a 3,000-byte unknown-length
stream at 2,048 bytes. The transport now permits bounded short/empty final
range responses, accepts an exact host EOF offset, retains exact validation
for known lengths, and rejects replies exceeding the requested range.
Producer failures complete the pipe with their error instead of normal EOF;
the copy buffer is reused across chunks.

Focused tests cover unknown-length tails, aligned EOF, missing peers, producer
I/O errors, hash mismatch, response bounds and host range handling. These
checks establish transport behavior with controlled dependencies; they do not
establish remote listed-radio routing. The host-scoped radio transport and
dual-participant checks remain required.

### Mesh transport final gates — 2026-09-28

All 26 focused regressions pass, including controlled RPC through the real host range handler, fetcher and reader. Final-source unit rerun: 5,198 passed; solution smoke: 74 passed; integration: 284 passed. Repository lint, whitespace and local-identity checks pass. Remote listed-radio routing remains open.


## Host-scoped radio transport — 2026-09-28

Implemented manual local ticket acquisition pinned to the actual overlay
Soulseek transport username, party, content and host capability. The host
checks permission and content for every read; no alternate-peer discovery or
general-preview fallback occurs. Controlled gateway/reader tests verify bytes
and ranges; a real production loopback TLS server, connector and service client
deliver a 44 KiB response and tail, then deny reads after revocation. Confidence
in this boundary is high. The browser radio case remains a controlled HTTP
ticket/stream workflow, rather than a full two-backend network workflow.

Reads are paced at five per second, fit the 64 KiB framed JSON limit and retain
the global 500-call RPC limit. One listener-node stream per host bounds aggregate
read pressure. Sustained throughput, full browser HTTP seek behavior across mesh
and high bitrate audio remain unverified.

## Final radio implementation gates — 2026-09-28 02:07 UTC

Final source passes 5,215 unit, 74 smoke and 284 integration tests; 986 Web
tests and 16 browser workflows pass. Real loopback TLS covers production
server/connector/client delivery and revocation. Repository/Web lint,
Release/frontend builds, bundle/output, CSRF/anonymous-endpoint, whitespace
and identity checks pass.

Completed implementation batch is cleared. Next Steps: verify sustained
two-backend browser radio, HTTP seek and capability expiry, dual-participant
room recovery, and remaining player audit rows. The full goal remains active;
no release tag or deployment was created.

## Connected radio discovery and seeking — 2026-09-28

A real two-backend Chromium workflow uses loopback overlay transport and no
intercepted directory, ticket or audio responses. The listener discovers the
host publication, acquires its scoped ticket, decodes initial audio, seeks
inside a 180-second generated WAV, receives a replacement 206 range and
continues decoded time progression. Confidence in this workflow is high.

The run exposed an empty mesh DHT table, native overlapping range responses
and repeated fairness admission after received audio. On-demand neighbor
bootstrap, same-ticket response replacement and ticket-lifetime fairness
admission address those boundaries without increasing concurrent streams or
read pacing. Live room subscriptions also enforce pod membership on join.

Still required: ticket/capability expiry and renewal, permission revocation in
the browser, source replacement and dual-participant reconnect. Fairness
lifecycle needs broader validation: overlay upload accounting has no production
call sites, so repeated playback admissions and reciprocal serving cannot yet
be treated as verified. Continuous membership revocation after a hub group
join is also unverified. These remain open alongside existing format, browser,
physical-device, accessibility and sustained-resource requirements.

## Connected radio final gates — 2026-09-28

Final source passes 5,224 unit, 74 smoke and 284 integration tests; 986 Web
tests and 17 browser workflows pass. Repository/Web lint, Release build, CSRF,
anonymous endpoint, whitespace and identity checks pass. The real two-node
browser case verifies remote directory discovery and decoded seek progression
with two successful byte ranges, without intercepted discovery or playback.

Completed source batch is cleared after commit/push. Next Steps: expiry/renewal,
reciprocal upload accounting and repeated admissions, source replacement, room
reconnect/revocation and the remaining player quality audit. Overall goal stays
active; no release tag or deployment is authorized.

## Radio accounting, revocation and refresh — 2026-09-28

The real two-node browser workflow now verifies host upload and listener
download totals, remote permission revocation, explicit stalled playback retry,
a 503 response rather than an unhandled 500, and disabled playback after manual
refresh reads the changed host announcement. No directory, ticket or audio
response is intercepted. Confidence is high for this loopback workflow.

Production loopback TLS tests verify payload accounting in both directions,
expired host capability denial and revocation without additional credit. Local
expired tickets are rejected before peer reads. These establish denial, not
successful browser renewal after elapsed expiry. Manual retry controls in both
layouts also have buffering regressions.

Latest gates: 5,233 unit, 74 smoke, 284 integration, 989 Web tests across 165
files and 17 browser workflows pass. Repository/Web lint, Release/frontend
builds, bundle/output, CSRF, anonymous-endpoint, identity and whitespace checks
pass. Open work includes expiry/renewal and repeated admissions, source changes,
dual-participant recovery, sustained resource measurements and the remaining
browser, format, accessibility and physical-device coverage above.

## Existing-link transport and identity limits — 2026-09-28

Three negative tests reproduced case-sensitive host reservation bypass,
production-client rejection of an established inbound TLS link and signed DHT
storage replacing a reachable transport address with a cryptographic ID. The
fixes retain publisher signature/quota checks and network budgets.

The production reverse client now delivers radio bytes over the same TLS link,
bootstraps an empty DHT through that inbound neighbor and shares a ten-call
reservation across username case variants. All pending calls release after
responses. The real two-backend browser workflow also verifies a publication
in the opposite direction reaches the original host without another connect.
Confidence is high for these checks. Thirty-four focused regressions, 5,234
full unit tests, 74 smoke, 284 integration and 17 browser workflows pass;
repository/Web lint, Release build, bundle/output, endpoint security, identity
and whitespace gates pass. The unchanged Web implementation retains its
989-test validated suite.

## Room subscription lifecycle — 2026-09-28

The real service accepts two authenticated non-admin SignalR clients. A runtime
case creates a public pod, joins both members, verifies shared publications,
explicit room leave/rejoin, disconnect/reconnect and current snapshot retrieval.
Banning one already-connected member stops subsequent private room state,
delivers one access-revoked notification and denies both rejoin and snapshot
read; the second participant continues receiving state. Confidence is high for
this same-node Long Polling workflow. It does not establish physical browser
following, automatic transport recovery or cross-node state propagation.

Regression tests verify removal and ban, administrator access, room isolation,
registry capacity, leave while membership lookup is pending, disconnected join
rejection and queued event/join ordering after revocation. The existing service
tracks at most 4,096 subscriptions and 16 per connection, performs one current
membership read per publication when needed and performs no idle polling.

Final gates pass: 992 Web tests across 165 files; 5,238 unit, 74 smoke and
284 integration tests; 18 runtime cases (17 Chromium and one real two-client
SignalR workflow). Repository/Web lint, Release/frontend builds, bundle/output,
CSRF/anonymous endpoint, identity and whitespace checks pass. The player
overhaul remains active with renewal, distributed room delivery, sustained
resources, accessibility, format/browser and physical-device work outstanding.

## Publication and radio admission reliability — 2026-09-28

A corrected negative regression proved Stop could finish while an earlier Play
was still routing. Complete publication is now serialized per room, with 16
reservations per room and 256 active room queues. Tests verify ordered delivery,
unrelated-room progress, cancellation, overload feedback and cleanup on failure.
Confidence in this controlled concurrency boundary is high.

Remote ticket acquisition checks fairness after host metadata validates
permission. Policy denial reports a stable 429 code before native playback;
local snapshots avoid overlay admission and existing stream checks remain.
Web regressions distinguish fairness, capacity and unavailable snapshots. The
real two-node browser case verifies a fresh admission is denied after download
traffic while earlier same-ticket seeks work.

An elapsed-clock Chromium case validates a local radio ticket with HTTP 206,
waits beyond its production two-minute lifetime, observes HTTP 401, manually
reselects the still-listed snapshot, and verifies a different ticket, HTTP 206
and continued playback. Confidence is high for local manual renewal; remote
renewal and the fifteen-minute host capability remain unverified.

The initial combined runtime run passed 18 cases but failed its reverse-directory
assertion. A standalone diagnostic and three repeated runs passed. This does
not explain the intermittent failure; retained logs and further investigation
remain required. Source inspection also finds no pod_message handler in the
control dispatcher, so cross-node room application is incomplete. It requires
an authenticated membership boundary, state ordering and no republishing loop.

Retained diagnostic logs reject the radio fixture's synthetic pod ID during
message storage. Its successful byte transport therefore does not prove durable
or distributed room publication. The service currently ignores storage failure
and only logs routing failure; validate real room existence and storage outcomes
before reporting room success. This additional publication gap remains open.

### Final reliability gates — 2026-09-28

Final source passes 5,244 unit / 74 smoke / 284 integration tests, 996 Web tests
across 165 files and 19 runtime cases (18 Chromium plus one two-client SignalR
case). Repository/Web lint, Release/frontend builds, bundle/output, endpoint,
identity and whitespace checks pass. Four standalone diagnostic executions also
passed the real radio workflow. The earlier reverse-directory failure remains
unexplained and open despite the green final suite.

Fresh ten-second native samples record 4.30% / 3.10% / 2.70% of one core and
305.75 / 325.99 / 316.13 MiB Chromium PSS for idle / playing / paused. All five
surviving processes supplied memory readings; one exited during idle and its
CPU delta is excluded. Native playback created zero AudioContexts. Confidence
is moderate: these short headless totals include the browser/application shell
and do not establish sustained retention, physical-device behavior or a
player-only resource budget.

## Room publication integrity and received-frame quotas — 2026-09-28

Negative regressions proved missing rooms and rejected writes could expose a new
broadcast. Publication now checks room existence, validates metadata and requires
accepted storage before changing snapshots, directory, now-playing or delivering
the update. Failed Play and Stop preserve prior state. API feedback distinguishes
missing rooms from temporarily unavailable storage, and allows manual retry.

Three real SQLite regressions proved conflicting bodies, signatures and signature
versions were silently accepted at an existing timestamp. Only equivalent retries
now succeed; conflicts retain the original row and supplied versions are stored.

A real TLS regression proved ten permitted frames removed the connection because
quota was charged for an eleventh pending read. Quota now counts received frames;
ten retain the connection and eleven in the same window remain denied. Confidence
is high for this reproduced defect. The earlier reverse-directory failure remains
unexplained: a passing runtime assertion does not prove its cause.

Radio fixtures now create actual unlisted rooms to isolate the workflow from
unrelated automatic pod-descriptor publication. The live room fixture verifies
missing-channel rejection and confirms successful publication in message storage.
Authenticated cross-node application still requires a receiver, permission checks
and prevention of republishing loops. Source inspection also found mesh Join trusts
caller-selected roles and lacks the native API's private/approval checks; repair
and negative proof are required before using it for remote room authorization.

### Final integrity gates — 2026-09-28

Final source passes 5,256 unit, 74 smoke and 284 integration tests; 998 Web tests
across 165 files; and all 19 runtime cases (18 Chromium and one actual two-client
SignalR case). Repository/Web lint, Release/frontend builds, bundle/output,
controller/fetch CSRF, anonymous endpoint, identity and whitespace gates pass.
The retained final node logs are available for investigation. No cross-node
listen-along receiver or automatic recovery is established by these results.

Fresh ten-second samples measure 3.60% / 2.70% / 2.30% of one core and
301.43 / 315.95 / 306.71 MiB Chromium PSS for idle / playing / paused. All five
surviving processes supply memory readings; one exits during idle, so its CPU
delta is excluded. Native playback creates zero AudioContexts. Confidence is
moderate for these short measurements; sustained sessions and device validation
remain due. The overall player goal remains active.

## Remote room permission prerequisites — 2026-09-28

Twenty-three negative adapter regressions reproduced permission failures before
repair: requested elevated join roles, private/approval admission, private listed
metadata and details exposed to unrelated peers, history/posting without active
membership and empty transport identities. RPC and stream history now check
current membership before messaging dispatch. Public metadata remains readable,
existing approved members retain their roles/keys and new joins grant member only.

Two direct negative regressions proved banned LeaveAsync succeeded in both pod
service implementations. Ban records now survive attempted self-removal, so
public-room rejoin cannot erase denial. Tests also exercise ordinary leave/rejoin
and the real in-memory service behind the mesh adapter. Production membership
queries omit banned records; underlying row retention and join rejection remain
necessary rather than assuming the adapter can inspect every ban.

Confidence is high for the reproduced adapter and direct-service boundaries.
Real two-node permission calls, authenticated state application, replay/ordering,
automatic transport recovery and actual browser follow remain completion work.

### Final permission gates — 2026-09-28

Final source passes 5,292 unit, 74 smoke and 284 integration tests, repository
lint and Release build. Web lint, controller/fetch CSRF, anonymous endpoint,
identity and whitespace gates pass. The unchanged Web implementation retains
the preceding 998-test full suite and frontend/bundle/output validation.

The updated actual two-client runtime passes against the final Release build.
A banned client's leave returns 404, rejoin returns 400, hub rejoin and snapshot
read remain denied, and the other member keeps receiving updates. This adds
ban-retention evidence to the preceding 19-case runtime suite; it is not a new
claim that cross-node permission calls or browser follow were exercised.

## Routed room follow and recovery — 2026-09-28

Listen-along controls now appear on the actual Messaging V2 pod-channel routes,
with direct messages and Soulseek rooms excluded. Compact and expanded Follow
buttons expose pressed state; directory and mesh-streaming opt-ins do likewise.
Mobile controls have 44-pixel minimum targets and supported visible glyphs.
Paused snapshots align to within an explicit 50-millisecond tolerance instead
of retaining the playing drift allowance.

A real Chromium workflow verifies local audio progress, Pause and Seek, a held
SignalR negotiation outage, automatic rejoin plus fresh snapshot, keyboard
follow changes and Stop. It checks 320/390-pixel controls and screenshots.
The actual two-client subscription case also passes. Two real TLS endpoints
verify pod identity, private admission, approved membership, revocation and ban
retention in both call directions. The messaging collaborator is a spy, so that
test establishes dispatch permissions, not signed message persistence.

Offline browser fixtures now explicitly disable DHT; radio mesh fixtures keep
LAN-only discovery with no public bootstrap routers. Prior no_connect isolation
claims were wrong: retained startup logs proved public DHT activity. Generated
configuration and actual startup logs are now asserted by the browser case.

Retained radio logs connect the failed reverse-directory lookup to a message-rate
disconnect. The exact burst sequence and production pacing repair remain open;
the interrupted combined runtime run is not a passing gate. Keep the inbound
quota intact when repairing legitimate outgoing traffic. Confidence is high for
the reproduced UI, pause, isolation and permission boundaries, and moderate for
the radio failure explanation until a focused reproduction proves its sequence.

Remaining work includes player-owned follow continuity across navigation,
host playback-event publication, authenticated cross-node state application,
remote/long capability renewal, sustained resource and browser/format/device
validation. This batch does not establish those requirements.

### Additional LAN-only boundary finding — 2026-09-28

Retained radio-node startup logs report LanOnly=true, then dozens of DHT nodes
and public peer discovery. The current offline fixture disables DHT entirely
and passes; explicit LAN-only settings alone do not prove radio isolation.
Further radio runtime runs remain stopped until the installed engine's bootstrap
boundary is repaired and directly verified. This may also contribute unrelated
traffic to the observed message-rate disconnect. Confidence is high for the
recorded public discovery and unknown for its precise dependency-level cause.

### Routed room final gates — 2026-09-28

Final source passes 5,293 unit / 74 smoke / 284 integration tests, 1,005 Web
tests across 165 files, and 18 runtime cases: 16 ordinary player cases, one
routed browser follow/recovery workflow and one actual two-client room case.
Repository/Web lint, frontend build, bundle/output, controller/fetch CSRF,
anonymous endpoint, identity and whitespace gates pass. Mobile screenshots
were inspected at 320 and 390 pixels. Radio cases are excluded from this gate
because LAN-only discovery requires repair; the interrupted combined run is
not counted. The previous Release executable has unchanged production C#.

This implementation batch is complete after commit/push and exact-range release
preview. Next Steps: repair LAN-only DHT bootstrap, preserve mesh quota while
pacing legitimate bursts, then follow ownership across navigation and distributed
state delivery. Full player goal remains active. No tag or deployment is authorized.

## LAN-only radio isolation and diagnostics — 2026-09-28

Decompiling the exact installed MonoTorrent assembly established the leak's
cause: its initialization replaces an empty router list with three public
bootstrap defaults and caches bootstrap nodes statically. LAN-only now skips
constructing/starting the public engine and saved public node tables. Shared
UDP overlay/QUIC routing is initialized independently when configured; known
peer transport and connection maintenance remain available. Explicit public
announce/discovery requests and DHT peer callbacks honor the same boundary.
Ordinary public-mode defaults remain unchanged. ADR-0016 records this decision.

A negative lifecycle regression held overlay startup through Stop and reproduced
a stale completed-start marker. Cancellation is rechecked and startup/beacon
state is cleared after awaiting initialization. Five LAN-only regressions cover
engine absence, shared listener initialization, peer callback rejection,
idempotent start and stop during initialization. The focused suite passes 37.

The full-instance integration runner also configured public routers for local
workflows. It now uses LAN-only rendezvous and no bootstrap routers. All 5,298
unit / 74 smoke / 284 integration tests pass with that configuration. Real
radio playback, seeking, permission revocation, reverse-directory delivery and
elapsed ticket expiry/manual reselection pass. Both actual nodes expose zero
public DHT nodes with a live mesh connection; retained logs show no public
engine startup, bootstrap, discovery or announcements.

Three negative Web regressions reproduced a false DHT-not-running health penalty
and a missing LAN-only explanation. Diagnostics now treat the absent public
engine as expected in LAN-only mode while retaining public-mode warnings.
Both field spellings are covered. All 1,007 Web tests in 165 files pass.
Repository/Web lint, Release/frontend builds, bundle/output, endpoint and
identity/whitespace gates pass. A complete 20-case runtime run is still live
and must finish before source commit/push; its network page also checks the
actual LAN-only snapshot and absence of a false public-engine warning.

Confidence is high for the observed isolation, lifecycle and diagnostic fixes.
The earlier reverse-directory failure coincided with a message-rate disconnect;
a passing repaired radio run does not establish burst handling under sustained
or concurrent legitimate calls. Keep the inbound quota unchanged and reproduce
that traffic separately. The full player goal remains active, with follow
continuity across navigation, host event publication, authenticated distributed
room state, capability renewal and sustained/device/browser/format work due.

### Final ownership validation — 2026-09-28

The first complete runtime run passed all 20 cases and its actual network-health
page verified a connected LAN-only mesh without the false public-engine warning.
A subsequent ownership review moved resource detachment after canceling and
awaiting initialization, closing a possible late listener assignment outside the
cleanup set. The final Release build passes with only its two existing dependency
support warnings. Full backend and 20-case runtime validation now run against
this final ownership ordering before source commit/push.

### Retained bootstrap interruption — 2026-09-28

The final-ownership complete runtime attempt passed 19 cases and failed one
queue-switch case during shared login setup, before any player action. Six
ERR_NETWORK_CHANGED resource errors left an empty React root and blank retained
screenshot. This does not establish a queue defect. The originating network
change remains unknown. All preceding runtime/backend processes are terminal;
the unchanged complete suite is rerunning with traces enabled. No test retries,
product code, timeout or assertions were changed to conceal the failure.

### Final LAN-only gates — 2026-09-28

The final ownership ordering passes 5,298 unit / 74 smoke / 284 integration
tests, 1,007 Web tests in 165 files and all 20 runtime cases in the unchanged
complete rerun with traces retained. The earlier 19-pass bootstrap-interrupted
attempt remains documented; its originating network change is unknown and is
not claimed fixed. The previously blocked login/queue workflow passes in the
complete rerun. No assertion, timeout, product code or retry policy was weakened.

Actual radio nodes retain live mesh connections with zero public DHT nodes;
no public engine startup, bootstrap/discovery/announce or message-rate disconnect
appears in the final retained node logs. The actual network-health page reports
that connected LAN-only state without a false engine warning. A direct scoring
check also verifies public-mode absence still warns. Trace-enabled timing is
correctness evidence, not a new player performance measurement.

Repository/Web lint, final Release/frontend builds, bundle/output,
controller/fetch CSRF, anonymous endpoint, identity and whitespace gates pass.
This implementation batch is complete after exact-range release preview and
commit/push. Next Steps: bounded legitimate mesh RPC bursts preserving inbound
quota behavior; player-owned follow across navigation; ongoing host control
publication; authenticated distributed state, capability renewal and remaining
resource/browser/format/accessibility/device requirements. Full goal remains
active. No tag or deployment is authorized.

## Persistent room following — 2026-09-28

The prior build failed a real routed workflow: follow a room, use the actual SPA
Downloads link, publish Pause at 16 seconds, and observe that audio never aligns.
The repaired player retains the room connection independently of its panel.
The focused rebuilt workflow now aligns offscreen, returns with Follow selected,
and does not open another connection. This proves navigation continuity for
that observed browser and same-node room; it does not prove distributed state.

The owner reuses a single hub per room and bounds distinct rooms to two. Tests
cover unrelated-room revocation, reconnect snapshot deduplication, offscreen
host Stop, provider disposal and the connection bound. A negative delayed-empty
snapshot test reproduced a stale-render follow-switch race; owned previous state
now governs ended-room detection. The player renders waiting state without a
missing host and displays room connection feedback even away from the panel.
Final Web evidence is 1,016 passing tests in 166 files, alongside all 5,656
backend tests and lint/build/bundle/output gates. The final complete browser
suite remains pending before commit/push, including local Stop away from a room.

The retained 320-pixel screenshot confirms the room controls fit and remain
44 pixels. It also shows small auxiliary player tools and clipped metadata;
those need broader responsive and accessibility work. This is correctness and
layout evidence, not sustained memory/CPU or physical-device evidence.

Remaining completion work still includes legitimate outgoing RPC bursts,
ongoing host control publication, authenticated cross-node room state,
capability renewal, fixture resource cleanup and sustained/browser/format/
physical-device/assistive-technology validation. Automatic transport recovery
and navigation continuity have direct evidence; neither substitutes for those
remaining requirements. The earlier blank-login network change is unexplained.

### Final persistent-follow gates — 2026-09-28

The final rebuilt complete suite passes all 20 cases in 4.8 minutes with traces
retained. Actual SPA navigation preserves host Pause and returning Follow state
without another connection. Local Stop while offscreen clears playback; a later
host Play and returning room snapshot do not restart it. Automatic transport
recovery, two-participant membership, radio playback/seek/revocation/reverse
publication, elapsed ticket expiry, queue/format/crossfade and layout cases pass.

Final Web gate: 1,016 tests in 166 files. Backend: 5,298 unit / 74 smoke / 284
integration tests. Repository/Web lint, Release/frontend builds, bundle/output,
CSRF/anonymous endpoint, identity and whitespace gates pass. Release build keeps
its two existing dependency support warnings. Retained runtime logs show no
public DHT startup or message-rate disconnect. Fixture file-handle GC warnings
remain visible and tracked; they are not claimed fixed. Trace-enabled timing is
not a new performance baseline. Earlier unrelated bootstrap failure remains
unexplained. Source publication requires exact-range release preview and fork
verification; no tag or deployment is part of this batch.

Next Steps: repair legitimate outgoing mesh RPC bursts while preserving inbound
quota behavior, publish ongoing host controls, implement authenticated cross-node
room state and renewal, and complete sustained/browser/format/physical-device/
assistive-technology work, responsive auxiliary controls and fixture cleanup.
The full player goal remains active.

## Responsive player controls — 2026-09-28

Negative real-browser regressions reproduced a 24-pixel mobile rating button,
a hidden track header after closing Tools, and a 36-pixel playback button in a
touch-enabled 768-pixel context. Rating and visual controls now use responsive
rows; touch layouts activate for narrow width or a coarse primary pointer.
Rating text no longer truncates, selected controls expose pressed state, and
closing Tools returns scroll/focus to the main deck. Optional visual controls
remain under the existing Tools toggle on touch layouts. Desktop visual buttons
have at least 24-pixel targets; touch controls have at least 44 pixels.

Focused layout evidence covers 1440/768/390/320 widths, auxiliary/compact bounds,
keyboard rating and focus outline. An initial measurement test incorrectly
queried an intentionally hidden compact button; it now measures rendered
accessible controls and retains explicit hidden-state checks. Complete final
browser evidence remains pending against the rebuilt source, including a new
touch-enabled tablet case. All 1,018 Web tests and 5,298 unit / 74 smoke / 284
integration tests pass. This work does not establish physical-device or
assistive-technology completion, and trace timing is not performance evidence.

The full player goal remains active: mesh burst handling, ongoing host controls,
authenticated distributed room state, renewal, sustained resources, supported
browser/format/device/accessibility work and fixture cleanup remain open.

### Final responsive-control gates — 2026-09-28

The final complete rebuilt suite passes all 21 cases in 4.7 minutes, including
the new touch-enabled tablet workflow and broader compact/expanded controls.
Actual browser evidence verifies 44-pixel rating/visual/visible compact controls
on narrow screens, coarse-pointer tablet control sizing, complete rating text,
pressed state, keyboard rating without pausing audio, visible track title and
actual playback focus after closing Tools. Final screenshots were inspected
without hover popups obscuring the controls. Outline presence does not establish
contrast or assistive-technology completion; those remain explicit follow-ups.

All 1,018 Web tests in 166 files and 5,298 unit / 74 smoke / 284 integration
tests pass. Repository/Web lint, final Release/frontend builds, bundle/output,
CSRF/anonymous endpoint, identity and whitespace gates pass. The two existing
Release dependency warnings remain. Retained logs show no public DHT startup
or message-rate disconnect. Fixture file-handle GC warnings remain tracked.
Trace-enabled timing is not a new performance baseline. This batch is ready
for exact-range release preview and fork-verified source publication.

Next Steps remain broad: legitimate mesh bursts, ongoing host control events,
authenticated distributed room state, renewal, sustained resources, supported
browser/format/device/accessibility work and fixture cleanup. Full player goal
remains active; no release tag or deployment is included.

## Listed-radio mesh RPC pacing — 2026-09-28

Ordinary sequential radio metadata requests reproduced a quota disconnect on a
real TLS connection before the fix. Calls and replies now share a bounded
connection-owned writer with a 140-millisecond interval. Receive loops enqueue
replies without delaying ingress checks; controls bypass pacing. Actual sent
radio payloads trigger upload accounting, and cancellation, overflow, failure
and shutdown release owned work. ADR-0018 records the design.

Ten focused real TLS cases pass, covering forward/reverse metadata bursts,
raw ten/eleven-frame boundaries, canceled entry skipping, control progress,
queue overflow and observed writer failure. The first full backend gate exposed
eleven constructor-bypassing fixture disposal failures; registry/router/peer-sync
fixtures now invoke the real constructor. Gotchas 0z1146 and 0z1147 record both
failures. All 1,018 Web tests and Web lint pass. Frontend build and bundle/output
gates pass. Final backend, Release and complete runtime gates remain pending.

This proves focused burst handling, not sustained high-rate radio formats or
arbitrary control bursts. Ongoing host publication, authenticated cross-node
room state, renewal, sustained resources, browser/format/device/accessibility
coverage and fixture cleanup remain open. Full player goal remains active.

### Final mesh unit gate — 2026-09-28

The final corrected source passes all 5,304 unit tests and 74 smoke tests,
including eleven real TLS transport cases. Caller cancellation now skips
waiting entries while allowing a committed frame to finish under connection
ownership. Active write timeouts remain failures even when the caller canceled.
Integration, final Release and rebuilt runtime gates remain pending.

### Next host-publication implementation boundary

Code review confirms that manual room publication and its serialized request
chain still belong to PodListenAlongPanel. Leaving that panel loses publication
ownership. The persistent PlayerProvider already owns following, the current
track and the audio element. The next host-control implementation should retain
an explicitly started broadcast there, observe actual Play/Pause/Seek/track
changes, preserve ordered writes and room identity, and stop publication on
explicit Stop or loss of permission. Position reporting must use the existing
player position mapping, including transcoded playback, rather than adding a
second clock or continuous network polling. No host capability is claimed fixed
by the mesh transport work.

### Final mesh pacing gates — 2026-09-28

All 5,304 unit / 74 smoke / 284 integration tests and 1,018 Web tests in 166
files pass. Eleven actual TLS cases cover forward/reverse RPC bursts, raw
ten/eleven-frame boundaries, queued/committed cancellation, control progress,
overflow, writer failure and shutdown. Repository/Web lint, frontend and final
Release builds, bundle/output, controller/fetch CSRF, anonymous endpoint and
identity/whitespace gates pass. Final Release retains its two existing dependency
support warnings. A newly introduced comment-spacing warning was corrected
before the final rebuilt browser run; the interrupted run is not counted.

The complete final browser suite passes all 21 cases in 4.9 minutes, including
real two-node radio playback/seeking, reverse directory delivery, revocation,
elapsed ticket expiry/manual reselection, room following/recovery, media controls,
queue/format/crossfade/analyzer/PiP and responsive/touch cases. Final browser
metadata reports passed with no failed tests; a final expanded screenshot was
inspected. Retained node logs contain no message-rate disconnect or public DHT
engine startup. Fixture file-handle GC warnings remain tracked, and trace-enabled
resource samples do not establish sustained performance.

This batch is complete for source publication after exact-range release preview
and fork verification. Next Steps: persistent ongoing host publication,
authenticated distributed room state and renewal; sustained resources and
throughput; supported browser/format/device/assistive-technology and focus
contrast validation; fixture cleanup. The full player goal remains active.
No tag, release or deployment is included.

## Persistent room host controls — 2026-09-28

PlayerProvider now owns an explicitly started host session through a dedicated
React hook. The panel delegates publication; Play/Pause/Seek and loaded-track
events publish mapped positions across navigation. Paused seeks preserve Pause.
One active request and one coalesced pending update bound work; a monotonic
250-millisecond interval limits publication without periodic position polling.
Stop follows in-flight work, failed queued requests reject, and Follow requires
a confirmed host Stop. Existing room connections and server authorization are
reused. Revocation, replacement, ended broadcast, local Stop, hide and unshareable
source selection release ownership. Persistent Retry/Stop controls expose failure.

All 1,036 Web tests in 167 files and 5,304 unit / 74 smoke / 284 integration
tests pass. Repository/Web lint, frontend/Release builds and bundle/output gates
pass before the new real two-browser workflow. Focused and complete browser
evidence remains pending. ADR-0019 and gotchas 0z1149–0z1151 record ownership,
Stop acknowledgment and publication-frequency decisions.

This does not establish authenticated cross-node delivery, long capability or
server lease renewal/cleanup, sustained resources, browser/format/device/assistive
coverage, focus contrast or fixture cleanup. Those remain open; full player goal
remains active. No release tag or deployment is included.

### Host publication identity and compact-mode checks — 2026-09-28

The complete pre-polish gate passed all 22 cases. Strengthened actual host
workflows then passed compact broadcasting feedback, collapse/expand ownership,
Tools closure and full title/Stop bounds at 320 pixels. A clean screenshot was
inspected without popups. The listed-radio workflow also verifies directory
removal after both retained-host Stop and reload/manual Stop.

Review repaired lost observed party identity: new host sessions seed from the
room snapshot, and unknown explicit Stop identity uses one abortable snapshot
inside the owned writer. Manual Stop of an unowned room no longer detaches a
different followed room. Gotchas 0z1152–0z1154 record these corrections. Final
rebuilt complete runtime and updated Web gates are running before publication.
The full player goal remains active; remaining scope is unchanged.

### Final persistent-host gates — 2026-09-28

The final source passes all 1,040 Web tests in 167 files and the rebuilt complete
22-case runtime suite in 5.2 minutes. Actual two-browser hosting covers SPA
navigation, Pause, paused/playing Seek, steady playback without extra
publications, track replacement with stable party identity, compact ownership
text fitting at 320 pixels, title/44-pixel Stop bounds with Tools closed, and
Stop that leaves host audio available while ending listener following. Listed
directory entries are removed by retained-host Stop and reload/manual Stop.
Clean compact and expanded screenshots were inspected. Final runtime metadata
reports passed with no failed tests.

The backend gate passes 5,304 unit / 74 smoke / 284 integration tests. Repository
and Web lint, frontend/Release builds, bundle/output, controller/fetch CSRF,
anonymous endpoint, local identity and whitespace gates pass. The final bundle
remains 94 assets / 3.68 MB with its lazy visualizer exception. Release retains
two existing dependency support warnings. Retained nodes show no quota
disconnect or public DHT startup. Fixture file-handle GC warnings remain open;
trace-enabled samples do not establish sustained resource performance.

This host-control batch is complete for validated source publication. Next Steps:
media-error/auth/lease/renewal behavior, authenticated distributed room state,
radio unlisting/replacement cleanup, publication delay/drift and sustained
resources, supported browser/format/device/assistive-technology/focus contrast
coverage and fixture cleanup. Full player goal remains active. No tag, release
or deployment is included.

## Radio directory withdrawal — 2026-09-28

Three negative service cases reproduced stale directory entries after unlisting,
replacement and Stop with a different ID. The fix keeps accepted room state
and local directory cleanup together, rejects stale refresh resurrection,
retains withdrawal retries and relisting, and serializes index mutations from
separate rooms on one server. Ordinary never-listed playback adds no DHT lookup.
Service race/retry and actual host/two-node runtime coverage are being validated.
Cross-node DHT index compare-and-swap remains unimplemented; Distributed conflict resolution remains open.

Fixture follow-up evidence: the node harness retains open stdout/stderr file
handles after successful startup, appends unbounded output strings, appends
stderr twice outside DEBUG mode, and leaves a five-second force-kill timer after
normal exit. Async data listeners also leave file-write failures unowned. These
need deterministic cleanup before using the harness for sustained measurements
of an entire session. Browser-only samples remain distinct from harness usage.

Ownership review additionally reproduced both live listings disappearing when
Stop or unlist referenced another room's ID. Cleanup now binds to accepted room
state; Stop messages use current room identity, and pending cleanup is tracked
separately from stale-read suppression. Thirteen added service cases cover these
boundaries and private playback after confirmed withdrawal. ADR-0020 records
ownership, retries, lifetime and the remaining distributed-index limitation.
A final build-worker failure (MSB4166) was terminal; its diagnostic directory
was unavailable. Retrying the same source with one build worker succeeded with
only the two existing support warnings. Its cause remains unknown.

### Final room-directory withdrawal evidence — 2026-09-28

The final corrected source passes all 5,317 unit / 74 smoke / 284 integration
tests and 1,040 Web tests in 167 files. Thirteen added service cases cover
withdrawal, stale refreshes, failed-index retry, renewal/relisting, private
playback after confirmed cleanup, concurrent rooms and preservation of another
room's listing on mismatched/repeated Stop or unlist.

The complete final rebuilt browser suite passes all 22 cases in 5.3 minutes.
Actual host API actions unlist/relist/replace/Stop while preserving a second
listed room. Actual two-node TLS directory reads observe unlisting, relisting,
replacement and Stop without a party ID. Final browser metadata reports passed
with no failed tests; the expanded 320-pixel host screenshot was inspected.
Retained node logs contain no quota disconnect or public DHT startup.

Repository/Web lint, final Release, controller/fetch CSRF, anonymous endpoint,
identity and whitespace gates pass. Both new release fragments validate. One
terminal MSBuild worker failure was retried successfully with one worker;
its cause remains unknown. Release retains the two existing support warnings.
Fixture file-handle warnings remain tracked, and short trace-enabled browser
samples do not establish sustained performance.

The room-directory task is complete. Next Steps: active-host settings application,
fixture lifecycle cleanup, authenticated distributed room state and index
conflict handling, host error/auth/lease/renewal behavior, sustained resources
and throughput, supported browser/format/device/accessibility/focus contrast
coverage. The full player goal remains active.

## Active host sharing controls — 2026-09-28

Two negative control cases reproduced missing active listing publication. A
stronger navigation case reproduced staged sharing choices leaking between
rooms. The owned writer now applies active changes, separates requested and
confirmed permissions, keeps Stop ordered, and retains explicit Retry on failure.
Unlisting clears streaming opt-in. Expanded controls have associated labels.
Thirteen added cases cover these paths within a passing 60-case focused suite.
The complete rebuilt runtime permission workflow is pending.

### Narrow room playback layout and ownership-loss feedback — 2026-09-28

A real 320-pixel negative regression measured Stop extending to 382 pixels. The
conversation view occupied the rail column and inserted playback into a layout
with only three rows. The corrected layout spans the mobile columns, constrains
its inner column and gives room playback its own automatic row; actions can wrap
with 44-pixel targets. Runtime assertions cover every room action's bounds.

Two further negative regressions reproduced generic publication feedback replacing
HTTP 403 or ownership-loss cancellation feedback. Explicit 403 handling and
cancellation preservation now pass, including both DOM and HTTP cancellation.
Gotchas 0z1160 and 0z1161 were committed immediately. Final browser validation
is running; this source change alone does not prove touch or layout behavior.

### Live settings and mobile control verification — 2026-09-28

All 63 focused panel/writer cases pass, including 16 added paths, and all 1,056
Web tests pass in 167 files. The rebuilt host workflow verifies live settings
while playing and paused, explicit Retry after storage failure, anonymous stream
capability revocation, fresh streaming opt-in after relisting and preservation
of playback. At a 320-pixel viewport, all five room actions measure 44 by 44
pixels and remain visible; Stop ends at 261 pixels. The failure/Retry screenshot
and expanded player screenshot were inspected.

The mobile conversation grid now spans its columns and allocates a separate
playback row. HTTP 403 and canceled publications retain access-revocation
feedback. Gotchas 0z1160 and 0z1161 document the reproduced failures. Final
Release, repository/Web lint, bundle/output, CSRF/anonymous endpoint and identity
gates pass; Release retains two existing build-task support warnings. The
unchanged backend source has passing 5,317 unit / 74 smoke / 284 integration
evidence. Full browser validation is still running. Fixture log-handle warnings
remain open and prevent treating this short runtime suite as sustained resource
evidence. The full player goal remains active.

### Final live-settings batch evidence — 2026-09-28

The complete final rebuilt browser suite passes all 22 cases in 5.5 minutes;
terminal exit is zero and browser metadata reports passed with no failed tests.
The real two-minute ticket-expiry test passed. Retained node logs contain no
quota-exceeded or public-bootstrap match. Both release fragments validate.
All 63 focused / 1,056 total Web tests, final Release and repository/Web lint,
bundle/output, controller/fetch CSRF, anonymous endpoint and identity gates pass.
The backend is unchanged from the passing 5,317 unit / 74 smoke / 284 integration
run. Browser TypeScript executes in Playwright and is outside Web ESLint scope.

Live settings, ownership-loss feedback and mobile room-control repairs are
complete for source publication. Next Steps: clean up fixture log handles and
shutdown before sustained resource measurement; then address distributed state
and index ownership, host error/auth/lease/renewal, sustained throughput and
browser/format/device/assistive-technology/focus contrast coverage. Fixture GC
warnings remain reproduced; short trace-enabled runs do not prove low resource
use across a sustained session. The full player goal remains active.

## Isolated player fixture lifecycle — 2026-09-28

Ten focused Node-environment lifecycle tests pass. A negative timer regression
proved successful Stop left a hard-kill deadline alive, and a held-peer negative
regression proved cleanup returned before every node finished. File streams now
own complete disk logs and drain after child close; in-memory diagnostics retain
only the last 64 Ki characters per stream. Stderr is retained once. Spawn and
log failures surface through awaited startup/cleanup; failed starts clean up
before escaping registration. Concurrent/repeated Stop shares cleanup, force
kill waits for close, and every peer settles before aggregated failures return.
Exit diagnostics also have an awaited owner. Gotchas 0z1162 and 0z1163 were
committed immediately. The explicit Node TypeScript check passes.

This batch is internal-only validation infrastructure. Final rebuilt playback
assets remain frozen from the prior verified source. The full browser suite is
running with the corrected harness to check that the reproduced descriptor GC
warnings are gone. Sustained player resource use is still unproven; full player
goal remains active.

### Final fixture lifecycle evidence — 2026-09-28

All 10 focused lifecycle tests and all 1,066 Web tests in 168 files pass. The
explicit Node TypeScript check passes with Node types enabled, as do repository
and Web lint, bundle/output, identity and CSRF/anonymous endpoint gates. The
complete 22-case real browser suite passes in 6.0 minutes with terminal exit zero
and passed metadata. Its log contains zero descriptor-GC warnings and zero
unhandled-rejection markers. The earlier uncorrected run reproduced descriptor
warnings. Temporary media fixtures were cleaned up. Application assets and
backend source remain unchanged from the prior validated Release and backend
suite. The internal-only validation release fragment parses without errors.

Fixture lifecycle cleanup is complete for source publication. Next Steps:
establish sustained idle/native playback/paused CPU and memory samples without
tracing or concurrent builds, ensure playback remains active for the full sample,
and extend to queue/visualizer/radio throughput. Distributed room state/index
ownership, host error/auth/lease/renewal, compatibility/device/accessibility
and focus contrast remain open. Ten-second trace samples are diagnostic evidence
and do not prove sustained low resource use. The full player goal remains active.

## Isolated repeated native resource measurement — 2026-09-28

The resource probe now lives in its own spec with top-level video/trace off, a
page.video() assertion, configurable repeated windows and workload checks. A
shared generated PCM fixture keeps extended playback active for its entire
measured phase. Idle has no loaded source, playing must advance without ending,
and paused playback must retain source and position. Window reports retain CPU
process coverage, Linux PSS availability, renderer/script work, heap and audio
context count. Each window is saved before validation. A documented package
command runs the probe without recorded functional QA.

The explicit main-player/resource TypeScript gate now passes. Shared navigation
predicates use URL.pathname/href, nullable DOM diagnostics normalize text and
HTML audio boundaries are typed. Gotchas 0z1164 through 0z1167 record the browser
global type error, URL predicates, observed video encoder contamination and
Playwright's file-scope worker option requirement. The contaminated run was
stopped explicitly; it is not a resource baseline.

The corrected two-window-per-state measurement is live with frozen input hashes,
no video encoder and no concurrent build/test jobs. Both 60-second idle windows
pass with complete process/PSS coverage, zero AudioContexts, 0.50–0.62% of one
core and 271–272 MiB whole-browser PSS. Playing and paused evidence is pending.
This is a measurement batch, not proof that the full low-resource goal is met.

### Repeated native baseline results — 2026-09-28

One isolated headless Chrome for Testing 153.0.8010.12 run on Linux x86_64 /
AMD Ryzen 9 9950X3D passed all six 60-second windows, with 15-second warmup per
state. The terminal result is passed (6.8 minutes), metadata has no failed tests
and frozen probe/helper/tone hashes match. Video was absent, tracing was off
and no build, lint or other test jobs ran concurrently with these windows.

| State | CPU (% of one core, weighted mean) | Whole-browser PSS range (MiB) | JS heap endpoints (MiB) | Processes sampled |
| --- | ---: | ---: | ---: | ---: |
| Idle | 0.558 | 270.97–271.99 | 9.70 → 10.61 | 4 |
| Playing | 2.008 | 350.99–353.67 | 20.13 → 20.88 | 5 |
| Paused | 0.508 | 330.77–337.45 | 20.24 → 22.38 | 5 |

All windows have zero added/removed processes, zero unavailable PSS readings
and zero created AudioContexts. Playing advances continuously from about 15.5
to 135.6 seconds without ending. Paused windows retain their source and exactly
the same 135.640435-second position. CPU returns toward idle after pausing.

These are whole-browser results with a generated 195-second mono PCM source,
not incremental player-only or backend costs. Heap endpoints increase within
each phase, with a drop between the last playing and first paused reading;
this does not establish either a leak or a sustained memory plateau. Confidence
is high in this run's recorded evidence and moderate in its generality. Next:
longer natural-GC sessions and queue/analyzer/visualizer/radio workloads, plus
format/browser/device/accessibility/focus contrast coverage and distributed
state/index ownership, host error/auth/lease/renewal. The full goal stays active.

Final functional and repository gates are running separately from the benchmark.
Their short resource samples must not replace the isolated baseline above.

### Membership collision found by the final gate — 2026-09-28

The first backend full gate failed ordinary SQLite leave/rejoin (5,316 unit
passes / one failure). Two deterministic negative regressions reproduced a
rejected rejoin and a unique-constraint ban failure under frozen/backward clocks.
Membership history's key includes pod, peer and Unix milliseconds; timestamps
now use max(current time, prior persisted event plus one) within the membership
write transaction. Leave and ban follow join's transaction discipline. The
existing schema/API remains intact, and optional TimeProvider follows the
listening-party pattern. Gotcha 0z1168 was committed immediately; ADR-0022
records local event ordering and its distributed/clock boundaries.

All nine focused SQLite service cases pass, including four added cases for
frozen/backward clocks, restored instances, per-pod/peer history, ban retention
and eight concurrent joins. The first 22-case browser gate passed in 6.1 minutes
before the corrected backend Release; a final rebuilt gate is still required.
The isolated native benchmark completed before backend validation and remains
valid. Full corrected backend, lint and Release gates are running.

### Writer-control deadline boundary — 2026-09-28

The next full backend run passed corrected membership cases but canceled one
mesh writer-control setup at its ten-second handshake deadline. All four modes
passed separately. Cold server/client RSA preparation shared that deadline.
Certificates now prepare before the timed protocol scenario and have explicit
disposal; the same ten-second deadline and all handshake/control/writer checks
remain. Four corrected modes pass. Gotcha 0z1169 records the observed failure
and the unmeasured scheduling contribution. This is internal-only test setup;
no production transport timeout changed.

Corrected Release and lint pass. The complete rebuilt browser gate is live.
The earlier backend gate is terminal (5,320 unit / one handshake setup failure,
74 smoke and 284 integration passed), and the final backend gate has started
with the corrected test boundary. This is not yet final publication evidence.

### Final repeated-resource and membership batch evidence — 2026-09-28

Final corrected source passes 5,321 unit / 74 smoke / 284 integration tests,
1,066 Web tests in 168 files and all 22 rebuilt browser cases. Terminal backend
and browser results are zero; browser metadata is passed with no failed tests.
The browser run contains zero descriptor-GC and unhandled-rejection markers.
The explicit main-player/resource TypeScript gate, documented command discovery,
repository/Web lint, Release, bundle/output, CSRF/anonymous endpoint and identity
gates pass. Two release fragments parse without errors. Release retains the two
existing build-task support warnings. Application frontend source is unchanged;
backend Release includes the membership ordering correction.

The isolated six-window native benchmark completed before these gates, with
complete process/PSS coverage and frozen input hashes. It remains separate from
the short resource samples in functional QA. Membership ordering has four added
regressions and nine focused cases; four real writer-control modes pass with
certificate preparation outside their unchanged protocol deadline. No new
production transport timeout or database migration is introduced.

- [x] 2026-09-28: Establish reproducible repeated native idle/play/pause resource
  measurement without video/tracing, with workload and coverage evidence.
- [x] 2026-09-28: Fix rapid SQLite membership history collisions and clock rollback
  ordering across service instances while preserving bans and concurrent joins.
- [ ] Follow longer native sessions to distinguish heap allocation/GC cycles from
  retained growth; do not infer a plateau from two windows per state.
- [ ] Extend resource budgets to queue, analyzer, visualizer, Picture in Picture,
  remote radio/high-rate formats and backend cost under sustained workloads.

Next Steps also retain distributed room state/index ownership, host media
error/auth/lease/renewal and browser/format/device/accessibility/focus contrast
coverage. The full player goal remains active; this batch does not establish
whole-session resource completion.


## Ten-minute-per-state disk-file native evidence — 2026-09-28

The isolated run completed with terminal zero, one passed browser case in
30.8 minutes and passed metadata with no failed tests. All 30 windows passed;
probe/helper/tone hashes match. The selected disk fixture is 675-second mono
PCM (29,767,544 bytes), using HeadlessChrome 153.0.8010.12 on Linux. Video and
trace were off, no forced GC was used and no test/build/lint gates ran during
measurement. Runtime application assets preceded the host-error source fix.

| State | Windows | CPU (% of one core, weighted mean) | CDP-process PSS range (MiB) | Renderer JS heap range (MiB) |
| --- | ---: | ---: | ---: | ---: |
| Idle | 10 | 0.542 | 266.84–277.50 | 8.22–11.13 |
| Playing | 10 | 1.970 | 287.74–316.02 | 8.99–10.00 |
| Paused | 10 | 0.523 | 290.89–295.30 | 10.06–12.12 |

All windows have zero created AudioContexts, zero CDP process churn and no
unavailable PSS readings in that list. Playback advances through the full
playing phase; all paused windows retain exactly 615.697574 seconds. Heap and
PSS show natural drops rather than monotonic retained growth. Playing DOM
counts span 1064–1073 and listeners 548–646. Paused counts fluctuate and drop;
those endpoint counters do not independently prove retained objects or a leak.
Confidence is high in this run, moderate in its generality.

Scope is CDP-reported browser processes: OS zygotes are omitted, as disclosed
above. This is one native format/browser/host, not backend, incremental player,
full OS-tree, queue/analyzer/visualizer/radio or multi-hour resource completion.
The longer input also differs from the prior 195-second RAM-buffer workload;
do not attribute their numeric differences solely to file backing.

Raw windows, metadata, frozen input hashes and a visually inspected trend plot
are retained as local validation artifacts. The independent full-tree collector
and its 11 focused Node cases now pass their initial type/test gates; PID reuse
during memory sampling and actual browser-tree coverage still need validation
before integration. Full player goal remains active.


## Repeated observed OS-tree native baseline — 2026-09-28

The final rebuilt source passed a separate isolated six-window run in 6.8
minutes (terminal zero; passed metadata). Each state has two 60-second windows
following a 15-second warmup. The selected disk file is 195-second mono PCM,
8,599,544 bytes, in HeadlessChrome 153.0.8010.12 on Linux. Video/trace were off,
no forced GC or concurrent validation jobs were used, and all 127 frozen
probe/harness/application/build hashes match after completion.

| State | Observed OS processes | OS CPU (% of one core, weighted mean) | Observed OS-tree PSS range (MiB) | CDP-process PSS range (MiB) | JS heap range (MiB) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Idle | 6 | 0.566 | 288.97–289.94 | 269.26–270.22 | 8.89–10.36 |
| Playing | 7 | 1.865 | 321.77–325.38 | 304.04–307.66 | 9.06–9.07 |
| Paused | 7 | 0.541 | 314.52–320.45 | 296.82–302.73 | 10.31–11.49 |

Every observed process retained its PID/start-time identity across its window;
there is zero observed churn and no unavailable PSS reading among those
processes. Three endpoint scans each failed to read one global proc stat
(two playing endpoints and one paused endpoint). Their ancestry is unknown;
this prevents claiming a complete atomic browser census. The report discloses
these enumeration gaps separately from complete readings of the observed tree.
CPU remains an endpoint-based measure of surviving identities, not an account
of processes that might start and exit between snapshots. Confidence is high
in the retained measurements and moderate in their complete-tree coverage.

Both playing windows remain active through about 135.77 seconds. Both paused
windows retain exactly 135.802296 seconds. All windows have zero created
AudioContexts. Video files and descriptor-GC/unhandled markers are absent.
These results cover one native browser workload. They do not establish
multi-hour, format, device, queue, analyzer, visualizer, radio or backend budgets.

The host-error browser workflow passes against final rebuilt assets after an
old-assets negative proved audio continued despite the displayed error. Its
injected media-error event verifies active pause, published full position,
follower synchronization and explicit recovery; actual decoder-format failure
coverage remains the separate playback suite. Four new native/decoded and
active/standby regressions, 12 OS collector cases and all 1,082 Web tests pass.
All 22 rebuilt browser cases, 5,321 backend unit / 74 smoke / 284 integration,
expanded types, lint, builds and ancillary gates pass. Release retains its two
existing support warnings. The full player goal remains active.


## Failed playback setup and explicit recovery — 2026-09-28

Three component negatives reproduce missing paused host intent after native
Play rejection, decoded seek setup failure and crossfade start rejection. The
native case additionally proves a resumed context remains running when playing
never changes from false. Retained negative logs distinguish these from the
browser results; the extended crossfade browser check passed against the
previous assets and is not an independent reproduction of that regression.

The player now reports paused intent at the decoded requested absolute target,
pauses/reports owned failed Play, and publishes the paused crossfade replacement.
Terminal playback status reconciles existing processing even if playing never
changed, while loading leaves graphs available. A late rejected Play from a
replaced track cannot overwrite the newer playback. Four new regressions bring
the focused PlayerBar/host-writer total to 94 and the full Web total to 1,086
across 169 files. Web lint, explicit browser/helper TypeScript, frontend and
Release builds pass; Release retains two dependency support warnings.

Two authenticated Chromium sessions verify rejected crossfade startup, paused
new-track intent, suspended contexts, explicit retry and follower recovery. A
controlled ticket HTTP 503 during actual FFmpeg-decoded seeking leaves host and
follower paused at 25 seconds, and retry resumes decoded playback at that offset.
A separate actual-browser native Play-promise rejection verifies initial failure
quiescence and explicit retry. These controlled faults do not add unsupported
decoder-format claims. Full suite, backend/repository gates and publication are
pending at this checkpoint; final results will be appended after completion.

Pending native suspend/resume ordering remains independent unfinished work.
These checks do not establish physical output-device behavior, wider browser or
format support, sustained host renewal or multi-tab/distributed ownership. The
prior long resource measurements remain scoped to their recorded source assets.
The full player objective stays active. Confidence: high for the tested paths.


The first expanded 22-case browser run finished with 21 passes and one login
setup failure before queue actions. The retained screenshot shows a rendered
login form; the helper still awaited ten-second network idleness. A new real
browser regression holds an unrelated request open and reproduces that timeout.
Login now navigates to DOM readiness and retains its root/credential/submit/API/
authenticated-navigation assertions and deadlines. Gotchas 0z1179/0z1180 record
route cleanup and the readiness contract. This is internal validation work.
The complete corrected browser run remains required; a focused pass cannot
replace that gate.


## Corrected complete player browser gate — 2026-09-28

After the login-readiness fix, all 23 functional player cases pass in 3.7
minutes, including the two-node radio expiry flow, two authenticated host/follower
startup and decoded-ticket recovery, and the pending-network login regression.
The earlier 21/22 run is retained as the negative reproduction; its only failure
was login navigation before player assertions. The held-request negative and
corrected one-case browser test establish the cause and repair.

The separate three-state, ten-second native resource run is in progress with
129 frozen source/build hashes; its result and OS/CDP coverage are pending.
Do not infer sustained resource budgets from the longer earlier runs after those
measure different source versions.


## Repeated current-source disk-file resource check — 2026-09-28

The rebuilt current assets pass six windows: two 60-second samples in each
idle, playing and paused state, after a 15-second warmup per state. Browser
input is a 40-second generated 22,050 Hz mono PCM disk file (1,764,044 bytes);
video/tracing are disabled. HeadlessChrome 153.0.8010.12 on Linux. There are no
created AudioContexts, process changes or missing PSS reads. One proc stat read
has unknown ancestry in the first idle and one in the second paused endpoint;
complete OS process enumeration is therefore not proven.

| State | OS-tree CPU (% one core) | OS-tree PSS (MiB) | CDP CPU (% one core) | CDP PSS (MiB) |
| --- | ---: | ---: | ---: | ---: |
| Idle | 0.600 | 289.24 | 0.600 | 267.88 |
| Playing | 2.057 | 324.58 | 2.057 | 305.20 |
| Paused | 0.616 | 313.94 | 0.616 | 294.57 |

Means are for two windows per state, not a full-session budget or plateau.
PSS rises while playing and remains above idle after pausing; heap ranges 9.06–
10.52 MiB idle, 9.35–9.90 MiB playing and 9.69–11.88 MiB paused. Two endpoints
per state do not establish monotonic retention or leak behavior. The same CPU
trend in separately scoped CDP and Linux process metrics and stable paused audio
position support the measured result. Do not generalize this native no-graph
workload to queues, visualizers, decoded/high-rate formats, output routing,
remote radio or long-running rooms. Confidence: high for the sampled windows,
moderate for comparative steady-state use. Resource JSON and process files
remain local. All 129 frozen source/build hashes match before and after.


## Serialized audio graph playback intent — 2026-09-28

A controlled Chromium race held an outgoing graph's native `suspend()` through
transport Pause, started newer Play, then released the old transition. The
negative run showed the media element unpaused and advancing while the context
ended suspended; physical audible output was not measured.

Each cached graph now records its latest desired run state and serializes
native transitions. New requests wait for the existing native operation and
reconcile again until actual context state matches the latest intent. Player
Pause, outgoing-fade cleanup and terminal quiescence use the same path as Play.
This adds no timer, polling loop, network request or dependency. Four focused
unit regressions exercise both pending-transition orders and a request arriving
at settlement. A rebuilt Chromium case verifies that Play waits while Pause's
suspension is held, then the audio element advances with the graph running.

The complete player browser group passes 25 cases, including two-node radio,
host/follower and room recovery, responsive controls, the new graph-race case,
and a quick native idle/play/pause sample. All 1,090 Web tests, 5,324 unit,
74 smoke and 284 integration tests pass. Web/repository lint, frontend and
Release builds, and browser-spec TypeScript pass.

A separate repeated native sample passes six one-minute windows, two each for
idle, playback and pause, after 15-second warmups. The input is a 195-second
mono PCM disk file (8,599,544 bytes); video and tracing are disabled. Chromium
is HeadlessChrome 153.0.8010.12 on Linux, and all 130 frozen source/build hashes
match before and after.

| State | Observed OS processes | OS-tree CPU (% one core) | OS-tree PSS (MiB) | CDP CPU (% one core) | CDP PSS (MiB) |
| --- | ---: | ---: | ---: | ---: | ---: |
| Idle | 6 | 0.608 | 279.54 | 0.608 | 259.29 |
| Playing | 7 | 2.140 | 315.94 | 2.140 | 297.56 |
| Paused | 7 | 0.658 | 309.90 | 0.649 | 291.52 |

There was no observed process churn, unavailable PSS or AudioContext. Nine
proc-stat enumeration reads had unknown ancestry across the six window
endpoints, so complete OS process enumeration is not proven. PSS averaged
about 36 MiB higher while playing and 30 MiB higher after pausing than idle;
this single-cycle residual footprint is not proof of a leak. It does require
repeat-cycle queue/visualizer and graph-enabled resource measurements before
making a whole-session efficiency claim. Physical output, cross-browser behavior
and the broader player audit remain open. Confidence: high for the sampled
windows, moderate for comparative steady-state use and full OS-tree coverage.

## Soulseek fairness accounting — 2026-09-28

The earlier audit noted that `AddOverlayUploadAsync` had no production call
sites. The current source now credits successful radio payload writes on both
mesh handlers, so that historical observation is stale. A new source audit
found the corresponding Soulseek gap: the fairness guard divides overlay
uploads by Soulseek uploads, but no production path incremented the Soulseek
upload total. Once overlay traffic accumulated, actual Soulseek sharing could
not restore the denominator.

`UploadService` now counts the transfer reporter's `actualBytes`, emitted after
the Soulseek payload write completes, and persists one aggregate per attempt.
This includes payload written before a failure and excludes requested size,
queued bytes, and unsent stream reads. A traffic-store error is logged without
changing the peer transfer result. Lifecycle regressions cover completed and
partial transfers, zero-byte callbacks, and accounting-store failure.

Repeated admission during sustained traffic and while a Soulseek upload is
still in progress remains open. The LAN-only two-node radio fixture has no real
Soulseek download path, so current proof stops at the actual upload service
reporter and unit-level accounting. Do not close the sustained-use task based
only on completed-attempt accounting.
