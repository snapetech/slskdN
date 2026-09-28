---
category: fixed
audience: users, operators
area: player
action: none
breaking: false
---
Listen-along updates finish in room order so a delayed Play cannot arrive after Stop. Pending publications are bounded and overloaded rooms report a retryable limit. Remote radio ticket acquisition now explains network fairness limits before playback; the player distinguishes fairness, capacity and unavailable snapshots while preserving existing stream admission policy.
