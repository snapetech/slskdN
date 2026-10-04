---
category: fixed
audience: users, operators
area: cover-traffic
action: none
breaking: false
---
Mesh cover-traffic iterators now cancel when their generator is disposed, including during privacy-layer reconfiguration, so pending reads stop before resources are released.
