---
category: security
audience: users, operators
area: pod-management
action: none
breaking: false
---
SQLite-backed Pod service and content-linked API diagnostics now escape exception details and caller-controlled Pod, peer, and channel identifiers. Canceled Pod reads, creates, updates, deletes, and joins propagate cancellation; active transactions roll back with an independent token, and post-commit publication is outside the transaction failure handler. Log events no longer attach raw exception objects; API responses and storage rollback behavior are unchanged.
