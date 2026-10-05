---
category: changed
audience: operators
area: packaging
action: use the .NET 10 SDK when building from source
breaking: false
---
Update contributor setup instructions to require the .NET 10 SDK pinned by `global.json`. Extend the runtime-matrix check to verify the SDK pin and every first-party project under `src`, `tests`, and `tools` match the application target.
