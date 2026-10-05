---
category: security
audience: operators
area: logging
action: none
breaking: false
---
The startup logger now escapes rendered log messages and callback exception text before writing its failure fallback to stderr, preventing line-break injection in this diagnostic path.
