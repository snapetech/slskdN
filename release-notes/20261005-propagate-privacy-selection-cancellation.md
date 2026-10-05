---
category: fixed
audience: operators
area: mesh-transport-selection
action: none
breaking: false
---
Canceled anonymity and obfuscated-transport selection now propagates instead of falling back to standard routing. Availability probes and connections canceled through a selected privacy transport also stop without opening a fallback transport.
