---
category: changed
audience: users
area: player
action: start the broadcast again if a Party ID ownership conflict is reported
breaking: false
---
Listening-party broadcasts now reject Party IDs already owned by another active room observed locally or in the DHT. The Player explains the conflict and can retry with a newly generated ID.
