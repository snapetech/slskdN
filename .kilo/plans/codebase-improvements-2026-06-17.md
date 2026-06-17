# Codebase Improvements Implementation Plan
**Date**: 2026-06-17  
**Status**: Ready for Implementation  
**Priority**: Critical bugs → Refactoring → Documentation

---

## Executive Summary

This plan addresses critical issues and refactoring opportunities identified in the comprehensive codebase evaluation:
- **Critical**: HttpClient socket exhaustion (35 instances)
- **Critical**: Thread-unsafe Random usage (4 instances)
- **High Priority**: Decompose massive components (MediaCore: 8610 lines)
- **Medium Priority**: Refactor large services (5 services >2000 lines)
- **Low Priority**: Remove defensive null checks (11 instances)

**Estimated Timeline**: 8-10 sprints  
**Risk Level**: Medium (thorough testing required)

---

## Phase 1: Critical Bug Fixes (Sprint 1)

### PR-01: Fix HttpClient Socket Exhaustion
**Priority**: P0 (Critical)  
**Complexity**: Medium  
**Files Affected**: 35 files  
**Test Impact**: Integration tests required

#### Implementation Steps

1. **Register HttpClient in DI** (`src/slskd/Program.cs`):
   ```csharp
   // In ConfigureDependencyInjectionContainer()
   services.AddHttpClient("slskdn-default")
       .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler 
       {
           AutomaticDecompression = DecompressionMethods.All
       });
   
   services.AddHttpClient("slskdn-no-redirect")
       .ConfigurePrimaryHttpMessageHandler(() => OutboundUriGuard.CreateNoRedirectHandler());
   
   services.AddHttpClient<RelayClient>();
   services.AddHttpClient<WebhookService>();
   ```

2. **Update affected files** (inject `IHttpClientFactory`):
   - `Common/Security/HttpTunnelTransport.cs`
   - `Common/Security/MeekTransport.cs`
   - `Common/GitHub.cs`
   - `Integrations/Webhooks/WebhookService.cs`
   - `Relay/RelayClient.cs` (3 instances)
   - `DhtRendezvous/NatDetectionService.cs`
   - `Sharing/API/SharesController.cs`
   - 26 additional files (grep for `new HttpClient(`)

3. **Pattern for migration**:
   ```csharp
   // BEFORE
   using var http = new HttpClient(handler, disposeHandler: true);
   
   // AFTER
   public class MyService
   {
       public MyService(IHttpClientFactory httpClientFactory)
       {
           HttpClientFactory = httpClientFactory;
       }
       
       private IHttpClientFactory HttpClientFactory { get; }
       
       public async Task DoWorkAsync()
       {
           using var http = HttpClientFactory.CreateClient("slskdn-no-redirect");
           // ...
       }
   }
   ```

4. **Testing**:
   - Run full test suite: `dotnet test`
   - Add integration test for HttpClient pooling behavior
   - Load test to verify no socket exhaustion
   - Check live logs for connection pool metrics

5. **Documentation**:
   - Document gotcha in ADR-0001: "HttpClient Socket Exhaustion"
   - Update ADR-0002 code patterns with HttpClient DI pattern
   - Update `memory-bank/progress.md`

**Validation**:
- [ ] All tests pass (`dotnet test`)
- [ ] No `new HttpClient(` instances remain (grep check)
- [ ] Integration test added for HttpClient pooling
- [ ] `./bin/lint` passes
- [ ] ADR-0001 gotcha documented
- [ ] Live deployment shows healthy connection metrics

---

### PR-02: Fix Thread-Unsafe Random Usage
**Priority**: P0 (Critical)  
**Complexity**: Low  
**Files Affected**: 4 files  
**Test Impact**: Unit tests required

#### Implementation Steps

1. **Replace `new Random()` with `Random.Shared`**:
   - `Common/Security/BucketPadder.cs`
   - `Common/Security/Honeypot.cs`
   - `Common/Security/RandomJitterObfuscator.cs`
   - `Bootstrap/StartupConsoleOutput.cs`

