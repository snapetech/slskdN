---
category: fixed
audience: operators
area: release-infrastructure
action: none
breaking: false
---
Optional media-tool images now install Rust crates from their published lockfiles, preventing transitive dependency drift from breaking the omnibus tester image.
