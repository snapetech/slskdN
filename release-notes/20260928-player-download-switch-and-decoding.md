---
category: fixed
audience: users, operators
area: player
action: none
breaking: false
---
Switching between unindexed local downloads no longer waits for another directory scan. The Files picker includes AIFF, ALAC, APE, M4B and WMA audio for native or on-demand decoded playback. Rapid decoded seeks abort the old stream and combine setup requests, keeping the latest position and Pause/Play intent without exceeding decode limits.