2. **Pattern for migration**:
   ```csharp
   // BEFORE
   private readonly Random random = new Random();
   var value = random.Next(100);
   
   // AFTER
   var value = Random.Shared.Next(100);
   ```

3. **Testing**:
   - Run focused tests for affected services
   - Add concurrency test to verify thread safety
   - Run full test suite

4. **Documentation**:
   - Document gotcha in ADR-0001: "Thread-Unsafe Random Usage"
   - Update `memory-bank/progress.md`

**Validation**:
- [ ] All tests pass
- [ ] No `new Random()` instances remain (grep check)
- [ ] Concurrency test added
- [ ] `./bin/lint` passes
- [ ] ADR-0001 gotcha documented

---

## Phase 2: Frontend Refactoring (Sprints 2-4)

### PR-03: Decompose MediaCore Component - Phase 1 (Status & Stats)
**Priority**: P1 (High)  
**Complexity**: High  
**Lines Reduced**: ~2500 lines  
**Test Impact**: Frontend tests required

#### Implementation Steps

1. **Extract MediaCoreStatus component** (~1200 lines):
   ```jsx
   // src/web/src/components/System/MediaCore/MediaCoreStatus.jsx
   - Pod status indicators
   - Connection state
   - Health checks
   - Online/offline peers
   ```

2. **Extract MediaCoreStats component** (~1300 lines):
   ```jsx
   // src/web/src/components/System/MediaCore/MediaCoreStats.jsx
   - Pod statistics
   - Transfer metrics
   - Storage stats
   - Activity graphs
   ```

3. **Update MediaCore index.jsx**:
   - Import new components
   - Replace inline sections with `<MediaCoreStatus />` and `<MediaCoreStats />`
   - Maintain state management in parent
   - Pass props down to children

4. **Testing**:
   - Run frontend tests: `cd src/web && npm test`
   - Visual regression testing in browser
   - Test all MediaCore tab interactions
   - Verify no functionality lost

5. **Documentation**:
   - Document refactoring in `memory-bank/progress.md`
   - Add comments explaining component hierarchy

**Validation**:
- [ ] Frontend tests pass (`npm test`)
- [ ] MediaCore/index.jsx reduced to ~6000 lines
- [ ] Visual regression test passes
- [ ] Frontend lint passes (`npm run lint`)
- [ ] Production build succeeds (`npm run build`)

---

### PR-04: Decompose MediaCore Component - Phase 2 (Tools & Config)
**Priority**: P1 (High)  
**Complexity**: High  
**Lines Reduced**: ~2500 lines  
**Test Impact**: Frontend tests required

#### Implementation Steps

1. **Extract MediaCoreTools component** (~1200 lines):
   ```jsx
   // src/web/src/components/System/MediaCore/MediaCoreTools.jsx
   - Optional media tool installation UI
   - Tool version checks
   - Tool configuration forms
   - Tool execution controls
   ```

2. **Extract MediaCoreConfig component** (~1300 lines):
   ```jsx
   // src/web/src/components/System/MediaCore/MediaCoreConfig.jsx
   - Pod configuration forms
   - Integration settings
   - Advanced options
   - Configuration validation
   ```

3. **Update MediaCore index.jsx**:
   - Import new components
   - Replace inline sections
   - Reduce to ~3500 lines (coordinator role)

4. **Testing**:
   - Run frontend tests
   - Test tool installation flows
   - Test configuration save/load
   - Visual regression testing

**Validation**:
- [ ] Frontend tests pass
- [ ] MediaCore/index.jsx reduced to ~3500 lines
- [ ] All functionality preserved
- [ ] Frontend lint passes
- [ ] Production build succeeds

---

### PR-05: Decompose MediaCore Component - Phase 3 (Finalization)
**Priority**: P1 (High)  
**Complexity**: Medium  
**Lines Reduced**: ~1000 lines  
**Test Impact**: Frontend tests required

#### Implementation Steps

