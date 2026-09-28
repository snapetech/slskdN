---
category: fixed
audience: users
area: player
action: none
breaking: false
---
An active playback error now pauses the failed audio element and publishes its stopped position to room followers, including the source offset for decoded playback. Errors from the standby element leave the active broadcast alone. Playback can resume through the existing recovery controls.
