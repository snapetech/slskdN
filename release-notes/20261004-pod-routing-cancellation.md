---
category: fixed
audience: users, operators
area: pod-message-routing
action: none
breaking: false
---
Pod message routing now propagates caller cancellation and leaves interrupted fan-outs retryable instead of marking them as already delivered.
