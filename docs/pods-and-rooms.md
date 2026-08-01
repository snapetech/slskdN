# Pods, Rooms, And Messages

This guide explains where pod, room, Gold Star, and direct-message features show
up for users.

## Primary UI Path

Use **Messages** for day-to-day conversation work.

Messages provides one workspace for:

- Soulseek direct messages.
- Joined Soulseek rooms.
- Pod room channels.

Conversations can open as panels, collapse into the dock, and restore across the
session. Pod direct channels are hidden from the normal list so they do not
duplicate normal Soulseek DMs.

## Rooms

Joined Soulseek rooms appear in Messages and can also be reached through legacy
room entry points where compatibility routes still exist. Room panels preserve
the transcript, composer, and member rail in the unified workspace.

Room user names should stay compact in dense transcripts. Rich reputation/user
cards belong in stable identity surfaces such as headers and member lists, not
beside every message row.

## Direct Messages

Soulseek direct messages remain the visible direct-message surface. Deleting a
saved DM must not reveal a hidden pod `DM` transport channel as a replacement
conversation.

## Pod Channels

Pod room channels can appear in Messages as room-style conversations. Pod direct
channels are mesh transport plumbing unless a deliberate mesh-DM product surface
is built later.

Listen Along controls are room-scoped. They appear for pod room/broadcast
channels, not direct-message channels.

## Gold Star Club

Gold Star Club is the early user/governance bootstrap pod and is strictly
opt-in for every node.

- `feature.Pods: false` prevents Gold Star pod creation, publication, and
  enrollment.
- Even with pods enabled, Gold Star remains dormant unless the daemon
  environment contains the exact value
  `SLSKDN_POD_GOLD_STAR_CLUB_AUTOJOIN=true`.
- This environment value is local to one daemon and its Soulseek account; it
  does not opt remote users or testers into Gold Star.
- Missing, malformed, `false`, `0`, and other values do not create or publish
  the reserved pod and do not auto-enroll the local Soulseek username.
- Users can leave the pod from the Web UI.
- Leaving is intentionally irreversible for local Gold Star status.

Gold Star should be described as a pod/room workflow, not as a separate chat
system.

## User Actions

Permanent or irreversible actions should be explicit:

- Delete saved DM threads only after confirmation.
- Leave rooms only after confirmation.
- Leave pod channels only after confirmation.
- Gold Star leave actions must preserve the irreversible-status warning.

## Network Boundaries

Messaging UI changes should not create hidden peer browsing, downloads, file
mutations, or background searches. Any mesh-wide broadcast, pod-DM transcript
merge, or global listening-party control should be added as a deliberate product
surface instead of falling out of the room/direct-message layout.
