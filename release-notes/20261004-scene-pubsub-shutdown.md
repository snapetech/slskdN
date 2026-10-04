---
category: fixed
audience: operators
area: virtualsoulfind-scene-pubsub
action: none
breaking: false
---
Scene pubsub shutdown now keeps cancellation resources until an in-flight DHT poll stops, including polls that exceed the bounded join timeout.
