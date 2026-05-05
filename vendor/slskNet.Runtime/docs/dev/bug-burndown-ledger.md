# Runtime Bug Burndown Ledger

This ledger tracks runtime-specific bug council findings for `slskNet.Runtime`. The scope is the .NET runtime library, its tests, package/release metadata, local scripts, and the example Web API only where it exercises runtime-facing path handling.

Statuses:

- `New`: discovered during static audit and awaiting confirmation.
- `Accepted`: confirmed or statically proven and queued for remediation.
- `Fixed`: remediated with local checks or regression tests.
- `Out of scope`: app-only, slskdN-specific, or not actionable in this runtime.

## Remediation Baseline

Run the local baseline before merging runtime hardening changes:

```bash
bash scripts/check-remediation-baseline.sh
```

The script verifies protocol parser count guards, frame and buffered-read limits, idempotent task completion, transfer cancellation/disconnect races, peer descriptor fail-closed behavior, Web API path containment, sensitive token/key pattern absence, fork branding metadata, and this command reference.

## First Council Sweep

Date: 2026-05-05

Static discovery covered `src`, `tests`, `examples`, `bin`, and `.circleci`.

| ID | Domain | Finding | Evidence | Status | Resolution |
| --- | --- | --- | --- | --- | --- |
| RT-001 | Protocol parsing | Repeated protocol collections could trust negative or impossible counts before allocation/read loops. | `src/Messaging/Messages/**` collection readers and regression coverage in `tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolCountHardeningTests.cs`. | Fixed | Added `ProtocolCountReader` and wired parser count/matching-count validation into protocol responses. |
| RT-002 | Network lifecycle/concurrency | Message and initialization frames needed pre-read length limits to prevent oversized buffered allocations. | `src/Network/MessageFrameValidator.cs`, `src/Network/MessageConnection.cs`, `src/Network/ListenerHandler.cs`, and `tests/Soulseek.Tests.Unit/Network/MessageFrameValidatorTests.cs`. | Fixed | Centralized frame validation against rotated-obfuscation limits before reading frame bodies. |
| RT-003 | Transfer streams | Buffered connection reads could allocate caller-declared lengths that are inappropriate for message handshakes. | `src/Network/Tcp/Connection.cs` and transfer paths using stream overloads for large payloads. | Fixed | Added `MaximumBufferedReadLength` guard and retained stream read/write overloads for transfers. |
| RT-004 | Network lifecycle/concurrency | Duplicate disconnect, denied, failed, and cancellation callbacks could complete task sources more than once. | `src/SoulseekClient.cs`, `src/Network/Tcp/Connection.cs`, `src/Common/Waiter.cs`, and `src/SearchInternal.cs`. | Fixed | Runtime-owned task completion uses `TrySetResult`, `TrySetException`, or `TrySetCanceled` for race-prone paths. |
| RT-005 | Transfer streams | Download/upload races between transfer IO, disconnect, remote failure, and caller cancellation needed deterministic loser cancellation. | `src/SoulseekClient.cs` download/upload transfer loops. | Fixed | Transfer methods race IO against disconnect/remote-failure tasks and cancel the losing path with linked tokens. |
| RT-006 | Peer capability/signature trust | Capability descriptors must not be treated as authorization and signature verification must fail closed on malformed metadata. | `src/Ed25519PeerDescriptorSigner.cs`, `src/PeerCapabilityRegistry.cs`, and README compatibility notes. | Fixed | Verifier returns false for malformed signatures and catches verifier exceptions; registry stores descriptors as discovery hints only. |
| RT-007 | Obfuscation | Obfuscated peer/distributed/transfer frame parsing needed the same frame bounds as regular message parsing. | `src/Network/MessageConnection.cs`, `src/Network/ListenerHandler.cs`, and `src/Network/Tcp/ObfuscatedTransferConnection.cs`. | Fixed | Obfuscated lengths are decoded then validated before body allocation/read. |
| RT-008 | Example Web API path safety | Shared-directory and download-output paths could be vulnerable to prefix sibling escapes or absolute remote names if path containment was string-prefix only. | `examples/Web/api/Extensions.cs`, `examples/Web/api/Controllers/TransfersController.cs`, and `tests/Soulseek.Tests.Unit/WebApiPathSecurityTests.cs`. | Fixed | Added normalized root containment and safe relative output path helpers with regression tests. |
| RT-009 | Release/package metadata | Forked runtime package metadata could drift back to upstream Soulseek.NET branding. | `src/Soulseek.csproj`, `README.md`, `.circleci/config.yml`, and `docs/fork-runtime-changes.md`. | Fixed | Package id, project URLs, description, CI image, and README identify `slskNet.Runtime`. |
| RT-010 | Tests/tooling | Runtime hardening checks needed a durable command and registry so future audits do not depend on ad hoc shell history. | `scripts/check-remediation-baseline.sh` and this ledger. | Fixed | Added a Bash-only remediation baseline; no root `package.json` is required. |
| RT-011 | Sensitive material | Static audit should catch accidental embedded API tokens, private keys, or access tokens in runtime files. | `scripts/check-remediation-baseline.sh` secret scan over tracked text files. | Fixed | Baseline scans source, tests, examples, docs, scripts, bin, and `.circleci` for high-confidence key/token patterns. |
| RT-012 | Integration environment | Live Soulseek integration tests require external credentials and network access. | `tests/Soulseek.Tests.Integration/Settings.cs`. | Out of scope | Unit and build verification are required for this sweep; live integration is attempted only when credentials/network are intentionally provided. |
| RT-013 | App-only slskdN checks | slskdN app database, UI, auth, and deployment checks from app council workflows do not apply directly to this runtime library. | Runtime repo has no app database or deployment service surface. | Out of scope | Do not copy app-only remediation checks into this runtime. |
| RT-014 | Protocol parsing | Room list parsing accepted negative per-room user counts from server payloads after collection count guards passed. | `src/Messaging/Messages/Server/RoomListResponseFactory.cs` and `tests/Soulseek.Tests.Unit/Messaging/Messages/ProtocolCountHardeningTests.cs`. | Fixed | Added `ProtocolCountReader.ValidateNonNegativeCount` and reject negative `RoomList` room user counts during parse. |
| RT-015 | Protocol parsing | Peer search/browse file parsers and transfer handshakes could accept negative 64-bit file sizes from malformed peer payloads. | `src/Messaging/MessageReaderExtensions.cs`, `src/Messaging/Messages/Peer/TransferRequest.cs`, `src/Messaging/Messages/Peer/TransferResponse.cs`, and peer parser tests. | Fixed | Reject negative parsed file sizes while preserving legacy Soulseek NS sign-extended unsigned 32-bit file-size normalization. |
| RT-016 | Protocol parsing | Server endpoint parsers accepted out-of-range ports from `GetPeerAddress`, `ConnectToPeer`, and distributed parent candidates. | `src/Messaging/Messages/ProtocolValueValidator.cs`, server endpoint response parsers, and endpoint parser tests. | Fixed | Added scalar protocol port validation so malformed endpoint ports fail closed with `MessageException`. |

## Verification Commands

Primary runtime verification:

```bash
bash scripts/check-remediation-baseline.sh
dotnet test tests/Soulseek.Tests.Unit/Soulseek.Tests.Unit.csproj --no-restore
dotnet build slskNet.Runtime.sln --no-restore
dotnet list slskNet.Runtime.sln package --vulnerable --include-transitive
```

Vendored runtime verification from `../slskdn/vendor/slskNet.Runtime`:

```bash
bash scripts/check-remediation-baseline.sh
dotnet test tests/Soulseek.Tests.Unit/Soulseek.Tests.Unit.csproj --no-restore
dotnet build slskNet.Runtime.sln --no-restore
dotnet build ../../slskd.sln --no-restore
```
