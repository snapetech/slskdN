---
category: security
audience: operators
area: logging
action: none
breaking: false
---
Rescue transfer filenames, transfer identifiers, and exception details are escaped at service and guardrail log boundaries, preventing remote control characters from forging log entries while preserving the values used by rescue operations.
