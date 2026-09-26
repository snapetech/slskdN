---
category: fixed
audience: operators
area: solid
action: Set solid.clientIdUrl to the canonical HTTPS URL when publishing the Solid-OIDC Client ID document.
breaking: false
---
The Solid guide and settings page now explain that an unset solid.clientIdUrl keeps the anonymous Client ID document disabled. Explicit localhost WebID testing now uses a no-redirect client for loopback targets; other hosts keep public-address checks.
