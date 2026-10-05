---
category: security
audience: users, operators
area: mesh-search
action: none
breaking: false
---
Mesh search now preserves caller cancellation and stops result processing after a peer disconnects. Request IDs, filenames, and exception details are escaped before they are written to logs while response correlation values remain unchanged.
