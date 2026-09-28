---
category: fixed
audience: users, operators
area: player
action: none
breaking: false
---
New radio fairness checks now include Soulseek payload bytes as soon as each network write completes, even while a long upload is still active. The bytes are persisted in one update per upload attempt rather than one database write per transfer chunk.
