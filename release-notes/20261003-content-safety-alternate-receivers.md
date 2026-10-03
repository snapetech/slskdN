---
category: security
audience: users, operators
area: downloads
action: none
breaking: false
---

The shared content-safety policy now covers completed pod/mesh, collection backfill, relay, multi-source, and VirtualSoulfind receives. Alternate receivers stage bytes and publish only accepted files; rejected content is quarantined or removed. Backfills keep the manifest filename when available and use `.bin` when the source filename is unknown.
