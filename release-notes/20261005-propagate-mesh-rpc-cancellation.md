---
category: fixed
audience: operators
area: mesh-rpc
action: none
breaking: false
---
Mesh DHT, Pods, VirtualSoulfind, content, introspection, and hole-punch requests now propagate caller-requested cancellation instead of returning an ordinary service error. DHT discovery no longer reports cancellation as a missing service, and canceled hole-punch sessions are removed before retry.
