---
category: fixed
audience: users
area: player
action: none
breaking: false
---
Pausing playback stops analyzer and MilkDrop redraws and keeps the audio graph suspended, reducing background work. Changing the native FPS cap or debug overlay no longer rebuilds the renderer or resets its current session.
