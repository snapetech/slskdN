# Security Roadmap

This document is for proposed or partially designed security systems that are not yet verified as shipped controls. These items must not be presented in the README as stable or implemented unless they are moved to `implemented-security.md` with concrete code, call sites, tests, and smoke notes.

## Roadmap-only until proven otherwise

The following items are treated as roadmap/design-only unless a later audit proves otherwise:

- `NetworkGuard` as a centralized incoming-message guard.
- `PeerReputation` as a behavioral security reputation system.
- `CryptographicCommitment` commit/reveal protocol.
- `ProofOfStorage` random chunk challenge protocol.
- `ByzantineConsensus` multi-source verification/voting.
- Canary traps.
- Honeypots.
- Entropy monitoring.
- Paranoid mode.
- Asymmetric disclosure.
- Temporal consistency checks.
- Advanced adversarial-resilience protocols beyond concrete implemented checks.

## Promotion requirements

A roadmap item can be promoted to implemented only after all of the following exist:

1. Concrete runtime code, not only pseudo-code in a design document.
2. Startup or feature-gated registration that is disabled by default unless safe.
3. Unit tests for core policy logic.
4. Integration or smoke tests for the real network/file/API path.
5. Operator documentation describing what the feature actually does and does not do.
6. A `FEATURE_INVENTORY.md` row updated from `design-only` to `experimental` or `stable`.

## Documentation rule

Security roadmap language should use future tense or explicit experimental language. Do not use words like "active", "enforced", "protected", "hardened", or "zero-trust" unless the enforcement point is implemented and tested.
