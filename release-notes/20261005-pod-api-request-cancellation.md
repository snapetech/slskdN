---
category: fixed
audience: users, operators
area: pod-api
action: none
breaking: false
---
Canceled Pod API requests now stop at cancellation boundaries instead of being reported as HTTP 500 failures. Other service errors retain their existing responses.
