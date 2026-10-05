---
category: security
audience: users, operators
area: pod-management
action: none
breaking: false
---
PodCore API and background-service diagnostics now escape exception text and caller- or peer-supplied identifiers before logging, preventing control characters in failed requests or remote data from forging log entries.
