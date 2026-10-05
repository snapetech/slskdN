---
category: fixed
audience: operators
area: anonymity-transport
action: none
breaking: false
---
I2P SAM availability checks now apply their five-second deadline to the full HELLO exchange, so a bridge that accepts a connection but does not respond is reported unavailable instead of leaving the probe pending.
