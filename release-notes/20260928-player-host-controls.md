---
category: fixed
audience: users
area: player
action: none
breaking: false
---
An explicitly started room broadcast now follows the player's Play, Pause, Seek and track changes across navigation. Paused seeks preserve paused playback. Host updates use a bounded queue without periodic position polling; the player exposes Retry and Stop after failures, and releases hosting on revocation, replacement, local Stop or hide.
