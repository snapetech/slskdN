---
category: fixed
audience: users, operators
area: mesh-dht
action: none
breaking: false
---
Kademlia node snapshots now return defensive copies of node identifiers, preventing consumers from changing the routing table's stored peer identity.
