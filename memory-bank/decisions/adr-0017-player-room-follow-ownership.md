# ADR-0017: Player Ownership of Room Following

## Context

The browser player survives SPA navigation, but the room panel owned its hub
connection and its independent Follow toggle. Leaving the room stopped updates
without clearing the player's following label. Returning lost the selected
state. The routed browser workflow reproduced a missed Pause after navigation.

## Decision

The persistent PlayerProvider owns room connection lifetimes through a React
hook. A panel observes its room, and explicit Follow retains that same room
independently of the panel. Follow state comes from the current room identity.
A returning panel reuses the retained connection and latest snapshot.

Use one connection per distinct room, bounded to two: the viewed room and the
followed room. This preserves the existing room-scoped revocation protocol
without adding shared-hub room multiplexing or changing backend contracts.
Release a connection when its final observer and follow ownership end. Provider
unmount releases all connections. No directory polling or peer discovery is
added. Reconnect rejoins and refreshes while preserving event/snapshot ordering
and deduplicated playback application. Revoked access requires explicit retry.

## Consequences

Following continues while browsing. Host Stop, revoked followed-room access,
manual track selection, clearing or hiding the player ends its follow ownership.
Revocation in another viewed room cannot stop the followed source. Document
reloads start a new session and do not automatically restore network following.
The two-room limit follows the current single-room routed UI; additional
simultaneous room panels would need an explicit product and resource decision.
Cross-node room state and ongoing host control publication remain separate work.

## Connection Feedback

Keep the retained room's connecting/error state in the player context so the
persistent subtitle reports interrupted updates even without the room panel.
Waiting for an empty room is represented by room identity, without inventing a
host. End detection uses the room's owned previous snapshot instead of global
metadata from a previous React render.
