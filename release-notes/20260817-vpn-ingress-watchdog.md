---
category: fixed
audience: operators
area: networking
action: enable slskdN-vpn-watchdog.timer for automatic ingress recovery
breaking: false
---
VPN ingress health checks now inspect each active forwarded WireGuard namespace
and the NAT-PMP renewal unit independently. Stale ingress tunnels are
reconciled before their public lease expires, while healthy mappings are not
rebuilt because of a short-lived handshake timestamp.
