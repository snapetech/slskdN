# Dependency Ownership Inventory

This document tracks why runtime/build dependencies exist. It is intentionally conservative: a dependency is not justified merely because a future design might use it.

## Classification values

- `required-core` - required for stable slskd-compatible behavior.
- `required-feature` - required for a concrete implemented feature.
- `experimental-feature` - required only when an experimental feature is enabled.
- `build-only` - should not be required by the runtime application.
- `unused` - remove unless a call-site audit proves otherwise.
- `unknown` - must be resolved before release claims are strengthened.

## Audit commands

Run these from the repository root:

```bash
dotnet list src/slskd/slskd.csproj package > artifacts/package-list.txt
rg "MonoTorrent|AWSSDK|dotNetRDF|Zeroconf|MathNet|Microsoft.CodeAnalysis|Microsoft.Build|FluentFTP|TagLibSharp|NSec|OpenTelemetry|Prometheus|Dapper|MessagePack|System.Reactive" src tests docs config > artifacts/dependency-callsite-scan.txt
```

## Initial inventory

| Package / family | Current suspected owner | Classification | Required follow-up |
|---|---|---|---|
| ASP.NET Core / JwtBearer / SignalR | Web API, auth, live UI updates | required-core | Verify all auth-sensitive endpoints are covered. |
| Entity Framework Core / Microsoft.Data.Sqlite | Persistent app/transfer state | required-core | Confirm migrations and DB contexts. |
| Serilog and sinks | Logging | required-core / required-feature | Loki/HTTP sinks may be optional integration dependencies. |
| prometheus-net / DotNetRuntime / SystemMetrics | Metrics endpoint and dashboard | experimental-feature | Confirm metrics auth and feature gate. |
| OpenTelemetry packages | Telemetry/exporters | experimental-feature | Gate exporters and document egress. |
| TagLibSharp | HashDb media attribute probing (`HashDbService`) and audio/library surfaces | required-feature | Keep because active share/media probing call sites exist; ensure slow/remote storage opt-out remains documented. |
| FluentFTP | FTP integration | experimental-feature | Gate integration and document credentials/egress. |
| Mono.Nat | NAT/port mapping | experimental-feature | Gate under mesh/VPN/network features. |
| MonoTorrent 3.9.0-alpha.unstable.rev0000 | Public BitTorrent DHT rendezvous and BitTorrent-backed swarm experiments | experimental-feature | Targets .NET 8 and is compatible with .NET 10; gated by DHT/multi-source surfaces. PrivateOnly removes tracker/web-seed metadata and disables torrent DHT/PEX; engine-wide local peer discovery is disabled. The public DHT layer discovers mesh endpoints and does not carry slskdN file data. |
| NSec.Cryptography | Mesh transport signing and ActivityPub key/signature work | experimental-feature | Keep with mesh/social federation gates; expand protocol tests before stable claims. |
| MathNet.Numerics | MediaCore perceptual hashing (`PerceptualHasher`) | experimental-feature | Keep while MediaCore hashing uses MathNet vectors; gate MediaCore/SongID claims until tests cover this path. |
| AWSSDK.S3 | VirtualSoulfind v2 S3 backend | experimental-feature | Gated by VirtualSoulfind; document credential/egress behavior before stable claims. |
| Zeroconf | User-triggered nearby-peer browsing and opt-in LAN advertising (`LanDiscoveryService`) | experimental-feature | Nearby browsing is behind Identity/Friends APIs; startup advertising separately requires `lan_discovery.advertise: true` and emits multicast traffic. |
| dotNetRDF | Solid/WebID resolver (`SolidWebIdResolver`) | experimental-feature | Keep while Solid WebID parsing uses `VDS.RDF`; gate social/Solid surfaces and document network behavior. |
| Dapper | VirtualSoulfind v2 SQLite catalogue store | experimental-feature | Gated by VirtualSoulfind; acceptable while catalogue store remains active. |
| MessagePack | Mesh/protocol serialization | experimental-feature | Gate under protocol features. |
| System.Reactive | VirtualSoulfind disaster-mode transfer progress subjects | experimental-feature | Gated by VirtualSoulfind; keep while `MeshTransferService` uses `Subject<T>`. |
| Microsoft.Build.* | Custom build tasks | build-only | Moved to `tools/slskd.BuildTasks`; keep out of the runtime app project. |
| Microsoft.CodeAnalysis.* | Build-time source inspection | build-only | `BuildTimeAnalyzer` runs from `tools/slskd.BuildTasks`; unit tests reference that project directly. The unused, unregistered `SlskdnAnalyzer` was removed. `Microsoft.CodeAnalysis.NetAnalyzers` remains private to the app build, and runtime output is asserted free of Roslyn assembly references. |

## Release rule

No dependency should remain `unknown` when a feature is promoted to `stable`. Experimental-only dependencies should be feature-gated and documented in `FEATURE_INVENTORY.md`.
