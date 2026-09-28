---
category: fixed
audience: users
area: player
action: none
breaking: false
---
Listed-radio mesh requests and replies now use a bounded, paced writer so ordinary RPC bursts do not exhaust the inbound message quota. Control messages remain responsive, cancellation and shutdown release pending work, and upload accounting runs after a successful write. Raw inbound abuse limits remain unchanged.
