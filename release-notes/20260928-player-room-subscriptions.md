---
category: security
audience: users, operators
area: player
action: none
breaking: false
---
Listen-along publications recheck current pod membership and stop delivering room state to banned or removed participants. Revoked listeners receive recovery feedback and must rejoin after access is restored. Leave and disconnect release bounded subscriptions; stale events cannot clear the revocation message.
