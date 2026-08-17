---
category: fixed
audience: users, operators
area: networking
action: none
breaking: false
---
VPN ingress now keeps one shared TCP forward for regular/type-1 Soulseek and mesh TCP, optionally one UDP forward for DHT/mesh/QUIC, advertises a provider's public port without rebinding the local listener, and renews dynamic mappings without tearing down the live ingress path.
