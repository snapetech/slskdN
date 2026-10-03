---
category: security
audience: users, operators
area: downloads
action: none
breaking: false
---

Completed direct Soulseek downloads are checked before entering the normal downloads directory. Disguised executable signatures are rejected, and file-type mismatches are quarantined by default. Rejected transfers and requests are marked failed. An empty quarantine path uses `<directories.downloads>/.quarantine`.
