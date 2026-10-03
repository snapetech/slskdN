---
category: fixed
audience: users, operators
area: collection-sharing
action: Set sharing.externalEndpoint to a peer-reachable URL; add an exact trusted private IP origin on recipients that need LAN or loopback backfills.
breaking: false
---
Cross-node shares now advertise an owner-configured peer-reachable endpoint. If none is set, recipients see that streaming is unavailable. Public URLs retain outbound SSRF protection; private or loopback backfills require recipients to trust the owner's exact IP origin in `sharing.trustedPrivateOwnerOrigins`.
