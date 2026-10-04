---
category: security
audience: operators
area: logging
action: none
breaking: false
---
Request diagnostics no longer log raw request targets or their query strings. Request paths and security-event text are escaped before they are written to logs while the security event feed retains the original event payload.
