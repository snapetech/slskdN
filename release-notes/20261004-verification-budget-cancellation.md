---
category: fixed
audience: users, operators
area: multi-source-verification
action: none
breaking: false
---
Soulseek verification now skips probes whenever the persisted per-peer budget cannot be safely loaded or saved, and caller cancellation propagates through HashDb lookups, probe downloads, and source discovery.
