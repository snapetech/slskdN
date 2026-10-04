---
category: fixed
audience: operators
area: signal-bus
action: none
breaking: false
---
Signal-bus shutdown now retains cancellation state until its cleanup worker finishes and leaves the subscriber gate alive while callbacks may still be unwinding.
