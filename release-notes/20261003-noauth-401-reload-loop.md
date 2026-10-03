---
category: fixed
audience: users, operators
area: authentication
action: Configure a matching `web.authentication.passthrough.allowed_cidrs` entry for each intended remote client range when remote no-auth is enabled.
breaking: false
---
When a protected API request returns 401 in no-auth passthrough mode, the Web UI now keeps passthrough active and reports the error to that request instead of clearing the mode and reloading the page. Remote no-auth access remains limited to loopback unless `web.allow_remote_no_auth` is enabled with a matching explicit client CIDR.
