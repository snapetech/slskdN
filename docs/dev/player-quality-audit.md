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
