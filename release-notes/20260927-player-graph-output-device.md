---
category: fixed
audience: users
area: player
action: none
breaking: false
---
Audio output selection now routes the Web Audio graph that actually carries playback. Both audible tracks switch together during crossfade. Supported browsers offer a speaker picker and refresh the device list when outputs change; browsers without graph output selection no longer show a control that cannot route sound.
