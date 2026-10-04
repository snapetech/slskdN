---
category: fixed
audience: operators
area: release-pipeline
action: none
breaking: false
---
Launchpad publication polling now allows three hours for the exact PPA binary after its source build succeeds, preventing a workflow timeout when Launchpad publishes the binary later than its previous 90-minute window.
