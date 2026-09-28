---
category: security
audience: users, operators
area: player
action: none
breaking: false
---
LAN-only rendezvous now skips the public BitTorrent DHT engine, saved public nodes, announcements and discovery. Empty router lists previously selected library defaults. Known-peer mesh and shared overlay/QUIC UDP remain available. LAN-only status reports zero public DHT nodes, and network health treats the absent public engine as expected.
