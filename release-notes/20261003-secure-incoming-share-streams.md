---
category: security
audience: users, operators
area: shared-streams
action: Allow the recipient app origin and the POST method with X-Share-Token in the share owner's Web CORS settings.
breaking: true
---
Incoming collection streams now exchange the reusable share credential in a request header and open only a short-lived, content-bound ticket URL. For remote shares, allow the recipient app origin, POST, and X-Share-Token in the share owner's Web CORS settings.
