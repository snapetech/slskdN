---
category: fixed
audience: users, operators
area: swarm-downloads
action: none
breaking: false
---
Experimental swarm downloads now keep caller job IDs out of filesystem paths, stop after three failed chunk attempts, clean per-job chunk data, and stream verified chunks into an atomically published output.
