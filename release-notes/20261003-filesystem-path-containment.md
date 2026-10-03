---
category: security
audience: users, operators
area: filesystem
action: none
breaking: false
---

Local file operations now resolve current symbolic-link targets against configured share and download roots before browsing, streaming, downloading, or deleting. Stale share-index paths are rejected and trigger a rescan, and peer-derived pod downloads use a rooted safe writer.