1. **Extract shared MediaCore utilities**:
   ```jsx
   // src/web/src/components/System/MediaCore/utils.js
   - Data formatting helpers
   - State transformation logic
   - API call wrappers
   ```

2. **Extract MediaCore hooks**:
   ```jsx
   // src/web/src/components/System/MediaCore/hooks.js
   - useMediaCoreStatus
   - useMediaCoreStats
   - useMediaCorePolling
   ```

3. **Finalize MediaCore index.jsx** (~2500 lines):
   - Coordinator for child components
   - Top-level state management
   - Tab navigation
   - Error boundaries

4. **Documentation**:
   - Create `MediaCore/README.md` explaining component hierarchy
   - Document props and state flow
   - Add JSDoc comments

**Validation**:
- [ ] Frontend tests pass
- [ ] MediaCore/index.jsx reduced to ~2500 lines
- [ ] Component README created
- [ ] Frontend lint passes
- [ ] Production build succeeds

---

### PR-06: Decompose Other Large Frontend Components
**Priority**: P2 (Medium)  
**Complexity**: Medium  
**Files Affected**: 3 components  
**Test Impact**: Frontend tests required

#### Implementation Steps

1. **Decompose Integrations/index.jsx** (3556 lines → ~1500 lines):
   ```jsx
   // Split into:
   - IntegrationsLidarr.jsx (~800 lines)
   - IntegrationsMusicBrainz.jsx (~600 lines)
   - IntegrationsWebhooks.jsx (~600 lines)
   - IntegrationsIndex.jsx (coordinator, ~500 lines)
   ```

2. **Decompose PlayerBar.jsx** (3064 lines → ~1200 lines):
   ```jsx
   // Split into:
   - PlayerControls.jsx (~600 lines)
   - PlayerProgress.jsx (~500 lines)
   - PlayerMetadata.jsx (~400 lines)
   - PlayerQueue.jsx (~600 lines)
   - PlayerBar.jsx (coordinator, ~400 lines)
   ```

3. **Decompose Visualizer.jsx** (2313 lines → ~1000 lines):
   ```jsx
   // Split into:
   - VisualizerWaveform.jsx (~500 lines)
   - VisualizerSpectrum.jsx (~400 lines)
   - VisualizerParticles.jsx (~400 lines)
   - Visualizer.jsx (coordinator, ~300 lines)
   ```

4. **Testing**:
   - Run focused frontend tests for each component
   - Visual regression testing
   - Test all interactions

**Validation**:
- [ ] Frontend tests pass
- [ ] All components reduced to <1500 lines
- [ ] Frontend lint passes
- [ ] Production build succeeds

---

## Phase 3: Backend Service Refactoring (Sprints 5-7)

### PR-07: Split SongIdService
**Priority**: P2 (Medium)  
**Complexity**: High  
**Lines Reduced**: 4406 → ~2000 total  
**Test Impact**: Unit tests required

#### Implementation Steps

1. **Create SongIdScoringService** (~1500 lines):
   ```csharp
   // src/slskd/SongID/SongIdScoringService.cs
   public interface ISongIdScoringService
   {
       Task<SongIdScore> ScoreRecordingAsync(string recordingId, File file);
       Task<List<SongIdCandidate>> RankCandidatesAsync(List<Recording> recordings);
   }
   ```

2. **Create SongIdLookupService** (~1200 lines):
   ```csharp
   // src/slskd/SongID/SongIdLookupService.cs
   public interface ISongIdLookupService
   {
       Task<List<Recording>> FindRecordingsAsync(AudioFingerprint fingerprint);
       Task<Recording> GetRecordingDetailsAsync(string recordingId);
   }
   ```

3. **Refactor SongIdService** (~1700 lines):
   - Keep coordination logic
   - Inject ISongIdScoringService and ISongIdLookupService
   - Delegate scoring to SongIdScoringService
   - Delegate lookups to SongIdLookupService

