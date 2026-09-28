# ADR-0021: Confirmed Broadcast Settings

**Status**: Accepted

**Date**: 2026-09-28

## Context

Listing and streaming controls changed staged panel state while the persistent
host continued using its previous settings. A settings write can be queued behind
a playback event, superseded by a newer event, or fail after the server accepted
part of the work. Showing the requested checkbox state as confirmed can mislead
the host about sharing permissions.

## Decision

Apply active-host setting changes through the existing paced publication writer.
Preserve current playback state, mapped position and party identity. Keep the
requested sharing configuration in the session, separately from the last
acknowledged settings. Each successful response confirms its own payload fields,
not a later queued configuration. Automatic playback events carry the latest
requested settings when they coalesce a pending update.

Render acknowledged settings while an update is pending, expose a status message,
and prevent additional settings edits or a new Broadcast until it completes.
Keep Stop available; once Stop is queued, prevent settings from replacing it.
Failures pause automatic publication and retain the requested configuration for
explicit Retry. Retry uses the current track and playback position. Ownership loss
releases hosting with specific rejoin feedback; deliberate request cancellation
must preserve that feedback. HTTP 403 uses the same access-revocation message. Refresh the
rendered directory when acknowledged sharing settings change.

Pre-start choices remain staged and never start hosting or playback. Unlisting
also turns streaming off; relisting requires a new streaming choice. Reset
staged choices on room navigation, and restore the retained host's acknowledged
settings only when returning to its room. Expanded checkboxes have unique IDs
and associated labels.

## Consequences

Settings are explicit user actions and use the existing bounded, paced queue.
No polling, peer scan, dependency or new backend endpoint is added. A failed
HTTP response leaves the actual server outcome unconfirmed, with visible Retry
feedback. Local audio remains under the user's existing playback controls.
Cross-node state delivery, durable host leases and long capability renewal
remain separate work.
