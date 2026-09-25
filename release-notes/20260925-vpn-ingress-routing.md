---
category: fixed
audience: users, operators
area: vpn-agent
action: none
breaking: false
---
WireGuard ingress namespaces now route tunnel endpoint traffic through the host gateway before applying the tunnel default route. Provider hostnames remain supported when they resolve to IPv4.
