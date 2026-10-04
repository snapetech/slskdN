---
category: changed
audience: users, operators
area: lan-discovery
action: Set `lan_discovery.advertise: true` to retain startup LAN advertising.
breaking: true
---
Startup mDNS advertising now defaults off and has its own opt-in setting, separate from Identity/Friends APIs. Nearby-peer browsing remains user-triggered.
