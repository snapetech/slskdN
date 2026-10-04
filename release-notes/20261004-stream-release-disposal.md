---
category: fixed
audience: users, operators
area: streaming
action: none
breaking: false
---
Stream lease release now runs once across concurrent synchronous and asynchronous disposal. Callback failures are surfaced after the wrapped response stream is disposed.
