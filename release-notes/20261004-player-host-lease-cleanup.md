---
category: changed
audience: users
area: player
action: none
breaking: false
---
Closing the Player document now sends a best-effort Stop. If it cannot reach the server, the host lease expiry ends the room broadcast and withdraws its radio directory entry, preventing stale listings after the host disappears.
