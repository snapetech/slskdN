# Non-Versioned Route Allowlist

New web-consumed JSON APIs should be versioned. Controllers listed here are allowed to expose non-versioned routes because they are compatibility shims, protocol-required endpoints, OAuth callbacks, or currently retained legacy surfaces under remediation.

| Controller | Rationale |
| --- | --- |
| `src/slskd/API/Compatibility/CompatibilityController.cs` | slskd compatibility shim. |
| `src/slskd/API/Compatibility/DownloadsCompatibilityController.cs` | slskd compatibility shim. |
| `src/slskd/API/Compatibility/LibraryCompatibilityController.cs` | slskd compatibility shim. |
| `src/slskd/API/Compatibility/RoomsCompatibilityController.cs` | slskd compatibility shim. |
| `src/slskd/API/Compatibility/SearchCompatibilityController.cs` | slskd compatibility shim. |
| `src/slskd/API/Compatibility/ServerCompatibilityController.cs` | slskd compatibility shim. |
| `src/slskd/API/Compatibility/UsersCompatibilityController.cs` | slskd compatibility shim. |
| `src/slskd/API/Mesh/MeshGatewayController.cs` | Mesh transport protocol route. |
| `src/slskd/API/Native/CapabilitiesController.cs` | Has versioned alias; legacy slskdN native route retained for compatibility. |
| `src/slskd/API/Native/JobsController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/API/Native/LibraryHealthController.cs` | Has versioned alias; legacy native route retained for compatibility. |
| `src/slskd/API/Native/SourceProvidersController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/API/Native/WarmCacheController.cs` | Has versioned alias; legacy native route retained for compatibility. |
| `src/slskd/API/VirtualSoulfind/BridgeAdminController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/API/VirtualSoulfind/BridgeController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/API/VirtualSoulfind/CanonicalController.cs` | Has versioned alias; legacy VirtualSoulfind route retained for compatibility. |
| `src/slskd/API/VirtualSoulfind/DisasterModeController.cs` | Has versioned alias; legacy VirtualSoulfind route retained for compatibility. |
| `src/slskd/API/VirtualSoulfind/ShadowIndexController.cs` | Has versioned alias; legacy VirtualSoulfind route retained for compatibility. |
| `src/slskd/Audio/API/AnalyzerMigrationController.cs` | Has versioned alias; legacy audio admin route retained for compatibility. |
| `src/slskd/Audio/API/CanonicalController.cs` | Has versioned alias; legacy audio route retained for compatibility. |
| `src/slskd/Audio/API/DedupeController.cs` | Has versioned alias; legacy audio route retained for compatibility. |
| `src/slskd/Jobs/API/DiscographyJobsController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/Jobs/API/LabelCrateJobsController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/LibraryHealth/API/LibraryHealthController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/SocialFederation/API/ActivityPubController.cs` | ActivityPub protocol route. |
| `src/slskd/SocialFederation/API/WebFingerController.cs` | WebFinger protocol route. |
| `src/slskd/SourceFeeds/API/SourceFeedImportsController.cs` | Has versioned alias; legacy route retained for compatibility. |
| `src/slskd/SourceFeeds/API/SpotifyConnectionController.cs` | Has versioned alias; legacy/OAuth route retained for compatibility and provider callbacks. |