4. **Update DI registration** (`Program.cs`):
   ```csharp
   services.AddSingleton<ISongIdScoringService, SongIdScoringService>();
   services.AddSingleton<ISongIdLookupService, SongIdLookupService>();
   services.AddSingleton<ISongIdService, SongIdService>();
   ```

5. **Testing**:
   - Run focused SongId tests
   - Run full test suite
   - Integration test for SongId flow

**Validation**:
- [ ] All tests pass
- [ ] SongIdService.cs reduced to ~1700 lines
- [ ] New services registered in DI
- [ ] `./bin/lint` passes
- [ ] No functionality lost

---

### PR-08: Split HashDbService
**Priority**: P2 (Medium)  
**Complexity**: High  
**Lines Reduced**: 3828 → ~2000 total  
**Test Impact**: Unit tests required

#### Implementation Steps

1. **Create HashDbQueryService** (~1200 lines):
   ```csharp
   // src/slskd/HashDb/HashDbQueryService.cs
   public interface IHashDbQueryService
   {
       Task<List<HashDbEntry>> FindByHashAsync(string hash);
       Task<HashDbEntry> FindByFileAsync(string filename);
       Task<List<HashDbEntry>> SearchAsync(HashDbSearchCriteria criteria);
   }
   ```

2. **Create HashDbIngestionService** (~1400 lines):
   ```csharp
   // src/slskd/HashDb/HashDbIngestionService.cs
   public interface IHashDbIngestionService
   {
       Task<HashDbEntry> IngestFileAsync(string filename, AudioVariant variant);
       Task<int> BackfillFromSearchHistoryAsync();
       Task<int> IngestCompletedDownloadAsync(Transfer transfer);
   }
   ```

3. **Refactor HashDbService** (~1200 lines):
   - Keep coordination and lifecycle
   - Inject IHashDbQueryService and IHashDbIngestionService
   - Delegate queries to QueryService
   - Delegate ingestion to IngestionService

4. **Update DI registration**:
   ```csharp
   services.AddSingleton<IHashDbQueryService, HashDbQueryService>();
   services.AddSingleton<IHashDbIngestionService, HashDbIngestionService>();
   services.AddSingleton<IHashDbService, HashDbService>();
   ```

5. **Testing**:
   - Run focused HashDb tests (66 tests)
   - Run full test suite
   - Integration test for ingestion pipeline

**Validation**:
- [ ] All tests pass
- [ ] HashDbService.cs reduced to ~1200 lines
- [ ] New services registered in DI
- [ ] `./bin/lint` passes
- [ ] No functionality lost

---

### PR-09: Split DownloadService
**Priority**: P2 (Medium)  
**Complexity**: High  
**Lines Reduced**: 2240 → ~1500 total  
**Test Impact**: Unit tests required

#### Implementation Steps

1. **Create DownloadQueueService** (~800 lines):
   ```csharp
   // src/slskd/Transfers/Downloads/DownloadQueueService.cs
   public interface IDownloadQueueService
   {
       Task EnqueueAsync(DownloadEnqueueRequest request);
       Task<List<Transfer>> GetQueuedDownloadsAsync();
       Task RemoveFromQueueAsync(Guid transferId);
   }
   ```

2. **Create DownloadExecutionService** (~900 lines):
   ```csharp
   // src/slskd/Transfers/Downloads/DownloadExecutionService.cs
   public interface IDownloadExecutionService
   {
       Task<Transfer> ExecuteDownloadAsync(Transfer transfer);
       Task CancelDownloadAsync(Guid transferId);
       Task RetryDownloadAsync(Guid transferId);
   }
   ```

3. **Refactor DownloadService** (~1000 lines):
   - Keep coordination and state management
   - Inject IDownloadQueueService and IDownloadExecutionService
   - Delegate queueing to QueueService
   - Delegate execution to ExecutionService

4. **Update DI registration and integration tests**

5. **Testing**:
   - Run focused download tests (33 tests)
   - Run integration tests (278 tests)
   - Test download flow end-to-end

