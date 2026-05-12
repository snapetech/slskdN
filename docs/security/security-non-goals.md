# Security Non-Goals

This document keeps security claims narrow and testable. It should prevent design language from being mistaken for active protection.

## Non-goals for current slskdN builds

Unless a feature is explicitly listed as implemented in `docs/security/implemented-security.md` and marked `stable` or `experimental` in `FEATURE_INVENTORY.md`, current slskdN builds do not claim to provide:

- A complete zero-trust security architecture.
- Byzantine-safe multi-source verification.
- Proof-of-storage guarantees for remote peers.
- Cryptographic commit/reveal guarantees for all transfers.
- A global peer reputation or trust network.
- Active honeypot/canary trap enforcement.
- Entropy-based anomaly detection.
- Paranoid-mode adversarial hardening.
- Fully adversarial mesh or DHT hardening.
- End-to-end anonymous or metadata-free operation.
- Automatic safety for arbitrary executable, archive, or script downloads.
- Security guarantees for experimental mesh, pod, DHT, federation, or swarm features.

## Operator expectations

Operators should assume experimental distributed features can expose metadata, contact third-party services, or behave differently from standard slskd-compatible operation unless the feature documentation says otherwise.

Security-sensitive features must be explicit opt-in. Defaults should favor normal slskd-compatible behavior, local-only safety, and no unexpected network publication.

## Documentation rule

If a feature is a non-goal, roadmap item, or experiment, documentation should say so directly. Avoid vague hardening language that sounds stronger than the implementation.
