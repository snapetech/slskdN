# Player quality audit

Updated: 2026-09-28. The player overhaul remains active.

## Quality target

Deliver dependable playback, intuitive controls, polished presentation,
unobtrusive behavior and low resource use across the integrated player.
Completion requires supported workflows to pass runtime checks, identified
player defects to be resolved, and resource behavior to be measured. Commit and
push completed batches with validated release fragments and durable bug records.

## Current evidence

The isolated Chromium suite in `src/web/e2e/player.spec.ts` uses generated PCM,
server-side AIFF decoding and a backend configured without remote peer connections.
It passes 15 workflows. Browser metadata/action checks call the registered Media
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
| Listen-along and radio | Existing implementation and regression tests | Reconnect and real-stream runtime evidence incomplete / unknown |

Final gates: 972 Web tests across 164 files; backend 74 smoke, 5,185 unit and
284 integration tests; Web/repository lint, production build, bundle budget,
build-output, whitespace and identity checks passed.

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

- Verify listen-along reconnect, host state recovery and radio failure/recovery
  with controlled streams and isolated participants.
- Exercise supported browser engines and additional audio formats, including
  failures, decode cancellation and recovery.
- Verify physical mobile interactions, physical output routing/media buttons,
  and Picture-in-Picture window sizing/focus.
- Measure repeated warm idle/play/pause intervals and sustained queue,
  visualizer, output-switch and floating-window cycles for retained memory,
  stray timers and active contexts.
- Inspect accessibility with keyboard-only and assistive-technology workflows.
- Resolve newly discovered defects and update this audit with direct evidence.
