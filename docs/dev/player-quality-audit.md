# Player quality audit

Updated: 2026-09-28. The player overhaul remains active.

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
| Listed radio | Reachable picker, directory failure/manual refresh, metadata-only controls, actual HTTP audio failure/retry, temporary URL exclusion | Real two-backend Chromium discovery, decoded playback/seek, revocation, counters and reverse directory publication verified / high; local elapsed expiry/reselection verified; remote renewal, host capability renewal and sustained sessions unverified |
| Listen-along recovery | Startup retry, closed/rejoin/refresh failure controls, disposed callbacks and live-event precedence | Two authenticated real SignalR clients on one backend verify leave, explicit disconnect/rejoin, snapshot recovery and live ban / high; automatic transport recovery and cross-node propagation unverified |

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

## Remaining completion work

- Complete remote-radio expiry/renewal, repeated admissions, source replacement and sustained playback across realistic latency. Real two-backend discovery, decoded HTTP seeking, revocation, accounting and reverse publication are verified.
- Verify automatic transport reconnect and cross-node listen-along state propagation. Same-node explicit disconnect/rejoin, snapshot recovery and live membership revocation now have real-client coverage; elapsed radio renewal remains due.
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
