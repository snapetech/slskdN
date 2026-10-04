---
category: fixed
audience: operators
area: release-pipeline
action: none
breaking: false
---
PPA source builds now derive the signing fingerprint from the imported secret key and verify unattended signing before package creation, so rotating the configured key no longer leaves the workflow requesting a stale fingerprint.
