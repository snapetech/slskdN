---
category: fixed
audience: operators
area: validation
action: none
breaking: false
---
Isolated player validation now closes and drains node logs, bounds diagnostic memory, reports output failures and cleans up failed starts. Shutdown clears its force-kill deadline and waits for every peer even when one cleanup fails. This changes the test harness only.
