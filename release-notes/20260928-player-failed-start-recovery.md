---
category: fixed
audience: users
area: player
action: none
breaking: false
---
Failed Play and crossfade starts now publish a paused position to room followers. Decoded seek setup publishes its requested position before replacing the stream, preserving recovery at that point. Failed startup suspends existing audio processing even when playback never began; late failures from replaced tracks are ignored.
