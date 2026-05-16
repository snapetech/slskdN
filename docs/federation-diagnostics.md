# Federation Diagnostics

System -> Integrations includes a read-only federation diagnostics panel for ActivityPub and pod-signing posture.

The panel checks:

- ActivityPub enablement, public/hermit posture, and HTTP signature enforcement.
- Pod join and message signature modes. Pod join/leave Enforce mode requires
  `ed25519:<base64 signature>` payload signatures, fresh timestamps, valid
  Ed25519 public keys, and join nonces for replay protection; Off/Warn retain
  legacy compatibility for older local workflows.
- Whether public federation is configured with signature validation and bounded payload handling.
- Operator-facing blockers that explain why federation or pod signing is not ready for public exposure.

The diagnostics panel does not publish actors, send ActivityPub activities, route pod messages, fetch remote keys, validate provider credentials, or mutate local pod/federation state. Use it before enabling public federation or pod workflows so unsafe combinations are visible without creating network traffic.
