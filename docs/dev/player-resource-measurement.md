# Player resource measurement

Measure the native browser player independently of functional recording. The
probe uses generated PCM selected from a private temporary disk file, an
isolated backend with remote connections disabled,
and one browser. It creates no native audio processing graph. Video and tracing
are disabled at file scope; `page.video()` must be null.

## Prepare the inputs

Build the frontend and Release backend before measuring. Finish tests, lint and
other browser runs first. Keep application assets and probe inputs fixed during
the measurement. Use the repository's usual build commands:

```bash
pnpm --filter @slskdn/web build
dotnet build src/slskd -c Release --no-restore -m:1
```

## Run

The quick check takes one ten-second window per state:

```bash
pnpm --filter @slskdn/web test:player:resources
```

A repeated baseline takes two one-minute windows per state, following a
15-second warmup for each state:

```bash
SLSKDN_PLAYER_RESOURCE_WINDOW_SECONDS=60 \
SLSKDN_PLAYER_RESOURCE_WINDOWS=2 \
pnpm --filter @slskdn/web test:player:resources \
  --output=../../.local/player-resource-results
```

Window seconds must be an integer from 10 through 60; window count must be an
integer from 1 through 10. Every run covers idle, playing and paused states in
that order. Extended playback uses a tone longer than the entire playing phase;
it must remain playing and advance throughout each window. Paused playback must
retain its source and position. Idle must have no loaded audio source.

For a longer natural garbage-collection observation, set the window count to
10 with 60-second windows. This measures ten minutes per state after each
warmup and takes about 31 minutes overall. The generated file covers the entire
playing phase plus a margin; it is created before measurements and removed
after browser teardown.

The result is `native-resources.json` under the test output directory. Each
window is saved before validation, preserving diagnostics if a later check fails.
The test also attaches the completed report. A terminal passing result is
required before treating a report as verified evidence.

## Read the evidence

- Browser CPU covers processes reported by CDP `SystemInfo.getProcessInfo`.
  It is the sum of cumulative CPU-time deltas for processes present at
  both ends of a window, divided by elapsed time and expressed as a percentage
  of one core. Added and removed process counts disclose incomplete coverage.
- Linux PSS apportions shared memory rather than adding every process's RSS.
  The probe reads the same CDP process list. Report measured and unavailable
  process counts. Non-Linux runs mark PSS
  unsupported; missing readings are not zero memory use.
- Complete coverage of that list does not establish complete OS browser-tree
  coverage. A live Linux headless Chromium snapshot had seven OS processes but
  five CDP-reported processes; two omitted zygotes held about 18 MiB combined
  PSS. Label these totals as CDP-reported browser-process measurements. Full-tree
  totals need independent descendant enumeration and process-churn checks.
- Renderer task time, script time and JS heap are browser measurements, not
  whole-application or backend measurements.
- DOM nodes, documents and event listeners help distinguish accumulated browser
  objects from temporary heap allocation. Unsupported counters are null. Rising
  counts alone do not identify a leak; inspect longer trends and their owners.
- Audio state before and after each window verifies the workload. Zero created
  AudioContexts confirms that the native baseline did not activate an analyzer.
- Natural garbage collection remains enabled. Do not force collection and then
  call the resulting low watermark steady-state memory use.

Record the build/probe revision, browser version, platform, duration, process
coverage and workload alongside summarized results. Compare repeated windows
and state changes. Keep raw artifacts locally; publish aggregate measurements
without local account, machine or directory identifiers.

Each window records the actual CDP browser version, platform and media input
kind, bytes and duration. Disk-file input follows normal file selection. Earlier
Buffer-payload runs constructed an in-memory File and remain evidence for that
workload. Compare like-sized inputs before attributing memory differences to
file backing or player changes; a longer generated tone also changes input size.

On Linux, `osProcessTree` separately reports the owned browser root and its
descendants, including zygotes. CPU uses process user/system ticks and the
`getconf CLK_TCK` frequency recorded in the report; it excludes processes that
do not retain the same PID/start-time identity across the window. Added/removed
counts disclose that incomplete CPU coverage. PSS reads recheck identity after
sampling and count failed or reused processes as unavailable. Proc enumeration
failures are disclosed even when the failed process's ancestry is unknown.
Filesystem reads are bounded to 32 concurrent operations. Non-Linux runs report
null for this separate measurement; the retained CDP fields remain available.

These are sequential endpoint snapshots, not an atomic OS census. The OS
interval includes endpoint collection overhead and can differ slightly from
the renderer/CDP interval. Compare their recorded durations rather than
assuming exact alignment or silently substituting one scope for the other.

A native baseline does not establish queue, analyzer, visualizer, Picture in
Picture, remote radio, high-rate format or whole-session resource budgets. Those
workloads require their own sustained measurements. A short or recorded run does
not establish low resource use.

## Repeated player-control cycle soak

The opt-in Chromium cycle soak combines queue-dialog open/close, spectrum/scope/
off analyzer transitions, output-device selection changes, Document Picture-in-
Picture open/close, next/previous media navigation, and Butterchurn visualizer
mount/unmount. The `full` profile runs all of them once per cycle. It uses three
generated PCM files and a local backend with remote peer connections disabled.
Output enumeration and `AudioContext.setSinkId` are deterministic browser test
shims: this exercises the player's switching and cleanup paths, not physical
speaker routing. Document Picture-in-Picture support is required for the test.

Run the default 50 measured cycles, preceded by one warmup cycle, followed by a
30-second stopped-state observation:

```bash
pnpm --filter @slskdn/web test:player:resource-cycles \
  --output=../../.local/player-resource-cycles
```

