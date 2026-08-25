---
category: changed
audience: operators
area: dependency-management
action: none
breaking: false
---
Dependabot now keeps Roslyn analyzer packages below 5.4.0 until the SDK compiler, vendored runtime sync, and direct test references can be upgraded together without restore or synchronization failures.
