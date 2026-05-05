# Bug Council Sweep - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan section:

`Constructors accepting mutable collections or params arrays`

Candidate count: 28
Classification marker: 28/28 classified
Unclassified candidates: 0

This sweep fixes the loop flaws that allowed the council to stop after one or two high-confidence hits and miss multiline constructors. The section is closed only after every candidate below is classified and every accepted finding in the section is fixed or already covered by a prior fixed ledger row.

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/MeshRendezvousResult.cs:32` | Fixed | RT-057 | Similar users and capability records are copied into immutable snapshots and null entries are rejected. |
| `src/WishlistSearchScheduler.cs:45` | Existing guard | Existing | Constructor rejects null client/terms, trims/filter terms, snapshots with `ToList().AsReadOnly()`, and rejects empty term sets. |
| `src/WishlistSearchCompletedEventArgs.cs:34` | Fixed | RT-059 | Response collections are copied into immutable snapshots and null responses are rejected. |
| `src/RoomList.cs:41` | Fixed | RT-057 | Room lists and moderated room names are copied into immutable snapshots and null entries are rejected. |
| `src/ItemSimilarUsers.cs:39` | Fixed | RT-057 | Usernames are copied into immutable snapshots and null usernames are rejected. |
| `src/UserInterests.cs:40` | Fixed | RT-058 | Liked and hated interest collections are copied into immutable snapshots and null interests are rejected. |
| `src/ItemRecommendations.cs:39` | Fixed | RT-058 | Recommendation collections are copied into immutable snapshots and null recommendations are rejected. |
| `src/PeerCapabilityDescriptor.cs:35` | Existing guard | Existing | Feature inputs are normalized through trimming, blank/null filtering, distinct sorting, and immutable snapshot publication; capability hints are not authorization decisions. |
| `src/RecommendationList.cs:39` | Fixed | RT-057 | Recommendation and unrecommendation collections are copied into immutable snapshots and null entries are rejected. |
| `src/DistributedNetworkInfo.cs:49` | Existing guard | Existing | Children are copied before publication and topology scalar bounds are already validated; null child endpoint validation remains lower confidence because existing callers use default parent tuples. |
| `src/Directory.cs:40` | Fixed | RT-055 | File collections are copied into immutable snapshots and null files are rejected. |
| `src/SearchScope.cs:40` | Fixed | RT-062 | Params subjects are validated, copied into an immutable snapshot, and invalid subjects are rejected. |
| `src/SearchResponse.cs:47` | Fixed | RT-055 | File collections are copied into immutable snapshots, null files are rejected, and peer metadata is validated. |
| `src/SearchResponse.cs:93` | Fixed | RT-055 | Internal copy construction delegates to the hardened public constructor for replacement file collection validation. |
| `src/EventArgs/RoomTickerListReceivedEventArgs.cs:40` | Fixed | RT-057 | Tickers are copied into immutable snapshots and null tickers are rejected. |
| `src/Messaging/Messages/Server/RoomTickerListNotification.cs:40` | Fixed | RT-068 | Tickers are copied into immutable snapshots; count mismatches, negative counts, and null ticker entries are rejected. |
| `src/SearchQuery.cs:43` | Fixed | RT-061 | Term and exclusion collections reject null entries while preserving null-list-as-empty behavior. |
| `src/SearchQuery.cs:67` | Fixed | RT-061 | Split terms and exclusion collections reject null entries while preserving null-list-as-empty behavior. |
| `src/Options/SoulseekClientOptionsPatch.cs:91` | False positive | Existing | Hit is a delegate return type `Task<IEnumerable<Directory>>`, not a constructor-owned mutable collection. Resolver output is covered separately by peer handler validation. |
| `src/Options/SoulseekClientOptions.cs:114` | False positive | Existing | Hit is a delegate return type `Task<IEnumerable<Directory>>`, not a constructor-owned mutable collection. Resolver output is covered separately by peer handler validation. |
| `src/File.cs:43` | Fixed | RT-055 | Attribute collections are copied into immutable snapshots and null attributes are rejected. |
| `src/BrowseResponse.cs:42` | Fixed | RT-055 | Directory collections are copied into immutable snapshots and null directories are rejected. |
| `src/Common/WaitKey.cs:40` | Fixed | RT-063 | Params token parts are copied and equality handles null operands safely. |
| `src/RoomData.cs:42` | Fixed | RT-057 | User and operator collections are copied into immutable snapshots and null entries are rejected. |
| `src/RoomInfo.cs:57` | Fixed | RT-059 | User collections are copied into immutable snapshots and null users are rejected. |
| `src/Messaging/Messages/Server/MessageUsersCommand.cs:40` | Fixed | RT-066 | Usernames and embedded messages are copied into immutable snapshots and null entries are rejected. |
| `src/Messaging/Messages/Peer/FolderContentsResponse.cs:41` | Fixed | RT-060 | Directory collections are copied into immutable snapshots and null directories are rejected. |
| `src/Messaging/Messages/Server/NetInfoNotification.cs:41` | Fixed | RT-066 | Parent metadata is copied, count-matched, and validated for null endpoints, usernames, and invalid ports. |

Second selected scan section:

`Protocol count and length allocation candidates`

Candidate count: 221
Classification marker: 221/221 classified
Unclassified candidates: 0
New accepted candidates from this section: 0

This section was classified as a whole section after re-running `bash scripts/scan-bug-council-candidates.sh` at `2026-05-05T22:48:47Z`. Counts below are grouped by file families because the scanner intentionally reports every count read, loop, allocation, and fixed-size buffer candidate; the grouped totals add to the scanner count of 221.

| Candidate group | Hits | Classification | Ledger | Rationale |
| --- | ---: | --- | --- | --- |
| `src/Messaging/Compression/*` zlib internals | 80 | Existing guard | RT-002, RT-007 | These are internal compression buffers and loops. Protocol decompression is bounded by `MessageReader.BoundedMemoryStream` and frame limits before payload publication. |
| `MessageReader`, `MessageBuilder`, `MessageReaderExtensions`, `ProtocolCountReader` primitives | 15 | Fixed | RT-001, RT-003, RT-015 | String/byte reads reject negative and overrun lengths, file attribute/file counts use `ProtocolCountReader`, and buffered reads are capped. |
| `src/Network/*` frame, transfer, and socket buffer candidates | 34 | Existing guard | RT-002, RT-003, RT-007, RT-049, RT-051 | Message and obfuscated frame lengths are bounded; buffered reads are capped; transfer stream loops fail on premature EOF instead of silently succeeding. |
| Distributed and initialization token/scalar readers | 7 | Existing guard | RT-031 | These hits are fixed-size token, branch, depth, and ping scalar reads with no untrusted allocation loop; invalid topology scalars are already rejected. |
| Peer protocol parser candidates | 21 | Fixed | RT-001, RT-015, RT-018, RT-028 | Browse/search/folder collection counts use `ProtocolCountReader`; picture bytes are bounded against remaining payload; peer transfer and queue scalars are validated. |
| Server protocol parser candidates | 64 | Fixed | RT-001, RT-014, RT-016, RT-018, RT-032, RT-033, RT-068 | Server list counts use `ProtocolCountReader`, parallel counts are matched, endpoint ports and booleans are validated, and room ticker count metadata is now validated. |

Remaining candidate classes:

| Class | Current hits | Status | Next action |
| --- | ---: | --- | --- |
| Protocol scalar emission candidates | 145 | Queued | Classify outbound scalar constructors and burn down accepted gaps. |
| Resolver output and raw stream candidates | 341 | Queued | Classify application-supplied resolver and stream boundaries. |
| Task, cancellation, timer, and semaphore lifecycle candidates | 201 | Queued | Classify lifecycle ownership and cancellation races. |
| Example Web API path, request, and lifecycle candidates | 302 | Queued | Classify example API path/request/disposable boundaries. |
| Mutable public byte arrays and array properties | 12 | Mostly fixed | Reclassify residual public array hits after snapshot fixes. |
| Value equality and hash-code comparisons | 4 | Mostly fixed | Reclassify residual equality operator hits after `WaitKey` and `ConnectionKey` fixes. |
| Security-sensitive material candidates | 2 | Baseline gated | Confirm scanner self-hits only and keep high-confidence secret scan active. |
| Non-idempotent task completion candidates | 0 | Closed | Keep baseline rejecting `.SetResult`, `.SetException`, and `.SetCanceled` in runtime source. |
