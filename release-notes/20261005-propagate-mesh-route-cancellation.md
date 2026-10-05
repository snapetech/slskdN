---
category: fixed
audience: operators
area: mesh-routing
action: none
breaking: false
---
Mesh transport and route diagnostics now propagate caller cancellation instead of failing over or returning an ordinary result. Incomplete circuits dispose previously opened hop streams.
