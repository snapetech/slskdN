---
category: added
audience: users, operators
area: networking
action: Enable `dht.share_overlay_tcp_port_with_soulseek` only when the experimental shared TCP listener is desired.
breaking: false
---
The experimental DHT mesh overlay can now share the Soulseek TCP listen port with plain and obfuscated peer traffic, using a conservative first-byte classifier while remaining disabled by default.
