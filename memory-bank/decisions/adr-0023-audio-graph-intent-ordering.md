# ADR-0023: Audio Graph Playback Intent Ordering

**Status**: Accepted
**Date**: 2026-09-28

## Context

Each player audio element owns a Web Audio graph. Pause, crossfade cleanup,
terminal playback cleanup and Play can request native `AudioContext.suspend()`
or `resume()` while an earlier operation is still pending. A controlled Chromium
regression held Pause's native suspension, started newer Play, then released
the old suspension. The graph could finish suspended after the media element
resumed and its current time advanced.

## Decision

Keep the latest desired run state on each cached graph and serialize native
state changes through one per-graph synchronizer. Every request updates that
intent, waits for any in-flight native transition, then rechecks actual context
state and intent until they agree or the context closes. Route player-owned
suspension through the same helper as resumption; do not poll or create a
second scheduler.

## Consequences

A newer Play waits for an older suspension to settle, then resumes the graph
before starting media playback. A newer Pause similarly wins a pending resume.
Rapid requests coalesce to the latest intent. The approach adds no timers,
network traffic or new dependencies. Unit coverage exercises both transition
orders and a request at the settle boundary; Chromium verifies the Pause/Play
browser path. These checks establish browser state and media-time behavior, not
physical audible output.
