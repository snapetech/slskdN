---
category: fixed
audience: users, operators
area: player
action: none
breaking: false
---
Radio fairness now counts Soulseek file payload bytes only after the network write completes, including payload sent before an interrupted upload. Queued or unsent bytes do not earn credit, and reciprocal sharing can restore eligibility for later radio admissions.
