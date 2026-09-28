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
