---
category: fixed
audience: users, operators
area: player-streaming
action: none
breaking: false
---
Canceled Player transcodes now keep draining FFmpeg diagnostics and wait for the decoder process tree to exit before releasing stream slots, preventing abandoned background work after a client disconnects.
