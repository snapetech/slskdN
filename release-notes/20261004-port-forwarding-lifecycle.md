---
category: fixed
audience: operators
area: private-port-forwarding
action: none
breaking: false
---
Local port forwarding now rejects starts after shutdown, closes tunnel opens that complete late, passes connection cancellation to mesh operations, and uses asynchronous cleanup that reports worker completion only after mapping and send-queue work has stopped.
