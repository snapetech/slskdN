---
category: fixed
audience: users, operators
area: cover-traffic
action: none
breaking: false
---
Cover-traffic shutdown now joins its worker before releasing resources. If a send exceeds the bounded wait, cleanup remains attached until that operation completes.
