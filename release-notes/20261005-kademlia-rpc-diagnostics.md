---
category: security
audience: operators
area: mesh-dht
action: none
breaking: false
---
Kademlia FIND_NODE, FIND_VALUE, PING, and STORE diagnostics now escape peer addresses, remote error text, and exception details before logging them. RPC behavior and cancellation handling are unchanged.
