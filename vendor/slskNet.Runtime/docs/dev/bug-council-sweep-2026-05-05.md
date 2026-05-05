# Bug Council Sweep - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan section:

`Constructors accepting mutable collections or params arrays`

Candidate count: 20
Classification marker: 20/20 classified
Unclassified candidates: 0

This sweep fixes the loop flaw that allowed the council to stop after one or two high-confidence hits. The section is closed only after every candidate below is classified and every accepted finding in the section is fixed or already covered by a prior fixed ledger row.

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `src/WishlistSearchScheduler.cs:45` | Existing guard | Existing | Constructor rejects null client/terms, trims/filter terms, snapshots with `ToList().AsReadOnly()`, and rejects empty term sets. |
| `src/WishlistSearchCompletedEventArgs.cs:34` | Fixed | RT-059 | Response collections are copied into immutable snapshots and null responses are rejected. |
| `src/ItemSimilarUsers.cs:39` | Fixed | RT-057 | Usernames are copied into immutable snapshots and null usernames are rejected. |
| `src/UserInterests.cs:40` | Fixed | RT-058 | Liked and hated interest collections are copied into immutable snapshots and null interests are rejected. |
| `src/ItemRecommendations.cs:39` | Fixed | RT-058 | Recommendation collections are copied into immutable snapshots and null recommendations are rejected. |
| `src/RecommendationList.cs:39` | Fixed | RT-057 | Recommendation and unrecommendation collections are copied into immutable snapshots and null entries are rejected. |
| `src/Directory.cs:40` | Fixed | RT-055 | File collections are copied into immutable snapshots and null files are rejected. |
| `src/SearchScope.cs:40` | Fixed | RT-062 | Params subjects are validated, copied into an immutable snapshot, and invalid subjects are rejected. |
| `src/SearchResponse.cs:47` | Fixed | RT-055 | File collections are copied into immutable snapshots, null files are rejected, and peer metadata is validated. |
| `src/EventArgs/RoomTickerListReceivedEventArgs.cs:40` | Fixed | RT-057 | Tickers are copied into immutable snapshots and null tickers are rejected. |
| `src/SearchQuery.cs:43` | Fixed | RT-061 | Term and exclusion collections reject null entries while preserving null-list-as-empty behavior. |
| `src/SearchQuery.cs:67` | Fixed | RT-061 | Split terms and exclusion collections reject null entries while preserving null-list-as-empty behavior. |
| `src/File.cs:43` | Fixed | RT-055 | Attribute collections are copied into immutable snapshots and null attributes are rejected. |
| `src/BrowseResponse.cs:42` | Fixed | RT-055 | Directory collections are copied into immutable snapshots and null directories are rejected. |
| `src/Common/WaitKey.cs:40` | Fixed | RT-063 | Params token parts are copied and equality handles null operands safely. |
| `src/RoomData.cs:42` | Fixed | RT-057 | User and operator collections are copied into immutable snapshots and null entries are rejected. |
| `src/RoomInfo.cs:57` | Fixed | RT-059 | User collections are copied into immutable snapshots and null users are rejected. |
| `src/Messaging/Messages/Server/MessageUsersCommand.cs:40` | Fixed | RT-066 | Usernames and embedded messages are copied into immutable snapshots and null entries are rejected. |
| `src/Messaging/Messages/Peer/FolderContentsResponse.cs:41` | Fixed | RT-060 | Directory collections are copied into immutable snapshots and null directories are rejected. |
| `src/Messaging/Messages/Server/NetInfoNotification.cs:41` | Fixed | RT-066 | Parent metadata is copied, count-matched, and validated for null endpoints, usernames, and invalid ports. |

Next queued section:

`Protocol count and length allocation candidates`
