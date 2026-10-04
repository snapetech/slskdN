---
category: fixed
audience: users, operators
area: request-cancellation
action: none
breaking: false
---
Optional source planning, private-gateway requests, and profile loading now preserve caller cancellation instead of continuing fallback work or reporting an ordinary service failure. Profile hostname discovery also uses cancellable asynchronous DNS.
