# Anonymous Endpoint Allowlist

Anonymous endpoints must be deliberate and reviewed. This allowlist is file-scoped: if a controller contains `[AllowAnonymous]`, it must appear here with a rationale. Endpoint details remain visible in `docs/system-surfaces-current.md`.

| Controller | Rationale |
| --- | --- |
| `src/slskd/Core/API/Controllers/ApplicationController.cs` | Public build/version metadata supports logged-out footer display and does not expose control-plane mutation. |
| `src/slskd/Core/API/Controllers/SessionController.cs` | Login/session bootstrap endpoints must be reachable before authentication. |
| `src/slskd/Identity/API/ProfileController.cs` | Public peer profile lookup is an intentional identity discovery surface. |
| `src/slskd/ListeningParty/API/ListeningPartyController.cs` | Radio stream access supports explicit unauthenticated playback paths. |
| `src/slskd/PodCore/API/Controllers/PodDhtController.cs` | Public pod metadata retrieval is part of DHT discovery. |
| `src/slskd/PodCore/API/Controllers/PodDiscoveryController.cs` | Public pod discovery by name/tag/content is intentional for listed pods. |
| `src/slskd/PodCore/API/Controllers/PodVerificationController.cs` | Public verification checks support validating membership/message/role claims without exposing mutation authority. |
| `src/slskd/SocialFederation/API/ActivityPubController.cs` | ActivityPub actor, inbox, outbox, followers, and following routes are protocol-required public surfaces. |
| `src/slskd/SocialFederation/API/WebFingerController.cs` | WebFinger discovery is protocol-required. |
| `src/slskd/SourceFeeds/API/SpotifyConnectionController.cs` | Spotify OAuth callback must be reachable by the provider redirect flow. |
| `src/slskd/Streaming/MeshStreamsController.cs` | Mesh preview playback redeems short-lived opaque tickets created by an authenticated read/write user; the unauthenticated GET exists so browser media elements can fetch the stream without exposing API credentials. |
| `src/slskd/Streaming/PeerStreamsController.cs` | Peer preview playback redeems short-lived opaque tickets created by an authenticated read/write user; the unauthenticated GET exists so browser media elements can fetch the stream without exposing API credentials. |
| `src/slskd/Streaming/StreamsController.cs` | Stream endpoint supports explicit ticket/token playback access. |