**Validation**:
- [ ] All tests pass
- [ ] DownloadService.cs reduced to ~1000 lines
- [ ] New services registered in DI
- [ ] `./bin/lint` passes
- [ ] Integration tests updated

---

### PR-10: Refactor MultiSourceDownloadService
**Priority**: P3 (Low)  
**Complexity**: Medium  
**Lines Reduced**: 1873 → ~1200 total  
**Test Impact**: Unit tests required

#### Implementation Steps

1. **Create MultiSourceCoordinatorService** (~800 lines):
   - Source discovery
   - Source ranking
   - Transfer orchestration

2. **Refactor MultiSourceDownloadService** (~1000 lines):
   - Keep high-level coordination
   - Inject IMultiSourceCoordinatorService

3. **Testing**:
   - Run focused multi-source tests
   - Integration test for multi-source flow

**Validation**:
- [ ] All tests pass
- [ ] MultiSourceDownloadService.cs reduced to ~1000 lines
- [ ] `./bin/lint` passes

---

### PR-11: Refactor SourceFeedImportService
**Priority**: P3 (Low)  
**Complexity**: Medium  
**Lines Reduced**: 1661 → ~1000 total  
**Test Impact**: Unit tests required

#### Implementation Steps

1. **Create SourceFeedParserService** (~600 lines):
   - Feed parsing logic
   - Format detection
   - Schema validation

2. **Refactor SourceFeedImportService** (~1000 lines):
   - Keep import coordination
   - Inject ISourceFeedParserService

3. **Testing**:
   - Run focused source feed tests
   - Test various feed formats

**Validation**:
- [ ] All tests pass
- [ ] SourceFeedImportService.cs reduced to ~1000 lines
- [ ] `./bin/lint` passes

---

## Phase 4: Code Quality Improvements (Sprint 8)

### PR-12: Remove Defensive Null Checks in Internal Code
**Priority**: P3 (Low)  
**Complexity**: Low  
**Files Affected**: 11 files  
**Test Impact**: Minimal

#### Implementation Steps

1. **Remove defensive null checks** in internal methods (not API boundaries):
   - Identify 11 instances via: `grep -r "ArgumentNullException\.ThrowIfNull" src/slskd`
   - Remove checks in internal service methods
   - Keep checks at API controller boundaries

2. **Pattern**:
   ```csharp
   // BEFORE (internal method)
   public async Task ProcessAsync(string username)
   {
       ArgumentNullException.ThrowIfNull(username); // Remove this
       var user = await GetUserAsync(username);
   }
   
   // AFTER
   public async Task ProcessAsync(string username)
   {
       var user = await GetUserAsync(username); // Let it fail with clear stack
   }
   
   // KEEP (API boundary)
   [HttpPost]
   public async Task<IActionResult> Process([FromRoute] string username)
   {
       ArgumentNullException.ThrowIfNull(username); // Keep this
       await ProcessAsync(username);
   }
   ```

3. **Testing**:
   - Run full test suite
   - Verify clear stack traces on null failures

**Validation**:
- [ ] All tests pass
- [ ] No ArgumentNullException.ThrowIfNull in internal code
- [ ] Checks remain at API boundaries
- [ ] `./bin/lint` passes

---

### PR-13: Fix Catch-Return-Null in MonoTorrentBitTorrentBackend
**Priority**: P3 (Low)  
**Complexity**: Low  
**Files Affected**: 1 file  
**Test Impact**: Unit test required

#### Implementation Steps

1. **Fix MonoTorrentBitTorrentBackend.cs**:
   ```csharp
   // BEFORE
   try { return InfoHash.FromHex(s); } catch { return null; }
   
   // AFTER
   if (!InfoHash.TryFromHex(s, out var hash))
   {
       logger.LogWarning("Invalid info hash format: {Hash}", s);
       return null;
   }
   return hash;
   ```

2. **Add test** for invalid hash format handling

3. **Document gotcha** in ADR-0001

**Validation**:
- [ ] Test added for invalid hash
- [ ] `./bin/lint` passes
- [ ] ADR-0001 updated

---

