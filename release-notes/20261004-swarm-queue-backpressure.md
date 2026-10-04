---
category: fixed
audience: users, operators
area: swarm-downloads
action: none
breaking: false
---
When the bounded swarm job queue is full, enqueue now reports rejection instead of returning success for work that was silently discarded, allowing callers to retry or report the unavailable capacity.
