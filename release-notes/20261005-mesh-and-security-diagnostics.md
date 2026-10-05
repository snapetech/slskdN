---
category: security
audience: operators
area: logging
action: none
breaking: false
---
Escape peer identities, hash keys, validation details, metadata search text, and ban reasons before logging. Mesh exception diagnostics retain escaped stack context while removing full remote message payloads; protocol, database, and search inputs retain their original values.