## Phase 5: Documentation & Finalization (Sprint 9)

### PR-14: Documentation Updates
**Priority**: P2 (Medium)  
**Complexity**: Low  
**Files Affected**: Documentation only

#### Implementation Steps

1. **Create architecture diagrams**:
   ```
   docs/architecture/
   ├── service-decomposition.md
   ├── httpclient-pooling.md
   ├── frontend-component-hierarchy.md
   └── diagrams/
       ├── songid-services.svg
       ├── hashdb-services.svg
       └── download-services.svg
   ```

2. **Update ADR-0002 with new patterns**:
   - HttpClient via IHttpClientFactory
   - Service decomposition pattern
   - Component decomposition pattern
   - Random.Shared usage

3. **Update memory-bank**:
   - Mark completed tasks
   - Update progress.md with all changes
   - Update activeContext.md

4. **Generate API documentation**:
   - Add Swashbuckle/NSwag for OpenAPI
   - Generate API docs from XML comments
   - Publish to `/api/docs`

**Validation**:
- [ ] All documentation updated
- [ ] Architecture diagrams created
- [ ] API docs generated
- [ ] memory-bank updated

---

## Testing Strategy

### Per-PR Testing
Each PR must pass:
1. **Unit tests**: `dotnet test --filter Category=Unit`
2. **Integration tests**: `dotnet test --filter Category=Integration`
3. **Smoke tests**: `dotnet test --filter Category=Smoke`
4. **Frontend tests**: `cd src/web && npm test`
5. **Lint**: `./bin/lint`
6. **Frontend lint**: `cd src/web && npm run lint`
7. **Build**: `dotnet build -c Release`
8. **Frontend build**: `cd src/web && npm run build`

### Integration Testing
After each phase:
1. Deploy to manual Docker test environment
2. Run live validation suite
3. Monitor logs for errors/warnings
4. Performance testing (load, stress)

### Regression Testing
Full regression after Phase 3:
1. End-to-end download flows
2. Multi-source download scenarios
3. HashDb ingestion pipeline
4. SongId identification flows
5. Frontend navigation and interactions

---

## Risk Mitigation

### High-Risk Changes
1. **HttpClient migration**: Socket exhaustion if misconfigured
   - **Mitigation**: Integration test + load test + gradual rollout
2. **Service decomposition**: Breaking dependencies
   - **Mitigation**: Comprehensive unit tests + interface contracts
3. **Frontend decomposition**: Lost functionality or state bugs
   - **Mitigation**: Visual regression tests + manual QA

### Rollback Plan
Each PR includes:
1. **Revert commit ready**: Document revert procedure
2. **Feature flags**: Gate major changes behind runtime flags
3. **Monitoring**: Add metrics for new code paths
4. **Alerts**: Set up alerts for error rate increases

---

## Success Metrics

### Code Quality
- [ ] No files >2500 lines
- [ ] No `new HttpClient()` instances
- [ ] No `new Random()` instances
- [ ] All tests passing (4581+ tests)
- [ ] Frontend tests passing (767+ tests)
- [ ] Lint clean (backend and frontend)

### Performance
- [ ] No socket exhaustion under load
- [ ] No thread-safety issues in production
- [ ] Build time <5 minutes
- [ ] Test execution time <10 minutes

### Documentation
- [ ] All gotchas documented in ADR-0001
- [ ] New patterns documented in ADR-0002
- [ ] Architecture diagrams created
- [ ] API documentation generated

---

## Timeline Estimate

| Phase | PRs | Sprints | Dependencies |
|-------|-----|---------|--------------|
| Phase 1: Critical Bugs | PR-01, PR-02 | Sprint 1 | None |
| Phase 2: Frontend Refactoring | PR-03, PR-04, PR-05, PR-06 | Sprints 2-4 | Phase 1 |
| Phase 3: Backend Refactoring | PR-07, PR-08, PR-09, PR-10, PR-11 | Sprints 5-7 | Phase 1 |
| Phase 4: Code Quality | PR-12, PR-13 | Sprint 8 | Phase 3 |
| Phase 5: Documentation | PR-14 | Sprint 9 | All phases |

