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

Remaining candidate classes:

| Class | Current hits | Status | Next action |
| --- | ---: | --- | --- |
| Protocol scalar emission candidates | 145 | Fixed | Closed by `docs/dev/bug-council-sweep-protocol-scalar-2026-05-05.md`; accepted outbound scalar constructor and builder gaps were fixed. |
| Resolver output and raw stream candidates | 341 | Fixed | Closed by `docs/dev/bug-council-sweep-resolver-stream-2026-05-05.md`; accepted resolver-output diagnostics were fixed. |
| Task, cancellation, timer, and semaphore lifecycle candidates | 203 | Fixed | Closed by `docs/dev/bug-council-sweep-lifecycle-2026-05-05.md`; accepted cancellation registration and fire-and-forget async gaps were fixed. |
| Example Web API path, request, and lifecycle candidates | 390 | Fixed | Closed by `docs/dev/bug-council-sweep-webapi-2026-05-05.md`; accepted shared-path, route-validation, cache-lifecycle, and upload lookup gaps were fixed. |
| Mutable public byte arrays and array properties | 12 | Fixed | Closed by `docs/dev/bug-council-sweep-residual-small-2026-05-05.md`; residual hits are defensive copies, internal compression buffers, or tests. |
| Value equality and hash-code comparisons | 4 | Fixed | Closed by `docs/dev/bug-council-sweep-residual-small-2026-05-05.md`; accepted ordinal identity gaps were fixed. |
| Security-sensitive material candidates | 2 | Fixed | Closed by `docs/dev/bug-council-sweep-residual-small-2026-05-05.md`; both hits are scanner/baseline regex self-hits and the high-confidence secret gate remains active. |
