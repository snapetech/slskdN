---
category: fixed
audience: users, operators
area: rooms
action: none
breaking: false
---
Rapid leave/rejoin and join/ban actions no longer collide in persisted membership history. History remains ordered when the clock moves backward and across service restarts. Membership changes and their history commit together; existing ban restrictions remain in force.
