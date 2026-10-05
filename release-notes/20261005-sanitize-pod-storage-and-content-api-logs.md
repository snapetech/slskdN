---
category: security
audience: users, operators
area: pod-management
action: none
breaking: false
---
Pod users get clean request cancellation across storage reads and writes. Pod persistence, DHT publish, and content-linked creation errors no longer allow control characters to forge log entries; transactions still roll back on failure, while committed updates stay committed if later publishing is canceled.
