---
category: security
audience: users, operators
area: dht-rendezvous
action: none
breaking: false
---
DHT and mesh-directory diagnostics now escape malformed peer data and exception details before logging, preventing remote control characters from forging log entries while preserving discovery and transport results.
