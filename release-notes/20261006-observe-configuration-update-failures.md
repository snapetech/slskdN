---
category: fixed
audience: operators
area: configuration-updates
action: none
breaking: false
---
Configuration reload callbacks now observe and report unexpected failures that occur before the update handler's own error boundary, rather than allowing an asynchronous void exception to escape unnoticed.
