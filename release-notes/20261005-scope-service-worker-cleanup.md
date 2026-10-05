---
category: security
audience: operators
area: web
action: none
breaking: false
---
Limit startup cleanup to the slskdN service worker with its exact scope and script URL, and remove only `slskdn-shell-*` caches. Other applications on a shared origin keep their service workers and cached data.
