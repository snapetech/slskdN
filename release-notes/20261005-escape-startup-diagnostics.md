---
category: security
audience: users, operators
area: startup-diagnostics
action: none
breaking: false
---
Startup diagnostics now escape configured paths and filesystem errors, and redact credentials and URL details from Loki endpoints before logging. Readable path context remains available without allowing control characters to forge log entries or exposing Loki URL secrets.
