---
category: security
audience: operators
area: pod-messaging
action: none
breaking: false
---
Pod backfill now propagates caller cancellation through sync and response processing. Peer-related exception details are escaped before logging without attaching raw exception metadata.
