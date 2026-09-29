---
category: fixed
audience: users
area: player
action: none
breaking: false
---
When FFmpeg rejects audio before producing output, the player now receives a server error and can show its decode recovery guidance instead of treating an empty stream as successful.
