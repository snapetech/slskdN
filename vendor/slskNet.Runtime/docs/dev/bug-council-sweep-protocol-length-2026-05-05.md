# Bug Council Sweep - Protocol Length And Counts - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Protocol counted collection loops`
- `Protocol length-prefixed reads and payload allocations`
- `Protocol compression boundary candidates`

Candidate markers:

- Protocol counted collection loops: 54/54 classified
- Protocol length-prefixed reads and payload allocations: 12/12 classified
- Protocol compression boundary candidates: 16/16 classified
- Unclassified candidates: 0

The broad `Protocol count and length allocation candidates` scan remains intentionally noisy. This sweep closes the loop flaw by adding countable sub-sections underneath that broad scan before burn-down decisions are made.

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
