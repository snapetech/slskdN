---
category: changed
audience: users, operators
area: system-interface
action: none
breaking: false
---
The Web UI now uses effective server feature-gate status across Search, Messaging, and System. Disabled SongID, Pods/PodCore, Mesh/DHT rendezvous, social federation, VirtualSoulfind, and multi-source surfaces show the relevant configuration settings and avoid calling gated APIs. Standard jobs and the base MediaCore registry remain available when their adjacent experimental panels are off.
