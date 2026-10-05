---
category: fixed
audience: users, operators
area: content-linking
action: none
breaking: false
---
Pod content lookups now propagate caller cancellation instead of returning ordinary misses. MusicBrainz failures keep their best-effort fallback, while remote exception details and unsupported-domain values are escaped in logs.
