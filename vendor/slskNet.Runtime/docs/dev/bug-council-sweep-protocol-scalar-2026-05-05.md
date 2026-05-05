# Bug Council Sweep - Protocol Scalar Emission - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Protocol scalar emission candidates`
- `Protocol scalar constructor guard candidates`

Candidate markers:

- Protocol scalar emission candidates: 145/145 classified
- Protocol scalar constructor guard candidates: 66/66 classified
- Unclassified candidates: 0

The broad scalar emission scan includes every protocol `WriteInteger`, `WriteLong`, `WriteByte`, `WriteString`, and `WriteBytes` hit. The targeted constructor-guard subgroup keeps future sweeps from stopping at individual write calls without checking the constructor or builder boundary that owns the emitted value.

## Fixed Findings

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/Messaging/MessageBuilder.cs:217` | Fixed | RT-072 | `WriteString` now rejects null strings at the protocol builder boundary instead of failing through encoding internals. |
| `src/Messaging/Messages/Server/AcknowledgePrivateMessageCommand.cs:35` | Fixed | RT-072 | Private message acknowledgement IDs now reject negative values in the internal command constructor. |
| `src/Messaging/Messages/Server/AcknowledgePrivilegeNotificationCommand.cs:35` | Fixed | RT-072 | Privilege notification acknowledgement IDs now reject negative values in the internal command constructor. |
| `src/Messaging/Messages/Server/GivePrivilegesCommand.cs:36` | Fixed | RT-072 | Privilege grant durations now reject non-positive values in the internal command constructor. |
| Outbound token/version constructors | Fixed | RT-072 | Server search/connect, peer transfer/folder, initialization, and distributed message constructors now reject negative tokens and minor-version scalars through `ProtocolArgumentValidator`. |

## Existing Guards

Classification: Existing guard.

The remaining scalar emission hits are already covered by domain model constructors, protocol parser scalar validation, public client API validation, or fixed protocol-builder primitives:

- File and directory writers emit `File`, `FileAttribute`, `Directory`, `BrowseResponse`, and `SearchResponse` values already hardened by RT-055 and RT-061.
- Count and speed commands emit values already hardened by `SetSharedCountsCommand`, `SendUploadSpeedCommand`, and their baseline checks.
- Transfer scalar messages emit direction, file size, queue position, and allow/deny flags already hardened by transfer/request/response constructors, token guards, and parser checks.
- Listen-port and obfuscation port emissions are already bounded by `SetListenPortCommand`.
- Branch level/depth emissions are already bounded by distributed scalar constructors.
- Boolean flag emissions are derived from bool properties or validated parser inputs.
- String emissions now share `MessageBuilder.WriteString` null rejection, with public API guards remaining responsible for user-facing whitespace policy.
- Compression helper and `MessageReader` scalar method hits are parser/infrastructure boundaries, not outbound protocol command emitters; their buffer limits are already covered by the protocol length/allocation sweep.

Remaining candidate classes:

| Class | Current hits | Status | Next action |
| --- | ---: | --- | --- |
| Resolver output and raw stream candidates | 341 | Queued | Classify application-supplied resolver and stream boundaries. |
| Task, cancellation, timer, and semaphore lifecycle candidates | 201 | Queued | Classify lifecycle ownership and cancellation races. |
| Example Web API path, request, and lifecycle candidates | 302 | Queued | Classify example API path/request/disposable boundaries. |
| Mutable public byte arrays and array properties | 12 | Mostly fixed | Reclassify residual public array hits after snapshot fixes. |
| Value equality and hash-code comparisons | 4 | Mostly fixed | Reclassify residual equality operator hits after `WaitKey` and `ConnectionKey` fixes. |
| Security-sensitive material candidates | 2 | Baseline gated | Confirm scanner self-hits only and keep high-confidence secret scan active. |
| Non-idempotent task completion candidates | 0 | Closed | Keep baseline rejecting `.SetResult`, `.SetException`, and `.SetCanceled` in runtime source. |
