---
category: fixed
audience: operators
area: mesh-peer-descriptor-refresh
action: none
breaking: false
---
Stopping the DHT peer-descriptor refresh service no longer logs an in-flight canceled publish as a refresh failure. Peer relay-descriptor updates also propagate caller cancellation instead of logging it as an update failure.
