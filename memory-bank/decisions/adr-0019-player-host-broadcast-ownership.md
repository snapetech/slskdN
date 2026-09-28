# ADR-0019: Persistent Player Ownership of Explicit Host Broadcasts

## Context

Manual room publication belonged to a route-owned panel. Player transport
changes and navigation could leave listeners following stale host state.
PlayerProvider already retains playback and owns room following connections.

## Decision

A player hook retains one explicitly started host session. It observes the same
room through the existing connection owner; leaving the panel releases only the
panel observer. Starting hosting detaches following. Following waits for a
confirmed host Stop. Existing per-room authorization stays on the server.

Publish actual Play/Pause/Seek and loaded-track events with the player's mapped
position, including transcoded offsets. Paused seeks publish Pause at the new
position. Time ticks stay local. Serialize one request in flight and retain one
latest pending update; Stop takes ownership immediately and waits after any
in-flight request. A monotonic 250-millisecond minimum interval bounds output to
four updates per second, with one cancelable timer only while a write is pending. Assign the response party identity at send time. Seed it from the observed
room state for existing broadcasts; an explicit Stop with no known identity
fetches one abortable snapshot inside the same writer so directory cleanup uses
the actual party ID. This is a manual recovery read, not periodic polling.

Release on permission loss, replacement or an ended broadcast. Stop on local
player Stop, hide or selection of browser-only/remote-radio content. Display
publication failures and explicit Retry/Stop controls in the persistent player.
Automatic updates pause after failure; retry uses current state or a pending
Stop. Abort and release owned resources on provider unmount.

## Consequences

Broadcasts survive SPA navigation without polling or extra host connections.
Rapid updates can coalesce intermediate positions. Losing a request does not
acknowledge a queued Stop. Hosting and following cannot republish each other's
state in one player. Browser-only and relayed-radio content are not host sources.

Closing/reloading the document ends browser ownership; this does not establish
server-side lease cleanup, long capability renewal, authenticated cross-node
state delivery or sustained performance completion. Those remain explicit
player follow-ups. Physical-device and assistive-technology validation remains
separate from browser tests.
