---
category: fixed
audience: users, operators
area: subprocess-lifecycle
action: none
breaking: false
---
SongID, audio sketching, Chromaprint fingerprints, perceptual hashing, Soulfind bridge, and obfs4 checks now drain redirected output concurrently. Canceled subprocess work stops its child process tree, preventing tool output from hanging these workflows or leaving background processes running.
