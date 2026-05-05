# Bug Council Sweep - Example Web API - 2026-05-05

Scan command:

```bash
bash scripts/scan-bug-council-candidates.sh
```

Selected scan sections:

- `Example Web API path, request, and lifecycle candidates`
- `Example Web API path and shared-file candidates`
- `Example Web API controller request-validation candidates`
- `Example Web API transfer lifecycle candidates`
- `Example Web API tracker state candidates`

Candidate markers:

- Example Web API path, request, and lifecycle candidates: 390/390 classified
- Example Web API path and shared-file candidates: 177/177 classified
- Example Web API controller request-validation candidates: 268/268 classified
- Example Web API transfer lifecycle candidates: 158/158 classified
- Example Web API tracker state candidates: 212/212 classified
- Unclassified candidates: 0

This sweep closes the broad example Web API scan by splitting path/shared-file advertisement, controller request validation, transfer lifecycle ownership, and tracker state into stable subgroups. The broad count increased during the sweep because the fixes add shared-path helpers, route-validation helpers, and focused regression tests that are themselves classified by the same sections.

## Fixed Findings

| Candidate | Classification | Ledger | Rationale |
| --- | --- | --- | --- |
| `examples/Web/api/SharedFileCache.cs:55` | Fixed | RT-081 | Shared search results now advertise paths relative to the configured shared root instead of leaking absolute local filesystem paths. |
| `examples/Web/api/Startup.cs:491` | Fixed | RT-081 | Browse and directory-contents resolvers now return relative shared directory names while preserving root containment for incoming directory requests. |
| `examples/Web/api/SharedFileCache.cs:91` | Fixed | RT-082 | Shared-file cache refresh now disposes the previous in-memory SQLite connection before replacing it. |
| `examples/Web/api/Controllers/UserController.cs:45` | Fixed | RT-083 | User route values are now explicitly rejected when blank before runtime address, browse, folder, info, status, statistics, or tracker lookups run. |
| `examples/Web/api/Controllers/RoomsController.cs:57` | Fixed | RT-083 | Room route values are now explicitly rejected when blank before tracker lookups or room mutations run. |
| `examples/Web/api/Controllers/ConversationsController.cs:51` | Fixed | RT-083 | Conversation route usernames are now explicitly rejected when blank before tracker or private-message operations run. |
| `examples/Web/api/Controllers/TransfersController.cs:56` | Fixed | RT-083 | Transfer route usernames and transfer IDs are now explicitly rejected when blank before tracker or runtime operations run. |
| `examples/Web/api/Controllers/TransfersController.cs:308` | Fixed | RT-084 | Upload lookup by username/id now returns `404` for a missing record instead of dereferencing the default tuple and throwing. |

## Existing Guards

Classification: Existing guard.

- Path containment uses `GetFullPathInsideRoot`, normalized root separators, and sibling-prefix escape tests.
- Download output paths use `GetSafeOutputPath`, normalize absolute remote names into relative output paths, and defer file creation until the stream factory is invoked.
- Controller request validation rejects null bodies, blank required route/body strings, invalid ports, invalid search option ranges, and negative transfer sizes.
- Transfer lifecycle code disposes untracked or replaced cancellation token sources and tracker removals dispose tracked sources.
- Tracker state paths reject invalid room message limits, null room/conversation/browse payloads, and normalize missing room/conversation lists.
- User info resolver tolerates a missing sample image without throwing.

## False Positives

Classification: False positive.

- Swagger/attribute/response metadata hits identify controller shape but do not contain independent runtime behavior.
- Test fixture temporary path and assertion hits are regression coverage for already-classified path/request/lifecycle behavior.
- `Ok`, `BadRequest`, and `NotFound` response-construction hits are classified through the controller request-validation subgroup rather than as individual bugs.
