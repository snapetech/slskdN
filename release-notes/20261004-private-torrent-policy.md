---
category: security
audience: users, operators
area: bittorrent
action: When enabling torrent acquisition, configure at least one permitted overlay or invite-list peer source.
breaking: true
---
PrivateOnly now removes tracker and web-seed URLs and disables BitTorrent DHT and PEX; only configured overlay or invite-list peers are admitted. Torrent acquisition remains disabled by default and requires an allowed peer source when enabled. Cross-peer swarm fallback requests are rejected until sender activation and job cancellation are integrated.
