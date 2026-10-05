---
category: security
audience: operators
area: logging
action: none
breaking: false
---
Escape Pod, channel, message, and peer identifiers before writing native API, message storage, signing, and routing diagnostics. Exception logs retain escaped stack context and redact search queries, message bodies, and private keys when they appear in exception text; requests and routing values remain unchanged.
