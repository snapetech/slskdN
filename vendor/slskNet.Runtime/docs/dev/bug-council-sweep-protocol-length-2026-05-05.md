# Bug Council Sweep - Protocol Length And Counts - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Protocol count and length allocation candidates`
- `Protocol counted collection loops`
- `Protocol length-prefixed reads and payload allocations`
- `Protocol compression boundary candidates`

Candidate markers:

- Protocol count and length allocation candidates: 221/221 classified
- Protocol counted collection loops: 54/54 classified
- Protocol length-prefixed reads and payload allocations: 12/12 classified
- Protocol compression boundary candidates: 16/16 classified
- Unclassified candidates: 0

The broad `Protocol count and length allocation candidates` scan remains intentionally noisy, but the current 221 hits are classified here so the broad queue cannot drift outside the council. Counted protocol loops, length-prefixed allocations, and compression boundaries are closed by the subgroups below. The remaining broad hits are fixed-size network buffers, guarded frame-read/write loops, scalar reads that delegate to hardened parser/model validation, lifecycle loops already covered by the lifecycle sweep, or legacy zlib internal loops behind bounded decompression.

## Protocol Count And Length Allocation Candidates

Classification: Existing guard / false positive.

The 221 broad hits are covered as follows:

- Obfuscated message and transfer frame allocations validate decoded lengths with `ValidateObfuscatedMessageLength`, `MessageFrameValidator`, or `RotatedObfuscation.MaxMessageLength` before allocating `8 + length`, payload, or frame buffers.
- Buffered connection reads reject negative, oversized, and over-limit lengths before allocating arrays; stream overloads use fixed-size buffers or pooled buffers and enforce governor forward progress.
- Transfer read/write loops bound each buffer by configured read/write sizes, frame payload limits, decoded buffer availability, or the caller-requested transfer length.
- Protocol counted collection loops are closed by the `Protocol counted collection loops` subgroup.
- Length-prefixed string, byte, and picture reads are closed by the `Protocol length-prefixed reads and payload allocations` subgroup.
- Scalar `ReadInteger` and `ReadLong` hits are not allocation/count loops by themselves; downstream constructors, protocol scalar validators, enum validators, and parser factories are covered by RT-001, RT-032, RT-041, RT-069, RT-071, and RT-072.
- Compression implementation loops and working buffers are behind bounded decompression and are closed by the compression subgroup.
- Network manager lifecycle loops are ownership/connection-drain loops, not protocol length/count trust boundaries; they are closed by the lifecycle sweep.

## Protocol Counted Collection Loops

Classification: Existing guard.

The 54 hits are the paired `ProtocolCountReader.ReadCount(...)` calls and their downstream loops. The central reader rejects negative counts and counts larger than the maximum possible item count for the remaining payload. Parallel protocol collection counts are validated separately with `ValidateMatchingCount`.

Covered files:

- `src/Messaging/MessageReaderExtensions.cs`
- `src/Messaging/Messages/Peer/BrowseResponseFactory.cs`
- `src/Messaging/Messages/Peer/FolderContentsResponse.cs`
- `src/Messaging/Messages/Peer/SearchResponseFactory.cs`
- `src/Messaging/Messages/Server/ExcludedSearchPhrasesNotification.cs`
- `src/Messaging/Messages/Server/ItemRecommendationsResponse.cs`
- `src/Messaging/Messages/Server/ItemSimilarUsersResponse.cs`
- `src/Messaging/Messages/Server/JoinRoomResponse.cs`
- `src/Messaging/Messages/Server/NetInfoNotification.cs`
- `src/Messaging/Messages/Server/PrivateRoomOwnedListNotification.cs`
- `src/Messaging/Messages/Server/PrivateRoomUserListNotification.cs`
- `src/Messaging/Messages/Server/PrivilegedUserListNotification.cs`
- `src/Messaging/Messages/Server/RecommendationsResponse.cs`
- `src/Messaging/Messages/Server/RoomListResponseFactory.cs`
- `src/Messaging/Messages/Server/RoomTickerListNotification.cs`
- `src/Messaging/Messages/Server/SimilarUsersResponse.cs`
- `src/Messaging/Messages/Server/UserInterestsResponse.cs`

## Protocol Length-Prefixed Reads And Payload Allocations

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/Network/Tcp/RotatedObfuscation.cs:55` | Fixed | RT-070 | Obfuscation encode now rejects null input before using `input.Length`. |
| `src/Network/Tcp/RotatedObfuscation.cs:70` | Fixed | RT-070 | Obfuscation decode now rejects null input before length checks and allocation. |
| `src/Network/Tcp/ObfuscatedTransferConnection.cs:137` | Existing guard | RT-029 | Buffered transfer reads reject negative, oversized, and over-limit lengths before allocation. |
| `src/Network/Tcp/ObfuscatedTransferConnection.cs:278` | Existing guard | RT-029 | Obfuscated transfer frame encoding rejects payloads larger than the frame maximum. |
| `src/Network/Tcp/ObfuscatedTransferConnection.cs:295` | Existing guard | RT-029 | Obfuscated transfer frame reads validate frame length before allocating the encoded frame buffer. |
| `src/Messaging/MessageReader.cs:146` | Existing guard | RT-001 | `ReadBytes` rejects negative counts and counts beyond remaining payload before slicing. |
| `src/Messaging/MessageReader.cs:240` | Existing guard | RT-001 | String reads route through the guarded length-prefixed string reader. |
| `src/Messaging/MessageReader.cs:252` | Existing guard | RT-001 | String length is rejected when negative or larger than the remaining payload. |
| `src/Messaging/MessageReader.cs:254` | Existing guard | RT-001 | The length field is validated before byte slicing and decoding. |
| `src/Network/MessageConnection.cs:324` | Existing guard | RT-029 | Obfuscated message length is validated before allocating the decoded frame. |
| `src/Network/ListenerHandler.cs:92` | Existing guard | RT-029 | Obfuscated initialization message length is validated before allocating the decoded init frame. |
| `src/Messaging/Messages/Peer/UserInfoResponseFactory.cs:60` | Existing guard | RT-001 | Picture length is rejected when negative or larger than remaining payload after trailing fields. |

## Protocol Compression Boundary Candidates

Classification: Existing guard / false positive.

The 16 hits are bounded decompression plumbing and legacy compression implementation buffers. Runtime decompression now writes through `BoundedMemoryStream` with `MaximumDecompressedPayloadLength`; the zlib implementation internals are not direct protocol count trust boundaries in this repo.

Covered files:

- `src/Messaging/MessageReader.cs`
- `src/Messaging/Messages/Peer/BrowseResponseFactory.cs`
- `src/Messaging/Messages/Peer/FolderContentsResponse.cs`
- `src/Messaging/Messages/Peer/SearchResponseFactory.cs`
- `src/Messaging/Compression/Deflate.cs`
- `src/Messaging/Compression/InfBlocks.cs`
- `src/Messaging/Compression/ZInputStream.cs`
- `src/Messaging/Compression/ZOutputStream.cs`
