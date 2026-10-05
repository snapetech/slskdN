---
category: security
audience: users, operators
area: pod-messaging
action: none
breaking: false
---
Pod routing logs now escape external identifiers, router errors, and exception details. Caller cancellation propagates through routing, statistics, and cleanup actions instead of becoming an HTTP 500 response.
