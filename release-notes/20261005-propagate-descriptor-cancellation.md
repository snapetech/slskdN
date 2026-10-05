---
category: fixed
audience: operators
area: media-core
action: none
breaking: false
---
Single descriptor retrieval and verification now propagate cancellation instead of returning a failed lookup or invalid result; batch retrieval keeps completed results when the caller cancels.