The cycle count can be set from 5 to 100 with
`SLSKDN_PLAYER_RESOURCE_CYCLE_COUNT`; natural settle time can be set from 10 to
60 seconds with `SLSKDN_PLAYER_RESOURCE_CYCLE_SETTLE_SECONDS`. The test records
five-cycle milestones and final stopped/settled snapshots to
`player-resource-cycles.json`. Browser CDP metrics include heap, DOM nodes,
documents and event listeners. Linux adds independently enumerated owned
browser descendants with process count, PSS coverage and enumeration failures.
Audio telemetry records created/closed/live context states, analyzer reads,
pending main-window animation frames, playback state and PiP presence. Visualizer
telemetry records WebGL context creation/loss events and the actual attributes
returned by `getContextAttributes()`.

Set `SLSKDN_PLAYER_RESOURCE_CYCLE_PROFILE` to `queue`, `analyzer`, `output`,
`pip`, `navigation` or `visualizer` to repeat one subsystem at a time when a
combined run shows unexplained growth; omit it for the full interaction cycle.
The profile name and connected document/player node counts are included in each
snapshot.
Set `SLSKDN_PLAYER_RESOURCE_CYCLE_TRACK_COUNT` from 2 to 10 to distinguish
per-track decoder retention from per-transition growth.

Assertions cover at most two retained non-closed AudioContexts after Stop, no
running context or PiP window, paused audio, and analyzer reads remaining
stopped during the natural settle interval. DOM nodes, documents, event-listener
counts, heap and process PSS are recorded as trends without fixed thresholds:
detached objects can remain visible until natural collection, and browser
process churn changes those values. The default run is a cycle soak, not
evidence of a lifetime memory plateau; retain the separate ten-minute per-state
native baseline for longer-session trends. The default run leaves garbage
collection natural. A separate opt-in queue/full check can force collection to
assert that closed queue dialogs are no longer reachable:

```bash
SLSKDN_PLAYER_RESOURCE_CYCLES=1 \
SLSKDN_PLAYER_RESOURCE_CYCLE_COUNT=20 \
SLSKDN_PLAYER_RESOURCE_CYCLE_SETTLE_SECONDS=10 \
SLSKDN_PLAYER_RESOURCE_CYCLE_PROFILE=full \
SLSKDN_PLAYER_RESOURCE_CYCLE_FORCE_GC=1 \
pnpm --filter @slskdn/web exec playwright test e2e/player-resources.spec.ts \
  --grep '@player-resource-cycles' --workers=1 --retries=0 --trace=off
```

That forced-GC sample is diagnostic reachability evidence; it is not a natural
memory endpoint or a lifetime resource budget.

### Repeated-cycle observations — 2026-09-29

The 20-cycle visualizer-only run passed with one warmup and a 30-second natural
settle. Its warmed browser-tree PSS was 519.8 MiB and JS heap 32.0 MiB; after
settle they were 453.4 MiB and 13.3 MiB. All 21 WebGL contexts, including
warmup, emitted `webglcontextlost`. Every observed context reported alpha,
antialiasing, depth, premultiplied alpha and stencil disabled, matching the
renderer request. Connected DOM stayed at 434 before Stop and 414 after Stop;
pending main-window animation frames were zero after teardown.

The 50-cycle full profile also passed, followed by a 60-second natural settle.
It exercised queue, analyzer, output, PiP, navigation and visualizer changes.
PSS rose from 532.0 MiB warmed to 947.4 MiB at Stop, then settled at 693.9 MiB;
renderer PSS settled 155.6 MiB above warmup while GPU PSS settled 16.6 MiB above
warmup. JS heap fell from 53.9 MiB to 18.7 MiB. All 51 visualizer contexts were
reported lost, their requested low-resource attributes were observed, audio
was paused with a suspended context, analyzer reads stopped, PiP was closed,
and pending animation frames were zero. Connected player DOM was 184 nodes
warmed and 167 stopped. The OS process tree had complete PSS reads at settle;
one process-ancestry enumeration read was unavailable at the stopped sample.

The combined run's browser DOM-node counter remained above warmup after settle,
although the connected DOM count fell slightly and Documents returned to one.
The event-listener counter also remained elevated. These counters do not identify
their owners; renderer PSS also remained elevated after the longer settle. Keep
this as an open retained-resource investigation and do not claim a lifetime
plateau. Raw reports remain in ignored `.local/player-resource-evidence/` files.

### Retained queue modal follow-up — 2026-09-29

The earlier 20-cycle forced-GC inspection found 21 detached queue modal roots
and 960 detached `div` elements after all queue dialogs had closed. The owner
was `@semantic-ui-react/event-stack@3.1.3`: its declarative component resolved a
React ref when subscribing, then resolved that ref again during unmount after
React had cleared it. Cleanup therefore targeted `document` and left the
original DOM target registered in the shared event stack.

The package patch now stores the resolved target for each subscription and
reuses it during update/unmount cleanup. A new forced-GC regression fails when
any detached queue modal remains. It passes after five direct DOM open/Escape
cycles and after 20 full player cycles covering queue, analyzer, output, PiP,
track navigation and visualizer. In the full run, all detached `div` elements
were collected: 0 remained after forced GC, against 2,028 DOM nodes and a 37.3
MiB JS heap at warmup. After the ten-second natural settle, JS heap was 45.1 MiB
and process-tree PSS 681.6 MiB; forced GC reduced them to 12.5 MiB and 547.7
MiB. Connected listener targets remained stable across checkpoints. The PSS
residual remains unattributed, and these cycles do not establish a lifetime
memory plateau. Raw artifacts remain local under ignored test output paths.
