---
category: fixed
audience: users
area: player
action: none
breaking: false
---
Mesh audio previews preserve short final chunks and accept exact end-of-file responses. Interrupted transfers and hash failures reach the playback reader instead of looking like successful completion. Oversized range replies are rejected, while known-length validation stays strict.
