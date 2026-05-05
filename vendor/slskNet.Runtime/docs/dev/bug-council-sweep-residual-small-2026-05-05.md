# Bug Council Sweep - Residual Small Queues - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Mutable public byte arrays and array properties`
- `Value equality and hash-code comparisons`
- `Security-sensitive material candidates`

Candidate markers:

- Mutable public byte arrays and array properties: 12/12 classified
- Value equality and hash-code comparisons: 4/4 classified
- Security-sensitive material candidates: 2/2 classified
- Unclassified candidates: 0

This sweep closes the small residual queues that stayed listed as "mostly fixed" after the broader constructor, protocol scalar, resolver, lifecycle, and Web API sweeps. Each hit below is classified so later council passes do not reopen the same queue without a new candidate.

## Fixed Findings

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/Network/Tcp/ConnectionKey.cs:72` | Fixed | RT-086 | Connection identity now compares usernames with `StringComparison.Ordinal` instead of culture-sensitive comparison. |
| `src/Network/Tcp/ConnectionKey.cs:96` | Fixed | RT-086 | Connection identity hash codes now use ordinal string hashing to match ordinal equality. |
| `src/Common/WaitKey.cs:94` | Fixed | RT-086 | Wait-key hash codes now use ordinal string hashing to match token equality. |

## Existing Guards

Classification: Existing guard.

- `src/PeerDescriptorSignature.cs:50` and `src/PeerDescriptorSignature.cs:55` return defensive copies of signature arrays.
- `src/UserInfo.cs:81` returns a defensive copy of the optional picture array.
- `src/Network/MessageConnectionEventArgs.cs:63`, `src/Network/MessageConnectionEventArgs.cs:100`, `src/Network/MessageConnectionEventArgs.cs:124`, and `src/Messaging/Messages/EmbeddedMessage.cs:54` return defensive copies for message payload arrays.
- `src/Common/WaitKey.cs:54` returns a copied token-parts array and the constructor snapshots params input.
- `src/SearchScope.cs:110` delegates to the constructor that snapshots and validates params subjects before publishing them as a read-only sequence.
- `src/Common/WaitKey.cs:56`, `src/Common/WaitKey.cs:61`, and `src/Common/WaitKey.cs:81` compare through `object.Equals` or ordinal token equality and handle null operands safely.

## False Positives

Classification: False positive.

- `tests/Soulseek.Tests.Unit/Network/Tcp/ConnectionKeyTests.cs:60` is test data, not a runtime mutable-array publication.
- `src/Messaging/Compression/ZStream.cs:78` and `src/Messaging/Compression/ZStream.cs:83` are internal zlib working buffers, not public runtime API state.
- `scripts/scan-bug-council-candidates.sh:142` is the scanner's own high-confidence secret regex.
- `scripts/check-remediation-baseline.sh:382` is the remediation baseline's own high-confidence secret regex.
