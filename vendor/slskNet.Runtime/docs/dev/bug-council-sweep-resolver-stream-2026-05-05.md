# Bug Council Sweep - Resolver Output And Raw Streams - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Resolver output and raw stream candidates`
- `Resolver delegate surface candidates`
- `Peer resolver dispatch candidates`
- `Transfer stream factory candidates`

Candidate markers:

- Resolver output and raw stream candidates: 341/341 classified
- Resolver delegate surface candidates: 61/61 classified
- Peer resolver dispatch candidates: 20/20 classified
- Transfer stream factory candidates: 49/49 classified
- Unclassified candidates: 0

This sweep closes the broad resolver/raw-stream scan by splitting it into ownership groups. Resolver delegate definitions are public configuration surfaces, peer resolver dispatches are the app-output-to-wire boundary, and transfer stream factories are long-running stream ownership/cancellation boundaries.

## Fixed Findings

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/Messaging/Handlers/PeerMessageHandler.cs:149` | Fixed | RT-074 | `UserInfoResolver` output now treats null as invalid resolver output and falls back to the default response with a targeted diagnostic. |
| `src/Messaging/Handlers/PeerMessageHandler.cs:537` | Fixed | RT-074 | Invalid place-in-queue resolver output is caught at response construction/write time and reported without falling through the generic peer-message handler. |

## Existing Guards

Classification: Existing guard.

- Search response resolver output is optional; null and empty responses are intentionally not sent, raw search streams have constructor guards and disposal coverage, and structured search responses are protected by domain model validation.
- Browse response resolver output is optional; null responses are intentionally ignored, raw browse streams have constructor guards and disposal coverage, and structured browse responses are protected by domain model validation.
- Directory contents resolver output is optional; invalid directory collections are rejected by `FolderContentsResponse` and reported by the peer handler without writing malformed data.
- User info resolver exceptions already fall back to the default user-info response; this sweep extends the same behavior to null resolver output.
- Place-in-queue resolver exceptions and null responses are already handled; this sweep extends targeted diagnostics to invalid non-null resolver output.
- Transfer input/output stream factories already reject null factories, validate readable/writable streams before use, observe linked cancellation during transfer races, reject early EOF, and dispose owned streams according to `TransferOptions`.
- Raw response streams are app-owned inputs with positive length and non-null stream constructor guards; write failures dispose streams and are reported through existing connection/diagnostic paths.

## False Positives

Classification: False positive.

- `ToByteArray()` hits outside resolver dispatch are normal message serializers already covered by protocol constructor/scalar/count sweeps.
- Compression `Stream` hits are parser infrastructure already covered by the protocol length/allocation sweep.
- XML documentation hits for stream factory parameters are command-reference context, not separate runtime paths.
