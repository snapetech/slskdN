---
category: changed
audience: users, operators
area: messaging
action: none
breaking: false
---
The capabilities response now reports effective runtime feature-gate status. Messaging hides disabled Pods controls, skips pod API requests, and explains direct `/pods` visits. Configuration-disabled API routes return 404; moved gates return 410 with the slskr project link.
