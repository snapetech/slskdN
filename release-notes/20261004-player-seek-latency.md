---
category: changed
audience: users
area: player
action: none
breaking: false
---
Listening-party playback now accounts for time between a host observing an active Play or Seek position and the server receiving it, and for time before listeners apply the update. Paused positions remain exact; server correction is bounded to ten seconds.
