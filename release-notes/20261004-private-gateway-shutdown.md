---
category: fixed
audience: users, operators
area: private-gateway
action: none
breaking: false
---
Private-gateway shutdown now retains cancellation resources until a cleanup worker that exceeds its one-second join timeout finishes, preventing late disposal faults.
