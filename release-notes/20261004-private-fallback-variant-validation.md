---
category: fixed
audience: operators
area: swarm
action: none
breaking: false
---
Cross-peer private torrent fallback signals now require the requested variant to match the variant bound to the referenced swarm job. Mismatches are rejected before security policy evaluation or torrent manager startup; fallback remains fail-closed until the sender and job lifecycle are connected.
