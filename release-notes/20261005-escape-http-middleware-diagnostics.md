---
category: security
audience: users, operators
area: http-middleware
action: none
breaking: false
---
HTTP exception and CSRF token middleware now escape request and exception details before logging, preventing request-triggered control characters from forging log entries while preserving response behavior and token handling.
