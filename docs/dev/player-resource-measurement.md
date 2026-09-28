# Player resource measurement

Measure the native browser player independently of functional recording. The
probe uses generated PCM, an isolated backend with remote connections disabled,
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

The result is `native-resources.json` under the test output directory. Each
window is saved before validation, preserving diagnostics if a later check fails.
The test also attaches the completed report. A terminal passing result is
required before treating a report as verified evidence.

## Read the evidence

- Browser CPU is the sum of cumulative CPU-time deltas for processes present at
  both ends of a window, divided by elapsed time and expressed as a percentage
  of one core. Added and removed process counts disclose incomplete coverage.
- Linux PSS apportions shared memory rather than adding every process's RSS.
  Report measured and unavailable process counts. Non-Linux runs mark PSS
  unsupported; missing readings are not zero memory use.
- Renderer task time, script time and JS heap are browser measurements, not
  whole-application or backend measurements.
- Audio state before and after each window verifies the workload. Zero created
  AudioContexts confirms that the native baseline did not activate an analyzer.
- Natural garbage collection remains enabled. Do not force collection and then
  call the resulting low watermark steady-state memory use.

Record the build/probe revision, browser version, platform, duration, process
coverage and workload alongside summarized results. Compare repeated windows
and state changes. Keep raw artifacts locally; publish aggregate measurements
without local account, machine or directory identifiers.

A native baseline does not establish queue, analyzer, visualizer, Picture in
Picture, remote radio, high-rate format or whole-session resource budgets. Those
workloads require their own sustained measurements. A short or recorded run does
not establish low resource use.
