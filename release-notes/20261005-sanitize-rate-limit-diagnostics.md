---
category: security
audience: users, operators
area: mesh-dht
action: none
breaking: false
---
Rate-limit diagnostics now escape caller-supplied bucket keys, peer IDs, operation names, and failure reasons so remote control characters cannot forge log entries.