**Total**: 9 sprints, 14 PRs

---

## Memory-Bank Task IDs

Tasks to add to `memory-bank/tasks.md`:

```markdown
- [ ] **T-IMPROVE-001**: Fix HttpClient socket exhaustion (35 instances)
  - Status: pending
  - Priority: P0 (Critical)
  - PR: PR-01
  - Sprint: 1

- [ ] **T-IMPROVE-002**: Fix thread-unsafe Random usage (4 instances)
  - Status: pending
  - Priority: P0 (Critical)
  - PR: PR-02
  - Sprint: 1

- [ ] **T-IMPROVE-003**: Decompose MediaCore component Phase 1 (Status & Stats)
  - Status: pending
  - Priority: P1 (High)
  - PR: PR-03
  - Sprint: 2

- [ ] **T-IMPROVE-004**: Decompose MediaCore component Phase 2 (Tools & Config)
  - Status: pending
  - Priority: P1 (High)
  - PR: PR-04
  - Sprint: 3
  - Depends: T-IMPROVE-003

- [ ] **T-IMPROVE-005**: Decompose MediaCore component Phase 3 (Finalization)
  - Status: pending
  - Priority: P1 (High)
  - PR: PR-05
  - Sprint: 3
  - Depends: T-IMPROVE-004

- [ ] **T-IMPROVE-006**: Decompose large frontend components (Integrations, PlayerBar, Visualizer)
  - Status: pending
  - Priority: P2 (Medium)
  - PR: PR-06
  - Sprint: 4

- [ ] **T-IMPROVE-007**: Split SongIdService into Scoring and Lookup services
  - Status: pending
  - Priority: P2 (Medium)
  - PR: PR-07
  - Sprint: 5

- [ ] **T-IMPROVE-008**: Split HashDbService into Query and Ingestion services
  - Status: pending
  - Priority: P2 (Medium)
  - PR: PR-08
  - Sprint: 6

- [ ] **T-IMPROVE-009**: Split DownloadService into Queue and Execution services
  - Status: pending
  - Priority: P2 (Medium)
  - PR: PR-09
  - Sprint: 7

- [ ] **T-IMPROVE-010**: Refactor MultiSourceDownloadService
  - Status: pending
  - Priority: P3 (Low)
  - PR: PR-10
  - Sprint: 7

- [ ] **T-IMPROVE-011**: Refactor SourceFeedImportService
  - Status: pending
  - Priority: P3 (Low)
  - PR: PR-11
  - Sprint: 7

- [ ] **T-IMPROVE-012**: Remove defensive null checks in internal code
  - Status: pending
  - Priority: P3 (Low)
  - PR: PR-12
  - Sprint: 8

- [ ] **T-IMPROVE-013**: Fix catch-return-null in MonoTorrentBitTorrentBackend
  - Status: pending
  - Priority: P3 (Low)
  - PR: PR-13
  - Sprint: 8

- [ ] **T-IMPROVE-014**: Create architecture documentation and diagrams
  - Status: pending
  - Priority: P2 (Medium)
  - PR: PR-14
  - Sprint: 9
```

---

## Notes

### Deferred Items
- **Options.cs** (4736 lines): Configuration monolith - difficult to split without breaking changes
- **Application.cs** (2964 lines): Main orchestrator - intentionally large coordinator class

### Future Improvements
- Consider feature flags for gradual rollout of major refactors
- Add performance benchmarks for service decomposition
- Consider migrating to .NET 9 when stable
- Evaluate moving to minimal APIs for new controllers

---

## Approval Checklist

Before starting implementation:
- [ ] Plan reviewed and approved
- [ ] Timeline acceptable
- [ ] Resource allocation confirmed
- [ ] Risk mitigation acceptable
- [ ] Testing strategy approved
- [ ] Tasks added to memory-bank/tasks.md

---

**Ready for implementation** ✓
