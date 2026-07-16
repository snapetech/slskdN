## Update 2026-07-16 16:54:43Z

- Current task: performance and efficiency improvements in progress; mesh-search content mapping streaming complete locally.
- Last activity:
  - Removed full mapping-list materialization from mesh-search response enrichment and stopped at the first advertisable row while retaining the first fallback.
  - At the covered 1,000-row best case, mapping reads fall 99.9%; the no-advertisable fallback remains unchanged without buffering.
- Validation:
  - Passed focused handler (`9/9`), broader DHT rendezvous (`147/147`), and full backend suites (`4958/4958`) tests.
  - Exact early-stop/fallback boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the mesh-search mapping slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 16:47:58Z

- Current task: performance and efficiency improvements in progress; Virtual Soulfind canonical single-pass selection complete locally.
- Last activity:
  - Replaced complete variant sorting/materialization with a stable O(n), constant-working-memory maximum scan.
  - The covered 10,000-variant selection allocates below 4 KiB while preserving codec/quality priority and first-tie behavior.
- Validation:
  - Passed focused controller (`6/6`), broader Virtual Soulfind (`397/397`), and full backend suites (`4956/4956`) tests.
  - Exact priority/case/quality/tie and allocation boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the canonical selector slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 16:40:52Z

- Current task: performance and efficiency improvements in progress; MediaCore dashboard registry-snapshot consolidation complete locally.
- Last activity:
  - Shared one immutable registry/domain snapshot between dashboard registry and IPLD aggregation while leaving standalone endpoints fresh.
  - For three domains, registry calls fall from eight to four (50% fewer) while both dashboard sections retain their results.
- Validation:
  - Passed focused stats (`1/1`), broader MediaCore (`229/229`), and full backend suites (`4954/4954`) tests.
  - Exact one-stats/one-domain-call boundary, registry/type/IPLD results, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the MediaCore dashboard snapshot slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 16:31:50Z

- Current task: performance and efficiency improvements in progress; Levenshtein shared-affix elimination complete locally.
- Last activity:
  - Added a case-insensitive equality fast path and zero-copy shared-prefix/suffix trimming before edit-distance evaluation.
  - At the covered 20,001-character near-match, distance cells fall from 400,040,001 to one while the exact score is unchanged.
- Validation:
  - Passed focused fuzzy matcher (`41/41`), broader MediaCore (`228/228`), and full backend suites (`4953/4953`) tests.
  - Exact long-prefix allocation/score, combined-prefix/suffix correctness, case/empty/distance behavior, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Levenshtein shared-affix slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 16:23:50Z

- Current task: performance and efficiency improvements in progress; bounded Levenshtein working memory complete locally.
- Last activity:
  - Replaced the full edit-distance matrix with two rolling rows sized to the shorter input.
  - At 2,048-by-2,048 characters, distance storage falls 99.90% and measured call allocation stays below 128 KiB while exact scores remain unchanged.
- Validation:
  - Passed focused fuzzy matcher (`39/39`), broader MediaCore (`226/226`), and full backend suites (`4951/4951`) tests.
  - Exact long-input score/allocation, case/empty/distance behavior, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the bounded Levenshtein slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 16:16:31Z

- Current task: performance and efficiency improvements in progress; fuzzy candidate descriptor reuse complete locally.
- Last activity:
  - Reused usable target/candidate descriptor retrievals within one fuzzy candidate pass while leaving missing/error results retryable.
  - At 100 unique candidates, retriever calls fall from 200 to 101 (49.5% fewer); duplicate candidate results retain multiplicity with one descriptor read.
- Validation:
  - Passed focused fuzzy matcher (`38/38`), broader MediaCore (`225/225`), and full backend suites (`4950/4950`) tests.
  - Exact target, duplicate-candidate, duplicate-result, and missing-target retry boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Documented gotcha `0z701` (`d11c44e84`). Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the fuzzy descriptor-reuse slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 16:04:58Z

- Current task: performance and efficiency improvements in progress; IPLD validation registry-membership caching complete locally.
- Last activity:
  - Cached ContentID registration results within each validation run and reduced orphan-source membership checks from once per link to once per source.
  - For 1,000 repeated links to one target, registry calls fall from 2,000 to two while per-link diagnostics and validation results remain unchanged.
- Validation:
  - Passed focused IPLD (`14/14`), broader MediaCore (`222/222`), and full backend suites (`4947/4947`) tests.
  - Exact repeated-target, unique-target, orphan-source, and diagnostic-multiplicity checks, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the IPLD validation caching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:53:55Z

- Current task: performance and efficiency improvements in progress; AdvancedDiscovery peer-metric batching complete locally.
- Last activity:
  - Added bounded 500-ID HashDb metric reads and a cache-aware batch service, then replaced serialized ranking lookups with one metrics map.
  - At 100 uncached peers, reads fall 99%; 501 IDs use two reads while source/cache/default/fallback/ranking/network semantics remain unchanged.
- Validation:
  - Passed focused (`23/23`), broader MultiSource/HashDb (`255/255`), and full backend suites (`4945/4945`) tests.
  - Exact 100-peer batch/zero-scalar, cache/default, 501-ID normalization/miss/index, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. The concurrent full run again hit the unrelated Mesh stream pipe timeout; its exact rerun and complete unit rerun passed. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the peer-metric batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:44:51Z

- Current task: performance and efficiency improvements in progress; bounded multi-source canonical skip reads complete locally.
- Last activity:
  - Replaced complete local variant hydration/sorting with the exact one-row HashDb best-variant read.
  - At the covered 1,003-row boundary, mapped rows fall 99.9% while quality threshold, improvement, missing, and skip semantics remain unchanged.
- Validation:
  - Passed focused (`4/4`), broader MultiSource (`130/130`), and full backend suites (`4942/4942`) tests.
  - Exact best-read/zero-list and decision boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. The first consolidated run had one transient Mesh stream pipe timeout; its exact rerun and the complete unit rerun passed. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the bounded canonical skip-read slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:38:43Z

- Current task: performance and efficiency improvements in progress; bounded MediaCore recording fallback complete locally.
- Last activity:
  - Added an exact HashDb best-deduplicated-variant query and replaced MediaCore's full recording variant hydration/group/sort fallback.
  - At 1,003 rows, mapped output falls 99.9% while direct-key, duplicate recency, quality/seen, exact identity, missing, and non-music semantics remain unchanged.
- Validation:
  - Passed focused (`3/3`), broader HashDb/MediaCore (`341/341`), and full backend (`4938/4938`) tests.
  - Exact result/duplicate-conflict/index and one-best-read/zero-list boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the bounded MediaCore recording fallback slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:31:26Z

- Current task: performance and efficiency improvements in progress; scalar recipient collection authorization complete locally.
- Last activity:
  - Added one indexed active direct/group collection-access query and replaced recipient GET's complete accessible-grant hydration.
  - With 1,000 unrelated accessible grants, hydration falls to zero while owner, expiry, identity/membership, GUID, malformed/unrelated, and not-found semantics remain unchanged.
- Validation:
  - Passed focused repository/service/controller (`35/35`), broader Sharing (`109/109`), and full backend (`4936/4936`) tests.
  - Exact result/SQL/materialization/query-plan and zero-list service/controller boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the scalar recipient collection-access slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:23:21Z

- Current task: performance and efficiency improvements in progress; indexed share-token content authorization complete locally.
- Last activity:
  - Added an exact collection/content existence query and matching fresh/existing-database composite index, then removed full collection hydration from both share-token streaming paths.
  - At 1,000 items, hydration falls to zero while case, scope, token/ticket, resolver, limiter, and not-found semantics remain unchanged.
- Validation:
  - Passed focused repository/service/controller (`49/49`), broader Sharing/Streaming (`196/196`), and full backend (`4933/4933`) tests.
  - Exact SQL/result/miss/scope/materialization/query-plan/upgrade and zero-list caller boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the indexed streaming membership slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:13:51Z

- Current task: performance and efficiency improvements in progress; exact single-grant access authorization complete locally.
- Last activity:
  - Added active grant-by-ID access resolution with group membership evidence scoped to that grant, then routed GET, backfill, and authenticated manifests through it.
  - At 1,001 direct grants, hydration falls 99.9%; grant GET also removes one redundant read while authorization and response semantics remain unchanged.
- Validation:
  - Passed focused repository/service/controller (`37/37`), broader Sharing (`103/103`), and full backend (`4930/4930`) tests.
  - Exact direct/group SQL/materialization/result and zero-list caller boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the exact single-grant access slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:06:54Z

- Current task: performance and efficiency improvements in progress; scoped collection-item update lookup complete locally.
- Last activity:
  - Added an untracked collection/ID item lookup and replaced the update controller's full-list scan.
  - At 1,000 items, hydration falls 99.9% while ownership, scope, missing-item, normalization, persistence, and response semantics remain unchanged.
- Validation:
  - Passed focused repository/service/controller (`33/33`), broader Sharing (`100/100`), and full backend (`4927/4927`) tests.
  - Exact SQL/materialization/result/service/controller boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the scoped collection-item lookup slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 15:01:26Z

- Current task: performance and efficiency improvements in progress; atomic single-row peer-member removal complete locally.
- Last activity:
  - Replaced peer-ID lookup, hydration, and tracked delete with one `rowid`-limited SQLite delete.
  - Commands fall 50% and hydration falls to zero while one-row duplicate, no-op, group-scope, and EF exception semantics remain unchanged.
- Validation:
  - Passed focused repository (`13/13`), broader Sharing (`98/98`), and full backend (`4925/4925`) tests.
  - Exact command/single-row/duplicate/no-op/exception boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the atomic peer-member removal slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 14:56:01Z

- Current task: performance and efficiency improvements in progress; atomic collection-item append complete locally.
- Last activity:
  - Replaced separate maximum-ordinal lookup and tracked insertion with one parameterized `INSERT ... SELECT ... RETURNING` statement.
  - Commands fall 50% and ordinal assignment is atomic while full-field, return-instance, sparse-order, and EF exception behavior remain unchanged.
- Validation:
  - Passed focused collection repository (`4/4`), broader Sharing (`95/95`), and full backend (`4922/4922`) tests.
  - Exact command/ordinal/materialization/field/exception boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the atomic collection-item append slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 14:43:53Z

- Current task: performance and efficiency improvements in progress; atomic share-group member admission complete locally.
- Last activity:
  - Replaced separate duplicate reads and inserts with one parameterized conditional insert for user- and peer-keyed membership.
  - Commands fall 50% and duplicate decisions are atomic while legacy peer, backward-compatible user ID, and `DbUpdateException` behavior remain unchanged. Documented gotcha `0z700` (`9f9eda8f6`).
- Validation:
  - Passed focused repository (`10/10`), broader Sharing (`93/93`), and full backend (`4920/4920`) tests.
  - Exact command/new/duplicate, legacy-peer, foreign-key, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the atomic member-admission slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 14:36:53Z

- Current task: performance and efficiency improvements in progress; bounded collection reorder persistence complete locally.
- Last activity:
  - Replaced full collection-item hydration and tracked ordinal updates with transactional 400-ID `CASE` updates.
  - At 1,000 items, hydration falls to zero and writes use three commands while order, duplicate/missing/untouched, collection-scope, and empty-input semantics remain unchanged.
- Validation:
  - Passed focused reorder (`2/2`), broader Sharing (`89/89`), and full backend (`4916/4916`) tests.
  - Exact command/materialization/order and edge-case boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the collection reorder batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 14:25:22Z

- Current task: performance and efficiency improvements in progress; atomic key-unique Sharing deletes complete locally.
- Last activity:
  - Replaced five read-hydrate-delete paths with one key-targeted `ExecuteDeleteAsync` command each.
  - Commands fall 50% and entity hydration falls to zero while missing-row, return-value, and cascade behavior remain unchanged; non-unique peer-ID removal deliberately retains single-row semantics.
- Validation:
  - Passed focused delete (`3/3`), broader Sharing (`87/87`), and full backend (`4914/4914`) tests.
  - Exact command/result/no-op/cascade boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the atomic Sharing delete slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 14:20:20Z

- Current task: performance and efficiency improvements in progress; set-based incoming collection item replacement complete locally.
- Last activity:
  - Replaced complete prior-item hydration and tracked deletes with one `ExecuteDeleteAsync` command inside an explicit transaction spanning all announcement writes.
  - At 1,000 prior items, replacement hydration falls 100% while item, collection, grant, validation, and rollback semantics remain unchanged. Documented gotchas `0z698` and `0z699` (`36a8f7f7c`).
- Validation:
  - Passed focused announcement (`6/6`), complete Sharing (`81/81`), and full backend (`4911/4911`) tests.
  - Exact command/materialization/result and injected-failure rollback boundaries, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the collection-announcement replacement slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 14:09:48Z

- Current task: performance and efficiency improvements in progress; indexed Wishlist ignored-result duplicate lookup complete locally.
- Last activity:
  - Replaced full per-item ignored-rule hydration with an exact composite-index lookup using existing `NOCASE` collations.
  - At 1,001 rules, hydrated duplicate candidates fall 99.9% while existing-rule identity and zero-write behavior remain unchanged.
- Validation:
  - Passed focused (`1/1`), broader Wishlist (`36/36`), and full backend (`4909/4909`) tests.
  - Exact SQL fields/limit, composite-index use, case/normalization semantics, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Wishlist ignored-result lookup slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 14:03:18Z

- Current task: performance and efficiency improvements in progress; share-group contact nickname batching complete locally.
- Last activity:
  - Added bounded 500-peer contact lookup and replaced per-member contact contexts/queries with one indexed result map.
  - At 100 peer-backed members, reads/contexts fall 99%; 501 IDs use two queries in one context.
- Validation:
  - Passed focused Identity/Sharing (`17/17`) and full backend (`4908/4908`) tests.
  - Exact batch/scalar call boundaries, 501-ID chunking, nickname/member semantics, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the contact/share-group batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 13:56:03Z

- Current task: performance and efficiency improvements in progress; discarded share-manifest contact hydration removed locally.
- Last activity:
  - Removed one complete contact-list read per manifest because owner application IDs cannot resolve peer contacts.
  - Preserved explicit peer-backed share-group nickname resolution; documented gotcha `0z697` (`8cd09ad07`).
- Validation:
  - Passed complete Sharing unit coverage (`79/79`) and full backend tests (`4907/4907`).
  - The manifest contact boundary is now exactly zero reads/rows. Repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Sharing manifest slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 13:50:47Z

- Current task: performance and efficiency improvements in progress; bounded native Jobs database pages complete locally.
- Last activity:
  - Replaced synchronous full job lists/JSON hydration/application pagination with a scalar HashDb union count and bounded page.
  - Added created/status indexes through HashDb migration 24 and removed the blocking full-list adapter contract. Documented gotcha `0z696` (`1d175baec`).
- Validation:
  - Passed focused (`12/12`), broader HashDb/Jobs (`115/115`), and full backend (`4907/4907`) tests.
  - The 100,000-job database median improved from 0.091 seconds to 0.033 seconds; returned rows fell 99.9%. Repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Jobs/HashDb page slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 13:33:47Z

- Current task: performance and efficiency improvements in progress; Search startup snapshot consolidation complete locally.
- Last activity:
  - Removed the duplicate normal REST history load, retained it as hub-failure fallback, and capped the hub snapshot at the shared 500-search default.
  - Normal Search list reads/payloads fall 50%; a 100,000-search hub snapshot falls 99.5%. Documented gotcha `0z695` (`43a92a1e3`).
- Validation:
  - Passed focused backend (`3/3`) and Web (`12/12`), full backend (`4903/4903`), and full Web (`880` passed, `4` skipped) suites.
  - Production Web build, changed-file ESLint, repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Search hub/Web startup slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 13:24:12Z

- Current task: performance and efficiency improvements in progress; bounded download-request attempt hydration complete locally.
- Last activity:
  - Replaced full attempt loading/sorting with aggregate/current-ID projection and current-row-only hydration.
  - Added a mixed-direction request/current index for fresh and migrated databases; documented direction parity gotcha `0z694` (`ed5679f6d`).
- Validation:
  - Passed focused tests (`4/4`), complete Transfers unit coverage (`269/269`), and full backend tests (`4902/4902`: `69` application, `4553` unit, `280` integration).
  - Synthetic 100,000-attempt median improved from 0.082 seconds to 0.021 seconds; hydration fell 95%. Repository lint, diff checks, and every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the download-request list/index slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 13:13:18Z

- Current task: performance and efficiency improvements in progress; unreachable auto-retry index migration repaired locally.
- Last activity:
  - Registered the existing ordered partial auto-retry index migration in the production startup migrator.
  - Added a registry reachability regression alongside the existing schema/idempotence test. Documented gotcha `0z693` (`1e4b65c37`).
- Validation:
  - Focused migration tests passed (`2/2`); broader validation will run with the next isolated performance slice.
  - Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the migration registration repair.
  2. Replace download-request list full attempt hydration with indexed aggregate/current-attempt projections.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 13:07:41Z

- Current task: performance and efficiency improvements in progress; bounded persisted peer ranking complete locally.
- Last activity:
  - Moved default peer-cost selection and the requested limit into HashDb while keeping the existing C# cost function authoritative for the bounded return order.
  - At limit 100 over 100,000 metrics, application hydration falls by 99.9%; the synthetic bounded-query median was 0.044 seconds versus 0.088 seconds for full-row streaming before application sorting.
- Validation:
  - Passed focused tests (`8/8`) and full backend tests (`4898/4898`: `69` application, `4549` unit, `280` integration).
  - Cost parity, first-row case-insensitive deduplication, tie order, limits, repository lint, and diff checks passed. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the peer-metrics ranking slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 12:49:02Z

- Current task: performance and efficiency improvements in progress; atomic warm-cache touch pass complete locally.
- Last activity:
  - Replaced warm-cache metadata read/hydrate/upsert with one targeted timestamp update.
  - Existing-entry touches fall from two SQLite commands/connections to one (50% fewer), with no full-row hydration or rewrite.
- Validation:
  - Passed focused touch tests (`2/2`), broader HashDb/warm-cache coverage (`97/97`), and full backend tests (`4891/4891`: `69` application, `4542` unit, `280` integration).
  - Identifier normalization, missing-entry no-op behavior, exact one-update/zero-legacy-call boundaries, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the warm-cache atomic-touch slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 12:41:43Z

- Current task: performance and efficiency improvements in progress; indexed recommendation Wishlist duplicate lookup complete locally.
- Last activity:
  - Added an idempotent case-insensitive Wishlist search-text index and untracked newest exact-match lookup, then routed federated recommendation promotion through it.
  - With 10,000 items, entity hydration falls from 10,000 tracked rows to one untracked row (99.99% fewer).
- Validation:
  - Passed focused lookup/promotion tests (`2/2`), broader Wishlist/recommendation/Lidarr coverage (`47/47`), and full backend tests (`4889/4889`: `69` application, `4540` unit, `280` integration).
  - Exact indexed SQL, newest case-insensitive duplicate selection, migration idempotence, zero full-list calls, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Wishlist index/exact-lookup slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 12:32:29Z

- Current task: performance and efficiency improvements in progress; bounded music metadata variant fallback pass complete locally.
- Last activity:
  - Added one bounded best-variant-per-recent-recording HashDb projection and replaced full recording-ID enumeration plus batch hydration in local metadata fallback matching.
  - Reads fall from two to one (50% fewer), and application memory is bounded at 256 variants instead of scaling with all variant recording IDs.
- Validation:
  - Passed focused regressions (`2/2`), broader HashDb/music-provider coverage (`137/137`), and full backend tests (`4888/4888`: `69` application, `4539` unit, `280` integration).
  - Exact one-query/zero-legacy-call boundaries, 256-recording limit, recency, case-insensitive selection, quality/seen-count ranking, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the HashDb/music fallback slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 12:23:47Z

- Current task: performance and efficiency improvements in progress; MediaCore music-variant sampling pass complete locally.
- Last activity:
  - Added one bounded recent-variant HashDb projection and replaced MediaCore's complete recording-ID read plus per-recording query loop.
  - At the default 100-item limit with one variant per recording, database reads fall from 101 to one (99.01% fewer), and application memory is result-bounded. Documented gotchas `0z691` (`520b57f5e`) and `0z692` (`010ab3816`).
- Validation:
  - Passed focused regressions (`2/2`), broader HashDb/MediaCore coverage (`323/323`), and full backend tests (`4888/4888`: `69` application, `4539` unit, `280` integration).
  - Exact one-query service boundary, zero legacy calls, recording recency, quality ordering, case-variant selection, result limit, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the MediaCore/HashDb recent-variant slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 12:11:57Z

- Current task: performance and efficiency improvements in progress; set-based warm-cache eviction pass complete locally.
- Last activity:
  - Replaced total-size/list/per-entry-delete orchestration with one SQLite windowed delete that selects the minimum cumulative oldest-unpinned prefix needed to meet the configured capacity.
  - Evicting 100 metadata entries falls from 102 commands/connections to one (99.02% fewer), without deleting cached files or changing network behavior.
- Validation:
  - Passed focused eviction coverage (`2/2`), broader HashDb/warm-cache coverage (`94/94`), and full backend tests (`4886/4886`: `69` application, `4537` unit, `280` integration).
  - Pinned-row retention, oldest-first selection, exact capacity, idempotence, zero legacy service calls, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the warm-cache eviction slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 12:04:16Z

- Current task: performance and efficiency improvements in progress; warm-cache popularity batching pass complete locally.
- Last activity:
  - Replaced the endpoint's per-hint worker channel with one bounded service call and added transactional 400-ID HashDb popularity upserts with grouped duplicate counts.
  - At the 100-hint endpoint maximum, database commands and transactions fall from 100 to one (99.0% fewer). Empty normalized batches retain their zero-database-work boundary. Documented gotcha `0z690` (`177404d1f`).
- Validation:
  - Passed focused warm-cache coverage (`7/7`), broader HashDb/warm-cache coverage (`92/92`), and full backend tests (`4884/4884`: `69` application, `4535` unit, `280` integration).
  - Exact 100-hint service boundaries, SQLite 401-distinct-ID batching, duplicate-hit persistence, empty-batch no-open behavior, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the warm-cache popularity batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 11:56:01Z

- Current task: performance and efficiency improvements in progress; accessible share-grant membership batching pass complete locally.
- Last activity:
  - Filtered the candidate grant read to active group grants plus the requested user's direct grants, deduplicated valid group IDs, and resolved membership through one set-based read in the existing context.
  - With 100 valid group grants, database reads fall from 101 to two (98.02% fewer) and contexts from 101 to one (99.01% fewer). Direct-only lookups remain one read; accessible-result semantics are unchanged.
- Validation:
  - Passed focused repository coverage (`1/1`), complete Sharing unit coverage (`79/79`), and full backend tests (`4881/4881`: `69` application, `4532` unit, `280` integration).
  - Exact SQLite command counts, duplicate-group handling, expiry and malformed-ID filtering, result semantics, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the accessible share-grant batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 11:45:30Z

- Current task: performance and efficiency improvements in progress; Virtual Soulfind page-scoped verified-copy hydration pass complete locally.
- Last activity:
  - Added a bounded newest-copy lookup backed by a local-file/time index and replaced global verified-copy snapshots with unresolved IDs from each 250-file upgrade/orphan page.
  - With 10,000 unresolved files and one million verified-copy rows, upgrade reads fall from 4,082 to 121 (97.04% fewer) and orphan reads from 4,042 to 81 (98.00% fewer); memory is page-bounded. Documented gotcha `0z689` (`258c6108b`, `a4f46eb08`).
- Validation:
  - Passed focused catalogue/reconciliation coverage (`44/44`), complete Virtual Soulfind v2 unit coverage (`205/205`), and full backend tests (`4880/4880`: `69` application, `4531` unit, `280` integration).
  - Exact 250-file call boundaries, SQLite 501-distinct-ID batching, newest-copy selection, zero global verified-copy reads, output ordering, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Virtual Soulfind verified-copy hydration slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 11:35:09Z

- Current task: performance and efficiency improvements in progress; Virtual Soulfind release-gap evidence batching pass complete locally.
- Last activity:
  - Added bounded artist, release-group, and release-track catalogue projections, then combined those with page-level copy-state hydration for each 250-release analysis page.
  - For 10,000 ten-track releases, complete-operation catalogue reads fall from 40,041 to 361 (99.10% fewer) with page-bounded memory and no remote work.
- Validation:
  - Passed focused catalogue/reconciliation coverage (`42/42`), complete Virtual Soulfind v2 unit coverage (`203/203`), and full backend tests (`4878/4878`: `69` application, `4529` unit, `280` integration).
  - Exact 250-release/2,500-track call boundaries, SQLite 501-distinct-ID batching, duplicate/missing key handling, release/track ordering, fallback behavior, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Virtual Soulfind release-analysis slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 11:25:25Z

- Current task: performance and efficiency improvements in progress; Virtual Soulfind upgrade track batching pass complete locally.
- Last activity:
  - Added bounded 500-ID catalogue track hydration and changed upgrade analysis to filter each 250-file page before one indexed metadata batch instead of reading tracks individually.
  - For 10,000 eligible files with no verified-copy rows, complete-operation catalogue reads fall from 10,042 to 82 (99.18% fewer) with page-bounded memory. Documented gotchas `0z687` (`5ecce3d1e`) and `0z688` (`7addea4a8`).
- Validation:
  - Passed focused catalogue/reconciliation coverage (`40/40`), complete Virtual Soulfind v2 unit coverage (`201/201`), and full backend tests (`4876/4876`: `69` application, `4527` unit, `280` integration).
  - Exact 250-file call boundaries, SQLite 501-distinct-ID batching, duplicate/missing ID handling, title fallback, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Virtual Soulfind upgrade-hydration slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 11:15:06Z

- Current task: performance and efficiency improvements in progress; Virtual Soulfind reconciliation copy-state batching pass complete locally.
- Last activity:
  - Added one combined, indexed catalogue projection for local-file and verified-copy presence in bounded 500-track batches, then used it across release missing-track checks, whole-catalogue missing-copy scans, and gap analysis.
  - A 1,000-track release falls from 2,001 track/copy reads to three (99.85% fewer); a full 250-track scan page falls from 252 catalogue reads to three (98.81% fewer). No remote traffic changed.
- Validation:
  - Passed focused catalogue/reconciliation coverage (`38/38`), complete Virtual Soulfind v2 unit coverage (`199/199`), and full backend tests (`4874/4874`: `69` application, `4525` unit, `280` integration).
  - Exact 1,000-track and 250-track call boundaries, SQLite 501-ID batching, inferred/verified state separation, partial-release counts, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Virtual Soulfind catalogue/reconciliation slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 11:04:00Z

- Current task: performance and efficiency improvements in progress; native shared-library directory indexing pass complete locally.
- Last activity:
  - Browser directory aggregation now indexes normalized paths, aggregate file counts, and immediate-child counts in one traversal instead of rescanning the complete share directory list twice for every returned entry.
  - In the 10,002-directory regression fixture producing 10,000 root entries, full-list traversals fall from 20,001 to one and directory visits from 200,050,002 to 10,002 (99.995% fewer). Duplicate records, normalized matching, and sort behavior remain unchanged.
- Validation:
  - Passed focused native-library coverage (`22/22`), broader native API coverage (`67/67`), and full backend tests (`4870/4870`: `69` application, `4521` unit, `280` integration).
  - Exact single-enumeration/visit-count coverage, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the native browser directory-indexing slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 10:53:47Z

- Current task: performance and efficiency improvements in progress; native shared-library hash batching pass complete locally.
- Last activity:
  - Search and browser pages now resolve their bounded file set once and batch exact FLAC-key evidence through HashDb before preserving the existing SHA fallback.
  - At the 100-item endpoint maximum, local HashDb reads fall from 100 to one (99.0% fewer). Documented gotchas `0z685` (`681eae85c`) and `0z686` (`b0f8217b4`).
- Validation:
  - Passed focused native-library/hash coverage (`23/23`), broader native API/HashDb coverage (`152/152`), and full backend tests (`4869/4869`: `69` application, `4520` unit, `280` integration).
  - Exact primary-key query-plan, search/browser call boundaries, SHA content-ID preservation, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the native shared-library batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 10:42:25Z

- Current task: performance and efficiency improvements in progress; audio analyzer-migration batching pass complete locally.
- Last activity:
  - Analyzer migration now keyset-pages 500 recording IDs, batch-loads variants, and applies transactional 100-row updates limited to recalculated analysis fields.
  - A 10,000-recording/three-stale-variant workload falls from 40,001 commands to 341 (99.1% fewer), and 30,000 write transactions to 20, with page-bounded memory. Documented gotcha `0z684` (`7886d94ca`).
- Validation:
  - Passed focused migration coverage (`2/2`), broader Audio/HashDb coverage (`148/148`), and full backend tests (`4866/4866`: `69` application, `4517` unit, `280` integration).
  - The 201-row normalization/duplicate/preserved-metadata fixture, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the audio analyzer-migration slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 10:30:40Z

- Current task: performance and efficiency improvements in progress; MusicBrainz Wishlist-promotion batching pass complete locally.
- Last activity:
  - Discography and Library Bloom promotion now collect unique missing seeds and use one bounded bulk persistence call. A 1,000-track discography promotion falls from 1,000 insert commands to 25 (97.5% fewer); the Bloom maximum of 250 falls from 250 to seven (97.2% fewer).
  - Bloom now reuses one Wishlist snapshot and indexes exact keys plus normalized search text. A 250-by-10,000 pass falls from up to 2.5 million prefix comparisons to 250 lookups. Extended gotcha `0z678` (`275539822`) and added `0z683` (`ca6a10641`).
- Validation:
  - Passed focused promotion coverage (`12/12`), complete MusicBrainz integration coverage (`65/65`), and full backend tests (`4864/4864`: `69` application, `4515` unit, `280` integration).
  - Large call-boundary/ID/cross-filter fixtures, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the MusicBrainz Wishlist-promotion slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 10:20:03Z

- Current task: performance and efficiency improvements in progress; Wishlist bulk-persistence pass complete locally.
- Last activity:
  - Added transactional 40-row Wishlist inserts and routed both CSV imports and Lidarr page synchronization through them. The 760-parameter boundary stays below SQLite's conservative 999-variable limit.
  - Default 100-item Lidarr sync falls from 100 insert commands/transactions to three commands and one transaction (97.0%/99.0% fewer). A 100-track CSV import falls from 100 insert commands to three. Documented gotcha `0z682` (`d51ac3a05`).
- Validation:
  - Passed focused persistence/Lidarr coverage (`10/10`), broader Wishlist/Lidarr coverage (`44/44`), and full backend tests (`4862/4862`: `69` application, `4513` unit, `280` integration).
  - Exact old/new SQLite command counts, complete field round-trip, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Wishlist/Lidarr batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 10:01:01Z

- Current task: performance and efficiency improvements in progress; canonical audio-stat batching pass complete locally.
- Last activity:
  - Candidate ranking now reuses loaded variants, reads all stored profile stats once, and batch-persists missing profiles. A 100-profile all-missing request falls from 301 SQLite commands to three (99.0% fewer).
  - Full recomputation now keyset-pages 500 recording IDs and batch-loads their variants before bounded 100-row writes. A 10,000-recording/three-profile workload falls from 70,001 commands to 341 (99.5% fewer) with page-bounded memory. Documented gotchas `0z680` (`171276e30`) and `0z681` (`2eda5d03a`).
- Validation:
  - Passed focused canonical-stat coverage (`7/7`), broader Audio/HashDb coverage (`145/145`), and full backend tests (`4858/4858`: `69` application, `4509` unit, `280` integration).
  - Exact canonical recording query-plan coverage, repository lint, and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the canonical audio-stat batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 09:45:59Z

- Current task: performance and efficiency improvements in progress; discography coverage batching pass complete locally.
- Last activity:
  - Batched cached album-target, release-track, and recording-hash reads while preserving graph ordering and sequential MusicBrainz cache-miss behavior.
  - A cached 100-release/1,000-track request falls from 1,200 SQLite commands to four (99.7% fewer) through three service read calls. Wishlist fallback now uses constant-time normalized search-text membership. Documented fixture gotcha `0z679` (`bed247404`).
- Validation:
  - Passed focused coverage (`5/5`), broader HashDb/MusicBrainz coverage (`144/144`), and full backend tests (`4854/4854`: `69` application, `4505` unit, `280` integration).
  - Repository lint and diff checks passed. Every substantive remediation check passed before the expected divergent-branch release-sync stop. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the discography coverage batching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 09:33:06Z

- Current task: performance and efficiency improvements in progress; job aggregate polling pass complete locally.
- Last activity:
  - Converted discography and label-crate child-status aggregation from four list passes to one and compare derived totals/status before writing the parent.
  - Unchanged polls fall from three DB operations to two (33.3% fewer) and avoid a write lock; a 10,000-release aggregate falls from 40,000 predicate visits to 10,000. Changed state still writes exactly once.
- Validation:
  - Passed focused aggregation coverage (`2/2`), complete Jobs coverage (`25/25`), and full backend tests (`4852/4852`: `69` application, `4503` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the job aggregate polling slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 09:25:29Z

- Current task: performance and efficiency improvements in progress; history-backfill page consolidation pass complete locally.
- Last activity:
  - Flattened one ordered retained-search page into one existing bounded HashDb ingestion transaction instead of one call/transaction per search.
  - With one FLAC response per search, the default 50-search page falls from 100 commands/50 transactions to two/one (98.0% fewer); the maximum 500-search page falls from 1,000 commands to six (99.4% fewer). Empty searches and the oldest progress timestamp remain intact. Extended gotcha `0z568` (`6380eebd5`).
- Validation:
  - Passed focused controller/backfill coverage (`12/12`) and full backend tests (`4850/4850`: `69` application, `4501` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the HashDb history-backfill controller slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 09:17:07Z

- Current task: performance and efficiency improvements in progress; Library Health scan-progress checkpointing pass complete locally.
- Last activity:
  - Retained per-file active in-memory progress but bounded durable status persistence to initial, unique 100-file checkpoints, and final/failure writes.
  - A 201-file scan falls from 203 status writes to four (98.0% fewer); 100,000 files fall from 100,002 to 1,002 (99.0% fewer). Atomic checkpoint ownership and monotonic visible progress remain correct under eight workers. Documented gotchas `0z677` (`bdf9f56ed`, `5c1d7dd00`) and `0z678` (`b7663cd73`).
- Validation:
  - Passed the exact 201-file regression (`1/1`), complete Library Health coverage (`30/30`), and full backend tests (`4849/4849`: `69` application, `4500` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Library Health progress-checkpoint slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 09:06:26Z

- Current task: performance and efficiency improvements in progress; release-job write batching pass complete locally.
- Last activity:
  - Normalized discography/label-crate composite job keys before transaction setup and replaced one release-status command per row with bounded 100-row SQLite upserts.
  - A 202-valid-row list falls from 202 commands to three (98.5% fewer) for either job type. Later duplicate statuses still win across batches; discography spaced keys are now reachable by normalized reads. Documented gotcha `0z676` (`504f7008c`).
- Validation:
  - Passed focused release-job coverage (`2/2`), the complete HashDb class suite (`80/80`), and full backend tests (`4848/4848`: `69` application, `4499` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the release-job persistence slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 08:58:04Z

- Current task: performance and efficiency improvements in progress; HashDb statistics aggregation pass complete locally.
- Last activity:
  - Collapsed total/capable peer, total/known inventory, and hash counts into one SQLite CTE snapshot with one scan per table.
  - Dashboard and mesh-hello statistics fall from five commands to one (80% fewer) and five scans to three (40% fewer). The filesystem database-size read remains unchanged.
- Validation:
  - Passed focused statistics coverage (`2/2`), the complete HashDb class suite (`79/79`), and full backend tests (`4847/4847`: `69` application, `4498` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the HashDb statistics slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 08:51:29Z

- Current task: performance and efficiency improvements in progress; album-target track-write batching pass complete locally.
- Last activity:
  - Replaced one SQLite insert per normalized album track with bounded 100-row commands inside the existing replacement transaction.
  - A typical 12-track album falls from 14 commands to three (78.6% fewer); a 202-row replacement falls from 204 commands to five (97.5% fewer). Zero-position fallback, later duplicate-position wins, normalization, and stale-row deletion remain intact.
- Validation:
  - Passed focused album persistence coverage (`2/2`), the complete HashDb class suite (`78/78`), and full backend tests (`4846/4846`: `69` application, `4497` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the album-target persistence slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 08:42:53Z

- Current task: performance and efficiency improvements in progress; HashDb mesh-merge batching pass complete locally.
- Last activity:
  - Replaced one exact/variant lookup per mesh hash and one store per new hash with bounded 500-key indexed reads and transactional 100-row inserts.
  - At the 1,000-entry sync cap, all-new merges fall from 2,000 local database commands to 12 (99.4% fewer); all-existing merges fall from 1,000 to two (99.8% fewer). Exact-key preference, variant aliases, local conflicts, duplicates, and local sequence IDs remain intact. Extended gotcha `0z675` (`88562aea3`).
- Validation:
  - Passed focused merge/index coverage (`4/4`), the complete HashDb class suite (`77/77`), and full backend tests (`4845/4845`: `69` application, `4496` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the HashDb mesh-merge slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 08:29:58Z

- Current task: performance and efficiency improvements in progress; atomic HashDb peer-state write pass complete locally.
- Last activity:
  - Replaced peer activity and capability read-before-write sequences with one normalized atomic upsert, and removed the redundant explicit lookup from peer interaction events.
  - Search/download events fall from three-to-five local database commands to one (66.7–80% fewer); touch and capability state preserve unrelated fields. Fixed spaced peer identifiers failing their final touch update. Documented gotcha `0z675` (`3ec78e1cf`).
- Validation:
  - Passed focused peer-state coverage (`2/2`), the complete HashDb class suite (`75/75`), and full backend tests (`4843/4843`: `69` application, `4494` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the HashDb peer-state slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 08:19:45Z

- Current task: performance and efficiency improvements in progress; passive FLAC ingestion batching pass complete locally.
- Last activity:
  - Streamed live and historical search FLACs through bounded 100-row SQLite upserts and inserted up to 500 distinct history peers per command in the same transaction.
  - At 100 live files, commands fall from 100 to one. At 100 historical one-file responses, inventory and peer commands fall from 200–400 to two (99.0–99.5% fewer), without changing Soulseek traffic or probing. Documented gotcha `0z674` (`47bfc7aea`).
- Validation:
  - Passed focused ingestion coverage (`2/2`), the complete HashDb class suite (`75/75`), and full backend tests (`4843/4843`: `69` application, `4494` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the passive FLAC ingestion slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 08:01:58Z

- Current task: performance and efficiency improvements in progress; Library Health remediation batching pass complete locally.
- Last activity:
  - Added schema-v23 remediation linkage indexing plus caller-ordered issue-ID and direct job/status reads. Persisting the remediation job ID fixes completion linkage beyond the generic 100-row default page.
  - Replaced per-issue transitions with bounded transactional updates. At 100 issues, creation falls from 102 database operations to two and completion from 101 to two (98.0% fewer). Documented gotchas `0z670` (`d6ac7dfca`), `0z671` (`10cc0d947`), `0z672` (`0198ed44a`, `c634efef7`), and `0z673` (`9208446cd`).
- Validation:
  - Passed focused remediation/index coverage (`4/4`), broader HashDb/Library Health coverage (`98/98`), and full backend tests (`4841/4841`: `69` application, `4492` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the schema-v23/Library Health remediation slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 07:47:33Z

- Current task: performance and efficiency improvements in progress; music metadata matching pass complete locally.
- Last activity:
  - Batched filtered album tracks and lightweight exact-match presence. Across 100 albums, exact tag resolution falls from up to 102 database reads to three (97.1% fewer).
  - Batched the full 256-recording variant fallback into one indexed read. A full miss falls from up to 358 reads to four (98.9% fewer). Documented gotcha `0z669` (`3f1fbbfb0`).
- Validation:
  - Passed focused exact/fallback coverage (`10/10`), complete HashDb/provider suites (`80/80`), and full backend tests (`4838/4838`: `69` application, `4489` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Run remediation and commit only the HashDb/music metadata-matching slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 07:38:13Z

- Current task: performance and efficiency improvements in progress; recent music enumeration pass complete locally.
- Last activity:
  - Added schema-v22 album-recency index and a database-limited recent-track projection, then batched advertisable presence. The default 50-item request falls from 52–101 queries to two (96.2–98.0% fewer), with at most 50 tracks hydrated.
  - Query-plan and provider regressions prove indexed traversal and zero legacy catalog/per-album/per-track calls. Documented gotcha `0z667` (`ac41ee106`) and fixture gotcha `0z668` (`f58451259`).
- Validation:
  - Passed focused recent-item/index coverage (`10/10`), complete HashDb/provider suites (`78/78`), and full backend tests (`4836/4836`: `69` application, `4487` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Run remediation and commit only the schema-v22/recent-items slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 07:27:33Z

- Current task: performance and efficiency improvements in progress; direct music recording lookup pass complete locally.
- Last activity:
  - Added schema-v21 case-insensitive album-track recording index and replaced album/per-release scanning with one direct query. A successful lookup across 100 albums falls from up to 103 database queries to two (98.1% fewer).
  - Deferred variant hydration until the indexed track lookup misses while preserving newest-release/earliest-track selection. Documented gotcha `0z666` (`170cd27a0`, `818d073aa`).
- Validation:
  - Passed focused lookup/index coverage (`9/9`), complete HashDb/provider suites (`75/75`), and full backend tests (`4833/4833`: `69` application, `4484` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Run remediation and commit only the HashDb migration/direct-lookup slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 07:15:50Z

- Current task: performance and efficiency improvements in progress; MusicBrainz album-completion pass complete locally.
- Last activity:
  - Batched release-track and full recording-hash evidence reads before assembling album completion. A 100-album, ten-track collection falls from 1,101 database queries to four (99.6% fewer) with the response contract and match order preserved.
  - Query-plan coverage proves the existing recording-ID index serves the batch without a temporary sort. Documented gotcha `0z664` (`86e24501e`) and caught lookup-shape gotcha `0z665` (`ce868ef92`).
- Validation:
  - Passed focused endpoint/query coverage (`3/3`), complete HashDb/controller suites (`72/72`), and full backend tests (`4831/4831`: `69` application, `4482` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Application, Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Run the remediation gate and commit only the HashDb/MusicBrainz completion slice.
  2. Continue the broader performance goal outside the dirty Application/Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 07:03:30Z

- Current task: performance and efficiency improvements in progress; Library Bloom album-analysis pass complete locally.
- Last activity:
  - Added bounded indexed multi-release track reads and grouped them for Library Bloom snapshot/comparison work. A 100-release operation falls from 101 database queries to two (98.0% fewer).
  - Replaced nested held-recording scans with a case-insensitive hash set. A 10,000-by-10,000 membership pass falls from up to 100 million comparisons to 10,000 expected O(1) lookups. Documented gotcha `0z663` (`1e4611295`).
- Validation:
  - Passed focused HashDb/Library Bloom coverage (`8/8`) and a fresh consolidated backend run (`4829/4829`: `69` application, `4480` unit, `280` integration). SQLite plan coverage proves indexed batch lookup without a temporary B-tree.
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. An earlier unit attempt exposed an unrelated `DownloadService.Dispose` timing race; all seven exact theory cases, the complete unit rerun, and the final consolidated run passed. Concurrent Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Run the final consolidated test/remediation gates and commit only the HashDb/Library Bloom slice.
  2. Continue the broader performance goal outside the dirty Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 06:48:55Z

- Current task: performance and efficiency improvements in progress; SignalBus receive-concurrency pass complete locally.
- Last activity:
  - Removed the global deduplication semaphore and use atomic concurrent-cache admission/maintenance. A 100,000-signal burst avoids 200,000 semaphore operations, while 128 simultaneous copies of one ID still deliver exactly once.
  - Preserved the removed wait's cancellation checkpoint so a pre-cancelled receive does not poison a later retry. Documented gotchas `0z661` (`1defd7ed1`) and `0z662` (`f0558e1a1`).
- Validation:
  - Passed focused SignalBus coverage (`15/15`) and complete smoke/unit/integration suites (`4826/4826`: `69` smoke, `4477` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. The initial integration attempt hit transient `/tmp` capacity; harness cleanup restored space and the full `280/280` rerun passed without manual deletion. Concurrent Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the SignalBus implementation and handoff slice.
  2. Continue the broader performance goal outside the dirty Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 06:32:01Z

- Current task: performance and efficiency improvements in progress; Source Discovery ingestion pass complete locally.
- Last activity:
  - Batched returned files into 100-row SQLite UPSERT commands within the existing transaction. At 100,000 files, command compilation/execution falls from 100,000 to 1,000 (99% fewer) with no change to Soulseek traffic or safety limits.
  - Moved optional hash verification outside the transaction rollback catch so a post-commit failure cannot attempt an invalid rollback. Documented gotchas `0z658` (`43406562e`), `0z659` (`d5a080944`), and `0z660` (`9165ba73f`).
- Validation:
  - Passed focused Source Discovery coverage (`2/2`) and complete smoke/unit/integration suites (`4823/4823`: `69` smoke, `4474` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Source Discovery implementation and handoff slice.
  2. Continue the broader performance goal outside the dirty Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 06:19:37Z

- Current task: performance and efficiency improvements in progress; Library Health release-analysis pass complete locally.
- Last activity:
  - Coalesced release completeness checks by release ID and directory after the parallel file pass, preventing every file in one album from repeating the same analysis and issue creation.
  - Batched indexed recording-ID presence checks in groups of at most 500. A ten-track release with ten files falls from 120 database reads to three (97.5% fewer). Documented gotcha `0z657` (`6aba3b354`, `b7dfc99dc`) and extended namespace-shadowing gotcha `0z646` (`329587f51`).
- Validation:
  - Passed focused Library Health/HashDb coverage (`7/7`) and complete smoke/unit/integration suites (`4822/4822`: `69` smoke, `4473` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Library Health/HashDb implementation and handoff slice.
  2. Continue the broader performance goal outside the dirty Mesh/Pod/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 06:02:15Z

- Current task: performance and efficiency improvements in progress; streaming file-retention pass complete locally.
- Last activity:
  - Replaced recursive filename materialization and triple resolve/age enumeration with one streaming destructive pass.
  - At 100,000 candidates, resolution and age checks fall from 300,000 to 100,000 and the full filename array is removed. Documented gotcha `0z655` (`3225e9209`) and caught static logger shadowing gotcha `0z656` (`fbe3037f7`).
- Validation:
  - Passed focused pruning coverage (`1/1`) and complete smoke/unit/integration suites (`4819/4819`: `69` smoke, `4470` unit, `280` integration).
  - Repository lint passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Mesh, Pod, and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the file-retention implementation and handoff slice.
  2. Continue the broader performance goal outside the dirty Mesh/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 05:50:50Z

- Current task: performance and efficiency improvements in progress; intent-batch queue-read pass complete locally.
- Last activity:
  - Reused batch-loaded pending intents behind atomic expected-status claims, removing the per-item same-record reload without permitting duplicate processing.
  - Gated expensive queue statistics behind debug-log enablement. A full ten-item cycle falls from 11 intent reads plus two whole-queue scans to one read and zero scans at normal info logging. Documented gotchas `0z653` (`e28ef2f58`) and `0z654` (`875e0dc4c`).
- Validation:
  - Passed focused intent tests (`18/18`) and complete smoke/unit/integration suites (`4818/4818`: `69` smoke, `4469` unit, `280` integration).
  - Repository lint and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`. Concurrent Mesh and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the intent processing implementation and handoff slice.
  2. Continue the broader performance goal outside the dirty Mesh/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 05:38:31Z

- Current task: performance and efficiency improvements in progress; Library Health scan-polling pass complete locally.
- Last activity:
  - Replaced async fixed-interval polling with completion-driven scheduling so a slow request cannot overlap another status check.
  - Hidden tabs pause status checks without resetting the absolute one-minute deadline; returning after expiry loads the dashboard directly. A fully hidden minute falls from up to 30 status calls to zero. Documented gotcha `0z652` in `68fbaad05`.
- Validation:
  - Passed focused Library Health tests (`9/9`), complete frontend tests (`878` passed, `4` skipped), and complete smoke/unit/integration suites (`4815/4815`: `69` smoke, `4466` unit, `280` integration).
  - Repository lint passed. The remediation baseline passed every substantive check before its expected release-sync stop because local `main` diverges from `origin/main`.
  - Concurrent Mesh and Shadow Index edits remain untouched.
- Next steps:
  1. Commit only the Library Health implementation and handoff slice.
  2. Continue the broader performance goal outside the dirty Mesh/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 05:26:31Z

- Current task: performance and efficiency improvements in progress; set-based Wishlist viewed-state pass complete locally.
- Last activity:
  - Replaced tracked bulk viewed-state updates with one predicate database command.
  - The 501-row regression proves one command, correct eligibility filtering, and preservation of already-viewed timestamps. Documented gotcha `0z651` in `d7e4f5827`.
- Validation:
  - Passed focused Wishlist tests (`17/17`), smoke (`69/69`), integration (`280/280`), and the complete unit suite excluding the unrelated in-progress Shadow Index fixture (`4464/4464`).
  - Repository lint, identity, and diff checks passed. The remediation baseline reached only its expected release-sync stop because local `main` diverges from `origin/main`.
  - Full `dotnet test` has one unrelated failure: untracked `ShardFormatTests.Serialize_HasFrozenCrossRuntimeMessagePackShape` still contains an empty expected-payload placeholder. Concurrent Mesh/Shadow Index changes remain untouched.
- Next steps:
  1. Commit only the Wishlist slice.
  2. Continue the broader performance goal outside the dirty Mesh/Shadow Index scope.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 05:17:30Z

- Current task: performance and efficiency improvements in progress; set-based Pod deletion pass complete locally.
- Last activity:
  - Replaced tracked child-history deletion with one existence query and four set-based deletes inside the existing transaction.
  - The 1,012-child regression proves bounded command count, unrelated-Pod retention, and no mutation for missing parents.
  - Extended gotcha `0z641` in `b66dd2cdd` and documented caught implementation issues as `0z649` (`a5c0a57b2`) and `0z650` (`5f1815f79`).
- Validation:
  - Passed focused Pod tests (`11/11`), complete smoke/unit/integration suites (`4811/4811`: `69` smoke, `4462` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this Pod deletion slice.
  2. Continue the broader performance goal from the next measured hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 05:07:02Z

- Current task: performance and efficiency improvements in progress; share content-hint producer N+1 pass complete locally.
- Last activity:
  - Replaced all-file hydration plus one content lookup per file with one indexed joined projection per share repository.
  - Preserved blocked/quarantined/non-advertisable filtering and cross-repository deduplication.
  - A 100,000-file repository falls from 100,001 queries to one (99.999% fewer). Added SQL boundary and producer call-count regressions; documented gotcha `0z648` in `33be38516`.
- Validation:
  - Passed focused share tests (`12/12`), complete smoke/unit/integration suites (`4807/4807`: `69` smoke, `4458` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this share producer slice.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 04:56:29Z

- Current task: performance and efficiency improvements in progress; search completion-polling payload pass complete locally.
- Last activity:
  - Changed Wishlist and Auto-Replace readiness checks to response-free search projections followed by one final response hydration.
  - Wishlist's bound falls from 40 full reads to 21 total reads; Auto-Replace's falls from 45 full reads to at most 24. Full payload hydration falls to one in both workflows.
  - Added exact light/final read boundary regressions and documented gotcha `0z647` in `e37d5a155`.
- Validation:
  - Passed focused Auto-Replace/Wishlist tests (`21/21`), complete smoke/unit/integration suites (`4805/4805`: `69` smoke, `4456` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this polling slice.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 04:47:48Z

- Current task: performance and efficiency improvements in progress; content-peer hint batching pass complete locally.
- Last activity:
  - Preserved conservative one-second content-hint pacing while batching the shared peer-content index read/write across at most 32 queued IDs.
  - Deduplicated pending IDs without suppressing later scan-driven TTL refreshes.
  - A 1,000-ID publication falls from 3,000 DHT operations to 1,064 (64.5% fewer). Added operation-count, queue-deduplication, and concurrent merge regressions; documented gotchas `0z645` (`cab572b52`) and `0z646` (`75f25c5d5`).
- Validation:
  - Passed focused publisher/service tests (`4/4`), complete smoke/unit/integration suites (`4805/4805`: `69` smoke, `4456` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this content-hint slice.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 04:38:31Z

- Current task: performance and efficiency improvements in progress; recurring backfill count-query pass complete locally.
- Last activity:
  - Replaced ten per-candidate daily-count queries and ten duplicate execution-time reads with one case-insensitive batch query plus a mutable cycle snapshot.
  - A ten-candidate cycle falls from 21 database queries to two (90.5% fewer) while keeping the same peer status calls and conservative per-peer limits.
  - Added batch normalization/collation, snapshot reuse, rate-limit, and cancellation regressions. Documented gotchas `0z643` (`6cb84fb88`) and `0z644` (`4c4779d97`).
- Validation:
  - Passed focused Backfill/HashDb tests (`65/65`), cycle-focused tests (`7/7`), complete smoke/unit/integration suites (`4803/4803`: `69` smoke, `4454` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this backfill slice.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 04:28:58Z

- Current task: performance and efficiency improvements in progress; periodic Pod discovery publication pass complete locally.
- Last activity:
  - Replaced one all-Pod query and one scoped reload per listed Pod with a single SQL-filtered, no-tracking listed snapshot every 30 minutes.
  - Added one batch publisher operation that retains per-Pod metadata writes but performs the shared listed-index read/write once per cycle and renews its TTL even when membership is unchanged.
  - A 100-Pod cycle falls from 101 database queries and 200 DHT operations to one query and 102 operations (99% and 49% fewer). Host cancellation now propagates through publisher/index paths.
  - Added SQL-boundary, exact DHT-operation, unchanged-index TTL, cancellation, and background-delegation regressions. Documented gotcha `0z642` in standalone commit `d832306b2` and extended shutdown gotcha `0z429` in `33ddd257f`.
- Validation:
  - Passed focused publisher/SQLite tests (`5/5`), broader PodCore tests (`381/381`), complete smoke/unit/integration suites (`4801/4801`: `69` smoke, `4452` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this Pod publication pass.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 03:53:51Z

- Current task: performance and efficiency improvements in progress; duplicate periodic mesh descriptor publication pass complete locally.
- Last activity:
  - Confirmed that Mesh bootstrap and refresh hosted services both published the same self descriptor every 30 minutes after an earlier fix removed only the startup duplicate.
  - Changed bootstrap to publish exactly once and complete, leaving configured periodic and IP-change refresh ownership to `PeerDescriptorRefreshService`.
  - Defaults avoid 48 duplicate DHT writes and up to 144 active STUN probes per day (50% of periodic self-publication work), and longer configured refresh intervals are no longer defeated by the fixed second loop.
  - Added bootstrap completion/single-publish coverage alongside the existing no-immediate-refresh regression. Extended gotcha `0z119` in standalone commit `4181cd45c`.
- Validation:
  - Passed focused Mesh scheduling tests (`2/2`), complete smoke/unit/integration suites (`4797/4797`: `69` smoke, `4448` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this duplicate-publication pass.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 03:42:46Z

- Current task: performance and efficiency improvements in progress; bounded search-history maintenance pass complete locally.
- Last activity:
  - Replaced full expired/excess/completed search materialization and one database context/transaction per row with stable 250-row `(StartedAt, Id)` pages and one set-based delete per page.
  - Applied the path consistently to automatic retention, legacy age pruning, and manual completed-history clearing while preserving every existing response-free per-search SignalR deletion payload.
  - The 501-row regression requires three delete commands instead of 501 (99.4% fewer); a 10,000-row cleanup uses 40 delete transactions with at most 250 summaries resident.
  - Added large expired-set command-boundary/notification/retention coverage and exact oldest-excess count coverage. Documented gotcha `0z641` in standalone commit `f1e916d6b`.
- Validation:
  - Passed focused Search lifecycle tests (`21/21`), complete smoke/unit/integration suites (`4796/4796`: `69` smoke, `4447` unit, `280` integration), repository lint, identity, and diff checks.
  - One unrelated Mesh pipe-read test hit its five-second timeout during the first combined run, then passed in isolation in 59 ms and the complete unit suite passed on rerun.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this bounded search-history maintenance pass.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 03:32:33Z

- Current task: performance and efficiency improvements in progress; automatic search-retention cadence pass complete locally.
- Last activity:
  - Confirmed that `Filters.SearchRetention.CleanupIntervalSeconds` was a dead option: its one-day default was ignored while the policy ran every five minutes.
  - Added a concurrency-safe due-time gate that consumes the current option value on every evaluation, runs immediately at startup, suppresses overlap, records only successful starts, and retries failures on the next five-minute tick.
  - Default database policy evaluations fall from 288 per day to one after startup (99.65% fewer), while the older optional search-age prune keeps its established cadence.
  - Added exact interval, failure-retry, and overlap regressions. Documented gotcha `0z640` in standalone commit `3990938a9`.
- Validation:
  - Passed focused application lifecycle tests (`10/10`), complete smoke/unit/integration suites (`4794/4794`: `69` smoke, `4445` unit, `280` integration), repository lint, identity, and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this search-retention scheduling pass.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 03:12:37Z

- Current task: performance and efficiency improvements in progress; Shadow Index publication/query pass complete locally.
- Last activity:
  - Replaced full-library distinct recording-ID sorting and fixed newest-batch selection with normalized case-insensitive keyset pages backed by HashDb migration 20.
  - Added cursor wrap so publication advances through the library while retaining the established interval and DHT operation ceiling.
  - Clamped candidates to the immediate DHT minute budget, avoiding 40 guaranteed excess builds under the default 100-shard/60-operation settings.
  - Removed the duplicate `IDhtRateLimiter` registration that caused custom `MaxDhtOperationsPerMinute` settings to be ignored.
  - A 100,000-ID proxy improved from 2.81 seconds for 100 old-shape queries to 0.01 seconds for 500 bounded pages; query-plan coverage requires an index range search without a temporary sort.
  - Added keyset normalization, cursor/wrap, bounded publishing, DHT-budget, DI composition, cancellation, and query-plan regressions. Documented gotchas `0z637` through `0z639` in standalone commits `d1139b06b`, `9c60c280b`, and `7527bfe9d`, and extended shutdown-cancellation gotcha `0z429` in `52975512d`.
- Validation:
  - Passed focused backend tests (`7/7`), complete smoke/unit/integration suites (`4791/4791`: `69` smoke, `4442` unit, `280` integration), repository lint, identity and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Run complete validation and commit this Shadow Index pass.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 02:46:03Z

- Current task: performance and efficiency improvements in progress; recurring download auto-retry candidate-materialization pass complete locally.
- Last activity:
  - Replaced full materialization of every retained eligible failed download on the default 60-second cycle with a minimal asynchronous SQLite stream ordered by oldest completion and stable ID.
  - Preserved peer-oldest priority, per-peer/global limits, cooldowns, attempt budgets, already-retried suppression, and audio filtering with a bounded accumulator that stops only once later candidates cannot alter the plan.
  - Added the idempotent partial `IDX_Transfers_AutoRetry_EndedAt` migration and model index; query-plan tests require the index and reject temporary sorting.
  - A 25,000-failure proxy improved from 327.5 ms and 30.53 MiB allocated to 76.9 ms and 0.31 MiB for the default ten-file plan (about 4.3x faster and 99% less allocation).
  - Added streaming-stop, underfilled-peer fairness, database filter/order/projection, migration, and query-plan regressions. Documented gotchas `0z634` through `0z636` in standalone commits `7a82a2c04`, `edefe5077`, and `c26337c7c`.
- Validation:
  - Passed focused backend tests (`9/9`), complete smoke/unit/integration suites (`4784/4784`: `69` smoke, `4435` unit, `280` integration), repository lint, identity and diff checks.
  - The remediation baseline passed every check before its expected release-sync stop because local `main` intentionally diverges from `origin/main`.
- Next steps:
  1. Commit this auto-retry pass.
  2. Continue the broader performance goal from the next measured recurring hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 02:21:44Z

- Current task: performance and efficiency improvements in progress; avoidable Bridge and Lyrics periodic-work pass complete locally.
- Last activity:
  - Identified System Bridge as the only remaining recurring System dashboard poll without visibility/overlap control and LyricsPane as a fixed 500-millisecond timer duplicating native media events.
  - Kept Bridge's ten-second visible cadence while adding initial-hidden and visibility lifecycle ownership, request coalescing, last-success retention, rendered-field signatures, Strict Mode lifecycle generations, and retryable shared config hydration.
  - Hidden Bridge traffic falls from six requests per minute to zero; uptime-only responses no longer cause dashboard state writes.
  - Replaced LyricsPane's fixed timer with `timeupdate`, `seeked`, and `loadedmetadata` events plus visibility catch-up. An open pane eliminates 120 fixed callbacks/state attempts per minute, including the prior paused-playback work.
  - Added Popup explanations to all touched Bridge buttons, documented gotchas `0z632` and `0z633` in standalone commits `a1c87b594` and `616ed4243`, and extended render-signature gotcha `0z350` in `42c0c69fc`.
- Validation:
  - Passed focused Bridge/Lyrics Web tests (`9/9`), frontend lint with zero errors, the polling lifecycle gate, and diff checks.
  - Passed complete backend suites (`4780/4780`: `69` smoke, `4431` unit, `280` integration), complete Web tests (`876` passed, `4` skipped), production Web build, repository lint, and diff checks.
  - The consolidated remediation baseline passed every check before stopping at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Commit this scheduling pass.
  2. Continue the broader performance goal from the next measured hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 02:04:20Z

- Current task: performance and efficiency improvements in progress; Security dashboard aggregation and polling pass complete locally.
- Last activity:
  - Replaced repeated materialization and retained-set rescans with single-pass exact aggregation across eleven Security dashboard collectors.
  - A representative 25-read proxy over 10,000 events, 50,000 peers, and 10,000 canaries improved from 369 ms to 178 ms (51.76%) after optimizing the three dominant collectors; the other eight collector changes remove further repeated passes.
  - Made the 30-second System Security refresh visible-only, non-overlapping, Strict Mode-safe, unchanged-result preserving, and last-success retaining. Hidden traffic falls from two dashboard requests per minute to zero.
  - Fixed the existing dynamic Tab contracts that rendered a menu without the Status body and threw when selecting Adversarial. Only the selected diagnostic pane now mounts.
  - Added exact backend aggregate tests and five Web lifecycle/render regressions, including hidden manual-refresh ownership, and added Popup tooltips to every touched Security button.
  - Documented gotchas `0z628` through `0z631` and extended dynamic-tab gotcha `0z351` in standalone commits.
- Validation:
  - Passed focused Security backend tests (`60/60`) and focused Web tests (`5/5`).
  - Passed complete backend suites (`4780/4780`: `69` smoke, `4431` unit, `280` integration), complete Web tests (`870` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, and diff checks.
  - The consolidated remediation baseline passed every check before stopping at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Commit this Security pass.
  2. Continue the broader performance goal from the next measured hot path.
  3. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 01:44:49Z

- Current task: performance and efficiency improvements in progress; passive Mesh diagnostics and polling lifecycle pass complete locally.
- Last activity:
  - Confirmed that every mesh transport-stat read could initiate STUN NAT detection while the cached result remained unknown, affecting System Mesh polling, combined Network snapshots, and health checks.
  - Added cached NAT state to `INatDetector` and made `MeshStatsCollector` report it without calling `DetectAsync`; startup/descriptor publishing and the explicit NAT action remain the active detection owners.
  - Made System Mesh's 30-second stats cadence visible-only, non-overlapping, Strict Mode-safe, unchanged-result preserving, and last-success retaining.
  - Repaired the polling lifecycle gate to enforce false initialization plus true setup and false cleanup, then aligned all eight covered System pollers with that Strict Mode-safe contract.
  - Each diagnostic read falls from as many as three external STUN probes and a three-second timeout window to zero probes. A hidden Mesh tab falls from two transport HTTP requests per minute to zero.
  - Added backend/Web regressions, updated network/System/release documentation, documented gotcha `0z627` in standalone commit `8d18a1590`, and extended lifecycle gotchas `0z606`/`0z618` in standalone commit `a34349d48`.
- Validation:
  - Passed focused collector tests (`2/2`), focused Mesh tests (`6/6`), and the broader affected System test slice (`78/78`).
  - Passed complete backend suites (`4777/4777`: `69` smoke, `4428` unit, `280` integration), complete Web tests (`865` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, and the strengthened polling lifecycle gate.
  - The consolidated remediation baseline passed every check before stopping at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Continue the broader performance goal from measured hot paths, with Security dashboard retained-set aggregation still under review.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 01:31:53Z

- Current task: performance and efficiency improvements in progress; listen-along directory polling/DHT hydration pass complete locally.
- Last activity:
  - Compared Bridge, Security, Mesh, and listen-along dashboard costs; selected listen-along because every 30-second browser poll synchronously fetched one DHT index plus every party announcement.
  - Removed all directory hydration from the only production call site's compact mode because that render path never displays the directory.
  - Changed full-panel polling to a visible-only, non-overlapping one-minute cadence with immediate visibility catch-up, unchanged-result suppression, and last-success retention.
  - Coalesced concurrent HTTP callers onto one process-wide, cancellation-safe DHT refresh and reused the result for one minute; failed refreshes remain immediately retryable.
  - Current compact traffic falls from two HTTP requests and `2 x (N + 1)` DHT reads per minute to zero. A full panel halves HTTP frequency, while backend DHT hydration is bounded at `N + 1` reads per minute for the process regardless of client count.
  - Added backend/Web regressions, updated listening-party and release documentation, and documented gotcha `0z626` in standalone commits `59004f367` and `7c0df4f73`.
- Validation:
  - Passed focused Listening Party backend tests (`4/4`) and focused Web tests (`6/6`).
  - Passed complete backend suites (`4776/4776`: `69` smoke, `4427` unit, `280` integration), complete Web tests (`862` passed, `4` skipped), production Web build, frontend lint with zero errors, and repository lint.
  - The consolidated remediation baseline passed every check before stopping at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 01:17:27Z

- Current task: performance and efficiency improvements in progress; Library Health aggregate/dashboard pass complete locally.
- Last activity:
  - Replaced the System Library Health page's four-request load with one transactional SQLite dashboard snapshot and bounded 100-issue detail hydration.
  - Moved summary, type, artist, release, and codec groupings off the default 100-row entity page, fixed persisted JSON codec handling, and returned authoritative filtered issue totals.
  - Added 1-250 page bounds plus HashDb migration 19's recent-issue index; 25 loads over a 100,000-row proxy improved from 9.653 seconds to 1.958 seconds (79.72%), while HTTP requests and rich entity materialization fall 75%.
  - Added backend/Web regressions, regenerated the route inventory, documented gotchas `0z621`-`0z625`, and extended `0z542` in standalone commits.
- Validation:
  - Passed focused backend tests (`79/79`), focused Web tests (`12/12`), complete Web tests (`858` passed, `4` skipped), production Web build, complete backend suites (`4773/4773`: `69` smoke, `4424` unit, `280` integration), frontend and repository lint, route/security/polling/identity checks, and every post-sync remediation check.
  - The consolidated release baseline stopped only at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 00:50:17Z

- Current task: performance and efficiency improvements in progress; Lidarr external-status polling pass complete locally.
- Last activity:
  - Made the paired 30-second external Lidarr system/local sync status cycle visible-only, non-overlapping, Strict Mode-safe, and immediate on visibility restoration.
  - Added 15-second external status reuse with concurrent-request coalescing and suppressed hidden, unmounted, or unchanged React state writes.
  - Reduced hidden dashboard status traffic from two external plus two local requests per steady-state minute to zero; ten hidden minutes avoid 20 requests to each endpoint without changing the visible cadence.
  - Added cache/expiry, equality, Strict Mode, overlap, and visibility regressions; documented gotcha `0z590` in standalone commit `e7ba4233d`.
- Validation:
  - Passed focused Web tests (`5/5`), complete Web tests (`857` passed, `4` skipped), production Web build, complete backend suites (`4763/4763`: `69` smoke, `4414` unit, `280` integration), frontend and repository lint, route/security/polling/identity checks, and every post-sync remediation check.
  - The consolidated release baseline stopped only at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 00:34:00Z

- Current task: performance and efficiency improvements in progress; Search user-history aggregation pass complete locally.
- Last activity:
  - Moved retained download-history grouping from full managed entity materialization into one asynchronous SQLite aggregate without changing the API contract.
  - Added 30-second client reuse and concurrent-request deduplication for Search Detail history ranking.
  - Reduced a 134,777-row/1,000-user proxy from 40,066,203 to 35,112 intermediate bytes (99.912%) and from 2,972 ms to 867 ms across 25 runs (70.83%).
  - Rejected a new partial index after measurement showed only another 2.8% improvement.
  - Added backend SQL-shape/semantics and Web cache/expiry regressions; repaired a wall-clock-sensitive existing speed test and documented gotcha `0z620` in standalone commit `c0cbec90a`.
- Validation:
  - Passed focused transfer tests (`254/254`) and focused Search Web tests (`72/72`).
  - Passed complete Web tests (`852` passed, `4` skipped), production Web build, complete backend suites (`4763/4763`: `69` smoke, `4414` unit, `280` integration), frontend and repository lint, route/security/polling/identity checks, and every post-sync remediation check.
  - The consolidated release baseline stopped only at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 00:18:24Z

- Current task: performance and efficiency improvements in progress; Search result metadata fan-out pass complete locally.
- Last activity:
  - Added a cached, authenticated group batch endpoint with a 100-username request bound and changed Search Detail to batch only visible result users.
  - Reused search-response upload speed, queue length, and free-slot metadata in each user card, eliminating initial remote user-info contacts.
  - Deferred reputation and opinion lookups until hover or keyboard focus while preserving supplied primary metadata during supplemental loading.
  - Reduced a default 25-result page from 100 automatic per-user metadata requests to one (99% fewer) and from 25 remote Soulseek peer contacts to zero.
  - Added backend and Web regressions, regenerated the route inventory, and documented gotcha `0z619` in standalone commit `21ac9339b`.
- Validation:
  - Passed focused controller tests (`17/17`) and focused Web tests (`13/13`).
  - Passed complete backend suites (`4761/4761`: `69` smoke, `4412` unit, `280` integration), complete Web tests (`851` passed, `4` skipped), production Web build, frontend and repository lint, route/security/polling/identity checks, and all post-sync remediation checks.
  - The consolidated release baseline stopped only at its expected branch-sync gate because this local work is intentionally unpushed.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-16 00:02:29Z

- Current task: performance and efficiency improvements in progress; MediaCore ContentID registry-read and stats-polling pass complete locally.
- Last activity:
  - Replaced full-registry stats parsing and domain/type scans with mutation-maintained mapping counts and exact-key secondary indexes while preserving normalized and case-insensitive query behavior.
  - Improved 25 reads over 100,000 mappings from 1,196 ms to 2 ms for stats (99.83%) and from 1,307 ms to 93 ms for a domain query (92.88%); the two-domain dashboard path reduces parsing passes by 75%.
  - Removed empty reverse buckets after last-member remaps, preventing retained dictionaries and false registered-state results while retaining shared ContentID mappings.
  - Made System MediaCore's one-minute stats refresh visible-only and non-overlapping with immediate catch-up, initial-only loading transitions, unchanged-field render suppression, and Strict Mode-safe mount-guard setup. Hidden requests fall from one per minute to zero and unchanged refresh rerenders from two to zero.
  - Added backend correctness/performance and Web lifecycle regressions; documented gotchas `0z616` through `0z618` in standalone commits `3cab6b0f2`, `8d857f4fe`, and `baa7c4966`.
- Validation:
  - Passed focused registry tests (`24/24`), broader MediaCore unit tests (`218/218`), MediaCore integration tests (`26/26`), and focused Web tests (`9/9`).
  - Passed complete backend suites (`4759/4759`: `69` smoke, `4410` unit, `280` integration), complete Web tests (`847` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, polling-lifecycle, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 23:39:36Z

- Current task: performance and efficiency improvements in progress; search progress-persistence and response-hydration pass complete locally.
- Last activity:
  - Repaired background ownership of live search progress, bounded the repaired cadence at one second, and eliminated response-driven partial database writes while preserving durable state/final saves.
  - Reduced the unbroken 15-second progress model from 61 to 16 summaries (73.77% fewer), or 8,662 to 2,272 across 142 searches; actual typically observed response-driven partial writes fall from one per search to zero.
  - Deferred ordinary Soulseek response hydration until completion, reducing detail response requests from two to one (50%), while preserving durable early mesh hydration through an explicit availability flag derived during REST projections.
  - Preserved source/wishlist provenance through search copies and projections, added immediate launch-failure cleanup, and cleared stale results when the reused detail component changes search IDs.
  - Added lifecycle, persistence, projection, provenance, hydration, and reused-route regressions; documented gotchas `0z609` through `0z615` in standalone commits `becb0f6f4`, `439432f0a`, `d91a71754`, `494cf01b4`, `445f6c727`, `2765881dd`, `9ac280d45`, `2824a7678`, and `479a0e9cd`.
- Validation:
  - Passed focused search lifecycle tests (`19/19`), focused Search Detail/Searches tests (`15/15`), and broader search Web tests (`72/72`).
  - Passed complete backend suites (`4755/4755`: `69` smoke, `4407` unit, `279` integration), complete Web tests (`842` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, polling-lifecycle, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 23:06:25Z

- Current task: performance and efficiency improvements in progress; Browse Session progress-polling pass complete locally.
- Last activity:
  - Changed pending-browse status polling from 500 milliseconds to one second while retaining independent browse completion.
  - Added overlap rejection, mounted and stale-generation guards, initial-hidden suppression, immediate visibility catch-up, and unchanged-progress state suppression.
  - Reduced maximum visible status traffic from 120 to 60 requests per pending minute (50%); a browse opened while hidden now issues zero status requests instead of 120 per minute.
  - Added cadence, overlap, initial-hidden, visibility, unchanged-state, and stale-generation regressions; documented gotcha `0z608` in standalone commit `3552e53b7`.
- Validation:
  - Passed focused Browse tests (`8/8`).
  - Passed complete backend suites (`4751/4751`: `69` smoke, `4403` unit, `279` integration), complete Web tests (`840` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, polling-lifecycle, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 22:49:43Z

- Current task: performance and efficiency improvements in progress; Swarm Analytics snapshot and polling pass complete locally.
- Last activity:
  - Added one bounded dashboard endpoint that builds all rendered analytics sections from a single top-100 peer-ranking snapshot while preserving the legacy leaf APIs and omitting unused trend data.
  - Changed the page from five requests and four complete peer-table reads/ranks per refresh to one request and one read/rank; recommendations now reuse one ranking snapshot and efficiency propagates request cancellation.
  - Added overlap rejection, visible-only polling, immediate visibility catch-up, stale-filter rejection, failure-cache retention, response normalization, and unchanged-dashboard suppression.
  - Reduced a 100,000-row SQLite proxy from 7.997 seconds for four snapshots to 1.964 seconds for one across 20 refreshes (75.44%). First-minute visible requests fall from 10 to 2 and hidden requests fall from 10 per minute to zero.
  - Added service/controller/integration, API-client, rendering, cache, stale-filter, cadence, overlap, and visibility regressions; regenerated the route inventory; documented gotchas `0z602` through `0z607` in standalone commits `2220f3ac7`, `bc5cfba43`, `9baa35325`, `6f4032ee0`, `54807c7c5`, and `e30160a04`.
- Validation:
  - Passed focused backend/API tests (`48/48`: `39` unit, `9` integration) and focused Web tests (`31/31`).
  - Passed complete backend suites (`4751/4751`: `69` smoke, `4403` unit, `279` integration), complete Web tests (`837` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, route-inventory, polling-lifecycle, CSRF, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 22:32:13Z

- Current task: performance and efficiency improvements in progress; Port Forwarding preview, polling, and statistics pass complete locally.
- Last activity:
  - Changed available-port hydration from an eager 64,512-port response to a lazy 100-port preview with authoritative counts, preserving the endpoint's default response compatibility.
  - Replaced overlapping five-second status and fabricated ten-second statistics timers with one ten-second visible-only, non-overlapping cadence with immediate catch-up, unchanged-state suppression, and failure-cache retention; VPN member counts now load only on their tab.
  - Materialized one forwarding snapshot per stream-statistics request, exposed real stream/performance fields, and removed the unused duplicate Port Forwarding component.
  - Reduced the modeled availability response from 378,116 bytes to 565 bytes (99.851%) and the default first visible minute from 15 requests to 8 (46.67%); hidden status requests and fabricated updates fall to zero.
  - Added backend/API-client and component regressions; fixed an existing Integrations test's async-readiness race; documented gotchas `0z598` through `0z601` in standalone commits `2a3a5c3e6`, `cb10bb0aa`, `362478e1d`, `0ad2585d1`, and `6431bf466`.
- Validation:
  - Passed focused Port Forwarding tests (`18/18`: `12` backend, `6` Web).
  - Passed complete backend suites (`4742/4742`: `69` smoke, `4395` unit, `278` integration), complete Web tests (`832` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, route-inventory, polling-lifecycle, CSRF, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 22:12:24Z

- Current task: performance and efficiency improvements in progress; Messaging V2 member-rail and Pod membership-history pass complete locally.
- Last activity:
  - Changed active room/Pod member hydration from an overlapping five-second timer to a non-overlapping ten-second visible-only cadence with immediate visibility catch-up, stable-state suppression, failure-cache retention, and authoritative successful-empty handling.
  - Reused the Pod membership snapshot already loaded for non-administrator authorization, projected only current member fields, and moved earliest-join/latest-seen calculation into a grouped SQLite query limited to current peers.
  - Reduced a representative authenticated non-administrator Pod with 250 current members and 250,000 retained events from 6,000,000 managed history rows to 1,500 summaries in the first visible minute (99.975% fewer), while requests fall from 12 to 6; hidden requests fall to zero.
  - Added SQL-shape/timestamp, authorization-reuse, cadence, overlap, visibility, failure-cache, successful-empty, and adapter-contract regressions; documented gotcha `0z597` in standalone commit `0b0091e20`.
- Validation:
  - Passed focused backend tests (`2/2`) and focused messaging Web tests (`13/13`).
  - Passed complete backend suites (`4738/4738`: `69` smoke, `4391` unit, `278` integration), complete Web tests (`826` passed, `4` skipped), production Web build, frontend lint with zero errors, repository lint, route-inventory, polling-lifecycle, CSRF, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 21:50:17Z

- Current task: performance and efficiency improvements in progress; global transfer-speed polling pass complete locally.
- Last activity:
  - Replaced four full-entity transfer lists per two-second speed-cache refresh with one four-column active projection and one grouped SQL byte-total query while preserving the existing response contract and fallback speed calculation.
  - Reduced a 200,000-row SQLite proxy from 2.947 seconds to 0.651 seconds across 20 runs (77.91% lower before EF materialization overhead); against the representative 134,777-row history, 4,043,310 retained entities per visible minute are no longer materialized.
  - Stopped both app-wide footer timers while hidden and added immediate visibility catch-up, dropping hidden footer polling from 36 requests per minute to zero.
  - Added concrete SQL-shape, aggregate correctness, API response, overlap, hidden, and visibility-catch-up regressions; documented gotcha `0z596` in standalone commit `03fd92d37`.
- Validation:
  - Passed focused backend tests (`2/2`) and focused Footer tests (`6/6`).
  - Passed complete Web tests (`822` passed, `4` skipped), production Web build, and frontend lint with zero new errors.
  - Passed complete backend suites (`4736/4736`: `69` smoke, `4389` unit, `278` integration), repository lint, route-inventory, polling-lifecycle, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 21:28:27Z

- Current task: performance and efficiency improvements in progress; Jobs and swarm visualization polling pass complete locally.
- Last activity:
  - Split two-second swarm status from ten-second trace-summary hydration so live progress remains responsive without rereading up to 50,000 trace events on every status tick.
  - Added per-domain in-flight and stale-resource rejection, concrete-contract field comparisons, cached-state retention on transient failures, and hidden-document stop/restart behavior to the Jobs page and visualization modal.
  - Reduced a representative visible first minute with the modal open from 72 requests to 48 (33.33%); trace scans and their worst-case five-MiB/50,000-event work fall 80%. Hidden polling falls from 72 requests per minute to zero.
  - Added cadence, overlap, API-progress, resource-switch, hidden, visibility-catch-up, optional-failure, and failure-cache regressions; documented gotchas `0z593` through `0z595` in standalone commits `9e94a09e4`, `d80dde3c9`, and `bd872ad71`, and extended `0z350` in `189714e0b`.
- Validation:
  - Passed focused Jobs/Swarm Visualization and API-boundary tests (`54/54`).
  - Passed complete Web tests (`820` passed, `4` skipped), production Web build, frontend lint with zero new errors, polling-lifecycle guard, and whitespace checks.
  - Passed complete backend suites (`4735/4735`: `69` smoke, `4388` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 21:14:43Z

- Current task: performance and efficiency improvements in progress; incremental room-message hydration pass complete locally.
- Last activity:
  - Added stable in-memory room-message IDs and an optional Unix-millisecond `since` cursor to the joined-room messages endpoint.
  - Changed unified Messaging and legacy Rooms to use one-millisecond cursor overlap, stable identity merging, bounded 100-message caches, and failure-safe cached results.
  - Materialized room users, messages, and joined-room responses while holding the tracker room lock so serialization cannot race list mutation.
  - Reduced a representative 25-message/256-byte room from 268,080 response bytes per idle minute to 10,094 bytes (96.235% fewer); one-new-message-per-poll traffic falls to 33,207 bytes (87.613% fewer).
  - Added controller, API-client, cursor/merge, initial-bound, failure-cache, and legacy-room regressions; documented gotchas `0z591` and `0z592` in standalone commits `a60fbdd98` and `2a95ba26a`.
- Validation:
  - Passed focused backend tests (`14/14`) and focused Web tests (`17/17`).
  - Passed complete Web tests (`814` passed, `4` skipped), production Web build, and frontend lint with zero new errors.
  - Passed complete backend suites (`4735/4735`: `69` smoke, `4388` unit, `278` integration), repository lint, route-inventory, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 21:00:23Z

- Current task: performance and efficiency improvements in progress; bounded initial transfer hydration and paged successful-history pass complete locally.
- Last activity:
  - Changed initial transfer hydration to return active and failed records plus server-derived total counts, excluding the large successful terminal history by default.
  - Added stable 250-record successful-history pages behind `Hide Complete`, an explicit load-older action with a tooltip, hidden-document suspension, and initial-seed ordering so secondary hydration cannot be erased.
  - Added partial actionable/history indexes, a covering `(Removed, Direction)` count index, distinct EF model identities for same-column indexes, and an idempotent migration that removes the redundant direction-only index.
  - Reduced a modeled 134,777-row/99%-successful initial response from 79,653,208 bytes to about 796,749 bytes (99.0%). Across 100 synthetic 200,000-row runs, actionable snapshots improved 90.816%, counts 67.506%, and completed-history pages 99.884%.
  - Added backend/API/Web paging, query-plan, migration, visibility, and seed-race regressions; regenerated the route inventory; extended gotchas `0z380`, `0z462`, and `0z587`; and added `0z588` through `0z590` in standalone documentation commits.
- Validation:
  - Passed focused backend tests (`43/43`) and focused Web tests (`9/9`).
  - Passed complete Web tests (`810` passed, `4` skipped), production Web build, and frontend lint with zero new errors.
  - Passed complete backend suites (`4731/4731`: `69` smoke, `4384` unit, `278` integration), repository lint, route-inventory, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 20:31:02Z

- Current task: performance and efficiency improvements in progress; incremental transfer reconciliation and indexed mutation timeline pass complete locally.
- Last activity:
  - Added authenticated `/api/v0/transfers/changes` with a server watermark, mutation-stamped `UpdatedAt`, soft-deletion deltas, and an idempotent `(Direction, UpdatedAt)` index migration.
  - Changed the transfer page from repeated full-history replacement to one initial seed plus overlapping cursor deltas, buffered pre-seed live-event replay, unchanged-state suppression, in-flight rejection, hidden polling/live-event suspension, and immediate visibility catch-up.
  - Added stable `RequestId` to transfer removal events so request-backed rows disappear without waiting for reconciliation.
  - Reduced representative 134,777-row first-minute response traffic by 78.74%, visible idle steady-state traffic from 299,744,052 bytes to 156 bytes per minute (99.9999%), and hidden polling to zero. Synthetic 200,000-row idle delta queries improved from 1,159.779 ms to 0.239 ms across 200 runs.
  - Added migration, stamping, query-plan, controller, serialization, event-identity, API-boundary, delta-merge, snapshot-race, overlap, cursor, and visibility regressions; documented gotchas `0z586` and `0z587` and extended `0z568` in standalone commits `41357ef0d`, `8cb418c2c`, and `9454dfd25`.
- Validation:
  - Passed focused backend tests (`39/39`) and focused Web tests (`21/21`).
  - Passed complete Web tests (`807` passed, `4` skipped), production Web build, and frontend lint with zero errors.
  - Passed complete backend suites (`4722/4722`: `69` smoke, `4375` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 20:11:26Z

- Current task: performance and efficiency improvements in progress; incremental private-chat polling and indexed timeline pass complete locally.
- Last activity:
  - Added an optional Unix-millisecond conversation cursor and bounded overlapping message merging to unified and legacy chat.
  - Prevented overlapping legacy requests, skipped unchanged state, preserved cached messages across transient failures, and stopped legacy polling while hidden.
  - Added an idempotent `(Username, Timestamp)` SQLite index, kept full unread totals in SQL outside the bounded display window, and fixed ISO timestamp ordering in chat and room adapters.
  - Reduced representative first-minute 100-message response traffic by 96.49% in unified chat and 92.04% in legacy chat; hidden legacy traffic falls from 12 requests per minute to zero. Synthetic 200,000-message timeline queries improved from 3,611 ms to 11 ms across 200 runs.
  - Added service, migration, query-plan, controller, API-client, cursor/merge, cache-failure, overlap, timestamp, and visibility regressions; documented gotchas `0z583` through `0z585` in standalone commits `a6a94ddfc`, `606a433e5`, `d480d948e`, and `e93833c2e`.
- Validation:
  - Passed focused backend tests (`15/15`) and focused Web tests (`28/28`).
  - Passed complete Web tests (`799` passed, `4` skipped), production Web build, and frontend lint with zero errors.
  - Passed complete backend suites (`4714/4714`: `69` smoke, `4367` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 19:53:55Z

- Current task: performance and efficiency improvements in progress; legacy Pods route polling pass complete locally.
- Last activity:
  - Reused complete list metadata for selection, moved Pod-list polling from five to sixty seconds, and removed redundant selected-detail requests.
  - Added bounded incremental channel-message merging with the existing overlapping `since` cursor, in-flight guards, stable state comparisons, and hidden-document suspension.
  - Fixed direct Pod channel URLs so route IDs no longer short-circuit initial detail, membership, and message hydration.
  - Reduced a representative ten-Pod/100-message/25-member first visible minute from 46 requests and 1,828,123 response bytes to 34 requests and 68,868 bytes (26.09% fewer requests and 96.23% fewer bytes); hidden steady-state polling falls from 42 requests per minute to zero.
  - Added direct-route, cadence, cursor/merge, overlap, and visibility regressions; documented gotchas `0z581` and `0z582` in standalone commits `071804522` and `0d50e30a6`, and extended `0z576` in `91d3de369`.
- Validation:
  - Passed focused legacy Pods tests (`4/4`), complete Web tests (`793` passed, `4` skipped), production Web build, and frontend lint with zero errors.
  - Passed complete backend suites (`4709/4709`: `69` smoke, `4362` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 19:37:31Z

- Current task: performance and efficiency improvements in progress; legacy Rooms route polling pass complete locally.
- Last activity:
  - Split active room messages and membership into two-second and ten-second cadences instead of two full requests every second.
  - Rejected overlapping active-room and joined-room hydration, ignored inactive/hidden/stale completions, and skipped unchanged message, user, and joined-room state.
  - Suspended every Rooms-route polling timer while the browser document is hidden and refreshed immediately when visible.
  - Reduced a representative 25-message/100-user first visible minute from 120 requests and 1,518,120 response bytes to 36 requests and 372,636 bytes (70% fewer requests and 75.45% fewer bytes); hidden steady-state polling is zero.
  - Added cadence, overlap, inactive, and visibility regressions; documented gotcha `0z580` in standalone commit `7ebbcd6bf`.
- Validation:
  - Passed focused Rooms tests (`11/11`), complete Web tests (`789` passed, `4` skipped), production Web build, and frontend lint with zero errors.
  - Passed complete backend suites (`4709/4709`: `69` smoke, `4362` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 19:20:12Z

- Current task: performance and efficiency improvements in progress; active Pod message polling pass complete locally.
- Last activity:
  - Reused the existing Pod `since` cursor with a bounded client cache and one-millisecond overlap instead of refetching the full retained message window every two seconds.
  - Derived stable message IDs from the Pod storage key so overlapping incremental reads merge deterministically.
  - Paused shared `MessageStream` polling while hidden, refreshed immediately on visibility, rejected overlapping slow calls, and ignored stale adapter results.
  - Reduced a representative idle first minute with 100 retained 256-byte messages from 1,363,783 response bytes to 44,053 bytes (96.77% fewer); hidden steady-state polling is zero.
  - Added cursor/cache/identity, overlap, and visibility regressions; documented gotchas `0z578` and `0z579` in standalone commits `c5145e3a8` and `3eb47dcd1`.
- Validation:
  - Passed focused Pod backend tests (`11/11`) and focused messaging Web tests (`19/19`).
  - Passed complete Web tests (`784` passed, `4` skipped), production Web build, frontend lint with zero errors, and the polling-lifecycle guard.
  - Passed complete backend suites (`4709/4709`: `69` smoke, `4362` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 19:07:05Z

- Current task: performance and efficiency improvements in progress; Messaging V2 hydration fan-out pass complete locally.
- Last activity:
  - Removed redundant per-pod detail requests because the saved-pod list already contains full channel metadata.
  - Split ten-second conversation/room hydration from sixty-second pod/discovery hydration, deduplicated overlapping work and unchanged state updates, scoped post-mutation refreshes, and paused both timers while hidden.
  - Reduced a ten-pod first visible minute from 98 hydration requests to 18 (81.63% fewer); hidden steady-state hydration is zero.
  - Added request-shape, cadence, overlap, and visibility regressions; documented gotchas `0z575` through `0z577` in standalone commits `0b0bf3237`, `61bec4b25`, and `aca0a1f91`.
- Validation:
  - Passed focused Messaging V2 tests (`4/4`), complete Web tests (`781` passed, `4` skipped), production Web build, focused ESLint, polling-lifecycle, and whitespace checks.
  - Passed complete backend suites (`4708/4708`: `69` smoke, `4361` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 18:52:41Z

- Current task: performance and efficiency improvements in progress; conversation unread-count query pass complete locally.
- Last activity:
  - Replaced unread-message entity materialization and per-conversation rescans with one correlated SQLite count projection.
  - Upgraded the acknowledgement index to covering `(IsAcknowledged, Username)` and made the existing migration replace its earlier single-column shape safely.
  - Measured 200 conversations and 200,000 unread messages at 59,088,895 intermediate bytes and 172 ms mean before versus 3,200 bytes and 4 ms after.
  - Added service, migration-upgrade, idempotence, and covering query-plan regressions; documented gotcha `0z574` in standalone commit `148298f27`.
- Validation:
  - Passed focused conversation-service tests (`4/4`) and the broader messaging unit slice (`40/40`).
  - Passed complete backend suites (`4708/4708`: `69` smoke, `4361` unit, `278` integration), repository lint, route-inventory, identity-leak, and whitespace checks.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 18:42:23Z

- Current task: performance and efficiency improvements in progress; Network dashboard and shared aggregate-status fan-out pass complete locally.
- Last activity:
  - Added authenticated `GET /api/v0/network/stats` as one bounded server-side snapshot with optional detailed peer lists.
  - Replaced seven footer aggregate requests per cycle and eight Network-pane requests per cycle with one summary request for each consumer.
  - Changed the Network pane from five-second polling to ten seconds, stopped polling while hidden, and rejected overlapping cycles.
  - Reduced first-minute visible aggregate requests across footer and Network from 153 to 14 (90.85%), excluding unchanged transfer-speed polling.
  - Normalized real peer/job envelopes and property names so mesh peers, discovered peers, swarm jobs, capability features, HashDb size, and coverage render correctly.
  - Regenerated the route inventory and documented gotchas `0z569` through `0z573` in standalone commits `b8ea55643`, `fe1a2111b`, `0e62eb348`, and `e934d133e`.
- Validation:
  - Passed focused backend tests (`2/2`) and focused Network/slskdn/Footer Web tests (`19/19`).
  - Passed complete Web Vitest (`777` passed, `4` skipped) and the production Web build.
  - Passed complete backend suites (`4707/4707`: `69` smoke, `4360` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 18:18:33Z

- Current task: performance and efficiency improvements in progress; direct-message navigation polling pass complete locally.
- Last activity:
  - Added an authenticated one-bit direct-message activity endpoint backed by a scalar SQLite existence query.
  - Replaced app-wide full conversation aggregation polling with the bounded boolean response.
  - Added an acknowledgement covering index to the messaging model and an idempotent migration for existing databases.
  - Measured a representative 100-conversation response at 12,091 bytes before versus 4 bytes after (99.97% smaller), and verified the indexed query plan.
  - Regenerated the current API route inventory and documented gotchas `0z567` and `0z568` in standalone commits `bb1c75756` and `7e3559044`.
- Validation:
  - Passed focused backend tests (`14/14`) and focused App/chat Web tests (`36/36`).
  - Passed complete Web Vitest (`772` passed, `4` skipped) and the production Web build.
  - Passed complete backend suites (`4705/4705`: `69` smoke, `4358` unit, `278` integration) and repository lint.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 18:02:25Z

- Current task: performance and efficiency improvements in progress; global room-activity polling fan-out pass complete locally.
- Last activity:
  - Added one authenticated room-activity summary endpoint that returns the latest incoming timestamp per joined room.
  - Replaced app-wide joined-room plus per-room message polling with the bounded summary request.
  - Paused navigation polling in hidden tabs, rejected overlapping cycles, and prevented post-unmount poll updates.
  - Measured a representative 20-room/25-message poll at 21 requests and 58,141 message bytes before versus one request and 471 bytes after (99.19% smaller payload).
  - Regenerated the current API route inventory and documented gotcha `0z566` in standalone commit `3d3dfeccd`.
- Validation:
  - Passed focused controller tests (`10/10`), focused App/rooms Web tests (`41/41`), focused ESLint, and the Web polling lifecycle guard.
  - Passed complete Web Vitest (`771` passed, `4` skipped) and the production Web build.
  - Passed complete `dotnet test` (`4701/4701`: `69` smoke, `4354` unit, `278` integration).
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 17:47:33Z

- Current task: performance and efficiency improvements in progress; footer polling and HashDb capability-count pass complete locally.
- Last activity:
  - Split footer transfer-speed and aggregate-stat polling into 2-second and 10-second cadences, respectively.
  - Added in-flight and mounted lifecycle guards so slow polling cannot overlap or update an unmounted footer.
  - Added HashDb migration 18 and optimization-service coverage for the `Peers(caps)` covering index.
  - Measured 500 capable-peer counts at 1.615s without the index and 0.089s with it on 200,000 synthetic peers.
  - Reverted a conditional-aggregate attempt after measurement proved it about 2.5 times slower, and documented gotchas `0z562` through `0z565` in required standalone commits.
- Validation:
  - Passed complete `dotnet test` (`4700/4700`: `69` smoke, `4353` unit, `278` integration) and focused HashDb/optimization tests (`57/57`).
  - Passed complete Web Vitest (`769` passed, `4` skipped), production build, focused ESLint, polling lifecycle guard, repository lint, and `git diff --check`.
- Next steps:
  1. Continue the broader performance goal from measured hot paths.
  2. Do not create a release tag unless explicitly requested.

## Update 2026-07-15 04:10:00Z

- Current task: GitHub and GitLab CI failure remediation complete.
- Last activity:
  - Identified GitHub dependency submission failures as a direct/transitive package downgrade in the performance tests.
  - Identified GitLab's jobless failed pipelines as invalid CI configuration caused by an unquoted colon in a `before_script` entry.
  - Documented gotcha `0z561`, aligned the NuGet reference, and quoted the GitLab script scalar.
- Validation:
  - GitLab's CI lint API reports the full configuration valid with zero errors or warnings, and the corrected `main` push created no GitLab pipeline under the tag-only workflow rule.
  - GitHub Automatic Dependency Submission passed and submitted its snapshot; Local Identity Leak Check and CodeQL also passed.
- Next steps:
  1. No CI remediation work remains.
  2. Historical failed pipeline/run records remain visible but no longer reproduce on the corrected head.

## Update 2026-07-15 03:35:00Z

- Current task: share-token URL exposure remediation complete locally.
- Last activity:
  - Moved share-manifest authentication from query strings to `X-Share-Token`.
  - Added share-token exchange for short-lived, content-bound playback tickets.
  - Added frontend and backend regression coverage and documented gotcha `0z560`.
- Next steps:
  1. Validate, commit, and push the complete working tree.
  2. No release tag was requested or created for this post-`.277` work.

## Update 2026-07-15 03:25:00Z

- Current task: `.277` release and GitLab notification remediation complete.
- Last activity:
  - Stopped the initial `.277` pre-tag gate before tag creation when it exposed a stale XML documentation parameter reference.
  - Confirmed GitLab admitted every branch pipeline despite the tag-only policy, causing the reported `main` pipeline-failure emails.
  - Documented gotchas `0z558` and `0z559`, restricted GitLab workflows to tags, and corrected the XML documentation.
  - Published and verified release `2026071502-slskdn.277` from corrected commit `5a87a1aa9`, then synchronized the workflow-generated stable packaging metadata to GitLab without creating a branch pipeline.
- Validation:
  - Passed warning-free Release build, repository lint, complete local and hosted release gates, frontend tests/build, backend unit tests (`4347/4347`), smoke tests (`69/69`), integration smoke (`40/40`), all six archive checksums, required release assets, and Linux x64 version verification.
  - AUR, Nix metadata, Homebrew, PPA, announcements, and both Docker publishers succeeded. Chocolatey and COPR failed at their external upload steps.
- Next steps:
  1. No code, synchronization, or release-artifact work remains.
  2. Investigate the Chocolatey and COPR external publisher failures separately if those channels are required.

## Update 2026-07-15 02:27:55Z

- Current task: repository synchronization and replacement stable release complete.
- Last activity:
  - The `.275` tag completed its local gate but failed the hosted release gate before publishing artifacts because a mesh hash-mismatch test timed out.
  - Documented pipe completion ownership gotcha `0z557` in standalone commit `a683cf7e5`.
  - Fixed the pipe race in `8928aef73`, synchronized all committed work with GitHub and GitLab, and merged the workflow-generated stable metadata without rewriting either history.
  - Published and verified release `2026071501-slskdn.276`; the primary and omnibus Docker images also completed successfully.
- Validation:
  - Passed focused Release `MeshStreamServiceTests` (`3/3`), repository lint, the complete local and hosted release gates, all six archive checksum checks, required release-asset checks, and Linux x64 version verification.
  - AUR, Nix metadata, Homebrew, PPA, announcements, and both Docker publishers succeeded. Chocolatey and COPR failed at their external upload steps.
- Next steps:
  1. No code, synchronization, or release-artifact work remains.
  2. Investigate the Chocolatey and COPR external publisher failures separately if those channels are required.

## Update 2026-07-14 16:00:00Z

- Current task: release and live deployment complete.
- Last activity:
  - Committed and pushed the complete working tree through `be76e0f09`.
  - Published release `2026071415-slskdn.274` from tag `build-main-2026071415-slskdn.274` and verified its downloadable artifacts and checksums.
  - Replaced the live validation service with immutable GHCR image `2026071415-slskdn.274`, retaining the prior unit and image for rollback.
- Validation:
  - Passed the guarded release build, full tests, lint, archive checksums, and Linux x64 version verification.
  - Live service reports the matching version, Docker health `healthy`, restart count `0`, and `/health=Healthy`.
  - VPN forwarding is ready; Soulseek connected and logged in after restart.
  - COPR and Chocolatey external publishing failed; other completed package/release jobs passed, and Docker was completing post-push actions after the image became pullable.
- Next steps:
  1. No implementation or deployment work remains.
  2. Investigate COPR and Chocolatey publisher failures separately if those channels are required.

## Update 2026-07-14 00:00:00Z

- Current task: durable Wishlist false-positive suppression complete.
- Last activity:
  - Added reversible per-wishlist, per-peer, per-directory ignored-result rules.
  - Applied rules to result display, hit statistics, album candidates, and auto-download selection.
  - Added quoted phrase exclusions and ignored-folder management in the Wishlist editor.
- Validation:
  - Passed full `dotnet test --no-restore` (69 smoke, 4327 unit, 278 integration).
  - Passed full Web Vitest (765 passed, 4 skipped), production build, changed-file ESLint, `./bin/lint`, and `git diff --check`.
- Next steps:
  1. Review and commit the implementation when ready.
  2. No release tags were created.

## Update 2026-07-10 00:00:00Z

- Current task: none; the 30-item security and reliability bug report is complete.
- Last activity:
  - Implemented, documented, committed, and pushed one fix per reported bug to `snapetech/slskdN`.
  - Completed the final privacy-batching correlation fix at commit `081302e43`.
  - Focused validation and repository lint passed for every fix.
- Validation:
  - Passed: full `dotnet test --no-restore` (`4668/4668`: 68 smoke, 4322 unit, 278 integration).
  - Passed: `./bin/lint` and `git diff --check` for the final implementation.
- Next steps:
  1. No implementation follow-up remains from the supplied report.
  2. No release tags were created.

## Update 2026-06-24 00:00:00Z

- Current task: README long-form restoration complete.
- Last activity:
  - Identified `31dec86be` (`docs: demote README feature claims`) as the commit that replaced the 927-line README with the short maturity-warning version.
  - Restored `README.md` from `31dec86be^`, the last long-form README before that demotion.
  - Removed one restored trailing-whitespace violation.
- Validation:
  - Passed: `git diff --check`.
- Next steps:
  1. Review and commit the README restoration.
  2. No release tags were created.

## Update 2026-06-19 15:16:00Z

- Current task: Live VPN-required Soulseek connectivity incident fixed.
- Last activity:
  - Diagnosed a healthy app/container with Soulseek disconnected because the Gluetun-compatible VPN status helper on `127.0.0.1:8010` was stopped.
  - Restarted the helper on the live Docker host and re-enabled it under the app service dependency.
  - Verified VPN status `running`, forwarded port present, `/health=Healthy`, Docker health `healthy`, zero container restarts, and Soulseek login/active transfers after recovery.
  - Patched the packaged VPN helper unit so it is wanted by both supported app service names.
  - Patched conversation acknowledgement endpoints to return 503 while Soulseek is disconnected or logging in instead of surfacing runtime acknowledgement exceptions.
  - Documented ADR-0001 gotcha `0z496` in docs-only commit `ad7ca1e8a`.
- Validation:
  - Passed: focused `ConversationsControllerTests` (`5/5`).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
  - Passed: live health/VPN/Soulseek resampling after helper restart and re-enable.
- Next steps:
  1. Commit and push the code/docs fix.
  2. Build/deploy a fresh app image if the 503 acknowledgement guard is needed live before the next release.
  3. No release tags were created.

## Update 2026-06-17 13:10:00Z

- Current task: Codebase improvements plan PR-01 through PR-05 complete, PR-06+ deferred to future sessions.
- Last activity:
  - **PR-01** (HttpClient): replaced `new HttpClient()` with `IHttpClientFactory.CreateClient(OutboundUriGuard.NoRedirectHttpClientName)` in `RelayClient`, `SharesController`, `NatDetectionService`, `Application`/`GitHub`.
  - **PR-02** (Random): replaced `new Random()` with `Random.Shared` in `BucketPadder`, `Honeypot`, `RandomJitterObfuscator`, `StartupConsoleOutput`.
  - **PR-03** (MediaCore Stats): extracted `MediaCoreStats.jsx` (~830 lines).
  - **PR-04** (MediaCore Pods): extracted `MediaCorePods.jsx` (~4800 lines of PodCore state + handlers + JSX).
  - **PR-05**: already achieved with PR-04 — `index.jsx` at 2969 lines.
  - **PR-06** (Integrations cards): deferred — 9 cards each ~400 lines, needs dedicated session.
  - **PR-07-11** (backend splits): `TransferService.cs` already split (68 lines). Other large services deferred.
  - **PR-12-13** (code quality): verified clean — only 2 legitimate parser catch-return-null instances.
  - Documented gotchas `0z493` and `0z494` in ADR-0001.
- Validation:
  - Passed: `dotnet test` (4581/4581), `./bin/lint`, frontend lint (0 errors), frontend tests (767/767), frontend build.
- Next steps:
  1. Commit and push changes.
  2. PR-06 (Integration card extraction) and PR-07-11 (backend service splits) in future sessions.
  3. Deploy for manual validation when ready.

- Current task: Release tag pending after final manual validation.
- Last activity:
  - Committed and pushed disconnect teardown fix `2be213804`.
  - Built/published manual version `0.0.0-manual.20260616195705.2be213804056`.
  - Deployed the manual image to the live validation host.
  - Verified image/app version match, Docker health `healthy`, restart count `0`, and `/health=Healthy`.
  - Waited through the next maintenance tick and rescanned logs.
- Validation:
  - Passed: vendored `SearchInternalTests` (`40/40`).
  - Passed: slskd classifier tests (`47/47`).
  - Passed: full `dotnet test --no-restore` (`4581/4581`: 68 smoke, 4235 unit, 278 integration).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
  - Passed: release-shaped build/publish and live post-maintenance log scan with `ERR=0`, `FTL=0`, `OOM=0`, `prune=0`, `variant=0`, `unobserved=0`, `lock=0`.
- Next steps:
  1. Commit/push this final validation note.
  2. Run `scripts/create-release-tag.sh build-main-2026061620-slskdn.271`.
  3. Watch the tag-triggered build and verify release artifacts after publication.

## Update 2026-06-16 19:49:01Z

- Current task: Pre-release live-log rescan fixes in progress.
- Last activity:
  - Resampled the running manual image logs.
  - Confirmed previous event-prune OOM and HashDb sidecar issues stayed quiet.
  - Found a transient VPN-client status timeout caused warning/error/fatal teardown noise.
  - Documented ADR-0001 gotcha `0z491` in docs-only commit `c18e996f7`.
  - Patched intentional VPN disconnect unobserved-task classification, Soulseek read-loop teardown classification, and vendored search disposal.
- Validation:
  - Passed: vendored `SearchInternalTests` (`40/40`).
  - Passed: slskd classifier tests (`47/47`).
- Next steps:
  1. Run full backend validation and lint.
  2. Commit and push all changes.
  3. Build/deploy a fresh manual image, resample live logs, and only then cut the requested release tag if the new sample is release-clean.

## Update 2026-06-16 18:28:26Z

- Current task: Live manual-build log cleanup complete and deployed.
- Last activity:
  - Committed and pushed the HashDb sidecar fix through `1bd0bfb8f`.
  - Built and deployed manual Docker image `0.0.0-manual.20260616181744.1bd0bfb8fe31`.
  - Verified app/image version match, Docker health `healthy`, container restart count `0`, and `/health=Healthy`.
  - Waited through the next five-minute maintenance tick and resampled logs.
- Validation:
  - Passed: focused event tests (`13/13`).
  - Passed: focused HashDb tests (`66/66`).
  - Passed: full `dotnet test --no-restore` (`4579/4579`: 68 smoke, 4233 unit, 278 integration).
  - Passed: `./bin/lint`.
  - Passed: release-shaped Web/backend build and Linux x64 publish.
  - Passed: post-maintenance live log scan with `ERR=0`, `FTL=0`, `OOM=0`, `prune_failed=0`, `variant_metadata_failed=0`.
- Next steps:
  1. Have Bas retest TauAs browsing, the hrust82 Cyrillic path, and the budznbeerz download against the final manual build.
  2. If retest passes, proceed with the official release process only when a build tag is explicitly requested.

## Update 2026-06-16 18:10:34Z

- Current task: Live manual-build log cleanup follow-up in progress.
- Last activity:
  - Built and deployed manual Docker image `0.0.0-manual.20260616175803.0ed04b45635d` from commit `0ed04b45635d`.
  - Verified app/image version match, Docker health `healthy`, container restart count `0`, Web health `Healthy`, and no event-prune OOM after the next five-minute maintenance tick.
  - Confirmed second-chance transfer fallback now logs at Info instead of Warning.
  - Found one remaining actionable warning: HashDb tried to derive audio metadata for a downloaded PDF sidecar.
  - Documented ADR-0001 gotcha `0z490` and committed the docs-only entry as `43661c14d`.
  - Patched HashDb completed-download ingestion to skip non-audio sidecars before hashing, fingerprinting, or audio variant metadata derivation.
- Validation:
  - Passed: focused HashDb tests (`66/66`).
- Next steps:
  1. Run full backend validation and lint for the HashDb sidecar fix.
  2. Commit/push the HashDb fix.
  3. Rebuild/redeploy a fresh manual image and resample live logs for HashDb sidecar warnings.

## Update 2026-06-16 17:55:21Z

- Current task: Live manual-build log follow-up fixes complete locally; redeploy pending.
- Last activity:
  - Inspected logs from the manual Docker build on the live validation host.
  - Found the service active/running and Docker-healthy with zero container restarts.
  - Found two event pruning errors where `EventService.PruneAsync` materialized expired `EventRecord` payloads and hit `System.OutOfMemoryException` under the managed heap cap.
  - Found successful second-chance transfer fallback diagnostics back at warning level after the vendored runtime sync.
  - Documented ADR-0001 gotcha `0z489` and committed the docs-only entry as `a6f3c4219`.
  - Patched event pruning to use `ExecuteDelete()` and restored second-chance fallback diagnostics to Info/Debug.
- Validation:
  - Passed: focused event tests (`13/13`).
  - Passed: full `dotnet test --no-restore` (`4578/4578`: 68 smoke, 4232 unit, 278 integration).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the code/docs update.
  2. Build and deploy a fresh manual image if requested, then resample live logs to confirm the event-prune OOM and second-chance warnings are gone.

## Update 2026-06-16 17:39:56Z

- Current task: Manual Docker test build deployed to the live validation host.
- Last activity:
  - Verified the GitHub target and found the checkout clean with `main` already matching `origin/main`.
  - Explicitly pushed `main` to `snapetech/slskdN`; GitHub reported everything up to date.
  - Published manual Linux x64 payload `0.0.0-manual.20260616172930.a2621a7c6e30` from commit `a2621a7c6e3016c2a557f1e293c5e4d4396984fa`.
  - Stopped the live host service restart loop caused by the previous manual Docker image tag no longer being present locally.
  - Transferred the publish payload, built local image `slskdn:0.0.0-manual.20260616172930.a2621a7c6e30` from the released base with the new `/slskd` payload, updated the systemd image tag, and started the service.
- Validation:
  - Passed: Web Vitest (`767/767`).
  - Passed: Web production build.
  - Passed: Release backend build with `0 Warning(s), 0 Error(s)`.
  - Passed: Release unit tests (`4231/4231`), smoke tests (`68/68`), and integration tests (`278/278`).
  - Passed: service active/running, Docker health `healthy`, restart count `0`, apphost version match, Web root HTTP 200, and `/health=Healthy`.
  - Observed: authenticated application API returns 401 without credentials as expected.
- Next steps:
  1. Have Bas retest TauAs browsing, the hrust82 Cyrillic path, and the budznbeerz download against the manual build.
  2. If retest passes, proceed with the official release process by creating the requested build tag.

## Update 2026-06-16 17:12:00Z

- Current task: slskdN open Dependabot PR/security-alert sweep complete locally.
- Last activity:
  - Applied the remaining open Dependabot NuGet/npm updates directly to `main`.
  - Cleared npm audit findings by upgrading `esbuild` to `0.28.1` and accepting
    the audit transitive fixes.
  - Applied the follow-up Dependabot `AWSSDK.S3` `4.0.24.5` patch from PR #253.
  - Applied the follow-up Dependabot `react-router-dom` `7.18.0` patch from PR
    #254.
  - Migrated the Transfers virtual list to the `react-window` v2 `List` API.
  - Fixed the `Serilog.Sinks.Grafana.Loki` 9.x startup break by skipping Loki
    sink construction when no URI is configured.
  - Fixed the player listening-history import test fixture that aged out of the
    default 30-day visible stats range.
  - Documented ADR-0001 gotchas `0z487` and `0z488`.
- Validation:
  - Passed: focused Loki startup regression test.
  - Passed: full Web Vitest suite (`767/767`).
  - Passed: `npm audit --audit-level=low` with zero vulnerabilities.
  - Passed: `npm run build` with existing non-fatal Rolldown annotation warnings
    from `@microsoft/signalr`.
  - Passed after PR #254: full Web Vitest suite, `npm audit --audit-level=low`,
    and `npm run build`.
  - Passed: `dotnet list slskd.sln package --vulnerable --include-transitive`
    with no vulnerable packages.
  - Passed: unit tests (`4231/4231`), integration tests (`278/278`), and smoke
    tests (`68/68`) through the serial full solution test.
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the dependency sweep.
  2. Verify GitHub alerts and close superseded Dependabot PRs if GitHub does
     not auto-close them after main updates.

## Update 2026-06-16 16:49:39Z

- Current task: slskNet.Runtime standalone and vendored runtime sync complete locally.
- Last activity:
  - Committed and pushed the standalone runtime instruction file, legacy peer path-encoding fix, and council backlog whitespace cleanup through standalone commit `74243f52`.
  - Re-exported the tracked standalone runtime tree into `vendor/slskNet.Runtime` and verified the vendored copy matches the standalone archive.
  - Preserved the local tester-feedback slskdN changes: 60 second peer/transfer inactivity defaults, config docs, tests, and ADR/task/progress notes.
  - Replaced the vendored Web example's vulnerable CRA/yarn tree with the standalone Vite/npm tree.
  - Fixed the slskdN `MessagePack` high-severity advisory by upgrading `MessagePack` from `3.1.4` to `3.1.7`.
- Validation:
  - Passed: `git diff --check`.
  - Passed: `dotnet list slskd.sln package --vulnerable --include-transitive` with no vulnerable packages.
  - Passed: full `dotnet test` (`4577/4577`: 68 smoke, 4231 unit, 278 integration).
  - Passed: vendored Web `npm ci`, `npm audit --audit-level=low`, and `npm run build`.
  - Passed: `./bin/lint`.
- Next steps:
  1. Push the sync commit; no release tags were created.
  2. Build/deploy only when requested, then have Bas retest TauAs, hrust82, and budznbeerz.

## Update 2026-06-16 16:21:13Z

- Current task: Tester peer browse/download compatibility fix complete locally.
- Last activity:
  - Triaged Bas's reports for TauAs browse failures, hrust82 Cyrillic path compatibility, and budznbeerz remote enqueue timeouts.
  - Increased default Soulseek peer inactivity and transfer inactivity timeouts to 60 seconds.
  - Added Windows-1251 fallback protocol decoding for likely Cyrillic path bytes.
  - Preserved file/directory/folder/transfer string encoding metadata through Soulseek.NET message parsing and writing.
  - Added a per-user remote path encoding cache so later folder-content and download requests serialize legacy paths with the same bytes the remote peer sent.
  - Updated `config/slskd.example.yml`, `docs/config.md`, `memory-bank/tasks.md`, and `memory-bank/progress.md`.
  - Documented ADR-0001 gotcha `0z486` and committed the docs-only entry as `7da8d676d`.
- Validation:
  - Passed: focused vendor regressions (`33/33`).
  - Passed: broader Soulseek.NET related tests (`265/265`).
  - Passed: focused slskd startup/download tests (`41/41`).
  - Passed: full `dotnet test` (`4577/4577`: 68 smoke, 4231 unit, 278 integration).
  - Passed: `./bin/lint`.
- Next steps:
  1. Build/deploy when requested; no release tags were created.
  2. Have Bas retest TauAs browsing, the hrust82 Cyrillic path, and the budznbeerz download; capture exact logs if any failure persists.

## Update 2026-05-25 20:04:29Z

- Current task: Downloads request-identity fix deployed and verified on the live Docker host.
- Last activity:
  - Built release-shaped Web/backend payload `0.0.0-manual.20260525195707.9654eac5f35d` from `9654eac5f35d`.
  - Published Linux x64 self-contained output and built replacement Docker image `slskdn:0.0.0-manual.20260525195707.9654eac5f35d` from the previous live image to preserve optional tools and hardening.
  - Updated the live `slskd.service` image tag and restarted the service; no release tags were created.
  - Verified the running app and image report the matching manual version.
  - Verified `/`, `/downloads`, `/uploads`, `/wishlist`, `/searches`, the main JS/CSS, and the rebuilt Transfers JS/CSS return HTTP 200.
  - Confirmed the deployed Transfers chunk contains the new request-id handling.
  - Confirmed optional media tools checked in the previous image remain available.
- Validation:
  - Passed: local release build with `0 Warning(s), 0 Error(s)`.
  - Passed: Docker health `healthy`, restart count zero, service `active/running`.
  - Passed: `/health=Healthy`.
  - Passed: current-process log scan after startup under live transfer activity had `err=0`, `wrn=0`, `ftl=0`, `tracked_not_slskd=0`.
  - Observed: one info-level remote peer unavailable download and one info-level Lidarr timeout; no warning/error/fatal entries from the new process.
- Next steps:
  1. Collect one failed direct-download sample from a tester: username, exact remote filename, UI state, and nearby daemon log lines.
  2. Have a user watch Downloads during active album queues to confirm rows no longer duplicate or jump between reconcile ticks.

## Update 2026-05-25 19:50:39Z

- Current task: Bas tester feedback triage; Downloads row-churn fix complete locally.
- Last activity:
  - Triaged Bas's feedback about continued peer download failures and Downloads page instability.
  - Found and fixed a concrete Downloads row identity bug: SignalR transfer state/progress events did not carry `RequestId`, while REST snapshots and the Web store keyed request-backed rows by `RequestId`.
  - Added `RequestId` to `TransferActivity`, enriched progress events with the persisted transfer record when available, and made the Web transfer store patch existing request rows when legacy/missing-identity events arrive.
  - Added transfer-store regressions for request-keyed rows receiving legacy activity/progress events.
  - Documented ADR-0001 gotcha `0z485` and committed it as `abfe4166f`.
- Validation:
  - Passed: focused transfer-store Vitest (`13/13`).
  - Passed: frontend lint with `--max-warnings=0`.
  - Passed: focused backend transfer tests (`67/67`).
  - Passed: full `dotnet test` (`4577/4577`: 68 smoke, 4231 unit, 278 integration).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Deploy/re-test Downloads under live transfer activity to confirm rows no longer duplicate or jump between reconcile ticks.
  2. Collect one failed direct-download sample from a tester: username, exact remote filename, UI state, and nearby daemon log lines.

## Update 2026-05-25 02:57:06Z

- Current task: Live log rescan and cleanup fixes deployed.
- Last activity:
  - Resampled live logs after the tester-feedback deployment.
  - Fixed rescue mode spending work on non-audio sidecars by filtering rescue eligibility to safe audio extensions.
  - Downgraded startup DHT re-announce-before-ready from warning to deferred/info because the normal DHT loop announces once ready.
  - Changed Lidarr wanted-sync `HttpClient.Timeout` handling to log concise unavailable messages instead of warning stack traces.
  - Documented ADR-0001 gotchas `0z483` and `0z484`; updated the existing Lidarr external-timeout gotcha `0z426`.
  - Deployed manual image `slskdn:0.0.0-manual.20260525025408.ee802eb0347e`; no release tags were created.
- Validation:
  - Passed: focused Lidarr/DHT/rescue lifecycle tests (`51/51`).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
  - Passed: release build and publish with `0 Warning(s), 0 Error(s)`.
  - Passed: staged apphost and running container version match.
  - Passed: Docker container is `healthy`, service is `active/running`, restart count zero.
  - Passed: Web root returns HTTP 200 and `/health` returns `Healthy`.
  - Passed: fresh post-restart logs show `err=0`, `ftl=0`, `dht_not_ready_warning=0`, `lidarr_timeout_stack=0`, `rescue_sidecar=0`, `tracked_not_slskd=0`, `tau_matches=0`.
  - Observed: only remaining warning is the intentional public-DHT hardening notice.
- Next steps:
  1. Exercise the original tester flows in the live UI: Wishlist new-results filter/edit loop and manual retry of a timed-out download.
  2. Recheck logs after those manual actions create fresh download/retry activity.

## Update 2026-05-23 22:29:47Z

- Current task: Live log follow-up after completed layout deployment.
- Last activity:
  - Checked live `kspls0` health, restart state, and logs since the manual image deployment.
  - Found no warning/error/fatal log-level entries and no exception matches.
  - Confirmed AudioSketch stayed quiet (`AudioSketch=0`).
  - Identified one cleanup issue: download auto-retry re-queued failed album sidecar artwork (`cover.jpg`), creating avoidable network/log churn.
  - Documented ADR-0001 gotcha `0z470` and committed it as `b6aee23da`.
  - Patched auto-retry planning to skip non-audio sidecar files while preserving explicit user/download behavior.
  - Committed/pushed the fix as `649fd40c7` and deployed manual image `slskdn:0.0.0-manual.20260523223105.649fd40c72fb` to `kspls0`; no release tags were created.
- Validation:
  - Passed: focused `DownloadServiceTests` (`33/33`).
  - Passed: `./bin/lint`.
  - Passed: Release build and publish had `0 Warning(s), 0 Error(s)` and no publish warnings.
  - Passed: live image/app version match, Docker health `healthy`, restart count zero, Web root HTTP 200, and `/health=Healthy`.
  - Passed: fresh post-deploy log scan had `err=0`, `wrn=0`, `ftl=0`, `exception_matches=0`, `AudioSketch=0`, `cover_jpg=0`, and `auto_retry_requeued=0`.
- Next steps:
  1. Continue normal live testing of completed download folder naming and background retry behavior.

## Update 2026-05-23 21:01:36Z

- Current task: Completed download folder/file default build deployed to live Docker host.
- Last activity:
  - Cleaned the C# release-build warnings found during publish and committed/pushed `59f1a7fe5`.
  - Published warning-free Linux x64 payload `0.0.0-manual.20260523205920.59f1a7fe5841`.
  - Built replacement Docker image `slskdn:0.0.0-manual.20260523205920.59f1a7fe5841` from the previous live base to preserve installed tools and hardening.
  - Updated the live `slskd.service` image tag and restarted the service; no release tags were created.
- Validation:
  - Passed: Release `dotnet build` warning cleanup produced `0 Warning(s), 0 Error(s)`.
  - Passed: `./bin/lint`.
  - Passed: publish output had no C# warnings.
  - Passed: live image/app version match, Docker health `healthy`, restart count zero, Web root HTTP 200, and `/health=Healthy`.
  - Passed: post-restart log scan had `err=0`, `wrn=0`, `ftl=0`, and `exception_matches=0`.
  - Live config has no explicit completed-layout override, so the new `remote_folder` default applies.
- Next steps:
  1. Test a new completed album download and confirm it lands under the source folder name rather than a batch UUID.

## Update 2026-05-23 20:51:36Z

- Current task: Completed download folder/file default fix complete locally.
- Last activity:
  - Confirmed tester-reported UUID completed folders were the `batch_id` layout default, not a random filesystem failure.
  - Documented ADR-0001 gotcha `0z469` and committed it as `89936341b`.
  - Changed fresh/default completed downloads to use `remote_folder`, preserving the source folder/file naming while keeping explicit `batch_id` available.
  - Updated config examples, configuration docs, the download UX plan, and changelog.
- Validation:
  - Passed: focused `DownloadServiceTests` (`32/32`).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
  - Full `dotnet test` passed smoke (`68/68`) and integration (`278/278`) suites, but the full unit run had one unrelated LibraryHealth timeout; rerunning that exact test passed (`1/1`).
- Next steps:
  1. Commit and push the completed-download layout default change.

## Update 2026-05-23 20:38:00Z

- Current task: Live log follow-up and AudioSketch sidecar warning fix deployed.
- Last activity:
  - Rechecked `kspls0` logs after the UI deployment.
  - Found no error/fatal entries, but identified one actionable AudioSketch warning from ffmpeg being run against `cover.jpg`.
  - Patched AudioSketch to skip non-audio sidecar extensions before launching ffmpeg.
  - Documented ADR-0001 gotcha `0z468` and committed it as `503e39c71`.
  - Committed and pushed the code fix as `1bcef0404`.
  - Published and deployed manual image `slskdn:0.0.0-manual.20260523203442.1bcef0404d14` to `kspls0`; no release tags were created.
- Validation:
  - Passed: focused `AudioSketchServiceTests` (`7/7`).
  - Passed: manual frontend/backend build and Linux x64 publish; only existing C# warnings appeared.
  - Passed: live image/app version match, Docker health `healthy`, restart count zero, and `/health=Healthy`.
  - Passed: post-restart log scan had no error/fatal entries and no AudioSketch warnings.
  - Remaining warnings are expected startup/config notices for public DHT bootstrap exposure and MeshDHT missing configured/public-routable endpoints.
- Next steps:
  1. Continue monitoring live logs during normal download/library scan activity.

## Update 2026-05-23 19:14:55Z

- Current task: Web UI full-space route layout pass complete locally.
- Last activity:
  - Ran a headless Playwright sweep against the Vite dev server with mocked APIs.
  - Inspected Downloads and Uploads first; both already filled the app content width.
  - Confirmed most other top-level routes and System subtabs were capped at 1200px or by Semantic UI containers.
  - Reworked shared and route-specific layout containers so pages fill the available app content space like Transfers.
  - Fixed a System Metrics initial-load crash found during the sweep.
  - Documented ADR-0001 gotcha `0z467` and committed the docs-only entry as `c6b98d36e`.
- Validation:
  - Passed: Playwright layout sweep across Downloads, Uploads, top-level routes, and every System subtab; all measured at 100% app-content width.
  - Passed: focused Metrics Vitest (`2/2`).
  - Passed: frontend lint.
  - Passed: production Web build.
  - Passed: `./bin/lint`.
  - Passed: full `dotnet test` (`4562/4562`: 68 smoke, 4216 unit, 278 integration).
- Next steps:
  1. Deploy/build when ready; no release tags were created.

## Update 2026-05-23 19:18:12Z

- Current task: Commit dirty frontend layout/metrics changes and deploy them complete.
- Last activity:
  - Committed all then-dirty and untracked frontend changes as `9e277c701`.
  - Pushed `9e277c701` to `snapetech/slskdN`.
  - Rebuilt the frontend bundle into backend `wwwroot`, built the Release backend with tests skipped after focused validation, and published Linux x64 payload `0.0.0-manual.20260523191441.9e277c7014ef`.
  - Built a replacement Docker image from the previous live base and deployed it to `kspls0`; no release tags were created.
- Validation:
  - Passed: focused Metrics Vitest (`2/2`).
  - Passed: frontend lint with `--max-warnings=0`.
  - Passed: production frontend build.
  - Passed: Release backend build and publish completed with existing C# warnings only.
  - Passed: live image/app version match, Docker health `healthy`, restart count zero, Web root HTTP 200, `/health=Healthy`, and strict post-start log scan had no warning/error/fatal log-level entries.
- Next steps:
  1. Browser-test the full-width page layouts and System Metrics on the live instance.

## Update 2026-05-23 19:10:21Z

- Current task: Fix bad live `/health` state complete.
- Last activity:
  - Investigated the live `Degraded` health response and traced it to `MeshHealthCheck`.
  - Found the bug: optional mesh peer/routing absence was treated as app health degradation even when the daemon, Docker container, VPN, DHT bootstrap, and Soulseek login were otherwise operational.
  - Documented ADR-0001 gotcha `0z466` and committed it as `e20e774eb`.
  - Changed mesh health so peer/routing presence remains diagnostic data while subsystem responsiveness determines health; timeouts/exceptions still degrade.
  - Added `MeshHealthCheck_DoesNotDegradeForNoActivePeers`.
  - Deployed manual image `slskdn:0.0.0-manual.20260523190723.de82ad218550` to `kspls0`; no release tags were created.
- Validation:
  - Passed: focused `Phase8MeshTests` (`20/20`).
  - Passed: `./bin/lint`.
  - Passed: publish completed with existing C# warnings only.
  - Passed: live `/health=Healthy`, `/health/mesh=Healthy`, Docker health `healthy`, restart count zero, and app version matched the image.
  - Passed: strict post-start log scan had no warning/error/fatal log-level entries and no unhandled exception matches. INFO-level remote transfer rejections remained from remote peers.
- Next steps:
  1. Continue normal live monitoring; treat future non-Healthy `/health` as actionable.

## Update 2026-05-23 19:01:47Z

- Current task: Tester-reported Wishlist filter persistence fix deployed.
- Last activity:
  - Rebased the local Wishlist persistence commits on top of remote stable metadata `87561fe87`.
  - Pushed the fix to `snapetech/slskdN` as `81a2e3f44` and `8a244699a`.
  - Published Linux x64 payload `0.0.0-manual.20260523185819.8a244699a7d9`.
  - Built a replacement live Docker image from the previous live base to preserve installed tools and container hardening.
  - Deployed `slskdn:0.0.0-manual.20260523185819.8a244699a7d9` to `kspls0`; no release tags were created.
- Validation:
  - Passed: publish completed with existing C# warnings only.
  - Passed: live container image/app version match, Docker health is `healthy`, restart count is zero, Web root returns HTTP 200, and `/health` returns the existing `Degraded` state.
  - Passed: post-start journal sample after 13:01 CST showed `ERR=0`, `WRN=0`, `FTL=0`, and no exception matches.
- Next steps:
  1. Tester should retry editing a Wishlist filter, running a search, and revisiting the item without refreshing.

## Update 2026-05-23 16:21:02Z

- Current task: Tester-reported Wishlist filter persistence fix complete locally.
- Last activity:
  - Traced the filter field through the Wishlist UI/API/controller/service path.
  - Identified the reverting cause: wishlist search completion called whole-entity `Update(item)` after a network search, so stale in-flight/background item copies could overwrite newer user-edited filters.
  - Removed whole-entity stat saves and reloads queued background wishlist items before execution, skipping items disabled after the queue was built.
  - Added a regression test for editing a filter while a wishlist search is in flight.
  - Documented ADR-0001 gotcha `0z465` and committed the docs-only entry as `80bca6633`.
- Validation:
  - Passed: focused `WishlistServiceTests` (`1/1`).
  - Passed: full `dotnet test` (`4561/4561`: 68 smoke, 4215 unit, 278 integration).
  - Passed: `./bin/lint`.
- Next steps:
  1. Deploy/build when ready; no release tags were created.

## Update 2026-05-22 23:08:28Z

- Current task: Post-deploy log check and cleanup complete.
- Last activity:
  - Checked the live logs after the large page render deploy.
  - Found no errors or fatals; the only actionable warning noise was expected inbound search-response timeout logging and malformed overlay datagram warnings.
  - Patched expected search-response timeouts to return no response instead of throwing through Soulseek.NET, and moved malformed overlay datagram notices to information level.
  - Deployed manual image `slskdn:0.0.0-manual.20260522230601.ecde484ccc1d`; no release tags were created.
- Validation:
  - Passed: focused unit tests (`24/24`), `./bin/lint`, publish, app/image version match, Docker health is `healthy`, restart count is zero, Web UI returns HTTP 200, `/health` returns HTTP 200.
  - Passed: post-restart soak showed `ERR=0`, `WRN=0`, `FTL=0`; remaining exception strings are info-level remote transfer outcomes.
- Next steps:
  1. Continue normal use and report any UI route that still hangs.

## Update 2026-05-22 22:45:00Z

- Current task: Large Web page render audit deployed.
- Last activity:
  - Published current HEAD as manual version `0.0.0-manual.20260522224133.ef4950530284`.
  - Built a replacement Docker image from the previous live base and restarted the live service; no release tags were created.
- Validation:
  - Passed: image/app version match, Docker health is `healthy`, restart count is zero, Web UI returns HTTP 200, `/health` returned HTTP 200, and the fresh journal sample had `ERR=0`, `WRN=0`, `FTL=0`.
- Next steps:
  1. Retry the browser navigation paths that were locking up: Wishlist to other pages, Downloads, Lidarr, Search history, Collections, Contacts, and Shared with Me.

## Update 2026-05-22 22:41:02Z

- Current task: Large Web page render audit complete locally.
- Last activity:
  - Extended the live Wishlist paging fix pattern to Lidarr seeded wishlist rows, search history, collections, contacts, incoming shares, and share manifest items.
  - Changed Shared-with-me so the base share list paints before per-share collection details finish loading.
  - Documented the large-render gotcha in ADR-0001 and committed the docs-only entry as `39eca8305`.
- Validation:
  - Passed: frontend lint, focused Vitest slice (`63/63`), production Web build, `./bin/lint`, and `git diff --check`.
- Next steps:
  1. Deploy the new manual image to the live Docker host.
  2. Recheck live Web navigation and logs after restart.

## Update 2026-05-22 22:29:00Z

- Current task: Live Wishlist browser lockup fix deployed.
- Last activity:
  - Checked live logs after the newest deploy: daemon was healthy with no ERR/WRN/FTL counts, but `kspls0` had 8,301 wishlist rows.
  - Identified the browser lockup cause: Wishlist rendered every filtered row/card at once with Semantic UI controls and popups.
  - Added client-side Wishlist paging with 50/100/250/500 page-size options; filtering/sorting still operate over the full list, but only one page mounts.
  - Deployed manual image `slskdn:0.0.0-manual.20260522222650.d9f5d150680a`; no release tags were created.
- Validation:
  - Passed: frontend lint, focused Wishlist/acquisition Vitest (`16/16`), production Web build.
  - Passed: published apphost version, Docker image/version match, container health is `healthy`, restart count is zero, and Web UI returns HTTP 200.
- Next steps:
  1. User should hard-refresh the browser tab and retry Wishlist navigation; the page should mount/unmount with at most 100 rows by default.

## Update 2026-05-22 22:24:00Z

- Current task: Verify and deploy latest slskdN build to `kspls0` complete.
- Last activity:
  - Confirmed live `kspls0` was running `slskdn:0.0.0-manual.20260522211756.968ce0816474`, not the newest local code.
  - Reconstructed an old-shape transfer database from a live copy and tested `Z05292026_DownloadRequestMigration`: 69,584 request groups converted in 1.56s with zero unstamped download transfers.
  - Tested the `Migrator` wrapper path against the reconstructed database and confirmed it created a `transfers.pre-migration-backup.*.db` before conversion.
  - Fixed the integration-test `IDownloadService` stub for the new structured enqueue overload, documented the gotcha in ADR-0001, and committed the docs entry plus code fix.
  - Published and deployed manual image `slskdn:0.0.0-manual.20260522221917.1282619e3c84` to `kspls0`; no release tags were created.
- Validation:
  - Passed: migration copy test against 134,777 transfer rows and 69,584 request groups.
  - Passed: migrator backup wrapper test (`BackupCount=1`).
  - Passed: smoke tests (`67/67`), unit tests (`4215/4215`), integration tests (`278/278`).
  - Passed: `./bin/lint`, frontend lint, and `git diff --check`.
  - Passed: deployed apphost version, Docker image/version match, container health is `healthy`, restart count is zero, and Web UI returns HTTP 200.
- Next steps:
  1. Monitor live logs during normal use; `/health` remains the existing degraded operational state.

## Update 2026-05-22 20:09:27Z

- Current task: Wishlist hit-count/sort filters and download layout/details feature request complete locally.
- Last activity:
  - Added visible-hit Wishlist stats: visible hits, hidden locked hits, filtered-out hits, and raw responses.
  - Wishlist page now has persistent browser-side search, sort, new-results-only, enabled-only, auto-only, and has-results filters.
  - Main Wishlist hit/new-results counts now prefer visible hits instead of raw response counts.
  - Added `transfers.download.completed_layout` with `batch_id`, `uploader_folder`, `remote_folder`, and `flat` modes.
  - Completed transfer records now persist local filename and embedded audio tags for artist, album, title, track, and year; transfer table columns expose that context.
- Validation:
  - Passed: focused Wishlist/Download backend tests (`123/123`).
  - Passed: focused Wishlist/Download regression slice after adding hit-count/layout tests (`46/46`).
  - Passed: focused Wishlist Vitest (`8/8`).
  - Passed: full `dotnet test` (`4560/4560`: 67 smoke, 4215 unit, 278 integration).
  - Passed: `./bin/lint`.
  - Passed: frontend lint and production build.
  - Passed: `git diff --check`.
- Next steps:
  1. Deploy/build when ready; no tags were created.

## Update 2026-05-20 22:31:00Z

- Current task: Live Docker log check and rescue retry cooldown fix complete locally.
- Last activity:
  - Checked the live Docker install: running image/version is `slskdn:0.0.0-manual.20260520144909.9659b1bec14c`, container health is `healthy`, restart count is zero, and app version matches the image tag.
  - Sampled 8 hours of container logs: `ERR=0`, `WRN=0`, `FTL=0`.
  - Found a material noise/work bug: failed/no-op rescue attempts retried the same queued transfers every detector interval, producing about 112k `[RESCUE]` info lines in 8 hours and about 9.5k in the last 30 minutes.
  - Added per-transfer rescue attempt cooldown and prunes cooldown state once transfers leave the active set.
  - Fixed the detector to respect `rescue_mode.enabled` instead of only checking accelerated-download state.
  - Documented the rescue retry gotcha in ADR-0001 and committed that docs-only change as `98f7c84ff`.
- Validation:
  - Passed: focused `HostedServiceLifecycleTests` (`8/8`).
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore` with existing warnings only.
  - Passed: `./bin/lint`.
- Next steps:
  1. Deploy/build when ready; no tags were created.
  2. After deploy, recheck live logs to confirm `[RESCUE]` repeats drop to at most one attempt per active transfer per cooldown window.

## Update 2026-05-20 18:52:00Z

- Current task: Wishlist new-results badge and filter handoff tester request complete locally.
- Last activity:
  - Added wishlist search-result links that pass the wishlist filter as a `filter` query parameter so related search pages open with the same filter applied.
  - Search detail now prefers the URL `filter` parameter over the saved default filter and reapplies that initialization when navigating between search IDs.
  - Added per-item mark-viewed buttons for new-results badges in table and card views.
  - Improved empty linked-history messaging with a direct latest-search fallback link when a last search exists.
- Validation:
  - Passed: focused Wishlist/SearchDetail Vitest tests (`12/12`).
  - Passed: `cd src/web && npm run lint -- --max-warnings=0`.
  - Passed: `cd src/web && npm run build`.
  - Passed: `git diff --check`.
- Next steps:
  1. Deploy/build when ready; no tags were created.
  2. Tester should verify wishlist result links open with filters active and badges can be cleared without expanding history.

## Update 2026-05-20 14:58:00Z

- Current task: Wishlist/Search tester regression fixes deployed to the live Docker install.
- Last activity:
  - Built the Release web/backend payload with manual version `0.0.0-manual.20260520144909.9659b1bec14c`.
  - Published a release-shaped Linux x64 self-contained artifact and staged it on the live Docker host.
  - Built local image `slskdn:0.0.0-manual.20260520144909.9659b1bec14c` from the existing runtime base after clearing the old `/slskd` app payload.
  - Updated `slskd.service` to the new image tag and restarted the service.
- Validation:
  - Passed: staged apphost reports `0.0.0-manual.20260520144909.9659b1bec14c`.
  - Passed: running container uses `slskdn:0.0.0-manual.20260520144909.9659b1bec14c`.
  - Passed: Docker health is `healthy`, service restart count is zero, and Web UI returns HTTP 200 on host loopback.
  - Passed: `/health` returns the existing `Degraded` operational state.
  - Passed: fresh startup logs show application initialization complete and no fatal entries, search NULL exceptions, or unhandled request errors.
- Next steps:
  1. Tester should retry opening wishlist search results and editing filters/max-download limits against the live build.
  2. No tags were created.

## Update 2026-05-20 14:25:00Z

- Current task: Wishlist/Search tester regression fixes complete locally.
- Last activity:
  - Fixed persisted search response 500s by backfilling NULL/blank `Searches.Source` values to `manual` and adding the EF default.
  - Preserved `Source` and `WishlistItemId` for bridged searches so wishlist searches no longer appear as manual when Scene/Pod bridge is enabled.
  - Restored wishlist filters as filename/path include terms plus `-term` exclusions instead of extension-only matching; auto-download candidate selection now uses the same filter.
  - Persisted edited `MaxDownloads` values and made malformed Wishlist dates render as `Never`.
  - Documented the nullable migration gotcha in ADR-0001 and committed it as `9659b1bec`.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: focused C# Search/Wishlist tests (`30/30`).
  - Passed: `dotnet test` (`4555/4555`: 67 smoke, 4210 unit, 278 integration).
  - Passed: `cd src/web && npm run lint -- --max-warnings=0`.
  - Passed: focused Wishlist Vitest tests (`7/7`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Deploy/build when ready; no tags were created.
  2. Tester should verify old search result links no longer 500 after startup migration runs.

## Update 2026-05-19 04:10:00Z

- Current task: Audio metadata enrichment for transfers complete.
- Last activity:
  - Added `BitRate`, `SampleRate`, `BitDepth`, `Length` fields to internal Transfer model and API DTO.
  - `DownloadService.EnrichTransferMetadata()` reads audio properties from completed files via TagLib after the file move succeeds. Non-fatal on read failure.
  - `Extensions.WithSoulseekTransfer()` preserves metadata across transfer updates.
  - Frontend: `bitrate`, `samplerate`, `length` columns in transferColumns.js; cell rendering in TransferTable.
  - 4272 C# tests pass; frontend lint clean; production build succeeds.
  - Deployed to kspls0 as `slskdn:0.0.0-manual.20260519040543.ad4c8d4ed`.
- Next steps:
  1. Metadata appears for downloads after they complete (enrichment runs post file-move).
  2. Uploads don't get metadata enrichment (no local file to read); could be added later.

## Update 2026-05-19 03:45:00Z

- Current task: Column management + enrichment for Downloads/Uploads complete.
- Last activity:
  - Added 6 new columns: Type (extension), Folder (path), Elapsed, Remaining, Added, Done.
  - Implemented column management: drag-to-reorder headers, resize handles on edges, column chooser dropdown to show/hide.
  - Created `transferColumns.js` with localStorage persistence per direction.
  - Added `getExtension()` to util.js.
  - All 4272 C# tests pass; frontend lint clean; production build succeeds.
  - Deployed to kspls0 as `slskdn:0.0.0-manual.20260519033842.ad4c8d4ed`.
- Next steps:
  1. User can configure columns on the live instance.
  2. Optional future: add bitrate/duration columns (needs backend to include file metadata in transfer DTO).

## Update 2026-05-19 02:00:00Z

- Current task: Transfers UI compact layout complete (qBittorrent-like).
- Last activity:
  - Made Downloads/Uploads pages fill the entire space (no dead space around).
  - Shrunk rows, text, padding, and header.
  - `ROW_HEIGHT`: 40 → 28px; removed `MAX_VIEWPORT_HEIGHT` (640px cap) → dynamic `ResizeObserver` on grid element.
  - Grid columns narrowed; header flat (no `raised`); progress bars 6px.
  - Added `.view-transfer` CSS class (full-width, no padding, flex column).
  - Routes in `App.jsx` now use `class="view view-transfer"`.
  - All 4272 C# tests pass; frontend lint clean; production build succeeds.
- Next steps:
  1. User can validate on a live instance.
  2. Further compactness tweaks if desired.

## Update 2026-05-19 01:30:00Z

- Current task: Wishlist & Search UX plan fully complete.
- Last activity:
  - Sprint 4 (table/card view toggle, bulk operations, auto-disable after N downloads) already completed.
  - **Critical gap fixes**:
    - Idempotent schema migrations: `Source` + `WishlistItemId` on Searches table; `LastViewedAt` + `MaxDownloads` on WishlistItems table.
    - Search retention config: `SearchRetentionOptions` in `Options.cs` (`max_age_days`, `max_count`, `cleanup_interval_seconds`), `CleanupAsync` in `SearchService`, background job in `PruneSearches`, and `POST /api/v0/searches/cleanup` endpoint.
    - "Mark All as Viewed": backend endpoint, frontend API, and UI button on Wishlist header.
    - System → Search settings: added retention config inputs to `AdminPolicies` page.
    - Search count display: added "X / Y searches" count to the Searches page header (filtered vs total).
  - Documentation: added `docs/wishlist.md` guide, updated `config/slskd.example.yml` with retention config.
- Validation:
  - Passed: `dotnet build` (0 warnings, 0 errors).
  - Passed: `dotnet test` (4544 total: 67 smoke + 4199 unit + 278 integration, all passing).
  - Passed: `./bin/lint` clean.
  - Passed: `npm run build` clean.
  - Passed: `npx vitest run` (133 test files, 748 tests, all passing).
- Next steps:
  1. All Wishlist & Search UX plan items (Sprints 1–4 + gap fixes) are complete.
  2. User can validate the full wishlist/search UX overhaul on a live instance.
  3. 21 colour palettes ported from seerrng are now available in the theme picker.

## Update 2026-05-19 00:20:00Z

- Current task: Sprint 4 wishlist advanced features complete locally.
- Last activity:
  - Sprint 4.1: Added unified wishlist-search view mode — toggle between table view and card view. Card view shows each wishlist item as an expandable Segment with: enabled icon, search text, filter badge, unseen badge, request state label, stats row (last run, matches, runs, auto-download status, download progress), and quick action buttons (expand, view search, run, edit, delete). Expanded cards show search history and inline results (same as Sprint 3.3).
  - Sprint 4.2: Added bulk operations — checkboxes on table rows and cards, "Select All" checkbox in table header, bulk action bar appears when items are selected with Enable/Disable/Delete/Clear actions. Each action has a Popup tooltip.
  - Sprint 4.3: Added auto-disable after N successful downloads — `MaxDownloads` property (nullable int) on WishlistItem. When null (default), item disables after first auto-download (legacy behavior). When set (e.g., 5), item stays enabled until `TotalDownloadCount >= MaxDownloads`. Added input field in wishlist modal with tooltip. Card view shows "Downloads: X/Y" progress when auto-download is enabled with a maxDownloads limit.
- Validation:
  - Passed: `dotnet build` (0 warnings, 0 errors).
  - Passed: `dotnet test` (4544 total: 67 smoke + 4199 unit + 278 integration, all passing).
  - Passed: `./bin/lint` and `npm run build`.
- Next steps:
  1. User can validate Sprint 4 on a live instance.
  2. Update memory-bank tasks/progress docs.
  3. All Wishlist & Search UX plan items from 1779135555175-tidy-wolf.md are now complete.

## Update 2026-05-18 23:50:00Z

- Current task: Sprint 3 wishlist/search UX complete locally.
- Last activity:
  - Sprint 3.1: Fixed wishlist filter application — filter string (e.g., "flac OR mp3") is now parsed and applied as a `Func<File, bool>` file filter during search collection via `SearchOptions.WithFilters()`. Previously `filterResponses: true` was set but no actual filter function was passed, so the filter text was ignored during search and only affected auto-download.
  - Sprint 3.2: Added filter presets (FLAC, MP3, FLAC+MP3, FLAC+ALAC, Lossless, Any) as quick-select buttons in the wishlist modal. Added validation that rejects invalid filter syntax (only allows extensions and the OR keyword). Each preset button has a tooltip explaining what it filters.
  - Sprint 3.3: Enhanced wishlist-search detail view — added inline results expansion. Clicking the angle-down button on a search fetches and displays its responses inline (username, directory, file count, total size). Full search page link uses "external" icon. Results are limited to first 20 with a "showing X of Y" note.
- Validation:
  - Passed: `dotnet build` (0 warnings, 0 errors).
  - Passed: `dotnet test` (4544 total: 67 smoke + 4199 unit + 278 integration, all passing).
  - Passed: `./bin/lint` and `npm run build`.
- Next steps:
  1. User can validate Sprint 3 on a live instance.
  2. Update memory-bank tasks/progress docs.
  3. Sprint 4 (unified wishlist-search view, smart wishlist management) when ready.

## Update 2026-05-18 23:30:00Z

- Current task: Sprint 2 wishlist/search UX complete locally.
- Last activity:
  - Backend 2.1: Added `WishlistItemId` to `Search` record, wired through `SearchService.StartAsync` and `WishlistService.ExecuteWishlistSearchAsync`;
  - Backend 2.1: Added `GET /api/v0/wishlist/{id}/searches` endpoint;
  - Backend 2.2: Added `LastViewedAt` to `WishlistItem`, configured UTC conversion;
  - Backend 2.2: Added `PUT /api/v0/wishlist/{id}/mark-viewed` endpoint and `MarkViewedAsync` service method;
  - Frontend: Added `getSearches()` and `markViewed()` to `wishlist.js`;
  - Frontend: Updated `WishlistItemRow` to show unseen results badge, collapsible search history table, and auto-mark-viewed on expand;
  - Frontend: Added `handleMarkViewed` handler in main `Wishlist` component.
- Validation:
  - Passed: `dotnet build` (0 warnings, 0 errors).
  - Passed: `dotnet test` (4544 total: 67 smoke + 4199 unit + 278 integration, all passing).
  - Passed: `./bin/lint` and `npm run build`.
- Next steps:
  1. User can validate Sprint 2 on a live instance.
  2. Update memory-bank tasks/progress docs.
  3. Sprint 3 (wishlist bulk operations, search scheduling UI) when ready.

## Update 2026-05-18 21:00:00Z

- Current task: Sprint 1 wishlist/search UX complete.
- Last activity:
  - Finished all Sprint 1 items from plan 1779135555175-tidy-wolf.md;
  - Backend: `source` query param on `GET /api/v0/searches`, `cleanupSearches` in searches.js, source filter on `ISearchService.ListAsync`;
  - Frontend: source filter dropdown (All/Manual/Wishlist/Auto-Replace), source badges on SearchListRow, Clear All + Clear Old buttons;
  - Auto-replace panel already existed in TransfersHeader.jsx;
  - Search retention config already existed (`retention.search` in minutes, pruned every 5 min).
- Validation:
  - Passed: 146 backend search unit tests, 34 frontend search tests, `./bin/lint`, frontend lint.
- Next steps:
  1. User can validate Sprint 1 on a live instance.
  2. Sprint 2 (wishlist-search linking, unseen results tracking) when ready.

## Update 2026-05-18 00:45:00Z

- Current task: fix package-channel publication gaps found by package-smoke.
- Last activity:
  - found the active GitLab release path created the GitHub release but did not
    dispatch the GitHub package workflows that upload direct `.deb`/`.rpm` and
    publish PPA/COPR;
  - added GitLab dispatch for Linux archive, direct package, COPR, and PPA
    workflows after release creation;
  - added GitLab Docker Hub promotion to `snapetech/slskdn`;
  - changed PPA publishing to target both Jammy and Noble;
  - generated `debian/source/include-binaries` for the staged self-contained
    .NET payload so Launchpad source builds include the bundled runtime;
  - changed COPR publishing to create or modify the project with Fedora 43 and
    Rawhide chroots before building for those chroots.
- Validation:
  - Passed: workflow YAML parse.
  - Passed: package-smoke shell syntax and manifest syntax.
  - Passed: `git diff --check`.
- Next steps:
  1. Push these changes.
  2. Manually dispatch package publisher workflows for the current release tag,
     or let the next tag exercise the full GitLab release path.
  3. Re-run package-smoke against direct packages, PPA Jammy/Noble, COPR, and
     Docker Hub.

## Update 2026-05-17 17:55:00Z

- Current task: human-check private-message auto response complete locally.
- Last activity:
  - added default-off daemon-side matching for private messages that ask for
    proof the user is human or not a bot;
  - replies are sent through the existing conversation service, skipped for
    replayed/POD messages, and rate-limited per sender;
  - added shared Downloads and Messages toggles with local persistence and
    runtime option apply when remote configuration is enabled;
  - documented the persistent YAML shape in `config/slskd.example.yml`.
- Validation:
  - Passed: focused `ApplicationPrivateMessageAutoResponseTests` and
    `OptionsControllerTests` filter.
  - Passed: focused Transfers/Messaging/helper frontend tests.
  - Passed: `cd src/web && npm run lint -- --max-warnings=0`.
  - Passed: `./bin/lint` and `git diff --check`.
- Next steps:
  1. Try the toggle on a live daemon with remote configuration enabled.
  2. Tune match phrases if live PMs use wording outside the current
     human/bot-check pattern.

## Update 2026-05-16 03:05:00Z

- Current task: final pre-release cleanup pass complete locally.
- Last activity:
  - removed misleading placeholder wording from native job XML docs, playback
    feedback, content verification metadata, sharing manifests, mesh circuit
    comments, and the explicit network simulation no-op shell;
  - left intentionally gated experimental behavior in place;
  - re-ran build/lint and targeted stale-text scans.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj -c Release --no-restore`.
  - Passed: `./bin/lint`.
  - Passed: targeted placeholder scan and `git diff --check`.
- Next steps:
  1. Commit and push this comment/doc cleanup if the diff stays clean.
  2. Cut the release when ready.

## Update 2026-05-16 02:45:00Z

- Current task: project stink build-out pass complete locally.
- Last activity:
  - replaced Pod join/leave signature shape checks with real Ed25519
    canonical payload verification while preserving Off/Warn legacy behavior;
  - built out Synology SPK packaging so it publishes or consumes a real
    executable payload and packaging validation blocks placeholder payloads;
  - clarified stale branch/version docs without removing historical design
    context;
  - built out the remaining experimental implementation placeholder in
    `VirtualSoulfind/DisasterMode/MeshTransferService.cs` by mapping hashes
    through HashDb recording IDs into shadow-index peer hints.
- Validation:
  - Passed: focused PodCore/message/federation unit tests.
  - Passed: `dotnet build src/slskd/slskd.csproj -c Release --no-restore`.
  - Passed: Synology packaging validation and focused MediaCore frontend tests.
  - Passed: `./bin/lint` and `git diff --check`.
- Next steps:
  1. Commit and push the build-out patch.
  2. Continue scanning for stale implementation gaps after this patch lands.

## Update 2026-05-16 02:08:01Z

- Current task: broader log reclassification pass complete locally.
- Last activity:
  - reviewed current live warning/error shapes and the broader warning/error
    call-site list;
  - left security/auth/config warnings, explicit peer unavailability, and real
    degraded optional integration warnings at their current severity;
  - reclassified one additional shutdown-only search path: Soulseek runtime
    lock-disposal during application shutdown now completes the search as
    cancelled instead of logging a failed-search error;
  - preserved the same exception as noisy during normal runtime.
- Validation:
  - Passed: focused `SearchServiceLifecycleTests` (`14/14`).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the shutdown search diagnostic cleanup.
  2. Deploy/cut a release only when we want the accumulated log cleanups live.

## Update 2026-05-16 02:04:26Z

- Current task: follow-up omnibus Docker log snoop complete locally.
- Last activity:
  - sampled the current live Docker container and logs again;
  - confirmed the container remains healthy with zero restarts, API probes
    return 200, optional media tools are present, and no new fatal/crash,
    permission, missing-tool, or route/API pattern appeared;
  - found one additional low-risk log hygiene issue: successful second-chance
    transfer fallback was logged at warning level even though it is normal
    peer/client compatibility recovery;
  - changed the fallback attempt diagnostic to Info and the successful fallback
    diagnostic to Debug.
- Validation:
  - Passed: focused `ApplicationRuntimeInfoTests` and
    `MeshOverlayServerTests` (`9/9`).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the transfer fallback diagnostic cleanup.
  2. Deploy/cut a release only when we want the accumulated log cleanups live.

## Update 2026-05-16 01:54:45Z

- Current task: omnibus Docker log follow-up complete locally.
- Last activity:
  - sampled the freshly released omnibus Docker service logs;
  - confirmed the container is healthy with zero restarts, API probes return
    200, DHT bootstrapped, Soulseek recovered to Healthy after startup
    reconnect, and live downloads are completing;
  - fixed release-tagged prerelease SemVer builds being misclassified as local
    development builds;
  - changed Unix pod database startup handling to set `pods.db` permissions to
    `0600` instead of only logging a chmod instruction;
  - treated inbound mesh TLS handshake timeouts as expected handshake noise
    instead of warning with a stack.
- Validation:
  - Passed: focused `ApplicationRuntimeInfoTests` (`5/5`).
  - Passed: focused `MeshOverlayServerTests`.
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the startup diagnostics cleanup.
  2. Cut/deploy a new release only if we want these log cleanups live before
     the next planned release.

## Update 2026-05-16 00:25:00Z

- Current task: live Docker log follow-up and shutdown-noise cleanup complete
  locally.
- Last activity:
  - sampled current Docker/service logs after the latest manual all-tools
    deployment;
  - confirmed the container remains healthy with zero restarts and direct
    search-history calls remain fast;
  - verified post-deploy headless route/API crawl still has zero route,
    console, page, bad-response, or failed-request findings;
  - added shutdown-only handling for `ObjectDisposedException` from background
    search finalization and event record writes during intentional host
    restarts.
- Validation:
  - Passed: focused `SearchServiceLifecycleTests` (`11/11`).
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit/push the shutdown-noise cleanup.
  2. Deploy a fresh manual Docker image only if we want this log cleanup live
     before the next tagged release.

## Update 2026-05-15 23:35:00Z

- Current task: Docker optional-tools repeated-download fix is complete locally.
- Last activity:
  - added `packaging/docker/Dockerfile.all-tools` for cached local validation
    images;
  - made `install-optional-media-tools` cache-aware with
    `SLSKDN_KEEP_PACKAGE_CACHES=true`;
  - copied the current installer into the derived all-tools image before
    running it, so derived builds do not silently use an older base-image
    installer;
  - documented the derived-Docker installer gotcha.
- Validation:
  - Passed: `bash -n packaging/docker/install-optional-media-tools`.
  - Passed: one full cached all-tools Docker image build.
  - Partial: a second rebuild confirmed apt cache hits and cached Python
    Torch/CUDA wheels, then failed because the local Docker host had only about
    5 GB free.
- Next steps:
  1. Free local Docker storage before doing another all-tools rebuild or live
     image transfer.
  2. Commit/push the cache-aware Docker recipe when ready.

## Update 2026-05-15 22:55:55Z

- Current task: live Docker log investigation and low-risk hardening fixes are
  complete locally.
- Last activity:
  - sampled recent Docker/service logs from the live validation host;
  - fixed the actionable code/config issues behind the noise: uncapped
    search-list responses, unlimited default auto-retry churn, repeated
    mesh-health degraded warnings, expected Lidarr unavailability warnings,
    queue-position lookup noise, shutdown cancellation noise, and vague expected
    transfer failure wording;
  - documented the capped-list API gotcha.
- Validation:
  - Passed: focused touched-area unit tests (`110/110`).
  - Passed: full unit suite (`4147/4147`).
  - Passed: `dotnet build --no-restore`.
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the log-hardening patch.
  2. Build/deploy a manual Docker image for live validation only if the operator
     wants this patch running before the next tagged build.

## Update 2026-05-15 08:55:00Z

- Current task: `kspls0` direct search tabs, direct browse tabs, and tester
  remote-close download UI follow-up complete.
- Last activity:
  - deployed final manual build
    `0.0.0-manual.20260515085246.cc822043da4d` to `kspls0`;
  - verified direct `/browse?user=mysteryguest842` loads the SPA correctly and
    returns a controlled peer-unavailable 503 instead of an unhandled 500;
  - verified direct `/browse?user=xtdeck` succeeds live with HTTP 200 and
    renders `1320 directories, 15820 files`;
  - changed expected remote download failures such as `Remote connection closed`
    to display as `Peer unavailable` in the transfer list instead of generic
    `Error`.
- Validation:
  - Passed: focused backend Browse controller tests (`4/4`).
  - Passed: focused Browse UI tests (`5/5`).
  - Passed: focused Transfer UI tests (`4/4`).
  - Passed live: final app API reports
    `0.0.0-manual.20260515085246.cc822043da4d`, server state
    `Connected, LoggedIn`, service PID `4083276`, `NRestarts=0`.
  - Passed live headless: no direct browse/search deep-asset requests, no bad
    app asset responses, `mysteryguest842` reports peer unavailable cleanly, and
    `xtdeck` browse renders a directory/file count.
- Next steps:
  1. Tester can retry search-result tabs and Browse tabs on the live build.
  2. Treat `Remote connection closed` download failures as remote peer
     availability unless there is a reproducible local pattern across multiple
     peers/files.

## Update 2026-05-15 20:08:00Z

- Current task: Docker/container hardening pass complete locally; release
  workflow for `build-main-2026051519-slskdn.257` is still running remotely.
- Last activity:
  - reviewed the live Docker service and container isolation on the validation
    host;
  - hardened future Docker images by removing setuid/setgid bits from runtime
    OS utilities and changing `/.net` from `0777` to sticky `1777`;
  - documented the hardened Docker run pattern;
  - removed world-write permission from the live media mount roots while
    preserving group-write access for the media group.
- Validation:
  - Passed: `bash packaging/scripts/validate-packaging-metadata.sh`.
  - Passed: local Docker build `slskdn:hardening-test`.
  - Passed: image filesystem check showed no setuid/setgid files and sticky
    `/.net`.
  - Passed live: container can still write the download mount, and the library
    mount is not writable from inside the container.
- Next steps:
  1. Commit/push the Docker hardening patch when ready.
  2. Continue watching the `build-main-2026051519-slskdn.257` release workflow
     until it publishes or fails.
  3. Consider a slower maintenance-window recursive cleanup for any
     world-writable subdirectories under the live media tree.

## Update 2026-05-15 20:36:00Z

- Current task: Docker AppArmor and app-directory permission hardening complete
  locally.
- Last activity:
  - added an opt-in Docker AppArmor profile at
    `packaging/docker/apparmor/slskdn-docker`;
  - documented loading the profile and using
    `--security-opt apparmor=slskdn-docker` for hardened Docker launches;
  - added non-breaking app-directory permission warnings and opt-in
    `SLSKD_STRICT_APP_DIR_PERMISSIONS=true` handling that sets only the app
    directory itself to `0770`;
  - documented gotcha `0z437` after local validation caught the first strict
    mode implementation missing the capless `--user` startup path.
- Validation:
  - Passed: `bash -n packaging/docker/slskdn-container-start`.
  - Passed: `apparmor_parser -Q -T packaging/docker/apparmor/slskdn-docker`.
  - Passed: `bash packaging/scripts/validate-packaging-metadata.sh`.
  - Passed: local Docker image build `slskdn:apparmor-perms-test2`.
  - Passed local hardened container smoke with `--user`, read-only rootfs,
    tmpfs `/tmp` and `/run`, dropped capabilities, no-new-privileges, seccomp,
    strict app dir, and read-only shares.
  - Passed local API smoke: `/` HTTP 200, `/health` HTTP 200, protected
    `/api/v0/application` HTTP 401.
  - Passed local headless Chromium load of the Web UI root.
- Next steps:
  1. Push the hardening commit when ready.
  2. Load the AppArmor profile only on Docker hosts with AppArmor enabled; hosts
     without AppArmor should keep the other Docker hardening flags.

## Update 2026-05-15 20:46:00Z

- Current task: AppArmor enforced-container proof follow-up complete as a
  reusable smoke; local host cannot enforce AppArmor.
- Last activity:
  - confirmed the local Docker host reports seccomp/cgroupns only and
    `/sys/module/apparmor/parameters/enabled` is `N`;
  - added `packaging/scripts/run-docker-apparmor-smoke.sh` to load the profile
    and run the full hardened Docker/API/headless proof on AppArmor-enabled
    hosts;
  - documented the smoke command in `docs/docker.md`;
  - added packaging validation coverage for the smoke script.
- Validation:
  - Passed: `bash -n packaging/scripts/run-docker-apparmor-smoke.sh`.
  - Passed: `bash packaging/scripts/validate-packaging-metadata.sh`.
  - Local enforced smoke result: skipped with
    `SKIP: kernel AppArmor support is not enabled`, which is the expected
    result on this host.
- Next steps:
  1. Run `SLSKDN_DOCKER_IMAGE=<image> bash packaging/scripts/run-docker-apparmor-smoke.sh`
     on an AppArmor-enabled Docker host before considering AppArmor as a
     default-on deployment feature.

## Update 2026-05-15 20:44:00Z

- Current task: `kspls0` Docker log check complete; optional-tool warning spam
  fix pushed.
- Last activity:
  - inspected the live Docker container running
    `0.0.0-manual.20260515.c18c019a8`;
  - confirmed container health is healthy, no restarts, dropped capabilities,
    no-new-privileges, seccomp, read-only root filesystem, writable download
    mount, and read-only library mount;
  - reviewed current-process logs and found no fatal/error, permission-denied,
    read-only filesystem, remote-connection-closed, or invalid obfuscated-port
    signatures;
  - found frequent `[AudioSketch] ffmpeg not configured or missing` warnings
    because optional ffmpeg is absent in the container;
  - changed AudioSketch to log missing ffmpeg once per process instead of once
    per downloaded file and documented gotcha `0z438`.
- Validation:
  - Passed: focused `AudioSketchServiceTests`.
  - Passed: `./bin/lint`.
- Next steps:
  1. Include `643852d09` in the next manual Docker build/release if we want the
     quieter AudioSketch logging live on the validation host.

## Update 2026-05-15 20:52:00Z

- Current task: Docker runtime media prerequisites added locally.
- Last activity:
  - added `ffmpeg`, `yt-dlp`, and `libchromaprint-tools` to the Docker runtime
    image so default audio/SongID paths have `ffmpeg`, `ffprobe`, `yt-dlp`, and
    `fpcalc` available;
  - documented that heavier experimental tools remain derived-image/operator
    installs;
  - added packaging validation coverage and gotcha `0z439`.
- Validation:
  - Passed: `bash packaging/scripts/validate-packaging-metadata.sh`.
  - Passed: local Docker build `slskdn:docker-prereqs-test`.
  - Passed: image tool check for `ffmpeg`, `ffprobe`, `fpcalc`, `yt-dlp`, `jq`,
    `wget`, `tini`, and `gosu`.
  - Passed: image hardening check showed no setuid/setgid files and sticky
    `/.net`.
  - Passed: hardened local container smoke with Web/API/headless Chromium and
    no missing ffmpeg/tool log signatures.
  - Passed: `./bin/lint`.
- Next steps:
  1. Include `ac5c6379f` in the next manual Docker build/release if we want
     bundled media prerequisites live on the validation host.

## Update 2026-05-15 22:00:00Z

- Current task: live Docker UI/API validation and first burn-down pass in
  progress.
- Last activity:
  - ran headless live UI/API crawl against the Docker install;
  - found no UI route render failures, console errors, or page exceptions
    across the top-level application routes;
  - fixed duplicate telemetry metrics routing, native API-key auth mismatches,
    missing hashdb dashboard endpoint, and remote transfer rejection wording;
  - documented route-collision and transfer-wording gotchas.
- Validation:
  - Passed: `dotnet build --no-restore`.
  - Passed: `./bin/lint`.
  - Passed: backend unit test project during `dotnet test --no-restore`.
  - Known gap: full integration test project still fails five harness cases
    because the spawned test app config points at a non-existent Web content
    path.
- Next steps:
  1. Build and deploy a fresh manual Docker image with these fixes.
  2. Re-run the live headless crawl and log monitor against that image.
  3. Continue remaining TODOs from `memory-bank/tasks.md`.

## Update 2026-05-15 21:08:00Z

- Current task: Optional heavier SongID Docker tooling support complete
  locally.
- Last activity:
  - added `packaging/docker/Dockerfile.experimental-media` for opt-in OCR,
    Java, and Python runtime prerequisites;
  - documented the experimental media build path and derived-image guidance for
    Whisper, Demucs, SongRec, Panako, Audfprint, and C2PA tooling;
  - updated SongID capability reasons to tell Docker users which tool/file is
    missing and how to supply it without runtime self-install.
- Validation:
  - Passed: focused `SongIdCapabilityReporterTests`.
  - Passed: `bash packaging/scripts/validate-packaging-metadata.sh`.
  - Passed: `git diff --check`.
  - Passed: local Docker build `slskdn:experimental-media-test`.
  - Passed: image check for `tesseract`, `java`, `python3`, `pip3`, and no
    setuid/setgid files.
  - Passed: `./bin/lint`.
- Next steps:
  1. Push the optional media tooling commit.
  2. Build a new manual Docker image from the pushed head before deploying this
     path to a validation host.

## Update 2026-05-15 21:14:00Z

- Current task: Docker QUIC runtime support implemented locally.
- Last activity:
  - added Microsoft `libmsquic` to the default Docker image via a pinned
    architecture-specific package download;
  - documented QUIC as a bundled Docker runtime prerequisite;
  - documented gotcha `0z440` for missing native QUIC runtime dependencies in
    Linux containers.
- Validation:
  - Passed: local Docker build `slskdn:manual-quic-66d3fc39e`.
  - Passed: `ldconfig` sees `libmsquic.so.2`.
  - Passed: image tool check for `ffmpeg`, `ffprobe`, `fpcalc`, `yt-dlp`,
    `jq`, `wget`, `tini`, and `gosu`.
  - Passed: no setuid/setgid files and sticky `/.net`.
  - Passed: `bash packaging/scripts/validate-packaging-metadata.sh`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the Docker QUIC change.
  2. Deploy the QUIC-enabled manual image to the validation host and confirm the
     startup logs no longer report QUIC runtime support unavailable.

## Update 2026-05-15 08:36:00Z

- Current task: `kspls0` stale-browser/direct-tab fix deployed and verified with
  live headless browser coverage.
- Last activity:
  - confirmed the service-worker cleanup build did not fix direct search tabs by
    itself; headless Playwright hit `/searches/assets/...` 404s and an empty
    React root;
  - fixed the deep-link asset path by keeping Vite asset URLs root-absolute and
    retaining base-tag/url_base rewrite coverage;
  - committed `dc475fd9c` and deployed
    `0.0.0-manual.20260515083307.dc475fd9c371` after stopping the previous
    service on `kspls0`.
- Validation:
  - Passed: focused `CreateWebHtmlRewriteRules` unit tests (`2/2`).
  - Passed: production Web build emits root-absolute script/style assets.
  - Passed live raw check: direct `/searches/{id}` HTML references
    `/assets/index-DFWW6rUd.js`; `/searches/assets/index-DFWW6rUd.js` is 404,
    while `/assets/index-DFWW6rUd.js` is 200 JavaScript.
  - Passed live headless check: two fresh direct search tabs stayed on the
    route, both search API calls returned 200, 62 asset requests loaded, and
    there were no `/searches/assets/` requests, failed requests, bad asset
    responses, or console errors.
  - Passed live service check: `slskd.service` active, PID `3986614`,
    `NRestarts=0`, and the post-start journal scan had no error/fatal/exception,
    `Remote connection closed`, or `/searches/assets` matches.
- Next steps:
  1. Tester can retry direct search result tabs on the live build.
  2. Continue collecting separate failed-download evidence if the tester still
     sees `Remote connection closed`; this task only proves the tab/deep-link
     regression is fixed.

## Update 2026-05-15 08:28:42Z

- Current task: `kspls0` stale-browser/deep-tab fix deployed and verified.
- Last activity:
  - deployed `0.0.0-manual.20260515081737.9b5a8ba0df76` with the service-worker
    cleanup, then headless Playwright proved direct `/searches/{id}` tabs still
    failed because assets loaded from `/searches/assets/...`;
  - fixed root SPA fallback base-tag injection, documented ADR-0001 gotcha
    `0z433`, and committed `13f062dd2`;
  - rebuilt and deployed `0.0.0-manual.20260515082423.13f062dd254a` to
    `kspls0`.
- Validation:
  - Passed: focused `CreateWebHtmlRewriteRules` unit tests (`2/2`).
  - Passed live: authenticated Playwright opened two direct search detail tabs
    against `http://kspls0:5030/searches/{id}`; both stayed on the route, loaded
    search detail responses with HTTP 200, had zero asset 404s, zero failed
    requests, and no console errors.
  - Passed live service check: `slskd.service` active, PID `3937120`,
    `NRestarts=0`, running executable path is the new manual release, and recent
    logs had no error/fatal/exception or `/searches/assets` signatures.
- Next steps:
  1. Let the tester retry direct search result tabs on the live build.
  2. Continue collecting failed direct-download evidence separately if
     `Remote connection closed` remains reproducible; the current live sample
     shows some downloads completing on this build.

## Update 2026-05-15 08:12:21Z

- Current task: tester feedback triage partly complete.
- Last activity:
  - verified the stable release tag and Linux x64 release artifact are current,
    including bundled direct search-detail route code;
  - identified stale browser service workers as the likely reason fixed search
    result tabs can still look broken after upgrade;
  - retired the web service worker, added startup cleanup for old workers and
    caches, documented ADR-0001 gotcha `0z432`, and committed `0f111e324`.
- Validation:
  - Passed: `scripts/verify-release-artifacts.sh build-main-2026051501-slskdn.253`.
  - Passed: focused Web tests for service-worker/Search route behavior
    (`13/13`) and focused Web lint.
- Next steps:
  1. Ask tester to hard reload or use a browser profile after deploying a build
     containing `0f111e324`, then retry opening search-result tabs.
  2. For downloads, collect one failed transfer's username, remote filename,
     UI transfer state, and nearby daemon log lines; current evidence only shows
     expected remote peer closes, not a proven local packaging/stale-code issue.

## Update 2026-05-15 00:08:45Z

- Current task: extended `kspls0` live log cleanup and manual deploy complete.
- Last activity:
  - watched the live service longer after LVM returned and found one completed-download destination permission failure plus a search finalization disposal stack trace from the old process during deploy shutdown;
  - repaired group-write permissions on the live music destination tree;
  - fixed completed-download destination preflight and search cancellation-token capture;
  - deployed `0.0.0-manual.20260515000445.401ac6b42bb6` to `kspls0`.
- Validation:
  - Passed: focused `DownloadServiceTests` (`28/28`), focused `SearchServiceLifecycleTests` (`9/9`), `./bin/lint`, and `git diff --check`.
  - Passed live: `slskd.service` active/running with PID `1839564`, `NRestarts=0`, Web `5030` returned `200`, current symlink points at the manual release, and the executable reports `0.0.0-manual.20260515000445.401ac6b42bb6`.
  - Passed live fresh soak: no fatal/error, exception, permission-denied, SQLite, search-finalization, or warning markers in the sampled current-process window.
- Next steps:
  1. Continue monitoring tester traffic on the current manual build.
  2. Cut a release tag only if the user explicitly asks for release work.

## Update 2026-05-14 23:49:41Z

- Current task: live log cleanup and final manual deploy complete.
- Last activity:
  - checked current `kspls0` logs and found no recurrence of the HashDb or Lidarr stack traces;
  - fixed duplicate expected download timeout warnings, planned-shutdown cancellation warning stack traces, and local incomplete-directory permission failures escaping as unobserved task stack traces;
  - repaired group-write permissions on the live incomplete-download directory tree;
  - deployed `0.0.0-manual.20260514234659.e73f72f935b3` to `kspls0`.
- Validation:
  - Passed: focused `DownloadServiceTests` (`28/28`), focused Download/Wishlist slice (`46/46`), `./bin/lint`, and `git diff --check`.
  - Passed live: `slskd.service` active/running with `NRestarts=0`, Web `5030` returned `200`, current symlink points at the final manual release, and the executable reports `0.0.0-manual.20260514234659.e73f72f935b3`.
  - Passed live current-build scan: no fatal/error, permission-denied, SQLite, duplicate timeout, shutdown-cancellation, `HttpRequestException`, `ObjectDisposed`, or `MessageConnection` signatures.
- Next steps:
  1. Continue monitoring tester traffic on the current manual build.
  2. Cut a release tag only if the user explicitly asks for release work.

## Update 2026-05-14 22:33:00Z

- Current task: second slskdN/slskNet.Runtime red-team pass complete locally.
- Last activity:
  - rechecked slskdN process launch, path/write/delete, bridge download,
    streaming, outbound HTTP, and runtime parser/framing/obfuscated-transfer
    surfaces;
  - found no additional slskdN behavior fix that could be made conservatively;
  - compared vendored slskNet.Runtime with the standalone runtime checkout and
    found source drift only in dependency metadata;
  - synced standalone `/home/keith/Documents/code/slskNet.Runtime` to the
    vendored runtime `System.Memory` `4.6.3` baseline and committed it there as
    `09b16f96`;
  - noted that `/home/keith/Documents/code/slskdNet.Runtime` does not exist on
    this host.
- Validation:
  - Passed: slskdN guard scripts for outbound HTTP, path containment, async task
    observation, sensitive placeholders, and local identity leaks.
  - Passed: focused slskdN path/stream/bridge/moderation tests (`156/156`).
  - Passed: focused slskNet.Runtime parser/framing/obfuscation/listener/envelope
    tests in both the vendored and standalone runtime checkouts (`113/113`
    each).
  - Passed: standalone runtime `git diff --check` and local identity leak scan.
- Next steps:
  1. Push local commits in slskdN and slskNet.Runtime when ready.
  2. Cut a release tag only if the user explicitly asks for release work.

## Update 2026-05-14 22:07:00Z

- Current task: slskdN/slskNet.Runtime red-team pass complete locally.
- Last activity:
  - reviewed runtime message framing/parsing, decompression, protocol counts, and high-risk slskd HTTP/filesystem surfaces;
  - confirmed runtime wire guards already cover message lengths, buffered reads, decompressed payload size, string slices, picture payloads, and protocol collection counts;
  - fixed HTTP LLM moderation endpoint validation to use the shared DNS-aware outbound guard before sending any request;
  - added regression coverage proving unsafe local endpoints do not reach the HTTP handler;
  - documented ADR-0001 gotcha `0z427` and committed the fix as `36145133c`.
- Validation:
  - Passed: focused moderation unit tests (`13/13`).
  - Passed: focused slskNet.Runtime parser/framing tests (`67/67`).
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `./bin/lint`.
  - Passed: outbound HTTP guard, sensitive placeholder, and local identity leak scripts.
  - Passed: `git diff --check`.
- Next steps:
  1. Push local commits when ready.
  2. Cut a release tag only if the user explicitly asks for release work.

## Update 2026-05-14 21:25:57Z

- Current task: `kspls0` manual deploy and log-soak validation complete.
- Last activity:
  - built manual Linux x64 payload `0.0.0-manual.20260514212132.6b0e566d4144` from current HEAD;
  - deployed it over the package build on `kspls0` by updating `/usr/lib/slskd/current` and restarting `slskd.service`;
  - verified Web `5030`, authenticated application state, Soulseek login, VPN readiness, shares readiness, and completed downloads;
  - soaked logs after restart and confirmed the fixed HashDb unique-constraint warning and Lidarr `HttpRequestException` stack traces did not recur.
- Validation:
  - Passed before deploy: focused HashDb unit tests (`52/52`), focused Lidarr unit tests (`13/13`), and `./bin/lint`.
  - Passed live after deploy: `slskd.service` active with `NRestarts=0`, Web `5030` returned `200`, app API reported the manual version/executable path, Soulseek was `Connected, LoggedIn`, VPN was ready/connected, and shares were ready.
  - Passed live log soak: no fatal/error, HashDb unique-constraint, SQLite, `HttpRequestException` stack-trace, `ObjectDisposed`, or `MessageConnection` signatures; Lidarr connection failures now log concise warnings because the external Lidarr endpoint is refusing requests.
- Next steps:
  1. Continue monitoring tester traffic on the manual build.
  2. Cut the next stable release when the user explicitly asks for release/tag work.

## Update 2026-05-14 21:19:42Z

- Current task: second `kspls0` log cleanup pass complete locally.
- Last activity:
  - rechecked live logs and confirmed the HashDb `Peers.peer_id` SQLite warning had not recurred recently;
  - identified repeated Lidarr auto-import `HttpRequestException` stack traces for external connection refusals and 5xx responses;
  - changed Lidarr auto-import to classify expected external HTTP failures like wanted sync and log concise warnings without stack traces;
  - added focused classifier coverage and ADR-0001 gotcha `0z426`;
  - committed the fix as `88940a84a`.
- Validation:
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~LidarrImportServiceTests|FullyQualifiedName~LidarrSyncServiceTests" --no-restore` (`13/13`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Deploy or release a build containing `88940a84a` and `9bd3645d3` when ready.
  2. After deployment, recheck `kspls0` logs for Lidarr stack traces and HashDb peer unique-constraint warnings.

## Update 2026-05-14 20:29:58Z

- Current task: HashDb concurrent peer-creation warning fix complete locally.
- Last activity:
  - traced the live `kspls0` SQLite warning to `HashDbService.GetOrCreatePeerAsync` racing on concurrent passive peer-tracking events for the same username;
  - changed peer creation to use `INSERT OR IGNORE` and then reload the row;
  - added a concurrent same-peer regression test and ADR-0001 gotcha `0z425`;
  - committed the fix as `9bd3645d3`.
- Validation:
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter FullyQualifiedName~HashDbServiceTests --no-restore` (`52/52`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Deploy or release a build containing `9bd3645d3` when ready.
  2. After deployment, recheck `kspls0` logs for `UNIQUE constraint failed: Peers.peer_id`.

## Update 2026-05-14 16:02:46Z

- Current task: `kspls0` log cleanup and manual-build validation complete.
- Last activity:
  - inspected live `kspls0` logs and found a real current `MessageConnection` unobserved-task fatal, then classified that Soulseek.NET dispose race as expected teardown;
  - fixed live Swagger 500s from duplicate compatibility routes by hiding duplicate legacy job controllers from API Explorer and enabling Swagger conflict resolution;
  - corrected the manual deploy wrapper after a bad nested-quote rewrite dropped systemd's `--config` argument.
- Validation:
  - Passed: focused classifier tests (`40/40`), focused job/route tests (`22/22`), and serial backend builds.
  - Deployed `0.0.0-manual.20260514160011.57326ef1fe5b` to `kspls0`.
  - Live: service active with `NRestarts=0`, `/swagger/v0/swagger.json` returns `200` JSON, `/` returns `200`, Soulseek is connected, and downloads completed after restart.
  - Live: post-restart log scan found no `[ERR]`, `[FATAL]`, `MessageConnection`, remote-close, file create/move, directory cleanup, Lidarr timeout, or Swagger conflict signatures.
- Next steps:
  1. Let the final manual build soak under tester traffic.
  2. Cut the next stable release when the user is ready.

## Update 2026-05-14 14:50:21Z

- Current task: stable release tester feedback fixes deployed to `kspls0`.
- Last activity:
  - inspected `kspls0` logs after the release and found expected remote peer failures plus actionable local incomplete-file cleanup failures;
  - made completed-download directory cleanup best-effort and retried file creation once when sibling transfer cleanup removes the incomplete directory between mkdir/open;
  - added direct search-detail loading for `/searches/:id` so opening search result pages in fresh tabs does not depend on the initial search list snapshot.
  - deployed `0.0.0-manual.20260514145223.b781b218c2f0` to `kspls0` and restored the package-style root entrypoint wrapper so `ExecStart=/usr/lib/slskd/slskd` runs `/usr/lib/slskd/current/slskd`.
- Validation:
  - Passed: focused backend file/classifier tests (`68/68`).
  - Passed: focused Search/Browse Web tests (`15/15`) and focused Web lint.
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore` and `git diff --check`.
  - Passed live: `kspls0` authenticated application API reports the manual build, release-directory executable path, and `Connected, LoggedIn`.
  - Passed live: 90-second journal watch found no fixed download failure signatures.
- Next steps:
  1. Let testers retry downloads and search-result new-tab flows on `kspls0`.
  2. Cut a follow-up stable release if this manual build validates cleanly.

## Update 2026-05-13 17:57:08Z

- Current task: Program.cs decomposition complete for this pass; parity/reconciliation follow-up remains in progress.
- Last activity:
  - verified `Program.cs` is 404 lines and now owns entrypoint orchestration, public process state, command-line attribute binding, and the public log event/buffer bridge;
  - updated `docs/dev/feature-coherence-next-patches.md` to mark the Program split acceptance criterion complete for this pass;
  - left `RaiseLogEmitted` in Program because it protects the public `LogEmitted` event surface.
- Validation:
  - Documentation/status-only change after the last passing Program build, focused Program/log lifecycle tests, and lint.
- Next steps:
  1. Continue remaining MediaCore/G5 UX reconciliation forms and broader validation.
  2. Avoid moving command-line argument population out of `Program.cs`; ADR-0001 `0z406` documents why it must stay.

## Update 2026-05-13 18:00:54Z

- Current task: parity/reconciliation follow-up in progress.
- Last activity:
  - simplified MediaCore descriptor retrieval so cached single lookup remains the default path;
  - grouped cache bypass and batch DHT retrieval behind advanced disclosure with network-impact guidance;
  - covered the new advanced retrieval controls in the focused MediaCore component test.
- Validation:
  - Passed: `cd src/web && npm test -- --run src/components/System/MediaCore/index.test.jsx` (`4/4`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue scanning remaining non-pod MediaCore forms for controls that should be read-first or advanced-only.
  2. Keep Program.cs decomposition closed for this pass unless a concrete regression or wrapper reappears.

## Update 2026-05-13 18:03:06Z

- Current task: parity/reconciliation follow-up in progress.
- Last activity:
  - simplified MediaCore fuzzy matching so pairwise perceptual/text similarity remains the default path;
  - grouped candidate search behind advanced disclosure because it can scan registry entries;
  - covered the new advanced similarity search controls in the focused MediaCore component test.
- Validation:
  - Passed: `cd src/web && npm test -- --run src/components/System/MediaCore/index.test.jsx` (`4/4`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue scanning remaining non-pod MediaCore forms for any final read-first progressive disclosure cleanup.
  2. Keep Program.cs decomposition closed for this pass unless a concrete regression or wrapper reappears.

## Update 2026-05-13 18:04:47Z

- Current task: parity/reconciliation follow-up in progress.
- Last activity:
  - reconciled G5 status after the MediaCore form-disclosure cleanup;
  - recorded that the remaining work is broader guided flows, System panel grouping, and validation of advanced/admin labels rather than more obvious MediaCore mutation-form disclosure.
- Validation:
  - Documentation/status-only change after focused MediaCore component tests and focused Web lint passed.
- Next steps:
  1. Continue G5 with guided flows or System grouping.
  2. Run broader validation when the reconciliation pass is ready to close.

## Update 2026-05-13 18:08:19Z

- Current task: parity/reconciliation follow-up in progress.
- Last activity:
  - labeled System admin and experimental panels directly in the tab menu;
  - added focused System component coverage for the labels.
- Validation:
  - Passed: `cd src/web && npm test -- --run src/components/System/System.test.jsx` (`2/2`).
  - Passed: `cd src/web && npm run lint -- src/components/System/System.jsx src/components/System/System.test.jsx`.
- Next steps:
  1. Continue G5 guided-flow work or run broader validation for the reconciliation pass.
  2. Keep Program.cs decomposition closed for this pass unless a concrete regression or wrapper reappears.

## Update 2026-05-13 18:10:58Z

- Current task: parity/reconciliation follow-up in progress.
- Last activity:
  - updated the route/UI parity matrix for MediaCore form-disclosure completion and System admin/experimental label maintenance;
  - updated the System surfaces guide to document that those navigation labels should stay current.
- Validation:
  - Documentation/status-only change after focused System component tests and focused Web lint passed.
- Next steps:
  1. Run broader reconciliation validation or continue G5 guided-flow work.
  2. Keep Program.cs decomposition closed for this pass unless a concrete regression or wrapper reappears.

## Update 2026-05-13 18:17:19Z

- Current task: parity/reconciliation validation pass mostly complete.
- Last activity:
  - ran full frontend tests, frontend production build, full backend tests, repo lint, and remediation baseline;
  - remediation baseline passed all substantive checks and stopped only at the release branch sync guard because local `main` is ahead of `origin/main`.
- Validation:
  - Passed: `cd src/web && npm test` (`131/131` files, `716/716` tests).
  - Passed: `cd src/web && npm run build`.
  - Passed: `dotnet test --no-restore` (`67/67` smoke, `4056/4056` unit, `276/276` integration).
  - Passed: `./bin/lint`.
  - Partial: `npm run check:remediation` reached only the release branch sync guard.
- Next steps:
  1. Push local commits before any release-tag validation, if the user wants release work.
  2. Run live/E2E checks only if needed for the target release.

## Update 2026-05-13 18:20:52Z

- Current task: parity/reconciliation documentation cleanup complete locally.
- Last activity:
  - updated the feature parity plan baseline, implementation order, current snapshot, and completion pass status to match the completed validation pass;
  - updated the remediation completion report with MediaCore/System UI safety work and validation results.
- Validation:
  - Documentation/status-only change after the completed G6 validation pass.
- Next steps:
  1. Push local commits before any release-tag validation, if the user wants release work.
  2. Run live/E2E checks only if needed for the target release.

## Update 2026-05-13 18:23:23Z

- Current task: route/UI parity reconciliation complete locally.
- Last activity:
  - updated DHT/bootstrap/NAT from partial System visibility to mostly covered via System Network/Mesh posture;
  - updated VirtualSoulfind v2 from gap to mostly covered through Source Providers capability/risk/priority catalog and Search provider gating.
- Validation:
  - Documentation/status-only change; existing focused Source Providers/Search coverage already exercises the described surfaces.
- Next steps:
  1. Run focused Source Providers/Search tests if more confidence is needed after this doc update.
  2. Push local commits before any release-tag validation, if the user wants release work.

## Update 2026-05-13 16:56:30Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved ASP.NET hardening validation, builder configuration, service registration, DI build, pipeline setup, no-start handling, and run lifecycle into `Bootstrap/StartupWebApplicationRunner`;
  - updated ADR-0001 gotcha `0z403` for the extracted runner logger ambiguity;
  - `Program.cs` is down to 680 lines.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`58/58`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with the remaining compatibility wrappers/static state.
  2. Continue scanning MediaCore for remaining read-first reconciliation work.

## Update 2026-05-13 16:49:30Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved configured startup identity, system, directory, compatibility-warning, and logging-target diagnostics into `Bootstrap/StartupDiagnostics`;
  - preserved SQLite initialization ordering by splitting identity logging before SQLite and configuration usage logging after SQLite;
  - `Program.cs` is down to 754 lines.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`58/58`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition by extracting the ASP.NET bootstrap/build/run flow.
  2. Continue scanning MediaCore for remaining read-first reconciliation work.

## Update 2026-05-13 16:44:30Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved startup configuration provider loading, binding, raw security-section diagnostics, and validation into `Bootstrap/StartupConfiguration`;
  - updated ADR-0001 gotcha `0z403` for the extracted validation extension namespace;
  - `Program.cs` is down to 784 lines.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`58/58`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition by extracting another cohesive startup-flow segment.
  2. Continue scanning MediaCore for remaining read-first reconciliation work.

## Update 2026-05-13 16:38:20Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved startup application-directory resolution into `Bootstrap/StartupApplicationDirectoryResolver`;
  - moved derived default directory calculation and default directory validation into the same helper;
  - `Program.cs` is down to 807 lines.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`58/58`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition by extracting another cohesive startup-flow segment.
  2. Continue scanning MediaCore for any remaining read-first reconciliation work.

## Update 2026-05-13 16:34:30Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved primitive startup command-mode handling into `Bootstrap/StartupCommandMode`;
  - Program now delegates version/help/env output, certificate generation, and secret generation to that helper;
  - `Program.cs` is down to 816 lines.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`58/58`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition by extracting another cohesive startup-flow segment.
  2. Continue scanning MediaCore for any remaining read-first reconciliation work.

## Update 2026-05-13 16:31:10Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - rewired Web pipeline setup to call extracted web rewrite, antiforgery recovery, and startup file-system helpers directly;
  - rewired experimental mesh service registration to call the QUIC overlay factory directly;
  - kept Program compatibility wrappers for tests and existing callers.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`58/58`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with small compatibility-wrapper splits.
  2. Continue scanning remaining MediaCore non-pod controls for read-first progressive disclosure.

## Update 2026-05-13 16:23:00Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore retrieval management so cache clearing is advanced-only after stats review;
  - simplified MediaCore dashboard management so global stats reset is advanced-only after dashboard review;
  - kept unrelated local edits in workflows, `global.json`, packaging, `StartupLogging`, and DownloadService files separate.
- Validation:
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
- Next steps:
  1. Continue scanning for any remaining non-pod MediaCore admin controls that should be advanced-only.
  2. Continue Program.cs decomposition with small compatibility-wrapper splits when no UX slice remains.

## Update 2026-05-13 16:21:10Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved owned physical file provider construction into `Bootstrap/StartupFileSystem`;
  - kept Program's compatibility wrapper for current tests and call sites;
  - updated ADR-0001 gotcha `0z403` after the extraction repeated the provider namespace trap.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests" --no-restore` (`46/46`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with another small compatibility-wrapper split or switch back to remaining MediaCore reconciliation forms.
  2. Keep unrelated local edits in workflows, `global.json`, packaging, `StartupLogging`, and DownloadService files separate.

## Update 2026-05-13 16:13:40Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved startup mutex-name construction into `Bootstrap/StartupSingleInstance`;
  - moved unobserved-task exception classification into `Bootstrap/StartupExceptionClassifier`;
  - documented ADR-0001 gotcha `0z404` after parallel dotnet validation caused a build-task generated-file lock.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~SharedEventEmitterTests" --no-restore` (`46/46`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with another small compatibility-wrapper split.
  2. Run backend validation serially to avoid `tools/slskd.BuildTasks` output locks.

## Update 2026-05-13 16:08:10Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore ContentID registration so resolve/validate stay first and registry mutation sits behind advanced disclosure;
  - simplified MediaCore metadata import so export/conflict-analysis stay first and import sits behind advanced disclosure;
  - kept unrelated local edits in workflows, `global.json`, packaging, and DownloadService files separate from this work.
- Validation:
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
- Next steps:
  1. Continue simplifying any remaining non-pod MediaCore mutation-heavy forms.
  2. Keep unrelated local edits in workflows, `global.json`, packaging, and DownloadService files separate from reconciliation commits.

## Update 2026-05-13 16:02:24Z

- Current task: Bas Arch/download report handled; Program.cs decomposition still has unrelated local edits in progress.
- Last activity:
  - lowered the .NET SDK floor from `10.0.202` to `10.0.100` across `global.json` and workflow pins so Arch systems with SDK `10.0.104` can build the source package;
  - fixed direct-download retry aggregate timeout classification so repeated remote `TimeoutException`s mark transfers as `TimedOut` instead of generic `Errored`;
  - aligned Snap packaging metadata and checksum to stable release `2026051221-slskdn.247`;
  - fixed a recursive Serilog sink startup crash found during local artifact smoke and documented ADR-0001 gotcha `0z355`;
  - built manual deploy payload `0.0.0-manual.20260513161219.eff21f143493` at `/tmp/slskdn-deploy-current` and `/tmp/slskdn-deploy-current.tar.gz`;
  - local isolated smoke reached `/api/v0/application` and reported the manual version.
  - documented ADR-0001 gotcha `0z354` and committed that documentation separately.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~DownloadServiceTests|FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`23/23`).
  - Passed: `bash scripts/check-codeql-dotnet-version.sh`.
  - Passed: `bash packaging/scripts/validate-packaging-metadata.sh`.
  - Passed: `./bin/lint`.
  - Passed: `git diff --check`.
  - Blocked: `kspls0` deploy cannot proceed because SSH times out during banner exchange or resets pre-auth with `Not allowed at this time`; HTTP `:5030` accepts then times out.
- Next steps:
  1. Recover `kspls0` host access through reboot/local console/out-of-band service recovery.
  2. Copy `/tmp/slskdn-deploy-current.tar.gz`, stop `slskd.service`, install under `/usr/lib/slskd/releases/`, repoint `/usr/lib/slskd/current`, restart, and verify HTTP/API/version.
  3. Run live download timeout validation once the service is reachable.

## Update 2026-05-13 16:04:20Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore descriptor publishing so retrieval/statistics remain the default path;
  - grouped descriptor publish, batch publish, descriptor update, and republish-expiring controls behind advanced disclosure;
  - kept unrelated local edits in workflows, `global.json`, and DownloadService files separate from this work.
- Validation:
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
- Next steps:
  1. Continue simplifying additional advanced MediaCore forms into task-focused panels or progressive disclosure.
  2. Keep unrelated local edits in workflows, `global.json`, and DownloadService files separate from reconciliation commits.
  3. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 16:01:27Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved assembly/informational version normalization, semantic/full version strings, canary/development flags, and executable-path lookup into `Bootstrap/ApplicationRuntimeInfo`;
  - preserved Program's public compatibility fields/properties used by state, telemetry, integration, and API surfaces.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ApplicationControllerTests|FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`58/58`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Re-scan MediaCore pod workflows for any remaining advanced forms that should move behind progressive disclosure.
  2. Keep unrelated local edits in `global.json` and `DownloadService.cs` separate from Program decomposition commits.
  3. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 15:54:58Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved SQLitePCL provider initialization and serialized threading fail-fast validation into `Bootstrap/StartupSqlite`;
  - kept Program's private SQLite startup wrapper intact.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`47/47`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Re-scan the parity/reconciliation plan for any non-Program follow-ups still open.
  2. Consider whether remaining Program public/static state should stay as the process bootstrap surface.
  3. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 15:52:36Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved command-line argument help, environment-variable listing, and startup logo rendering into `Bootstrap/StartupConsoleOutput`;
  - kept Program's private wrappers for the existing primitive CLI flow.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`47/47`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with remaining SQLite/runtime identity helpers if worth extracting.
  2. Re-scan the parity/reconciliation plan for any non-Program follow-ups still open.
  3. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 15:49:21Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved global Serilog setup into `Bootstrap/StartupLogging`;
  - moved process-exit, unhandled-exception, and unobserved-task telemetry wiring into `Bootstrap/StartupShutdownTelemetry`;
  - kept Program's public `LogEmitted`/`LogBuffer` surface and private `RaiseLogEmitted` wrapper intact.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~SharedEventEmitterTests|FullyQualifiedName~ApplicationLifecycleTests|FullyQualifiedName~LogsControllerTests" --no-restore` (`47/47`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with remaining primitive CLI/help/version/SQLite helpers if worth extracting.
  2. Re-scan the parity/reconciliation plan for any non-Program follow-ups still open.
  3. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 14:53:23Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved QUIC overlay/data client construction, overlay server construction, and standalone UDP overlay selection into `Mesh/Overlay/QuicOverlayFactory`;
  - kept existing Program wrappers for current mesh bootstrap call sites.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests" --no-restore` (`36/36`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with a deliberate logging/shutdown telemetry split, or leave it until the public `LogEmitted`/`LogBuffer` coupling is designed.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 14:50:52Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved startup directory validation, missing config-file recreation, and generated X509 certificate export into `Bootstrap/StartupFileSystem`;
  - kept private Program wrappers for existing startup call sites.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests" --no-restore` (`36/36`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with the next bounded helper cluster, likely logging/shutdown telemetry or remaining QUIC overlay construction wrappers.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 13:54:37Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved startup configuration provider composition into `Configuration/SlskdConfigurationBuilderExtensions`;
  - kept Program responsible for choosing the configuration file, reload posture, volatile overlay source, and startup logger.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests" --no-restore` (`36/36`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with the next bounded helper cluster, likely certificate generation, shutdown/logging helpers, or remaining QUIC overlay construction wrappers.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 13:49:54Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved stale antiforgery token detection, stale XSRF cookie stripping, cookie clearing, and retry-on-key-ring-mismatch token issuance into `Core/Security/AntiforgeryCookieRecovery`;
  - kept existing `Program` wrappers for current call sites and tests;
  - documented and separately committed the extracted-helper namespace qualification gotcha.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests" --no-restore` (`36/36`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with the next bounded helper cluster, likely configuration provider setup, certificate generation, or shutdown/logging helpers.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 13:45:26Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved app-relative write-path resolution into `Configuration/AppPathResolver`;
  - moved web HTML asset rewrite rule construction into `Bootstrap/WebHtmlRewriteRules`;
  - kept existing `Program` wrappers for current call sites and tests.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests" --no-restore` (`36/36`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue Program.cs decomposition with the next bounded helper cluster, likely antiforgery-cookie helpers or configuration provider setup.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 04:09:53Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved initial Soulseek client listener, transfer-limit, diagnostics, and obfuscation option construction into `Soulseek/SoulseekClientOptionsFactory`;
  - kept `Program.CreateInitialSoulseekClientOptions(...)` as the internal compatibility wrapper.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ProgramExpectedNetworkExceptionTests" --no-restore` (`38/38`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Re-scan `Program.cs` for remaining bounded helper clusters such as path/HTML rewrite helpers, shutdown/logging, or antiforgery helpers.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 04:06:02Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved expected Soulseek network/disconnect exception classification into `Soulseek/SoulseekNetworkExceptionClassifier`;
  - kept `Program.IsExpectedSoulseekNetworkException(...)` as the internal compatibility wrapper;
  - documented and separately committed the `slskd.Soulseek` namespace shadowing gotcha.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests|FullyQualifiedName~ProgramExpectedNetworkExceptionTests" --no-restore` (`38/38`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Re-scan `Program.cs` for remaining bounded helper clusters such as shutdown/logging, path/HTML rewrite helpers, or antiforgery helpers.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 03:59:39Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved legacy configuration compatibility warning parsing into `Configuration/ConfigurationCompatibilityWarnings`;
  - updated `ProgramPathNormalizationTests` to call the new helper owner;
  - documented the test-callsite ownership gotcha in ADR-0001 and committed it separately.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~ProgramPathNormalizationTests" --no-restore` (`36/36`).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue reducing remaining `Program.cs` helper clusters where ownership is clear.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 03:54:13Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved application run/lifecycle hooks, E2E server probes, and LAN discovery advertising start/stop into `Bootstrap/ApplicationRunExtensions`;
  - left `Program.cs` with `app.RunSlskdApplication(OptionsAtStartup)`.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `./bin/lint`.
- Next steps:
  1. Re-scan `Program.cs` for any remaining bounded startup/lifecycle blocks worth extracting.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 03:50:55Z

- Current task: Program.cs decomposition follow-up in progress.
- Last activity:
  - moved web listener/Kestrel setup into `Bootstrap/WebHostConfigurationExtensions`;
  - left `Program.cs` with `builder.ConfigureSlskdWebHost(OptionsAtStartup, AppName)`.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `./bin/lint`.
- Next steps:
  1. Re-scan `Program.cs` for any remaining bounded startup/lifecycle blocks worth extracting.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 03:47:53Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Message Routing so deduplication checks and routing stats appear first;
  - grouped message send, mark-seen, and cleanup controls behind advanced disclosure;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
- Next steps:
  1. Re-scan MediaCore pod workflows for any remaining mutation controls that should be advanced-only.
  2. Continue Program.cs decomposition if another bounded startup block is worth extracting.
  3. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:40:40Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Membership Management so get/verify/statistics appear first;
  - grouped membership publishing, role/ban changes, and cleanup behind advanced disclosure;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely message routing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:38:43Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore PodCore DHT Publishing so metadata retrieval and publishing stats appear first;
  - grouped DHT publish/unpublish controls behind advanced disclosure;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely membership management or message routing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:36:16Z

- Current task: Program.cs decomposition and parity/reconciliation follow-up in progress.
- Last activity:
  - moved database migration, optional audio reanalysis, and forced construction of event-subscriber integrations into `Bootstrap/ApplicationStartupTaskExtensions`;
  - left `Program.cs` with `app.RunSlskdStartupTasks(OptionsAtStartup)`.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely DHT pod publishing or message routing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:32:50Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Message Storage so stats/search remain the default path;
  - grouped local cleanup, search-index rebuild, and vacuum controls behind advanced disclosure;
  - simplified Pod Message Backfill so stats/timestamp review remains first and sync is behind advanced disclosure.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely DHT pod publishing or message routing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:31:03Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Content Linking so content search/validation remains the default path;
  - grouped content-linked pod creation behind advanced disclosure after validation;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely message storage cleanup/backfill sync or DHT pod publishing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:29:32Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Opinion Management so review, statistics, aggregation, recommendations, and affinity lookup appear first;
  - grouped opinion publishing and affinity recalculation controls behind advanced disclosure;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely content-linked pod creation or message storage cleanup.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:27:56Z

- Current task: Program.cs decomposition and parity/reconciliation follow-up in progress.
- Last activity:
  - moved E2E hosted-service tracing and host startup timeout/concurrency options into `Bootstrap/HostDiagnosticsServiceCollectionExtensions`;
  - left `Program.cs` with a single `.AddSlskdHostDiagnostics()` call after runtime service registration.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely opinion publishing or content-linked pod creation.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:18:57Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Channel Management so channel load/review appears first;
  - grouped channel create, edit, and delete controls behind advanced disclosure;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely opinion publishing or content-linked pod creation.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:17:30Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Message Signing so verification/statistics appear first;
  - grouped private-key signing and key generation controls behind advanced disclosure;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely channel management or opinion publishing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:15:08Z

- Current task: Program.cs decomposition and parity/reconciliation follow-up in progress.
- Last activity:
  - moved MediaCore publisher, capability bridge, and DHT rendezvous registrations into `Bootstrap/CapabilitiesAndRendezvousServiceCollectionExtensions`;
  - reduced `Bootstrap/ExperimentalFeatureGraphServiceCollectionExtensions` to a delegation-only coordinator.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `./bin/lint`.
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~Capability|FullyQualifiedName~DhtRendezvous|FullyQualifiedName~MeshNeighbor|FullyQualifiedName~ContentPublisher" --no-restore` (`146/146`).
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely message signing, channel management, or opinion publishing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 02:09:41Z

- Current task: Program.cs decomposition and parity/reconciliation follow-up in progress.
- Last activity:
  - moved MediaCore, PodCore, content-domain provider, and peer-reputation registrations into `Bootstrap/MediaCorePodServiceCollectionExtensions`;
  - moved mesh, DHT, overlay, transport, realm, governance, gossip, social-federation, privacy, NAT, and service-fabric registrations into `Bootstrap/ExperimentalMeshServiceCollectionExtensions`;
  - reduced the remaining broad experimental graph coordinator to 105 lines;
  - updated feature-coherence and parity tracking docs.
- Validation:
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `./bin/lint`.
  - Passed: `dotnet test tests/slskd.Tests.Integration/slskd.Tests.Integration.csproj --filter "FullyQualifiedName~PodCore|FullyQualifiedName~MediaCore|FullyQualifiedName~Mesh" --no-restore` (`80/80`).
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter "FullyQualifiedName~PodCore|FullyQualifiedName~MediaCore|FullyQualifiedName~Mesh|FullyQualifiedName~Dht" --no-restore` (`1734/1734`).
- Next steps:
  1. Split the remaining capability/DHT-rendezvous/media-publisher coordinator if more Program decomposition is needed.
  2. Continue simplifying remaining MediaCore pod forms under the reconciliation plan.

## Update 2026-05-13 02:12:19Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - simplified MediaCore Pod Join/Leave so pending-request review appears first;
  - grouped signed join, leave, and approval JSON publishing controls behind advanced disclosure;
  - updated parity plan, changelog, tasks, and progress notes.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
- Next steps:
  1. Continue simplifying remaining MediaCore pod mutation-heavy forms, likely message signing, channel management, or opinion publishing.
  2. Run release-target validation when the branch is ready; remediation sync still requires pushing local commits first.

## Update 2026-05-13 01:50:00Z

- Current task: parity/reconciliation list follow-up in progress.
- Last activity:
  - regenerated `docs/system-surfaces-current.md`;
  - simplified MediaCore pod discovery so read-only discovery actions come first and public registry mutation controls are grouped under advanced disclosure;
  - updated `scripts/check-outbound-http-guards.sh` to follow the new bootstrap ownership for guarded HTTP client registration;
  - documented the bootstrap-check ownership gotcha in ADR-0001 and committed it separately.
- Validation:
  - Passed: `cd src/web && npm test -- System/MediaCore/index.test.jsx` (`3/3`).
  - Passed: `cd src/web && npm run lint -- src/components/System/MediaCore/index.jsx src/components/System/MediaCore/index.test.jsx`.
  - Partial: `npm run check:remediation` passes all focused remediation checks, then fails at release branch sync because local `main` is ahead of `origin/main`.
- Next steps:
  1. Commit the parity/reconciliation slice.
  2. Continue simplifying other MediaCore pod workflow forms or push the current commits before rerunning the release-sync-inclusive remediation baseline.

## Update 2026-05-13 01:33:49Z

- Current task: Program.cs decomposition multi-source split completed.
- Last activity:
  - moved multi-source transfer, swarm, tracing, warm-cache, playback-priority, and job-manifest registrations into `Bootstrap/MultiSourceFeatureServiceCollectionExtensions`;
  - kept the broad experimental graph delegating to the new bounded bootstrap module;
  - fixed partial integration test hosts to register a test `IFeatureGate` for controllers with `[FeatureGate(...)]` filters;
  - documented the partial-host feature-gate gotcha in ADR-0001 and committed it separately;
  - updated status, backlog, tasks, progress, and changelog tracking docs.
- Validation:
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter RoomsControllerTests --no-restore` (`9/9`).
  - Passed: `cd src/web && npm test -- Messaging.test.jsx` (`10/10`).
  - Passed: focused integration rerun for feature-gated VirtualSoulfind/MultiSource routes (`36/36`).
  - Passed: `dotnet build src/slskd/slskd.csproj --no-restore`.
  - Passed: `dotnet test` (`67/67` smoke, `4050/4050` unit, `276/276` integration).
  - Passed: `./bin/lint`.
- Next steps:
  1. Continue subdividing the remaining broad experimental feature graph by bounded context.

## Update 2026-05-12 23:41:45Z

- Current task: live room discovery polling regression fix in progress.
- Last activity:
  - confirmed `kspls0` room-list and room-join calls both timed out after repeated room-list polling;
  - removed available-room discovery from general Messaging V2 hydration;
  - made the backend room-list endpoint return empty optional data on SDK timeout.
- Validation:
  - Pending: focused Web Messaging tests.
  - Pending: focused RoomsController tests.
  - Pending: deploy and live API join check on `kspls0`.
- Next steps:
  1. Run focused tests, commit, push, rebuild, redeploy, and verify room join/list behavior live.

## Update 2026-05-12 23:32:51Z

- Current task: room list reconnect hardening completed.
- Last activity:
  - documented the optional room-list reconnect gotcha in ADR-0001 and committed it separately;
  - changed the available-room API to return an empty list until Soulseek is logged in;
  - added focused controller coverage for the disconnected path.
- Validation:
  - Passed: `dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj --filter RoomsControllerTests --no-restore` (`5/5`).
- Next steps:
  1. Commit, push, rebuild, and redeploy the aggregate to `kspls0`.

## Update 2026-05-12 23:27:09Z

- Current task: Messaging V2 room search/join fix completed.
- Last activity:
  - documented the room-list gotcha in ADR-0001 and committed it separately;
  - replaced the raw available room list in the Messages sidebar with a search-first room picker;
  - kept join/create on the same typed-room control while preserving joined room rows.
- Validation:
  - Passed: focused Messaging Web tests (`6/6`).
  - Passed: ESLint for `MessagingV2.jsx` and `Messaging.test.jsx`.
- Next steps:
  1. Build, commit, push, and deploy the aggregate to `kspls0` for manual testing.

## Update 2026-05-12 23:20:38Z

- Current task: feature-coherence/de-hallucination first slice completed.
- Last activity:
  - replaced overclaiming `README.md` with the maturity-focused README;
  - aligned `FEATURE_INVENTORY.md` with `HashFromAudioFileEnabled`;
  - made Paranoid mode an explicit security non-goal;
  - made the roadmap claim audit script repo-root aware.
- Validation:
  - Passed: `bash scripts/audit-feature-coherence.sh`.
  - Passed: `bash scripts/audit-readme-maturity-draft.sh`.
  - Passed: `bash scripts/audit-roadmap-claims.sh`.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the coherence README/audit fix.
  2. Continue the plan with bind-exposure wiring or public hash-from-audio cleanup as the next PR-sized slice.

## Update 2026-05-12 23:08:20Z

- Current task: Contacts hidden pane cleanup completed.
- Last activity:
  - documented Contacts under the hidden render-cost gotcha;
  - changed Contacts tabs to render only the active pane;
  - verified parent-owned state preserves loaded contacts/nearby data.
- Validation:
  - Passed: focused Contacts Web tests (`5/5`).
  - Passed: ESLint for `src/web/src/components/Contacts/Contacts.jsx`.
  - Passed: production Web build.
- Next steps:
  1. Commit and push the Contacts hidden pane cleanup.

## Update 2026-05-12 23:06:00Z

- Current task: Pods hidden polling cleanup completed.
- Last activity:
  - documented the Pods hidden pane polling gotcha in ADR-0001;
  - changed the nested Pods detail Tab to render only the active pane;
  - prevented hidden Port Forwarding from mounting and starting status/stats polling while VPN Gateway is active.
- Validation:
  - Passed: ESLint for `src/web/src/components/Pods/Pods.jsx`.
  - Passed: production Web build.
- Next steps:
  1. Commit and push the Pods hidden polling cleanup.

## Update 2026-05-12 23:02:10Z

- Current task: follow-up Web render/load performance pass completed.
- Last activity:
  - deferred Search Detail optional user notes and download stats until after first paint;
  - changed Library Health to render only the active tab so hidden issue tables are not built;
  - changed System Shares contents modal sorting to use copied file lists;
  - fixed the System Shares contents modal `open` prop to pass a boolean.
- Validation:
  - Passed: focused Search Detail/Library Health/Shares/System Files/Chat Web tests (`14/14`).
  - Passed: ESLint for touched files.
  - Passed: production Web build.
- Next steps:
  1. Commit and push the follow-up Web render/load performance pass.

## Update 2026-05-12 22:56:27Z

- Current task: additional hidden-work performance pass completed.
- Last activity:
  - extended ADR-0001 gotcha coverage for Chat, shared FileList, and System Files explorer hidden work;
  - rendered inactive chat tabs as lightweight shells without polling or optional `UserCard` metadata;
  - changed shared `FileList` to sort a memoized copy instead of mutating `files`;
  - changed System Files explorers to skip directory listing calls while inactive and fetch when activated.
- Validation:
  - Passed: focused Chat/Search/Transfers/System Files Web tests (`21/21`).
  - Passed: ESLint for touched files.
  - Passed: production Web build.
- Next steps:
  1. Commit and push the additional hidden-work performance pass.

## Update 2026-05-12 22:49:11Z

- Current task: deeper render-path performance pass completed.
- Last activity:
  - documented the hidden render-cost gotcha in ADR-0001 and committed it separately;
  - replaced Search response `JSON.stringify` update detection with a stable file signature;
  - deferred and cached Search user-group metadata lookups;
  - avoided mutating transfer file props while sorting rows;
  - rendered inactive room tabs as lightweight shells while keeping sessions mounted.
- Validation:
  - Passed: focused Rooms/Search/Transfers Web tests (`18/18`).
  - Passed: ESLint for touched files.
  - Passed: `git diff --check`.
  - Passed: production Web build.
- Next steps:
  1. Commit and push the render-path performance pass.

## Update 2026-05-12 22:43:00Z

- Current task: deeper Web performance pass completed.
- Last activity:
  - deferred optional `UserCard` metadata requests until after first paint;
  - deduped in-flight username lookups, capped metadata fetch concurrency at four, and cached user metadata briefly;
  - split optional Search panels with `React.lazy`;
  - split System tabs with `React.lazy` so inactive admin tabs do not inflate the route shell.
- Validation:
  - Passed: focused Search/UserCard/Transfers/Search Response Web tests (`19/19`).
  - Passed: focused System Web test (`1/1`).
  - Passed: ESLint for touched files.
  - Passed: `git diff --check`.
  - Passed: production Web build.
  - Build output: Search route chunk is ~68 kB; System shell chunk is ~8 kB, with heavy tabs/panels split into separate lazy chunks.
- Next steps:
  1. Commit and push the deeper performance pass.

## Update 2026-05-12 22:33:00Z

- Current task: page shell audit follow-up completed.
- Last activity:
  - audited route-level Web loaders for the same page-shell blocking pattern;
  - changed Collections, Shared with Me, Share Groups, Events, Files, Metrics, Network, and Source Providers to render stable page chrome before initial data arrives;
  - left scoped modal/inline loaders in place where they do not block page navigation.
- Validation:
  - Passed: focused Collections/SharedWithMe/ShareGroups/System Web tests (`8/8`).
  - Passed: ESLint for touched components.
  - Passed: `git diff --check`.
- Next steps:
  1. Commit and push the follow-up page-shell fixes.

## Update 2026-05-12 22:18:00Z

- Current task: kspls0 page-load stall fix completed.
- Last activity:
  - confirmed live Search was blocked by the SignalR initial list event before rendering the search input;
  - confirmed live active Downloads/Uploads polling failed because EF could not translate `IsSuccessfulTerminalTransfer(t.State)`;
  - replaced active transfer filters with translatable bitwise expressions and added a predicate-shape regression test;
  - changed Search and Transfers to render page shells before hub/API background work finishes;
  - cached the footer speeds/session aggregate response for short poll windows;
  - built and deployed `2026051216.slskdn.248-perf-fix` to kspls0.
- Validation:
  - Passed: focused `TransfersControllerTests` (`16/16`).
  - Passed: focused Search/Transfers Web tests (`17/17`).
  - Passed: `./bin/lint`.
  - Passed: full publish gate (`703/703` Web tests, production Web build, `4026/4026` unit tests, `57/57` smoke tests, `276/276` integration tests).
  - Passed live on kspls0: active downloads API 200, active uploads API 200, speeds API 200, searches API 200.
  - Passed live headless direct page-load checks: Search shell 228 ms, Uploads shell 198 ms, Downloads shell 901 ms.
- Next steps:
  1. Commit and push the page-load fixes.
  2. If a public release is needed, cut it through the guarded tag-only flow after the commit is on `origin/main`.

## Update 2026-05-12 21:55:00Z

- Current task: replacement release follow-up in progress.
- Last activity:
  - guarded `.247` did not push because the local release gate caught a Web full-suite Messaging slash-command test flake;
  - documented ADR-0001 gotcha `0z379`;
  - changed the test to wait for the controlled `/leave` composer value before firing Enter.
- Validation:
  - Passed: focused Messaging test file (`5/5`).
- Next steps:
  1. Run full Web tests through the guarded release flow.
  2. Commit/push the test hardening.
  3. Cut and monitor a replacement release tag.

## Update 2026-05-12 21:45:00Z

- Current task: replacement release follow-up in progress.
- Last activity:
  - confirmed replacement tag `.246` failed in the GitHub release gate before packaging;
  - root-caused the failure to mesh transfer status publication ordering: pollers could observe `State = Failed` before `ErrorMessage` was populated;
  - documented ADR-0001 gotcha `0z378`;
  - changed `MeshTransferService` to publish terminal failure details before switching to `Failed`.
- Validation:
  - Passed: focused Release mesh-transfer failure test once, then 30 repeated Release runs.
- Next steps:
  1. Run packaging metadata, lint, and release-target checks.
  2. Commit/push the status publication fix.
  3. Cut and monitor a replacement release tag.

## Update 2026-05-12 21:35:00Z

- Current task: replacement release follow-up in progress.
- Last activity:
  - confirmed replacement tag `.245` failed in the GitHub release gate before packaging, not in PPA;
  - root-caused the failure to a cancellation-sensitive unit-test mock counter in `ContentVerificationServiceTests.VerifySourcesAsync_BoundsConcurrentSoulseekProbes`;
  - documented ADR-0001 gotcha `0z377`;
  - changed the mock to decrement active probe accounting in `finally`.
- Validation:
  - Passed: focused Release content-verification tests once, then the concurrency test 20 repeated Release runs.
- Next steps:
  1. Run packaging metadata, lint, and release-target checks.
  2. Commit/push the flake fix.
  3. Cut and monitor a replacement release tag.

## Update 2026-05-12 21:17:26Z

- Current task: PPA release failure follow-up in progress.
- Last activity:
  - confirmed release `build-main-2026051220-slskdn.244` built app archives and created the GitHub release;
  - found Launchpad Jammy amd64 failed later during binary packaging because `packaging/debian/rules` used Bash brace expansion under `/bin/sh`;
  - documented ADR-0001 gotcha `0z376`;
  - replaced the Debian VPN unit rewrite loop with a POSIX-safe explicit unit list;
  - added packaging validation for the Debian rule and a PPA workflow binary-package preflight before Launchpad upload;
  - noted Chocolatey failed separately after five Chocolatey.org `504 Gateway Timeout` responses.
- Validation:
  - Passed: packaging metadata validation, `git diff --check`, and a local `debian/rules override_dh_auto_install` staged-payload simulation.
- Next steps:
  1. Commit the PPA fix.
  2. Merge the .244 release metadata commit from `origin/main`.
  3. Run the guarded release flow for a replacement tag.

## Update 2026-05-12 20:45:10Z

- Current task: live slskd app-log triage follow-up completed.
- Last activity:
  - found `kspls0` slskd logs were clean except duplicate queue-position timeout errors from remote peer lookups;
  - documented ADR-0001 gotcha `0z375`;
  - removed the download service log-and-rethrow path;
  - made the Transfers API return a sanitized 504 for expected queue-position timeouts while preserving the existing sanitized 500 path for unexpected failures;
  - added focused controller regression coverage.
- Validation:
  - Passed: focused `TransfersControllerTests` (`14/14`), `./bin/lint`, and `git diff --check`.
- Next steps:
  1. Commit and push the app-log noise fix.

## Update 2026-05-12 20:36:17Z

- Current task: failed transfer completion semantics completed.
- Last activity:
  - kept Soulseek's internal `Completed` terminal flag intact, but changed API/UI semantics so only `Completed, Succeeded` means user-facing complete;
  - changed `includeCompleted=false` transfer lists to hide only successful terminal transfers, so failed/aborted/timed-out downloads stay visible for retry or review;
  - changed bulk clear completed for downloads/uploads to remove only successful terminal transfers;
  - changed Web transfer labels from raw `Completed, Errored` style strings to `Error`, `Timed out`, `Aborted`, `Rejected`, `Cancelled`, or `Complete`;
  - renamed the UI toggle to `Hide Complete` and clarified that failed terminal downloads stay visible;
  - documented ADR-0001 gotcha `0z374`.
- Validation:
  - Passed: focused `TransfersControllerTests` (`13/13`), focused Transfers Web tests (`11/11`), Web lint for touched files, `./bin/lint`, and `git diff --check`.
- Next steps:
  1. Commit and push the transfer semantics fix.

## Update 2026-05-12 20:28:14Z

- Current task: `kspls0` log triage follow-up completed.
- Last activity:
  - confirmed `slskd.service` and `slskdN-vpn*` warning/error priority logs are clean and health API reports `Connected, LoggedIn`, VPN ready, and two port forwards;
  - found broader app log noise from failed downloads where peers advertise Windows-rooted remote filenames such as `c:\users\...\track.flac`;
  - changed remote filename localization to strip remote Windows/Unix roots before applying existing traversal and destination containment checks;
  - downgraded expected rescue misses for unresolved MusicBrainz Recording IDs from warning to debug;
  - documented ADR-0001 gotcha `0z373`;
  - noted unrelated failed host Proton WireGuard units: `proton.VPN.service`, `wg-proton-health.service`, `wg-quick@proton-torrent.service`, and an old transient `run-p2108162...` unit.
- Validation:
  - Passed: focused `ToLocalRelativeFilenameTests` (`14/14`), `./bin/lint`, and `git diff --check`.
- Next steps:
  1. Commit and push the log-triage fixes.
  2. Separately clean or disable stale host Proton WireGuard units if they are no longer used.

## Update 2026-05-12 20:10:49Z

- Current task: ports banner dismissal and compact layout completed.
- Last activity:
  - changed the network endpoint migration banner dismissal from forwarded-port-signature matching to a permanent browser-local flag;
  - preserved legacy dismissal keys so users who already closed the older banner do not get it back after this update;
  - condensed the banner to one small line that still says old builds needed 5 public forwards and names the current ports to keep reachable;
  - documented ADR-0001 gotcha `0z372`.
- Validation:
  - Passed: focused `App.test.jsx` (`11/11`), Web lint, production Web build, and `git diff --check`.
- Next steps:
  1. Commit and push the UI fix.

## Update 2026-05-12 20:03:59Z

- Current task: distro VPN helper packaging regression and `kspls0` connectivity check completed.
- Last activity:
  - diagnosed the Arch installer report: `/etc/slskd/slskd.yml.pacnew` is expected pacman behavior for a changed backed-up config file, but package messaging/docs now call out the merge workflow explicitly;
  - fixed the real packaging regression where distro-installed VPN helper units still referenced manual-install defaults (`/usr/local/bin/slskdN-vpn-agent`, `/etc/slskdN/slskd.yml`, `slskdN.service`, and `slskdN` service/user names);
  - updated AUR source/bin, Debian, and RPM packaging to rewrite VPN helper units to packaged `/usr/bin`, `/etc/slskd/slskd.yml`, and `slskd` defaults;
  - made the VPN helper prefer packaged config/service discovery when no explicit env is supplied, added watchdog timeout protection, and updated packaging metadata validation;
  - repaired the live `kspls0` helper units, cleared a stale watchdog process, restarted helper units, and verified the latest `.243` build is connected.
- Validation:
  - Passed: packaging metadata validation, helper Release build, `git diff --check`, AUR source package build, AUR bin package build, and extracted package unit checks.
  - Passed live on `kspls0`: app version `2026051219-slskdn.243`, server state `Connected, LoggedIn`, VPN ready, two port forwards, shares ready, `slskd.service` active, VPN ingress active, watchdog timer active, and watchdog service completed successfully.
- Next steps:
  1. Commit and push the packaging regression fix.
  2. Cut a follow-up tag-only release only after the user confirms the release tag/version.

## Update 2026-05-12 16:56:00Z

- Current task: guarded release tagging and aggregate artifact verification completed.
- Last activity:
  - added `scripts/create-release-tag.sh` so release tags can only be pushed
    after target verification, clean-tree checks, upstream branch sync,
    duplicate tag checks, tag-format validation, and the full release gate;
  - hardened `scripts/verify-release-artifacts.sh` so downloaded GitHub
    release assets must include `SHA256SUMS.txt`, expected platform archives,
    executable `vpn-agent/slskdN-vpn-agent`, Linux binary version output, and
    the bundled `slskdn-footer-session-total` Web marker;
  - updated release docs, the tagging ADR, changelogs, task/progress logs, and
    packaging metadata validation for the guarded path;
  - fixed macOS VPN helper pf enforcement so the anchor is installed and referenced from `/etc/pf.conf`;
  - added VPN helper payload publishing/checks to secondary Linux, PPA, and COPR release workflows;
  - exposed `slskdN-vpn-agent` from Homebrew and Chocolatey installs and kept the stable metadata updater in sync;
  - regenerated the API route inventory caught by the release gate;
  - fixed Transfers optimistic bulk-clear state so header rows disappear with the body rows.
- Validation:
  - Passed: `packaging/scripts/run-release-gate.sh` end-to-end after guarded release-tag workflow changes were pushed to `origin/main`.
  - Passed separately: shell syntax checks, `packaging/scripts/validate-packaging-metadata.sh`, `git diff --check`, and GitHub target verification.
  - Passed within gate: packaging metadata, remediation baseline, Web install/tests/build/subpath smoke, backend unit/smoke tests, release integration smoke.
  - Passed separately: self-contained VPN helper publishes for `linux-x64`, `win-x64`, and `osx-x64`; local `./bin/publish --no-prebuild --runtime linux-x64 --output /tmp/slskdn-release-linux-x64-pass2` with `vpn-agent/` payload.
- Next steps:
  1. Wait for explicit release tag/version before triggering a tag-only release through `scripts/create-release-tag.sh`.
  2. After the GitHub release publishes, run `scripts/verify-release-artifacts.sh <tag>`.

## Update 2026-05-12 16:15:00Z

- Current task: VPN helper packaging and cross-platform release readiness completed.
- Last activity:
  - packaged `slskdN-vpn-agent` into release archives for Linux, Windows, and macOS;
  - wired Linux direct install, AUR, Debian, and RPM packages to install the helper binary, systemd units, and `/var/lib/slskdN-vpn`;
  - fixed cross-platform helper config initialization so Windows/macOS split-routing paths do not resolve Linux service UIDs;
  - fixed local `bin/build`/`bin/publish` version normalization for legacy `slskdn.N` tags;
  - repaired stale unit-test fakes exposed by full release validation.
- Validation:
  - Passed: `dotnet test -c Release` (`57/57` core tests, `3953/3953` unit tests, `276/276` integration tests).
  - Passed: `./bin/lint`, `packaging/scripts/validate-packaging-metadata.sh`, `scripts/check-release-asset-matrix.sh`.
  - Passed: self-contained VPN helper publishes for `linux-x64`, `win-x64`, and `osx-x64`.
  - Passed: `./bin/publish --no-prebuild --runtime linux-x64 --output /tmp/slskdn-release-linux-x64`, including `vpn-agent/` payload with docs, installer, and systemd units.
- Next steps:
  1. Commit remaining packaging/test changes.
  2. Cut a tag-based release only after the exact release tag/version is confirmed.

## Update 2026-05-12 15:51:52Z

- Current task: kspls0 mesh/VPN recovery completed.
- Last activity:
  - fixed `dht.vpn_port_sync: target_port` startup crash by making DHT VPN port sync parse documented snake_case values instead of binding directly to a CLR enum;
  - documented ADR-0001 gotcha `0z362` and committed `ccf773241`;
  - published and deployed the rebuilt backend to `/usr/lib/slskd` on `kspls0`;
  - cleaned/restarted VPN ingress and verified public mesh overlay advertisement `40824 -> 50305`;
  - verified `slskd.service` and `slskdN-vpn-ingress.service` active, DHT running, 69 DHT nodes, 31 discovered peers, and two active inbound mesh overlay peers.
- Validation:
  - Passed: focused `DhtRendezvousOptionsTests` (`7/7`), Release build, Release publish, `./bin/lint`.
  - Partial: full `dotnet test` ran `slskd.Tests` (`57/57`) and `slskd.Tests.Integration` (`276/276`) but exited nonzero because `slskd.Tests.Unit` has a pre-existing compile failure: `LidarrImportServiceTests.FakeLidarrClient` does not implement `ILidarrClient.GetWantedMissingPageAsync`.
- Next steps:
  1. Fix the unrelated Lidarr unit-test fake compile failure when touching test infrastructure next.
## Update 2026-05-07 02:29:00Z

- Current task: Messaging V2 sizing/performance follow-up deployed to `kspls0`.
- Last activity:
  - added visible `-`/`+` whole-Messages UI size controls around the S/M/L/XL presets;
  - captured Ctrl/Cmd+wheel inside Messages so it changes Messages UI size instead of browser/page zoom;
  - bounded the Messages pane to the app content area using nav/footer/player CSS variables so it stays above the player/footer;
  - reduced inactive search render overhead in `MessageStream`;
  - documented ADR-0001 gotcha `0z348`;
  - validation passed: focused Messaging Vitest (`21/21`), Web lint, and production Web build;
  - synced the rebuilt web bundle into `/usr/lib/slskd/current/wwwroot` on `kspls0` and restarted `slskd.service`;
  - verified HTTP 200 on `http://kspls0:5030/`, active PID `3071009`, initialization complete, mesh neighbor connected, VPN ready, and Soulseek logged in as `Jarvis1984`.
- Next steps:
  1. User testing on `http://kspls0:5030/messages` with hard refresh if an old asset is cached.

## Update 2026-05-07 02:24:00Z

- Current task: Custom Messaging V2 build deployed to `kspls0` for user testing.
- Last activity:
  - built Web production assets and backend Release/self-contained `linux-x64` publish as `0.0.0-slskdn.manual.20260507022201.ae68db572`;
  - stopped the previously running `slskd.service` on `kspls0`;
  - deployed `/usr/lib/slskd/releases/manual-20260507022201.ae68db572` and repointed `/usr/lib/slskd/current`;
  - restarted `slskd.service`; PID `3057278` is active/running;
  - verified `http://kspls0:5030/` returns HTTP 200, listeners are bound on `5030`, `50300`, `50301`, and `50305`, startup completed, VPN is ready, and Soulseek logged in as `Jarvis1984`.
- Next steps:
  1. User testing on `http://kspls0:5030/messages`.

## Update 2026-05-07 02:20:00Z

- Current task: Retire `/messages` V1 route and validate Messaging V2 cutover.
- Last activity:
  - made `/messages` render `MessagingV2` directly without the `slskd-messaging-v2` localStorage flag;
  - removed the inline V1 pod channel session from the route while leaving legacy `ChatSession.jsx` and `RoomSession.jsx` available for the older dedicated Chat and Rooms pages;
  - added active-conversation search to `MessageStream` with highlighted matches, previous/next navigation, Enter cycling, and Escape clear;
  - changed `/leave` and `/part` in V2 to leave active rooms/pods instead of only closing the tab;
  - documented Phase 7 server-push messaging as deferred pending backend/SignalR contract work;
  - validation passed: focused Messaging Vitest (`21/21`), Web lint, production Web build, and headless Playwright cutover smoke with the legacy flag forced off.
- Next steps:
  1. Commit and push the validated Messaging V2 cutover if requested.

## Update 2026-05-07 01:15:18Z

- Current task: Fix missing Rooms `Join Room` button reported from kspls0.
- Last activity:
  - verified a fresh headless kspls0 browser session reached the login page and could not inspect authenticated Rooms state;
  - confirmed source-side regression: `RoomJoinModal` existed but `Rooms.jsx` no longer rendered the explicit join action;
  - restored the visible green `Join Room` toolbar button, hardened available-room payload normalization, and added a focused `slskdn` join regression;
  - recorded `BUG-20260506-104` and ADR-0001 gotcha `0z99`.
- Next steps:
  1. Run Web lint, focused Rooms test, diff checks, and git status.
  2. Commit and push the verified workflow fix.

## Update 2026-05-06 23:02:54Z

- Current task: Run every slskd council phase and broad non-runtime scans.
- Last activity:
  - added `npm run check:council` with `scripts/run-bug-council-all-phases.sh`;
  - added `scripts/check-bug-council-all-phases.sh` to remediation so the all-phases runner remains wired;
  - accepted and fixed `BUG-20260506-102` and `BUG-20260506-103`;
  - redacted VirtualSoulfind bridge proxy search, username, filename, and protocol-token request logs.
- Next steps:
  1. Run `npm run check:council`, focused backend build, remediation, lint, and diff checks.
  2. Commit and push the verified all-phases council batch.

## Update 2026-05-06 22:57:43Z

- Current task: Run updated slskd council Web-input adversarial fuzz phase.
- Last activity:
  - read `docs/dev/bug-council-phases.md` and identified phase 3 as the first pending slskd-specific row;
  - accepted and fixed `BUG-20260506-101`;
  - added `tests/slskd.Tests/WebInputAdversarialFuzzTests.cs` for malformed JSON, deterministic random byte bodies, and hostile query/path strings;
  - added `scripts/check-web-input-adversarial-fuzz.sh` to remediation and marked phase 3 done.
- Next steps:
  1. Run focused fuzz tests, remediation, lint, and diff checks.
  2. Commit and push the verified non-runtime council fuzz batch.

## Update 2026-05-06 22:44:05Z

- Current task: Continue non-runtime council Web System polling lifecycle cycle.
- Last activity:
  - classified System pollers after skipping runtime/vendor work;
  - accepted and fixed `BUG-20260506-093` through `BUG-20260506-100`;
  - Network, Mesh, Swarm Visualization, Swarm Analytics, Jobs, Security, Bridge, and MediaCore now guard post-await state updates after unmount;
  - added `scripts/check-web-polling-lifecycle.sh` to remediation and extended ADR-0001 gotcha `0z93`.
- Next steps:
  1. Run focused Web tests, remediation, Web lint, repo lint, and diff checks.
  2. Commit and push the verified non-runtime Web polling lifecycle batch.

## Update 2026-05-06 22:27:42Z

- Current task: Run broader non-runtime council negative-space cycle.
- Last activity:
  - read the updated slskd council phase tracker and negative-space gate;
  - accepted and fixed `BUG-20260506-092`;
  - corrected controller boundary sink paths to the active `src/slskd` API layout;
  - strengthened `scripts/check-council-negative-space.sh` so every declared boundary asserts validator presence and remediation-baseline registration;
  - documented ADR-0001 gotcha `0z97`.
- Next steps:
  1. Run lint, diff checks, and target verification.
  2. Commit and push the verified non-runtime negative-space batch without touching dirty vendored runtime files.

## Update 2026-05-06 22:00:48Z

- Current task: Continue non-runtime council active fork branch-wording cycle.
- Last activity:
  - classified active workflow comments and fork docs branch-wording references after skipping runtime/vendor/archive history;
  - accepted and fixed `BUG-20260506-090` and `BUG-20260506-091`;
  - CI/CodeQL comments, FEATURES quick-start/status text, and active security docs now describe the fork's `main` branch;
  - broadened `scripts/check-fork-main-branch-links.sh` and extended ADR-0001 gotcha `0z96`.
- Next steps:
  1. Run remediation, focused branch-link scanner, workflow YAML checks, docs checks, lint, and diff checks.
  2. Commit and push the verified non-runtime branch-wording batch.

## Update 2026-05-06 21:55:14Z

- Current task: Continue non-runtime council active fork branch-link cycle.
- Last activity:
  - classified active fork package/docs branch references after skipping runtime/vendor/archive history;
  - accepted and fixed `BUG-20260506-087` through `BUG-20260506-089`;
  - Unraid, Chocolatey, and the active E2E guide now target the fork's `main` branch;
  - added `scripts/check-fork-main-branch-links.sh` to remediation and documented ADR-0001 gotcha `0z96`.
- Next steps:
  1. Run remediation, focused branch-link scanner, packaging metadata checks, docs checks, lint, and diff checks.
  2. Commit and push the verified non-runtime branch-link batch.

## Update 2026-05-07 02:06:00Z

- Current task: Continue non-runtime council QUIC overlay/data stream lifecycle cycle.
- Last activity:
  - classified backend async/lifecycle candidates after skipping runtime/vendor;
  - accepted and fixed `BUG-20260506-085` and `BUG-20260506-086`;
  - QUIC overlay control streams and QUIC data streams are now tracked and drained during server shutdown;
  - extended `scripts/check-async-task-observation.sh` to reject raw detached stream handlers;
  - documented ADR-0001 gotcha `0z95`, and updated ledger/changelog/task log.
- Next steps:
  1. Run remediation, focused QUIC tests, build/lint, and diff checks.
  2. Commit and push the verified non-runtime QUIC lifecycle batch.

## Update 2026-05-07 01:49:00Z

- Current task: Continue non-runtime council Proxmox/raw Linux installer safety cycle.
- Last activity:
  - classified active release/ops installer candidates after skipping runtime/vendor and docs/archive;
  - accepted and fixed `BUG-20260506-082` through `BUG-20260506-084`;
  - Proxmox LXC installs now verify `SHA256SUMS.txt`, remove stale install trees before extraction, and converge service config/data/app ownership plus `UMask=0002`;
  - added `scripts/check-linux-installer-safety.sh` to remediation;
  - documented ADR-0001 gotcha `0z94`, and updated ledger/changelog/task log.
- Next steps:
  1. Run remediation, packaging installer checks, repo lint, and diff checks.
  2. Commit and push the verified non-runtime release/ops installer batch.

## Update 2026-05-07 01:39:00Z

- Current task: Continue non-runtime council Web lifecycle and hub mutation cycle.
- Last activity:
  - classified Web lifecycle candidates after skipping runtime/vendor;
  - accepted and fixed `BUG-20260506-079` through `BUG-20260506-081`;
  - Library Health scan polling now owns interval/timeout cleanup and handles poll errors;
  - Shares delayed post-scan refreshes are canceled on unmount;
  - Search hub mutation events now require object payloads with string ids;
  - documented ADR-0001 gotcha `0z93`, and updated ledger/changelog/task log.
- Next steps:
  1. Run focused Web tests, remediation, Web lint, repo lint, and diff checks.
  2. Commit and push the verified non-runtime Web lifecycle batch.

## Update 2026-05-07 01:33:00Z

- Current task: Continue non-runtime council Web boundary object-normalization cycle.
- Last activity:
  - classified Web storage and response-object boundary candidates after skipping runtime/vendor;
  - accepted and fixed `BUG-20260506-076` through `BUG-20260506-078`;
  - Experience settings now normalize persisted enum, boolean, and numeric-string fields;
  - user-note modal responses and MusicBrainz target lookup responses now reject or default malformed success bodies before updating UI state;
  - documented ADR-0001 gotcha `0z92`, and updated ledger/changelog/task log.
- Next steps:
  1. Run focused Web tests, remediation, Web lint, repo lint, and diff checks.
  2. Commit and push the verified non-runtime Web boundary batch.

## Update 2026-05-07 00:55:00Z

- Current task: Continue non-runtime council durable temp-move state writer cycle.
- Last activity:
  - classified direct/fixed-temp durable state writers after skipping runtime/vendor;
  - accepted and fixed `BUG-20260506-069` through `BUG-20260506-075`;
  - job manifests, quarantine jury state, Spotify token state, source-feed history, MusicBrainz radar/overlay state, and realm subject indexes now use `AtomicFileWriter`;
  - documented ADR-0001 gotcha `0z346`, and updated ledger/changelog/task log.
- Next steps:
  1. Run focused backend tests, remediation, repo lint, and diff checks.
  2. Commit and push the verified non-runtime durable state writer batch.

## Update 2026-05-07 00:36:00Z

- Current task: Continue non-runtime council DHT overlay async lifecycle cycle.
- Last activity:
  - classified detached app-side async lifecycle calls after skipping runtime/vendor;
  - accepted and fixed `BUG-20260506-066` through `BUG-20260506-068`;
  - DHT overlay inbound connection tasks, inbound message loops, and outbound message loops now use `TaskObservation`;
  - documented ADR-0001 gotcha `0z345`, and updated ledger/changelog/task log.
- Next steps:
  1. Run focused backend tests, remediation, repo lint, and diff checks.
  2. Commit and push the verified non-runtime DHT overlay async lifecycle batch.

## Update 2026-05-07 00:18:00Z

- Current task: Continue non-runtime council durable app-state persistence cycle.
- Last activity:
  - classified direct durable app-state writes after skipping runtime/vendor;
  - accepted and fixed `BUG-20260506-061` through `BUG-20260506-065`;
  - profile identity, peer reputation, DHT node state, auto-replace state, and verification probe budgets now use `AtomicFileWriter`;
  - documented ADR-0001 gotcha `0z344`, and updated ledger/changelog/task log.
- Next steps:
  1. Run focused backend tests, remediation, repo lint, and diff checks.
  2. Commit and push the verified non-runtime durable-state batch.

## Update 2026-05-06 23:58:00Z

- Current task: Continue non-runtime council app security-state persistence cycle.
- Last activity:
  - classified app-side security-state file writes after skipping runtime/vendor;
  - accepted and fixed `BUG-20260506-058` through `BUG-20260506-060`;
  - overlay keypairs, overlay TLS certificates, and mesh certificate pins now use flushed sibling temp files and atomic replacement;
  - documented ADR-0001 gotcha `0z343`, and updated ledger/changelog/task log.
- Next steps:
  1. Run focused backend unit tests, remediation, repo lint, and diff checks.
  2. Commit and push the verified non-runtime security-state persistence batch.

## Update 2026-05-06 23:40:00Z

- Current task: Continue non-runtime council Web error-rendering cycle.
- Last activity:
  - classified raw API error-body rendering in Collections, ShareGroups, SharedWithMe, and adjacent already-normalized Web paths;
  - accepted and fixed `BUG-20260506-055` through `BUG-20260506-057`;
  - Collections and ShareGroups now normalize structured mutation errors before rendering;
  - SharedWithMe now normalizes manifest/load errors and uses the correct `ErrorSegment` `caption` prop;
  - documented ADR-0001 gotcha `0z342`, and updated ledger/changelog/task log.
- Next steps:
  1. Run remediation, Web lint, repo lint, and diff checks.
  2. Commit and push the verified non-runtime Web error-rendering batch.

## Update 2026-05-06 23:28:00Z

- Current task: Continue non-runtime council Web response-object cycle.
- Last activity:
  - classified the response-object property-read section across Contacts, Library Health, SharedWithMe, Playlist Intake, user notes, jobs/bridge helpers, and guarded search panels;
  - accepted and fixed `BUG-20260506-051` through `BUG-20260506-054`;
  - Contacts now filters malformed contact entries, treats malformed invite creation responses as stable errors, and renders structured contact errors as text;
  - Library Health now requires a string scan id before polling and ignores malformed status bodies;
  - SharedWithMe now normalizes backfill result counts and structured backfill errors;
  - documented ADR-0001 gotcha `0z341`, and updated ledger/changelog/task log.
- Next steps:
  1. Run remediation, Web lint, repo lint, and diff checks.
  2. Commit and push the verified non-runtime Web response-object batch.

## Update 2026-05-06 23:15:00Z

- Current task: Continue non-runtime council Web/API contract cycle.
- Last activity:
  - classified the direct fetch, browser storage, response-shape, and persisted UI selection slice;
  - accepted and fixed `BUG-20260506-048` through `BUG-20260506-050`;
  - Transfers now ignores malformed/stale selected file entries and malformed directory payloads before bulk actions render;
  - Options edit saves now branch on the fresh validation result and render structured update errors as stable text;
  - documented ADR-0001 gotchas `0z339` and `0z340`, and updated ledger/changelog/task log.
- Next steps:
  1. Run remediation, Web lint, repo lint, and diff checks.
  2. Commit and push the verified non-runtime Web/API batch.

## Update 2026-05-06 23:03:00Z

- Current task: Continue non-runtime council release/ops workflow sweep.
- Last activity:
  - accepted and fixed `BUG-20260506-046` and `BUG-20260506-047`;
  - upstream sync is manual-only and now pushes to `main` instead of scheduled `origin master`;
  - upstream-access notification PRs target `main` and declare explicit `issues`/`pull-requests` permissions;
  - added `scripts/check-workflow-main-branch-targets.sh` to remediation;
  - documented ADR-0001 gotchas `0z337` and `0z338` and updated ledger/changelog/task log.
- Next steps:
  1. Run remediation, workflow scanners, lint, and diff checks.
  2. Commit and push the verified non-runtime workflow batch.

## Update 2026-05-06 22:43:00Z

- Current task: Continue non-runtime council API error-body leakage sweep.
- Last activity:
  - accepted and fixed `BUG-20260506-042` through `BUG-20260506-045`;
  - Spotify, SongID, and Listening Party controllers now return stable client-facing errors instead of raw `ex.Message` values;
  - Spotify OAuth callback HTML no longer reflects provider/caller-controlled `error` query text;
  - added `scripts/check-api-exception-bodies.sh` to remediation and focused controller regressions;
  - documented ADR-0001 gotcha `0z336` and updated ledger/changelog/task log.
- Next steps:
  1. Run remediation, lint, diff checks, and focused controller tests.
  2. Commit and push the verified non-runtime council batch.

## Update 2026-05-06 22:24:00Z

- Current task: Continue non-runtime council secret/log/error leakage sweep.
- Last activity:
  - accepted and fixed `BUG-20260506-041`;
  - relay authentication challenge mismatch logs now include only a hashed agent identifier, not supplied or expected HMAC credential values;
  - extended `scripts/check-sensitive-placeholders.sh` so the raw credential placeholders are rejected by remediation;
  - documented ADR-0001 gotcha `0z335` and updated the ledger/changelog/task log.
- Next steps:
  1. Run focused scanner, remediation, lint, and diff validation.
  2. Commit and push the verified non-runtime council batch without touching dirty vendor runtime files.

## Update 2026-05-06 22:08:00Z

- Current task: Continue non-runtime council mutable-ownership helper sweep.
- Last activity:
  - accepted and fixed `BUG-20260506-039` and `BUG-20260506-040`;
  - `ContentVerificationResult.BestSources`, `BestSemanticSources`, and `TransportPolicy.GetEffectivePreferenceOrder()` now return snapshot lists instead of backing mutable collections;
  - added focused regressions in `ContentVerificationServiceTests` and `TransportPolicyTests`;
  - documented ADR-0001 gotcha `0z334` and updated the bug burn-down ledger/changelog/task log.
- Next steps:
  1. Run remediation, lint, and diff validation.
  2. Commit and push the verified non-runtime council batch.

## Update 2026-05-06 19:24:22Z

- Current task: Continue non-runtime council mutable-ownership sweep across constructor/public accessor candidates.
- Last activity:
  - accepted and fixed `BUG-20260506-037` in `MeshPeer`;
  - `MeshPeer` now clones `IPEndPoint` values on construction, update, `Addresses` access, and `GetBestAddress()` to avoid shared mutable endpoint ownership;
  - fixed `BUG-20260506-038` after focused validation showed DHT STORE signing reflected into a read-only ACK message type;
  - added focused `MeshPeerTests` and `KademliaRpcClientTests` regressions plus ADR-0001 gotchas `0z332` and `0z333`.
- Next steps:
  1. Run focused `MeshPeerTests`, remediation baseline, lint, and diff checks.
  2. Continue the next non-runtime council section if validation remains green.

## Update 2026-05-06 21:40:00Z

- Current task: Continue non-runtime council mutable-ownership sweep across section-wide findings.
- Last activity:
  - fixed and verified `BUG-20260506-035` (`BatchedMessage` now deep-copies nested metadata values, including nested dictionaries, lists, and nested byte arrays);
  - fixed and verified `BUG-20260506-036` (`DhtStoreMessage.CreateSigned` now clones `key`, `value`, and `requesterId` byte arrays before signing/payload serialization);
  - added focused regressions in `tests/slskd.Tests.Unit/Common/Security/TimedBatcherTests.cs::BatchedMessage_DeepCopiesNestedMetadata` and `tests/slskd.Tests.Unit/Mesh/KademliaRpcClientTests.cs::CreateSigned_CopiesMutableInputs`;
  - updated ADR-0001 gotchas `0z330` and `0z331`.
- Next steps:
  1. Run focused non-runtime validation checks and update the burn-down ledger summary status.
  2. Update `memory-bank/tasks.md`, `memory-bank/progress.md`, and `docs/dev/bug-burndown-ledger.md`.
  3. Commit and push this verified cycle in one batch.

## Update 2026-05-06 21:00:00Z

- Current task: Continue non-runtime council scan and burn-down, avoiding one-hit loop behavior.
- Last activity:
  - fixed and verified `BUG-20260506-032` through `BUG-20260506-034` for remaining primitive-body web helper mismatches (`Events.raiseEvent`, `Rooms.setTicker`, `Rooms.addRoomMember`);
  - updated `docs/dev/bug-burndown-ledger.md` with class-wide section status and per-finding verification status;
  - confirmed remediation + targeted web tests pass.
- Next steps:
  1. Continue the next non-runtime section after a short, full-candidate rescan to keep the council loop moving by section rather than one finding at a time.
  2. Commit and push all non-runtime local changes when verified, including any remaining candidate classification rows.

## Update 2026-05-06 20:40:00Z

- Current task: Broad non-runtime council cycle in mutable ownership class.
- Last activity:
  - fixed `BUG-20260506-029` through `BUG-20260506-031`: copied mutable secret/signature/message bytes on input in security batching paths.
  - added `docs/dev/bug-burndown-ledger.md` rows for the three fixes and updated remaining mutable-input class notes.
  - added focused backend unit regressions in `CanaryTrapsTests`, `ContentSafetyTests`, and `TimedBatcherTests`.
- Next steps:
  1. Continue council scan of remaining sections/classes and classify full candidate batches before stopping.
  2. Keep fix/push cadence for completed cycles until the user-directed broad burn-down cycle is complete.

## Update 2026-05-06 18:05:00Z

- Current task: Non-runtime council async side-effect burn-down is implemented locally.
- Last activity:
  - baseline remediation passed before the cycle
  - classified the app async/lifecycle scan section and accepted `BUG-20260506-027`
  - replaced raw fire-and-forget side effects in Application, SignalBus, SharesController, PodMessaging, and DownloadService with observation helpers
  - added `scripts/check-async-task-observation.sh` to remediation and documented ADR-0001 gotcha `0z327`
  - focused unit validation passed for touched Application/SignalBus/Shares/Pod/Download paths
- Next steps:
1. Run broad remediation/lint/full solution tests.
2. Commit and push the implementation batch.

## Update 2026-05-06 18:22:00Z

- Current task: Non-runtime council async-lifecycle cleanup pass continuation.
- Last activity:
  - converted `DisasterModeRecovery.OnHealthChanged` and `DisasterModeCoordinator.OnHealthChanged` from `async void` to observed `Task` handlers so recovery/escalation faults are captured by `TaskObservation`.
  - added `scripts/check-async-void-handlers.sh` and wired it into `scripts/check-remediation-baseline.sh`.
  - added `BUG-20260506-028` and ADR-0001 entry `0z328` to lock this pattern for recurrence prevention.
- Next steps:
  1. Run `scripts/check-async-void-handlers.sh`, `scripts/check-async-task-observation.sh`, and `npm run check:remediation`.
  2. Run focused backend/lint verification and commit/push the complete dirty tree.

## Update 2026-05-06 17:10:00Z

- Current task: Non-runtime council async/path scan batch is complete locally.
- Last activity:
  - skipped `vendor/slskNet.Runtime` because runtime remains assigned to another agent
  - classified route-state navigation, primitive JSON body, outbound HTTP, async/lifecycle, and filesystem/path scan sections
  - accepted and fixed `BUG-20260506-023` through `BUG-20260506-026`
  - awaited and isolated SearchService traffic-observer notifications so async failures are observed and later responses still notify
  - made ContentLocator fallback lookup and Library Health recursive scans skip reparse points explicitly
  - aligned the integration auth fixture with admin-only security route smoke tests
  - documented ADR-0001 gotchas `0z325` and `0z326` and strengthened `scripts/check-path-containment.sh`
- Next steps:
  1. Run focused backend tests and remediation/path scanners.
  2. Commit and push the implementation batch.

## Update 2026-05-06 16:20:00Z

- Current task: Non-runtime council scan and accepted burn-down batch is complete locally.
- Last activity:
  - skipped `vendor/slskNet.Runtime` because runtime is assigned to another agent
  - fixed and verified `BUG-20260506-004` through `BUG-20260506-013` in `docs/dev/bug-burndown-ledger.md`
  - fixed Snap updater coverage, Flatpak manifest structure, CodeQL .NET setup, scheduled release-tag automation, Wishlist route/list guards, System Shares/SharedWithMe payload guards, SecurityController role drift, and anonymous build update side effects
  - added non-runtime scanners for CodeQL .NET setup, Flatpak manifest structure, Web list-shape guards, and Web search route segment encoding
  - validation passed: focused Web tests (`9`), focused backend tests (`17`), Web lint, repo lint, packaging/release/workflow scanner lane, and `git diff --check`
- Next steps:
  1. Commit and push the non-runtime council batch, excluding runtime files owned by the other agent.
  2. Continue the next non-runtime council section if requested.

## Update 2026-05-06 01:05:00Z

- Current task: Live `kspls0` Soulseek disconnect/log follow-up is fixed and deployed.
- Last activity:
  - checked service, authenticated application state, in-memory logs, Gluetun state, and service-user TCP egress
  - confirmed the daemon is currently connected/logged in, but had earlier connect/login timeouts before succeeding
  - found a real post-login runtime regression in live logs: valid distributed search requests with negative opaque tokens were rejected by scalar hardening
  - documented ADR-0001 gotcha `0z322`
  - fixed `DistributedSearchRequest` to accept signed opaque token values and added focused runtime tests
  - validation passed: focused distributed/protocol scalar runtime test slice (`80/80`) and `git diff --check`
  - deployed `0.0.0-slskdn.manual.20260506010228.a0f8ed7fff3` to `kspls0`
  - verified authenticated state remains `Connected, LoggedIn` and a 45-second post-deploy log check found no disconnect/reconnect or distributed-token rejection messages
- Next steps:
  1. Push the local commits to `snapetech/slskdN`.
  2. Let the user test the Web UI on `kspls0`.

## Update 2026-05-06 00:55:00Z

- Current task: `kspls0` is running the patched current build for user testing.
- Last activity:
  - published and installed `0.0.0-slskdn.manual.20260506003550.cfbff9ac1fd8` on `kspls0`
  - observed startup stuck waiting for VPN because the public outbound SSRF guard rejected `127.0.0.1:8010`
  - added a local no-redirect HTTP client and switched Gluetun to it
  - documented ADR-0001 gotcha `0z321`
  - validation passed: focused Gluetun/VPN/outbound guard tests (`9/9`), outbound HTTP scanner, and `git diff --check`
  - committed and pushed `82615e8cd`
  - deployed patched build `0.0.0-slskdn.manual.20260506005051.82615e8cd975`
  - verified `slskd.service` active, `/usr/lib/slskd/current` points at the patched release, HTTP `5030` serves the Web UI, and expected unauthenticated API `401` is returned
  - Soulseek server login was still retrying external `10000ms` timeouts at handoff
- Next steps:
  1. Let the user test the Web UI on `kspls0`.
  2. Watch Soulseek connectivity if peer/network testing depends on login.

## Update 2026-05-06 00:20:00Z

- Current task: GitHub Actions 0-second release workflow failures are diagnosed and fixed locally.
- Last activity:
  - confirmed the failures were workflow parser failures with no jobs/logs, not product test failures
  - fixed tab indentation in `release-linux.yml` and `release-ppa.yml`
  - fixed disabled `check-upstream-access.yml` multiline body YAML shape
  - added `scripts/check-workflow-yaml-syntax.sh` and wired it into remediation
  - documented ADR-0001 gotcha `0z320`
  - validation passed: workflow YAML syntax check, `npm run check:remediation`, and `git diff --check`
- Next steps:
  1. Commit and push the remediation scanner/docs sync.
  2. Watch the next Actions run to confirm the 0-second workflow parser failures stop.

## Update 2026-05-05 23:55:00Z

- Current task: Vendored runtime example Web API council sweep is implemented and broadly validated locally.
- Last activity:
  - added `RT-080` for closing the broad example Web API loop with narrower scanner subgroups
  - added `RT-081` through `RT-084` for shared path leakage, shared-cache SQLite lifecycle, explicit blank route validation, and missing upload lookup `404` handling
  - classified `390/390`, `177/177`, `268/268`, `158/158`, and `212/212` example Web API subgroup candidates with zero unclassified candidates
  - documented ADR-0001 gotchas `0z318` and `0z319`
  - validation passed: focused Web API tests (`58/58`), vendored/root remediation baselines, Web lint, repo lint, full `dotnet test slskd.sln --no-restore`, and `git diff --check`
- Next steps:
  1. Commit and push the implementation batch.
  2. Continue the next council scan section after push.

## Update 2026-05-05 23:28:00Z

- Current task: Vendored runtime lifecycle fire-and-forget council sweep is implemented and verified locally.
- Last activity:
  - preserved the prior lifecycle sweep commit for `RT-076`
  - added `RT-077` for the fire-and-forget subgroup loop fix, `RT-078` for distributed status/broadcast callback hardening, and `RT-079` for distributed message rebroadcast hardening
  - added guarded queue helpers, scanner/baseline enforcement, focused distributed message handler coverage, and ADR-0001 gotcha `0z317`
  - validation passed: focused runtime lifecycle tests (`294/294`), standalone/root/vendored remediation baselines and scans, Web lint, repo lint, `git diff --check`, parent `dotnet build slskd.sln --no-restore`, and full `dotnet test slskd.sln --no-restore`
- Next steps:
  1. Commit and push the final memory-bank sync.
  2. Continue the next council scan section after push.

## Update 2026-05-05 22:38:00Z

- Current task: Broad council cycle app/package burn-down is implemented locally.
- Last activity:
  - fixed and verified `BUG-20260505-113` through `BUG-20260505-123` in `docs/dev/bug-burndown-ledger.md`
  - fixed mesh gateway validation/auth, POST-only memory dumps, option log redaction, no-redirect tunnel/meek transports, pod/search route encoding, Quarantine Jury and MediaCore list guards, AUR/PPA tmpfiles drift, Snap version drift, and release-note asset drift
  - strengthened outbound HTTP and relay path-containment scanners
  - documented ADR-0001 gotchas `0z312` through `0z316`
  - validation passed: focused backend/Web tests and packaging/release/outbound/path scanner lane
- Next steps:
  1. Run broad remediation/lint/diff/full test gates.
  2. Commit and push current local commits.

## Update 2026-05-05 22:19:00Z

- Current task: CodeQL relay download user-controlled bypass alert #2552 is fixed locally.
- Last activity:
  - confirmed the alert is in app code, not the .NET runtime
  - changed relay download validation to return a token-bound server filename
  - changed `RelayController.DownloadFile` to ignore the request filename header for file selection
  - added focused controller/service regressions for invalid and tampered filename headers
  - documented ADR-0001 gotcha `0z311` in commit `44efb888a`
  - validation passed: focused relay unit tests (`12` tests), `git diff --check`, `./bin/lint`, and full `dotnet test slskd.sln --no-restore`
- Next steps:
  1. Commit and push the implementation/docs fix.

## Update 2026-05-05 22:45:00Z

- Current task: Frontend list-shape and redirect-client bug-council batch is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-105` and `BUG-20260505-106` in `docs/dev/bug-burndown-ledger.md`
  - guarded Messaging, RoomJoinModal, System Files, System Network, Soulseek Discovery, and Pods list fields
  - changed NAT public-IP and GitHub release helper direct HTTP clients to disable redirects
  - extended ADR-0001 gotchas in commit `ccf0cc14f`
  - validation passed: focused Web tests (`18` tests), focused Web lint, focused backend unit slice (`2` tests), and direct pattern scans
- Next steps:
  1. Run broad remediation/lint/diff/full test gates.
  2. Commit and push the full dirty tree per user request.

## Update 2026-05-05 22:30:00Z

- Current task: Broad network-health/docs drift bug-council batch is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-098` through `BUG-20260505-104` in `docs/dev/bug-burndown-ledger.md`
  - guarded SourceDiscovery, Users browse, Backfill candidates, Rescue, and ContentVerification peer-facing work with budgets/cancellation/concurrency limits
  - hardened MediaCore/Integrations list payloads and corrected Homebrew/Flatpak/build docs for current env/tag names
  - documented ADR-0001 gotchas in commits `2d9061ec9` and `c9ea8f5bb`
  - validation passed: focused backend unit slice (`21` tests), focused System Web tests (`16` tests), Soulseek network-health scanner, workflow trigger scanner, packaging metadata validation, and focused Web lint
- Next steps:
  1. Run broad remediation/lint/diff/full test gates.
  2. Commit and push the full dirty tree per user request.

## Update 2026-05-05 22:05:00Z

- Current task: Broad frontend/backend/release bug-council burn-down batch is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-086` through `BUG-20260505-097` in `docs/dev/bug-burndown-ledger.md`
  - hardened Browse/Chat/Rooms/BrowseSession/System/Swarm/Transfers Web workflows against malformed persisted state and nested list payloads
  - added compatibility browse Soulseek safety limiter/cancellation, no-redirect integration HTTP clients, AcoustID fingerprint redaction, build-dev tag support, glob-safe CI tag filters, and Helm/TrueNAS image/env corrections
  - documented ADR-0001 gotchas in commits `0288a045c` and `2c6f6a3d6`
  - validation passed: focused Web batch (`48` tests), focused backend unit slice (`68` tests), workflow trigger policy scanner, and packaging metadata validation
- Next steps:
  1. Run broader remediation/lint/diff/full test gates.
  2. Commit and push the full dirty tree per user request.

## Update 2026-05-05 21:36:00Z

- Current task: Continued broad frontend list-shape burn-down is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-081` through `BUG-20260505-085` in `docs/dev/bug-burndown-ledger.md`
  - guarded listening/player/playlist/search helper lists, player/Browse/Transfers metadata lists, ChatSession/UserCard payloads, search graph handoff lists, filter arrays, and admin diagnostics/settings arrays
  - documented ADR-0001 gotcha extensions in commits `bee9ac2a8` and `7107de805`
  - validation passed: focused frontend Vitest batch (`54` tests) and Web lint
- Next steps:
  1. Run broader remediation/diff/repo lint gates.
  2. Commit and push the full dirty tree.

## Update 2026-05-05 21:24:00Z

- Current task: Broad admin, pod, search, and URL-intent hardening batch is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-072` through `BUG-20260505-080` in `docs/dev/bug-burndown-ledger.md`
  - guarded admin dashboard lists, mesh/realm conflict payloads, pod/member/message/discovery lists, port forwarding lists, search source-provider arrays, and shared stream/pod route path segments
  - documented ADR-0001 gotcha extensions in commits `46af76599` and `4208eca44`
  - validation passed: focused admin/pod/search Vitest batch (`51` tests), Web lint, `git diff --check`, `npm run check:remediation`, and `./bin/lint`
- Next steps:
  1. Commit and push the full dirty tree.

## Update 2026-05-05 21:11:00Z

- Current task: Artist Release Radar API list payload hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-071` in `docs/dev/bug-burndown-ledger.md`
  - guarded subscriptions, notifications, and nested muted release group arrays before render state/counts
  - extended ADR-0001 gotchas `0z304` and `0z306` in commit `52975a792`
  - validation passed: focused Artist Release Radar Vitest suite (`4` tests), Web lint, `git diff --check`, direct `response.data || []` scan, and `npm run check:remediation`
- Next steps:
  1. Commit and push the full dirty tree.

## Update 2026-05-05 20:12:00Z

- Current task: App, Chat, and Rooms API list payload hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-068` through `BUG-20260505-070` in `docs/dev/bug-burndown-ledger.md`
  - guarded navigation activity, saved conversation tabs, joined room tabs, available room options, and join/leave refresh state before list operations
  - extended ADR-0001 gotcha `0z304` in commit `8e489ed2d`
  - validation passed: focused App, Chat, and Rooms Vitest suite (`15` tests), Web lint, `git diff --check`, `npm run check:remediation`, `./bin/lint`, and `dotnet test slskd.sln --no-restore`
- Next steps:
  1. Commit and push the full dirty tree per user request.

## Update 2026-05-05 20:00:00Z

- Current task: Messaging workspace server list hydration hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-067` in `docs/dev/bug-burndown-ledger.md`
  - guarded conversations, joined rooms, pods, and pod channels before Messaging workspace state updates
  - extended ADR-0001 gotcha `0z306` in commit `c14d25ca1`
  - validation passed: focused Messaging Vitest suite (`11` tests), Web lint, `git diff --check`, and `npm run check:remediation`
- Next steps:
  1. Commit and push the full dirty tree per user request.

## Update 2026-05-05 19:49:00Z

- Current task: Discography, Source Provider, and watchlist nested list hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-064` through `BUG-20260505-066` in `docs/dev/bug-burndown-ledger.md`
  - normalized Discography Coverage releases/tracks, Source Provider capability/priority lists, and watchlist expansion candidates before list operations
  - extended ADR-0001 gotcha `0z306` in commit `ebf384d34`
  - validation passed: focused Discography Coverage, Source Providers, and watchlists Vitest suite (`14` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for this nested-list cycle.
  2. Commit and push the accumulated dirty tree when the requested burn-down cycle is complete.

## Update 2026-05-05 19:42:00Z

- Current task: Nested array field shape hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-061` through `BUG-20260505-063` in `docs/dev/bug-burndown-ledger.md`
  - guarded Search Detail user notes, Album Decision rule candidate fields, and Federated Taste nested recommendation fields before map/reduce
  - documented ADR-0001 gotcha `0z306` in commit `abbf1745c`
  - validation passed: focused Search Detail, Album Decision Rules, and Federated Taste Vitest suite (`12` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for this nested-array cycle.
  2. Continue broad scans for remaining frontend workflow and release/network-health defects.

## Update 2026-05-05 19:33:00Z

- Current task: Remaining nested Web API list fallback hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-059` and `BUG-20260505-060` in `docs/dev/bug-burndown-ledger.md`
  - guarded Collections item search and Federated Taste recommendation arrays against malformed non-array payloads
  - extended ADR-0001 gotcha `0z304` in commit `1ae1940ff`
  - validation passed: focused Web API list-shape Vitest suite (`16` tests), and the `response.data?.x || []` scan is clean
- Next steps:
  1. Run Web lint, remediation, and diff checks for the completed list-shape cycle.
  2. Continue broad scans for remaining frontend workflow and release/network-health defects.

## Update 2026-05-05 19:28:00Z

- Current task: Events, Bridge, and Album Completion API list-shape hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-056` through `BUG-20260505-058` in `docs/dev/bug-burndown-ledger.md`
  - guarded System Events lists, Bridge clients, and Album Completion albums/tracks against malformed non-array payloads
  - extended ADR-0001 gotcha `0z304` in commit `2b85b1283`
  - validation passed: focused events, Bridge, and Album Completion Vitest suite (`11` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for this API list-shape cycle.
  2. Continue broad scans for remaining frontend workflow and release/network-health defects.

## Update 2026-05-05 19:20:00Z

- Current task: Discovery Graph persisted-state and response-shape hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-054` and `BUG-20260505-055` in `docs/dev/bug-burndown-ledger.md`
  - centralized saved branch, node, and edge array guards in `src/web/src/lib/discoveryGraph.js`
  - routed DiscoveryGraphAtlas, AtlasPanel, and Modal through those guards
  - extended ADR-0001 gotchas in commits `0acc7e044` and `ae5d59f5e`
  - validation passed: focused discoveryGraph Vitest suite (`6` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for the Discovery Graph cycle.
  2. Continue broad scans for remaining frontend workflow and release/network-health defects.

## Update 2026-05-05 19:07:00Z

- Current task: File Explorer Unicode/base64 route encoding fix is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-053` in `docs/dev/bug-burndown-ledger.md`
  - changed File Explorer file/directory helpers to UTF-8 encode before base64 and URL-encode base64 route segments
  - documented ADR-0001 gotcha `0z305` in commit `9bd03b5e8`
  - validation passed: focused files helper Vitest suite (`2` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for the File Explorer encoding cycle.
  2. Continue broad scans for remaining frontend workflow and release/network-health defects.

## Update 2026-05-05 19:01:00Z

- Current task: Component-local Web API list payload hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-049` through `BUG-20260505-052` in `docs/dev/bug-burndown-ledger.md`
  - guarded Contacts, Collections, Shared With Me, Share Groups, Soulseek Discovery, and Player launcher/browser list state against malformed non-array payloads
  - extended ADR-0001 gotcha `0z304` in commit `1b0fe6398`
  - validation passed: focused Contacts, Soulseek Discovery, and PlayerBar Vitest suite (`23` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for the component list payload cycle.
  2. Continue broad scans for remaining frontend workflow and release/network-health defects.

## Update 2026-05-05 18:48:00Z

- Current task: Web API list helper shape hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-047` and `BUG-20260505-048` in `docs/dev/bug-burndown-ledger.md`
  - changed quarantine-jury and listening-party list helpers to return arrays only for array payloads
  - documented ADR-0001 gotcha `0z304` in commit `710512576`
  - validation passed: focused helper Vitest suite (`4` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for the API helper cycle.
  2. Continue broader API list payload review for component-local `response.data || []` call sites.

## Update 2026-05-05 18:43:00Z

- Current task: Nested playlist/watchlist persisted-state hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-045` and `BUG-20260505-046` in `docs/dev/bug-burndown-ledger.md`
  - hardened Playlist Intake and watchlists against malformed persisted playlist/watchlist entries plus nested track and expansion-candidate entries
  - extended ADR-0001 gotcha `0z303` in commit `883bc625c`
  - validation passed: focused Playlist Intake and watchlist Vitest suite (`26` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for the second persisted-state cycle.
  2. Continue broad bug-class scans across remaining localStorage readers and Web workflow helpers.

## Update 2026-05-05 18:38:00Z

- Current task: Browser-local array item shape hardening is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-039` through `BUG-20260505-044` in `docs/dev/bug-burndown-ledger.md`
  - hardened community quality signals, Discovery Inbox, acquisition plans, Discovery Shelf, album decision rules, and listening history against malformed entries inside array-shaped localStorage values
  - documented ADR-0001 gotcha `0z303` in commit `ddd55071c`
  - validation passed: focused helper Vitest suite (`35` tests)
- Next steps:
  1. Run Web lint, remediation, and diff checks for the accumulated frontend persisted-state batch.
  2. Continue broad bug-class scans instead of stopping at the first finding.

## Update 2026-05-05 18:32:00Z

- Current task: System Events malformed JSON fix is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-038` in `docs/dev/bug-burndown-ledger.md`
  - changed System Events rendering to pretty-print valid JSON and show raw malformed data without crashing
  - documented ADR-0001 gotcha `0z302` in commit `3be065865`
  - validation passed: focused Events test
- Next steps:
  1. Continue broad bug-class scans and run aggregate Web validation after the next frontend batch.

## Update 2026-05-05 18:26:00Z

- Current task: Continued browser-local persisted-state burn-down is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-035` through `BUG-20260505-037` in `docs/dev/bug-burndown-ledger.md`
  - hardened Messaging workspace, audio verification cache, and native visualizer active preset storage against malformed top-level JSON shapes
  - extended ADR-0001 gotcha `0z301` in commit `fd82750b2`
  - validation passed: focused audioVerification, Messaging, and Visualizer tests plus `git diff --check`
- Next steps:
  1. Continue broad bug-class scans instead of stopping at the first finding.

## Update 2026-05-05 18:18:00Z

- Current task: Browser-local object-map persisted-state batch is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-032` through `BUG-20260505-034` in `docs/dev/bug-burndown-ledger.md`
  - hardened automation recipe state/inputs, System Experience preferences, and App room activity timestamps against array-shaped localStorage maps
  - documented ADR-0001 gotcha in commit `d3a9e431b`
  - validation passed: focused App, AutomationCenter, and ExperienceSettings tests plus `git diff --check`
- Next steps:
  1. Run Web lint and remediation baseline for the frontend persisted-state batch.

## Update 2026-05-05 18:10:00Z

- Current task: Search blocked-user persisted-state fix is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-031` in `docs/dev/bug-burndown-ledger.md`
  - changed `getBlockedUsers()` to require an array after parsing localStorage
  - added focused `searches.test.js` coverage for malformed blocked-user storage
  - documented ADR-0001 gotcha in commit `82ea8afb9`
- Next steps:
  1. Run Web lint, remediation, and whitespace checks for the new frontend helper change.

## Update 2026-05-05 18:02:00Z

- Current task: Additional bug-council network-health fix is complete locally.
- Last activity:
  - fixed and verified `BUG-20260505-030` in `docs/dev/bug-burndown-ledger.md`
  - gated all direct multi-source controller `Client.SearchAsync` paths through `TryConsumeSearchBudget`
  - strengthened `scripts/check-soulseek-network-health.sh` for the new controller guardrails
  - documented ADR-0001 gotchas in commits `7b08e99d8` and `b6c6299d0`
  - validation passed: focused `MultiSourceControllerTests` and Soulseek network-health scanner
- Next steps:
  1. Run the broader remediation/lint/test gates for the combined implementation batch.

## Update 2026-05-05 17:47:00Z

- Current task: First bug-council fix batch is complete locally.
- Last activity:
  - fixed verified rows `BUG-20260505-011` through `BUG-20260505-029` in `docs/dev/bug-burndown-ledger.md`
  - covered Web URL/state regressions, persisted tab shape validation, unsupported private room creation, room API malformed list responses, action labels/tooltips, Soulseek multi-source limiter use, backfill cancellation/probe fail-closed behavior, release tmpfiles/AUR drift, secret-safe logs, no-redirect notification/Spotify callers, stricter scanners, mounted Users URL changes, default Docker non-root execution, and vendored room-list negative count rejection
  - documented ADR-0001 gotchas in commits `034c8ee83` and `fc2fdc737`
  - validation passed: remediation baseline, packaging metadata/copy checks, Web lint, repo lint, `git diff --check`, focused Web/backend/vendor tests, and full `dotnet test slskd.sln --no-restore`
- Next steps:
  1. Review and commit the implementation batch.

## Update 2026-05-05 17:07:38Z

- Current task: Bug council ledger and remediation scanner setup is complete locally.
- Last activity:
  - added `docs/dev/bug-burndown-ledger.md` with taxonomy, operating rules, discovery commands, accepted scanner rows, and expert council intake findings
  - added and registered targeted remediation baseline scripts for URL intent, primitive JSON string bodies, mutating endpoint roles, guarded outbound HTTP, path containment, Soulseek network health, workflow trigger policy, release asset names, config drift, and systemd permissions
  - tuned the new checks to pass only high-confidence known guardrails and leave broader inventories in the ledger for triage
  - validation passed: `npm run check:remediation`, `./bin/lint`, `dotnet test slskd.sln --no-restore`, packaging metadata/copy checks, and `git diff --check`
- Next steps:
  1. Triage the `New` council intake rows in `docs/dev/bug-burndown-ledger.md`.
  2. Start the first fix batch with high-confidence High severity rows after adding focused repro tests.

## Update 2026-05-05 16:49:44Z

- Current task: Adjacent Web UI bug hunt is complete locally.
- Last activity:
  - fixed raw string body helpers for YAML config validation/update and Pod content ID validation
  - extended user-target navigation URLs so Browse, Users, and Chat work from direct URLs/new tabs instead of relying only on router state
  - checked the remaining primitive `[FromBody]` endpoints and found existing frontend helpers for conversations, room messages, room join, and server disconnect already use JSON string bodies; room ticker/member invite and event raise have no current Web UI callers
  - documented ADR-0001 gotcha extensions in separate commits `323950f89` and `87702b418`
  - validation passed: focused options/MediaCore/Browse/Chat/Users Vitest, Web lint, repo lint, and `git diff --check`
- Next steps:
  1. Review and commit the remaining URL-navigation code/test/memory-bank changes.

## Update 2026-05-05 16:43:30Z

- Current task: Soulseek room join/create Web UI fix is complete locally.
- Last activity:
  - changed `rooms.join()` to post the room name as a JSON string literal for ASP.NET `[FromBody] string` binding
  - confirmed room creation delegates to the same join path, so existing-room joins and new public-room attempts use the fixed request shape
  - added focused Web API wrapper and backend controller coverage
  - documented ADR-0001 gotcha `0z291` and committed it separately as `836c32df2`
  - validation passed: focused Web `rooms.test.js`, 2/2; focused `RoomsControllerTests`, 4/4; Web lint; `git diff --check`
- Next steps:
  1. Review and commit the rooms code/test/memory-bank changes.
  2. Continue the prior security-audit commit/push flow when ready.

## Update 2026-05-05 16:38:00Z

- Current task: Browse/search-result new-tab fix is complete locally.
- Last activity:
  - changed search result peer Browse links to encode the target as `/browse?user=...` while preserving router state for same-tab navigation
  - changed Browse to open requested user tabs from either URL query or router state, so direct loads/new tabs no longer lose the username
  - added focused Browse URL/state regression coverage
  - documented ADR-0001 gotcha `0z290` and committed it separately as `9e921bd17`
  - validation passed: focused Browse/Search Vitest, 8/8; Web lint; repo lint; `git diff --check`
- Next steps:
  1. Review and commit the Browse/Search code/test/memory-bank changes.
  2. Continue the prior security-audit commit/push flow when ready.

## Update 2026-05-05 16:34:13Z

- Current task: Follow-up security audit fix batch is complete locally.
- Last activity:
  - documented ADR-0001 gotcha `0z288` in separate commit `efda5cfcb`
  - fixed API rate limiting so raw `Authorization`/`X-API-Key` headers no longer bypass anonymous `/api` limits before authentication succeeds
  - added a shared no-redirect guarded outbound HTTP client that validates connected IPs, and moved guarded webhook/source-feed/SongID/Solid/HTTP/WebDAV/moderation callers onto it where appropriate
  - hardened relay-agent download notifications with destination `PathGuard` validation, no redirect-following, and a 1 GiB bounded copy
  - bounded pod mesh downloads into small chunks instead of materializing whole remote files in memory, and added the missing YAML update size check
  - validation passed: focused LLM/outbound/search unit tests, full `dotnet test slskd.sln --no-build`, `./bin/lint`, guarded-client/rate-limit/path scans, and `git diff --check`
- Next steps:
  1. Commit and push the security implementation changes.
  2. Cut a new build tag only when explicitly ready for the release.

## Update 2026-05-05 16:23:45Z

- Current task: Wishlist search stability fix is complete locally.
- Last activity:
  - changed Wishlist execution to use normal `SearchScope.Network` searches so saved Wishlist runs match manual Search view reruns
  - preserved producer attribution by passing safety/logging source `wishlist`
  - changed manual/bulk Wishlist run responses to return the completed search record after polling, so UI callers see completed response counts
  - checked adjacent Wishlist creators/callers: Lidarr wanted sync, MusicBrainz discography promotion, Library Bloom promotion, taste recommendation promotion, CSV import, Wishlist manual/bulk run, and Automation Center retry
  - added focused unit coverage for the Wishlist search scope/source/result-count contract
  - documented ADR-0001 gotcha `0z289` and committed it separately as `219d8a0f5`; amended the completion-result gotcha in `64c1d922d`
  - validation passed: focused `WishlistControllerTests`, 9/9; focused Wishlist/AutomationCenter Vitest, 13/13; `git diff --check`; `./bin/lint`
  - full `dotnet test slskd.sln --no-restore` completed with unrelated existing unit failure `LlmModerationTests.HttpLlmModerationProvider_ParseFailure_DoesNotLeakParserDetails` expecting `Failed to parse LLM response` but receiving `LLM service error`; integration and non-unit test projects passed
- Next steps:
  1. Review and commit the Wishlist code/test/memory-bank changes.
  2. Continue the prior security-audit commit/push flow when ready.

## Update 2026-05-05 16:08:51Z

- Current task: Comprehensive follow-up security audit fixes are complete locally.
- Last activity:
  - documented ADR-0001 gotcha `0z287` in separate commit `29514712e`
  - added explicit role requirements to every non-anonymous mutating controller action
  - made SongID run creation admin-only, limited local-file analysis to configured media roots, and guarded/capped Spotify fetches
  - hardened source-feed provider URL handling with exact host matches, outbound guard checks, and response-size caps
  - bounded anonymous pod verification/discovery inputs, tightened security diagnostic roles, removed script argument logging, required release installer checksum verification, and moved ListenBrainz tokens to session storage
  - validation passed: backend build, full `dotnet test slskd.sln --no-build`, `./bin/lint`, focused PlayerBar Vitest, Web lint, mutating endpoint role scan, source-feed/SongID/script risky-pattern scan, packaging metadata validation, and `git diff --check`
- Next steps:
  1. Commit and push the implementation changes.
  2. Cut a new build tag only when explicitly ready for the release.

## Update 2026-05-05 10:28:00Z

- Current task: Security and bug-hunt fix batch is complete locally.
- Last activity:
  - fixed remote-path traversal, relay path/upload limits, mutating API authorization, config/dump/secret handling, streaming ticket/content lookup limits, multi-source path containment, stable IDs/seeds, FTS parameterization, login attempt pruning, governance/pod/federation signature fail-closed behavior, and affected PPA/RPM/upstream release metadata
  - documented ADR-0001 gotchas in separate commits `40770fe6d` and `3c33ceea6`
  - validation passed: full `dotnet test slskd.sln --no-build`, `./bin/lint`, controller CSRF check, anonymous endpoint check, packaging metadata validation, sensitive placeholder scan, and web fetch CSRF scan
- Next steps:
  1. Review and commit/push the implementation changes.
  2. Create a new build tag only when ready to cut the requested release.

## Update 2026-05-05 09:34:00Z

- Current task: Systemd package permission fix is complete locally.
- Last activity:
  - documented ADR-0001 gotcha `0z286` and committed it separately as `d5cee636a`
  - added AUR `tmpfiles.d` rules for daemon-owned config/data paths
  - added `UMask=0002` to packaged and release-installer systemd units
  - updated the release installer to repair existing config/data ownership and group-write modes
  - extended tag-release AUR/RPM/PPA packaging to carry `slskd.tmpfiles`
  - documented optional `slskd` group membership for non-root human access to config/download paths
  - validation passed: AUR hash validation, packaging metadata validation, release workflow YAML parse, shell syntax checks, `git diff --check`, and repo lint
- Next steps:
  1. Commit/push the permission fix and publish updated AUR package metadata if desired.

## Update 2026-05-05 09:06:43Z

- Current task: Non-bin AUR source package .NET prune metadata fix and release-channel hardening are complete locally.
- Last activity:
  - added `-p:AllowMissingPrunePackageData=true` to the source AUR `dotnet publish` command
  - bumped `packaging/aur/PKGBUILD` from `pkgrel=3` to `pkgrel=4`
  - added packaging metadata validation for the publish property
  - hardened `bin/publish`, tag release, standalone Linux, COPR, and PPA publish commands with the same property
  - documented ADR-0001 gotcha `0z285` and committed it separately as `aa19d6f24`
  - validation passed: packaging metadata validation, AUR-shaped local `dotnet publish`, release workflow YAML parse checks, and `bin/publish` shell syntax
- Next steps:
  1. Push the packaging/release hardening commits and publish the updated AUR source package metadata if desired.

## Update 2026-05-05 06:17:11Z

- Current task: Release-preflight live mesh test gate is complete locally.
- Last activity:
  - found the broad test blocker was the optional live-account mesh smoke running because ignored local credential files exist
  - added explicit `SLSKDN_RUN_LIVE_MESH_ACCOUNT_TESTS` opt-in before the test can attempt public Soulseek login
  - documented the live-smoke gotcha in ADR-0001 as `0z283` and committed it separately as `e61cac61b`
  - documented the opt-in flag in the integration test README
  - validation passed: focused `TwoNodeMeshFullInstanceTests` integration slice, 3/3 passed
- Next steps:
  1. Run broader release preflight checks and commit/push the test-gate fix.

## Update 2026-05-05 06:12:38Z

- Current task: VPN ingress banner port-source correction is complete locally.
- Last activity:
  - confirmed the banner was using hard-coded default constants for current ports
  - changed current ingress rows to derive Soulseek peer/file transfer from `soulseek.listen_port`
  - changed mesh/DHT rows to derive from `dht.overlay_port` and `dht.dht_port`, splitting TCP and UDP rows when configured ports differ
  - documented ADR-0001 gotcha `0z282` and committed it separately as `cf6743cda`
  - validation passed: focused `App.test.jsx` and App lint
- Next steps:
  1. Commit and push the UI fix.

## Update 2026-05-05 05:59:50Z

- Current task: Project assessment cleanup follow-up is complete locally.
- Last activity:
  - fixed the remediation docs command checker to validate root package scripts by absolute repo path instead of caller cwd
  - made the root remediation npm command surface a tracked minimal `package.json` instead of an ignored local spillover file
  - made `bin/lint` executable so documented `./bin/lint` usage works
  - reconciled stale audit entries for T-1405, T-1410, and Phase 8 implementation status
  - clarified build docs so manual deploys use `bin/build`/`bin/publish` or explicitly copy `src/web/build` into backend `wwwroot`
  - validation passed: root and `src/web` remediation baseline checks, direct `./bin/lint`, `git diff --check`, and focused chunk reassignment/jobs pagination unit tests
- Next steps:
  1. Commit the cleanup changes on `main`.

## Update 2026-05-05 01:40:00Z

- Current task: Footer build info and GitHub update check are complete locally.
- Last activity:
  - added a public `/api/v0/application/build` payload for logged-out footer build metadata
  - changed GitHub release checks to use `snapetech/slskdn` and compare slskdN date-versioned release tags plus manual builds
  - updated the shared footer to display the current build for both auth states and show an update link when GitHub has a newer release
  - documented the anonymous build metadata endpoint and regenerated route inventory
  - validation passed: focused backend tests, focused footer test, remediation baseline, backend build, web lint/build, and repo lint via `bash ./bin/lint`
- Next steps:
  1. Commit and push the footer/update-check changes if desired.

## Update 2026-05-05 01:25:00Z

- Current task: Project gap assessment fixes are committed, pushed, validated, and deployed to `kspls0`.
- Last activity:
  - pushed the full dirty tree requested by the user, including unrelated local changes
  - added the final versioned route alias tranche for active legacy native slskdN, VirtualSoulfind, and Audio controllers
  - wired the remediation docs command-reference check into the combined remediation baseline
  - validation passed: `npm run check:remediation`, full `dotnet test`, and full `./bin/build`
  - deployed `0.0.0-slskdn.manual.20260505010919.48e7e08771f8` to `/usr/lib/slskd/releases/manual-20260505010919.48e7e08771f8` on `kspls0`
  - restarted `slskd.service` and verified fresh logs show the new release path, version, host binding, and initialization completion
- Next steps:
  1. User testing on `http://kspls0:5030/`.

## Update 2026-05-05 00:00:00Z

- Current task: Project gap assessment fixes are complete locally.
- Last activity:
  - aligned CI/E2E PR triggers to `main` and kept enhanced CI off branch-push builds
  - wired remediation baseline checks into the release gate and added npm script aliases for the check scripts
  - documented vendored runtime test expectations, capability descriptor semantics, federation diagnostics, and corrected over-optimistic packaging TODO entries
  - replaced Library Health album-completion placeholder job IDs with real missing-recording remediation download requests
  - fixed playback buffer priority thresholding and documented ADR-0001 gotcha `0z278` in commit `bbb7bdcf9`
  - split MediaCore workflow helpers/data into smaller modules without changing page behavior
  - validation passed: remediation baseline checks, focused backend tests, focused MediaCore test/lint, frontend production build, `git diff --check`, and `bash ./bin/lint`
- Next steps:
  1. Review and commit the local implementation/doc changes when ready.

## Update 2026-05-03 00:00:00Z

- Current task: AUR source package archive-root casing fix is complete locally.
- Last activity:
  - reproduced the tester failure by listing the `2026050100-slskdn.218` GitHub archive root as `slskdN-2026050100-slskdn.218/`
  - documented ADR-0001 gotcha `0z277` and committed it separately
  - changed the source PKGBUILD to use `_archive_root="slskdN-${pkgver//.slskdn/-slskdn}"` in both `build()` and `package()`
  - added packaging metadata validation so the lower-case archive-root path does not return
  - bumped the source AUR package to `2026050100.slskdn.218-3` and pushed the live AUR `slskdn` repo to `9ee2fb6`
- Next steps:
  1. Monitor tester retry of `yay -Syu slskdn`.

## Update 2026-05-01 20:33:13Z

- Current task: Shared DHT/UDP overlay/QUIC demux is corrected and deployed to `kspls0`.
- Last activity:
  - corrected the mistaken either-or QUIC/UDP overlay model; QUIC is an additional mesh transport, not a replacement for DHT rendezvous or UDP overlay
  - deployed `0.0.0-slskdn.manual.20260501202734.5d3478cb60fd` from commit `5d3478cb60fd` to `/usr/lib/slskd/releases/manual-20260501202734.5d3478cb60fd`
  - verified live sockets: public UDP `50305`, backend UDP `55305`, TCP `5030`/`50300`/`50301`/`50305`, and no stale UDP `50400`/`50306`/`50401`/`50402`
  - verified startup logs show the shared UDP listener with `UDP overlay enabled=True; QUIC backend=127.0.0.1:55305`
  - verified a workstation QUIC probe to `kspls0:50305` succeeds and post-probe logs have no decode-envelope warnings or exceptions
- Next steps:
  1. Push local commits.

## Update 2026-05-01 19:43:00Z

- Current task: Logged-out Web UI title tagline removal is complete locally.
- Last activity:
  - changed `src/web/index.html`, `src/web/public/index.html`, and `src/web/public/manifest.json` to use `slskdN` without the old unofficial-fork tagline
  - validation passed: stale-title search, focused `LoginForm`/`App` tests, Web UI production build, `bash ./bin/lint`
- Next steps:
  1. Push local commits when ready.

## Update 2026-05-01 19:10:00Z

- Current task: Review-reported non-security correctness bugs are fixed locally.
- Last activity:
  - fixed admin restart argument handling so the restarted process receives argv[1..], not a duplicated executable path
  - fixed streaming setup failure cleanup so the limiter slot is released if setup throws before `ReleaseOnDisposeStream` takes ownership
  - tightened passthrough no-auth XML docs to state that remote access requires both `AllowRemoteNoAuth=true` and a matching non-empty `AllowedCidrs`
  - added focused regression tests for restart argument trimming and stream setup failure limiter release
  - documented ADR-0001 gotchas `0z268`, `0z269`, and `0z270`
  - validation passed: focused controller tests, `bash ./bin/lint`, full `dotnet test`
- Next steps:
  1. Push local commits when ready.

## Update 2026-05-01 19:36:58Z

- Current task: Focused network/runtime validation is complete locally.
- Last activity:
  - ran the focused unit network/runtime slice (`123` tests passed)
  - ran the focused integration/full-instance mesh slice (`80` tests passed)
  - smoke-checked kspls0 service state, listener bindings, API health/options, native Soulseek global recommendations, and QUIC handshake through shared public mesh UDP `50305`
  - found a vendored runtime listener shutdown unobserved task exception in kspls0 logs
  - documented ADR-0001 gotcha `0z274` and committed it separately
  - patched `slskNet.Runtime` so `Forget()` observes swallowed faults and listener accept loops exit cleanly after `Stop()`
- Next steps:
  1. Commit and push the runtime shutdown fix plus memory-bank updates.
  2. Deploy the patched runtime to `kspls0` if we want the live instance to pick up the listener shutdown fix now.

## Update 2026-05-01 19:59:47Z

- Current task: Current repo build is deployed and smoke-tested on `kspls0`.
- Last activity:
  - built and deployed `0.0.0-slskdn.manual.20260501195252.8de0ba700dba` from commit `8de0ba700dba`
  - verified deliberate restart no longer logs the vendored listener shutdown `Not listening` unobserved task exception
  - found and fixed normal QUIC probe disconnects logging as warning stack traces
  - documented ADR-0001 gotcha `0z275` and committed it separately
  - verified live API version, Soulseek login, listeners on `5030`, `50300`, `50301`, `50305`, shared UDP `50305`, backend UDP `55305`, clean QUIC handshake through `50305`, and a live `test mp3` search returning `251` responses / `5379` files
  - validation passed: full `bin/publish --runtime linux-x64`, including Web UI tests/build, Release backend build, unit/API/integration suites, and publish
- Next steps:
  1. Push local commits.

## Update 2026-05-01 20:11:53Z

- Current task: kspls0 log inspection found the UDP overlay still needed to share the compact mesh UDP port.
- Last activity:
  - inspected current `kspls0` service logs and sockets
  - found UDP overlay `50400` still bound separately even though the compact public mesh UDP port should be `50305`
  - corrected the earlier either-or assumption: QUIC is an alternate mesh transport and does not supersede DHT rendezvous or UDP overlay
  - updated ADR-0001 gotcha `0z276` to say the shared mesh UDP listener must demux DHT, UDP overlay, and QUIC
  - changed the shared listener to route DHT packets, UDP overlay control envelopes, and QUIC proxy sessions on one public UDP socket
- Next steps:
  1. Run focused tests and redeploy the corrected shared-port build to `kspls0`.

## Update 2026-05-01 18:55:00Z

- Current task: Current repo code is built and running on `kspls0` for user testing.
- Last activity:
  - stopped `slskd.service` on `kspls0`
  - fixed stale obfuscation-only validation tests and documented ADR-0001 gotcha `0z266`
  - built and published `0.0.0-slskdn.manual.20260501185541.9d639c7d3.dirty` for Linux x64 after the vendored runtime hardening changes landed in the checkout
  - deployed `/usr/lib/slskd/releases/manual-20260501185541.9d639c7d3.dirty` on `kspls0`
  - corrected the stable launcher so it forwards `"$@"` and systemd keeps using `/etc/slskd/slskd.yml`; documented ADR-0001 gotcha `0z267`
  - verified live API runtime path, `/etc/slskd/slskd.yml`, Soulseek login, shares ready, DHT bootstrap, Web UI HTTP, and listeners on `5030`, `50300`, `50301`, and `50305`
  - validation passed: full `bin/publish --runtime linux-x64`, `bash ./bin/lint`
- Next steps:
  1. User testing on `http://kspls0:5030/`.
  2. Push local commits when ready.

## Update 2026-05-01 18:19:00Z

- Current task: Shared DHT/QUIC UDP demux and QUIC overlay restoration are complete locally and deployed to `kspls0`.
- Last activity:
  - changed the public mesh/DHT/QUIC overlay to use shared UDP `50305` with MsQuic listening on loopback backend UDP `55305`
  - restored QUIC overlay default enablement and advertised public QUIC port `50305`
  - added the shared UDP listener/proxy and tests for DHT routing, QUIC packet routing, and QUIC endpoint selection
  - fixed the multi-address wildcard UDP source-IP bug by binding per local IPv4 address and documented it as ADR-0001 gotcha `0z262`
  - updated `kspls0` host nftables and VPN ingress compact UDP configuration from stale `50306`/`50400` to shared UDP `50305`
  - deployed `/usr/lib/slskd/releases/manual-quicshare-20260501181348` on `kspls0`
  - verified live LAN QUIC handshake to `kspls0:50305`, live shared UDP sockets, DHT startup, QUIC backend startup, and overlay DHT announcement
  - live API search smoke ran but returned no responses, so no weak download candidate was queued
  - validation passed: `bash ./bin/lint`, Web UI App tests, Web UI production build, focused backend tests, full `dotnet test --no-restore`
- Next steps:
  1. Commit and push the shared DHT/QUIC port work.
  2. Trigger a tag build only if a release is intentionally requested.

## Update 2026-05-01 18:08:00Z

- Current task: Launchpad SFTP PPA upload setup is complete on GitHub and blocked on Launchpad account key registration.
- Last activity:
  - generated `~/.ssh/slskdn_launchpad_ppa_ed25519`
  - stored `LAUNCHPAD_SFTP_KEY` and `LAUNCHPAD_SFTP_USER` in `snapetech/slskdn` GitHub repository secrets
  - updated both PPA workflows to prefer IPv4-pinned SFTP when the key is configured and retain anonymous FTP as fallback
  - documented the SFTP-preferred PPA upload, SFTP IPv4-pinning, and SFTP auth-hang gotchas as separate commits
  - added non-interactive SSH options, a bounded SFTP auth probe, and a bounded `dput` upload timeout after the first IPv4 SFTP upload retry hung inside `ssh`
  - reran the standalone PPA workflow for `2026050100-slskdn.215`; GitHub reached Launchpad SFTP over IPv4 and failed fast with `Permission denied (publickey)`
  - tested obvious local private keys against Launchpad SFTP; none completed a noninteractive login
- Next steps:
  1. Register `~/.ssh/slskdn_launchpad_ppa_ed25519.pub` on the `~keefshape` Launchpad account.
  2. Rerun the standalone PPA workflow for `2026050100-slskdn.215` after Launchpad accepts the key.

## Update 2026-05-01 17:35:00Z

- Current task: slskdN runtime vendoring, static ingress notice cleanup, Docker Hub release-channel work, and `kspls0` deployment are complete.
- Last activity:
  - vendored `slskNet.Runtime` under `vendor/slskNet.Runtime` and moved app/test project references to the in-repo project
  - corrected the ingress-port migration notice to a static five-old-forwards versus two-current-forwards list with no public IPs, active/not-reported status, or obfuscation listener row
  - documented the static-notice gotcha immediately in ADR-0001 as `e83d0c396`
  - rebuilt the Web UI bundle locally and started full `bin/build`; frontend tests, frontend production build, backend build, unit tests, and API tests passed before the integration test run was interrupted by the user
  - added Docker Hub tags to release Docker jobs and configured `DOCKERHUB_USERNAME` / `DOCKERHUB_TOKEN` in `snapetech/slskdn`
  - reran full `bin/build`; Web UI tests/build, backend build, unit tests, API tests, and integration tests passed
  - deployed `/usr/lib/slskd/releases/manual-portnotice-20260501174927` to `kspls0`
  - deployed `/usr/lib/slskd/releases/manual-slsknet-repo-20260501173428` to `kspls0`
  - confirmed live asset text contains the reduced-port copy and no active/not-reported status, public endpoint text, `50301`, `PREVIOUS`, or `CURRENT`
  - confirmed live listeners on `50300/tcp`, `50301/tcp`, and `50305/tcp+udp`
  - live-smoked Soulseek search and a small download; the download completed successfully
  - updated `bin/lint` to exclude vendored runtime source from slskdN formatting enforcement while keeping the project reference for builds
- Next steps:
  1. Commit and push the corrected static ingress notice plus remaining release-channel cleanup.
  2. Trigger the next `build-main-*` tag only when a release build is intentionally wanted.

## Update 2026-05-01 17:30:00Z

- Current task: Launchpad SFTP PPA upload setup is in progress.
- Last activity:
  - generated `~/.ssh/slskdn_launchpad_ppa_ed25519`
  - stored `LAUNCHPAD_SFTP_KEY` and `LAUNCHPAD_SFTP_USER` in `snapetech/slskdn` GitHub repository secrets
  - updated both PPA workflows to prefer IPv4-pinned SFTP when the key is configured and retain anonymous FTP as fallback
  - documented the SFTP-preferred PPA upload, SFTP IPv4-pinning, and SFTP auth-hang gotchas as separate commits
  - added non-interactive SSH options, a bounded SFTP auth probe, and a bounded `dput` upload timeout after the first IPv4 SFTP upload retry hung inside `ssh`
- Next steps:
  1. Validate workflow YAML and run the standalone PPA upload workflow for `2026050100-slskdn.215`.
  2. Register the generated public key on the `~keefshape` Launchpad account if SFTP authentication rejects it.

## Update 2026-05-01 17:17:00Z

- Current task: slskdN is switched to `slskNet.Runtime` and deployed on `kspls0`.
- Last activity:
  - changed slskdN app/test project references to use the sibling `../slskNet.Runtime` runtime project
  - wired `soulseek.obfuscation` options into `SoulseekClientOptions` and `SoulseekClientOptionsPatch`
  - changed type-1 obfuscation runtime reporting from pending to active
  - published a `linux-x64` self-contained artifact and installed it as `/usr/lib/slskd/releases/manual-slsknet-runtime-20260501171217`
  - restarted `slskd.service`; confirmed Soulseek login, regular listener `50300`, obfuscated listener `50301`, and successful live search/download smoke
  - validated targeted startup/options tests, transfer tests, full unit suite, and repo lint
- Next steps:
  1. Commit the slskNet.Runtime wiring separately from the pre-existing PPA workflow changes if this branch is being prepared for review.
  2. If we want CI to build this without a sibling checkout, publish `slskNet.Runtime` to a private package feed or add a documented submodule/source-fetch step.

## Update 2026-05-01 16:09:39Z

- Current task: PPA reachability fix is complete locally.
- Last activity:
  - confirmed the successful `.197` PPA release used Launchpad anonymous FTP, so SFTP/SSH secrets were not the historical requirement
  - confirmed local and `kspls0` TCP reachability to `ppa.launchpad.net:21`
  - documented the IPv4 reachability gotcha in ADR-0001 and committed it separately
  - updated `build-on-tag.yml` and `release-ppa.yml` to pin `ppa.launchpad.net` to a resolved IPv4 address
  - added a Python TCP preflight so runner logs fail early if Launchpad FTP is unreachable
  - replaced the `dput` transfer with signed source verification plus bounded anonymous `curl` FTP uploads after IPv4-pinned `dput` reached Launchpad but failed during passive data transfer
- Validation:
  - Python TCP connect to Launchpad FTP passed locally
  - PyYAML parsed both edited workflows
  - `git diff --check`
- Next steps:
  1. Push the workflow fix and rerun the standalone PPA workflow for `2026050100-slskdn.215` to verify upload.

## Update 2026-05-01 03:30:00Z

- Current task: Obsolete slskdn-dev package channel cleanup is complete locally.
- Last activity:
  - reconciled completed QR invite display/scanning, DHT adaptive bootstrap diagnostics, native MilkDrop3 T-938, SongID parity notes, analyzer cleanup, and production placeholder burn-down tasks
  - removed the active `slskdn-dev` package channel instead of keeping the blocked re-enable task
  - stripped `build-dev-*` handling and dev package jobs from the tag release workflow
  - removed stale dev-only package manifests, docs, and release-note template surfaces
  - reduced the production placeholder scan to the allowed `FeatureNotImplementedException` gate infrastructure
  - cleared stale MeshSync test wording after the unavailable-transport message cleanup
  - restored `/tmp` test capacity by deleting generated `/tmp/slskd_*.dmp` files before rerunning the broad suite
- Validation:
  - `bash packaging/scripts/validate-release-copy.sh`
  - `bash packaging/scripts/validate-packaging-metadata.sh`
  - `git diff --check`
  - `npm test -- Contacts.test.jsx`
  - `npm run lint`
  - `bash ./bin/lint`
  - `dotnet format --verify-no-changes --no-restore --verbosity minimal`
  - `dotnet test --filter "FullyQualifiedName!~OptionalLiveAccounts_CanSearchAndDownloadHostedProbeOverOverlayMesh" --no-restore`
- Next steps:
  1. Preserve concurrent player/docs changes in the dirty tree; do not revert them during follow-up cleanup.

## Update 2026-05-01 03:20:30Z

- Current task: MilkDrop/Butterchurn compact player tile is deployed and live-smoke verified on `kspls0`.
- Last activity:
  - confirmed the live app serves `index-B9qp0NHC.js` and `index-DwvOKuMB.css`
  - added native MilkDrop WebGPU-to-WebGL2 fallback behavior for browsers without `navigator.gpu`
  - verified the live systemd process uses `/etc/slskd/slskd.yml` and listens on `0.0.0.0:5030`
  - ran headless Playwright against `http://kspls0:5030/playlist-intake`
  - confirmed Butterchurn, MilkDrop3 WebGL2, MilkDrop3 WebGPU, and WebGL2 retry all show visible visualizer canvases without analyzer fallback or error text
  - confirmed the compact visualizer controls remain a single 112x18 px row
  - captured nonblank tile screenshots for Butterchurn, native WebGL2, and native WebGPU
  - documented and committed the WebGPU fallback gotcha as `dd7f0143a`
  - validated focused Player visualizer tests and frontend lint
- Next steps:
  1. Hard refresh `/playlist-intake` if a browser tab is holding an older service-worker cache.
  2. Continue unrelated production placeholder burn-down only after preserving the current visualizer state.

## Update 2026-05-01 03:22:00Z

- Current task: Production placeholder burn-down is in progress.
- Last activity:
  - scoped the burn-down to production markers while excluding UI input
    placeholders, tests, generated assets, archive docs, and historical
    memory-bank notes
  - started P1 by removing fake hard-coded swarm analytics efficiency values
  - added unit coverage for derived efficiency metrics and empty trend output
  - cleaned the consensus policy success reason and added coverage for
    consensus-required denial versus consensus-not-required allowance
- Next steps:
  1. Validate the focused consensus policy tests.
  2. Continue with intentional disabled-feature cleanup and Mesh/Service Fabric
     streaming markers after the current dirty obfuscation lane settles.

## Update 2026-05-01 02:56:49Z

- Current task: Soulseek type-1 obfuscation default posture and README coverage are complete locally.
- Last activity:
  - changed `soulseek.obfuscation.enabled` to default `true`
  - kept default mode as `compatibility`, with `advertise_regular_port: true` and `prefer_outbound: true`
  - renamed the current runtime state to `configured_pending_runtime`
  - changed startup logging from warning to informational because the default mode preserves normal fallback and is currently a no-op until runtime support lands
  - added top-level README and docs-index entries for the feature
  - updated config docs, example YAML, and the dedicated guide to document default-on compatibility semantics
  - validated focused backend and Network tab tests, repo lint, and touched-file whitespace checks
- Next steps:
  1. Runtime activation still needs Soulseek.NET hooks or a slskdN-owned peer-message transport adapter.
  2. Keep `only` mode explicit and non-default because it can break clients that ignore obfuscated metadata.

## Update 2026-05-01 03:05:00Z

- Current task: Feature-expansion documentation hygiene is complete locally.
- Last activity:
  - fixed the documentation audit so the tracked current-doc broken-link list is
    marked resolved instead of still actionable
  - added historical snapshot banners to dated test/status planning docs
  - renamed pod-scoped tunnel docs in public prose to Pod Private Service
    Gateway and distinguished them from the host VPN agent
  - added focused host VPN agent companion guides for Linux/WireGuard, external
    tunnels, Windows/macOS, and the API contract
  - added the Pod Private Service Gateway guide to the docs index
  - verified targeted stale-link and stale-audit scans
- Next steps:
  1. Keep historical phase/audit docs clearly marked when they are retained in
     current paths.

## Update 2026-05-01 02:41:29Z

- Current task: Soulseek type-1 obfuscation feature options are integrated locally.
- Last activity:
  - inspected the sibling research workspace and confirmed public-server type-1 peer-message obfuscation metadata/transport is strong enough to design slskdN feature options
  - added `soulseek.obfuscation` settings for enablement, mode, dedicated port, regular fallback advertisement, and outbound preference
  - added validation and a runtime plan that reports the current Soulseek.NET activation gap as `configured_unsupported`
  - exposed the plan through the slskdN capabilities API and System -> Network status
  - updated config docs, example YAML, and the dedicated Soulseek type-1 obfuscation guide
  - validated focused backend and Network tab tests, frontend lint, repo lint, broad non-live `.NET` tests, and touched-file whitespace checks
- Next steps:
  1. Runtime activation requires Soulseek.NET hooks or a slskdN-owned peer-message transport adapter for SetWaitPort metadata, obfuscated listener accept, and obfuscated outbound dial.
  2. Keep compatibility/prefer modes as the practical defaults; only mode remains explicit because it breaks clients that ignore obfuscated metadata.

## Update 2026-05-01 02:41:03Z

- Current task: Feature-expansion public docs and README listings are updated locally.
- Last activity:
  - updated README with Acquisition Review, System admin surfaces, unified Messages, System Policies/Experience, native MilkDrop maturity, and links to new guides
  - refreshed `docs/getting-started.md` for current ports, default credentials, System tour, manual Search vs Acquisition Review, unified Messages, and conservative multi-source wording
  - added `docs/system-surfaces.md`, `docs/pods-and-rooms.md`, and `docs/songid-discovery.md`
  - updated docs index, feature overview, advanced features, config cross-links, Web UI surface audit, documentation audit, task log, and progress log
- Next steps:
  1. Treat Essentia/MIR, cover-similarity, embedding clustering, and real preset/device visualizer measurement as research/runtime scope, not documentation gaps.
  2. Remaining documentation cleanup is broad hygiene: broken-link cleanup, VPN guide splitting, and historical snapshot banners.

## Update 2026-05-01 02:20:56Z

- Current task: Remaining admin policy and experience Web UI surfaces are implemented locally.
- Last activity:
  - added System -> Policies and System -> Experience and wired them into the System tab router
  - surfaced guided YAML controls for webhooks/scripts, transfer policy, security/access, search/network policy, and retention/storage
  - included DHT LAN-only/bootstrap/ports, Scene Pod Bridge, rescue mode, HTTP rate windows, HTTPS certificate replacement, API/JWT secret replacement, transfer retry/schedule/auto-replace, and share media-probe controls
  - surfaced browser-local Search, Discovery Inbox, Player, and Messages preferences for page-specific backfill
  - kept the surfaces passive/configuration-only with no hook execution, peer contact, provider validation, daemon restart, transfer mutation, file mutation, or page behavior changes until follow-up code consumes settings
  - documented and committed the transfer YAML alias gotcha as `8cdb31d9e`
  - validated focused AdminPolicies/ExperienceSettings tests, touched AdminPolicies/ExperienceSettings/System ESLint, full Web UI lint, frontend production build, repo lint, and touched-file whitespace checks
- Next steps:
  1. Let backend lanes backfill live action execution behind these surfaces where needed.
  2. Let page-specific frontend lanes consume the browser-local Experience preferences where those pages need live behavior.
  3. Full repo `git diff --check` is still blocked by existing trailing whitespace in `docs/design/sharegroups-collections-streaming-assessment.md`.

## Update 2026-05-01 02:39:15Z

- Current task: NixOS VM smoke automation follow-up is complete locally.
- Last activity:
  - added `packaging/scripts/run-nixos-vm-smoke.sh`
  - the script builds a minimal NixOS VM around the flake package and required slskd module options
  - it boots headless with QEMU/KVM when available, waits for `slskd.service` to become active, and reports a serial success marker
  - it skips cleanly when Nix, Linux, or KVM are unavailable, with explicit TCG opt-in for slower runs
  - validated shell syntax; local execution skipped because Nix is not installed
- Next steps:
  1. Run final whitespace, lint, and broad non-live .NET validation for the accumulated backend/middle batch.
  2. Remaining unchecked entries are active visualizer work, artifact-dependent dev flake release work, live-runtime diagnostics, repo-wide analyzer debt, QR-library placeholder work, or future SongID MIR research.

## Update 2026-05-01 02:37:44Z

- Current task: Direct mesh transport runtime dependency gate is complete locally.
- Last activity:
  - registered `DirectQuicDialer` only when `QuicRuntime.IsAvailable()` succeeds
  - added an explicit startup warning when direct mesh transport is enabled but QUIC runtime support is unavailable
  - aligned `DirectQuicDialer.IsAvailableAsync()` with the same runtime gate used by descriptor publication and DI
  - marked the non-QUIC direct transport/runtime-gate task complete
  - validated focused DirectQuicDialer, PeerDescriptorPublisher, and TransportSelector tests
- Next steps:
  1. Run final whitespace, lint, and broad non-live .NET validation for the accumulated backend/middle batch.
  2. Remaining unchecked entries are active visualizer work, packaging artifact/live-runtime work, repo-wide analyzer debt, QR-library placeholder work, or future SongID MIR research.

## Update 2026-05-01 02:22:16Z

- Current task: Collection item display metadata follow-up is complete locally.
- Last activity:
  - added persisted `fileName`, `title`, `artist`, and `album` fields for collection items
  - added best-effort startup column upgrades for existing `collections.db` files
  - wired add/update requests, share manifests, and Playlist Intake collection creation to carry safe display labels
  - validated focused CollectionsController/SharingService tests and playlist-intake Web tests
- Next steps:
  1. Run final whitespace, lint, and broad non-live .NET validation for the accumulated backend/middle batch.
  2. Treat remaining unchecked entries as active visualizer, packaging artifact, live-runtime, or future research backlog unless explicitly reprioritized.

## Update 2026-05-01 02:18:47Z

- Current task: Sharegroups streaming security checklist is complete locally.
- Last activity:
  - added constant-time application-level comparisons for signed share token collection/share binding checks
  - fixed `ContentLocator` so non-advertisable repository hits cannot fall through to allowed-root `path:` fallback resolution
  - added focused regressions for tampered signatures and non-advertisable fallback bypass
  - documented and committed the gotcha as `c411d2ca9`
  - validated focused ShareToken, ContentLocator, StreamsController, and SharesController tests
- Next steps:
  1. Run final whitespace, repo lint, and broad non-live .NET validation for this batch.
  2. Remaining unchecked work is either active in another lane, artifact/live-runtime blocked, or future research backlog unless explicitly reprioritized.

## Update 2026-05-01 02:22:30Z

- Current task: Acquisition Review is corrected so it no longer intercepts active manual Search.
- Last activity:
  - removed the Search page action that saved a manual query into Discovery Inbox/Acquisition Review
  - renamed the visible Discovery Inbox nav/page heading to Acquisition Review
  - clarified UI and docs that the queue is for passive, imported, and generated candidates only
  - documented the manual-search review-loop gotcha and committed it as `4d8eb9e7e`
  - validated focused Search/Acquisition Review/App tests, frontend lint, production build, repo lint, and live `slskd` service status on `kspls0`
- Next steps:
  1. Hard refresh `/searches` and `/discovery-inbox` to pick up the new bundle names.
  2. Keep normal Search direct; use Acquisition Review only for generated/imported/passive acquisition candidates.

## Update 2026-05-01 02:12:30Z

- Current task: Unified Messages pod room/DM experience is deployed to `kspls0`.
- Last activity:
  - hid pod direct channels from Messages so Soulseek DMs are not duplicated by pod DMs
  - gave pod room channels a room-style transcript, composer, and member rail
  - kept Listen Along as a compact room-only affordance
  - pinned message-window controls to the top-right of panel headers
  - validated focused Messaging/App tests, frontend lint, production build, repo lint, and live `slskd` service status on `kspls0`
- Next steps:
  1. Hard refresh `/messages` to pick up `Messaging-BATI-J-x.js` and `Messaging-Bh-1851M.css`.
  2. If mesh-wide broadcast needs a top-level surface, build it separately from direct messages.

## Update 2026-05-01 02:17:50Z

- Current task: Remaining active transfer refusal bug is complete locally.
- Last activity:
  - added upload-side producer coverage for Soulseek `Connection refused` during upload execution
  - fixed `UploadService.UploadAsync(...)` catch paths so stale transfer snapshots do not overwrite terminal `TryFail(...)` state
  - documented and committed the gotcha as `da3632af1`
  - marked the `#201` task complete in `tasks.md`
  - validated focused upload lifecycle and network-exception tests, touched-file whitespace checks, repo lint, and the broader non-live .NET suite
- Next steps:
  1. Treat remaining unchecked entries as active visualizer work, artifact-dependent packaging, or future backlog unless the user explicitly prioritizes one of those lanes.
  2. If more coding continues immediately, avoid T-938 visualizer files while another lane owns them.

## Update 2026-05-01 02:09:06Z

- Current task: T-1400 Unified BrainzClient is complete locally.
- Last activity:
  - replaced the placeholder unified Brainz client with a real facade over MusicBrainz and AcoustID
  - registered `IBrainzClient` in DI
  - added unified release, recording, Discogs release, recording search, and fingerprint lookup paths
  - added focused unit coverage for cache reuse, dedupe, fingerprint enrichment, and fallback metadata
  - validated focused Brainz tests, touched-file whitespace checks, repo lint, and the broader non-live .NET suite
- Next steps:
  1. Remaining unchecked items are not immediate feature-expansion implementation work: T-938 is active in another visualizer lane, packaging tasks require real published artifacts, and broad future backlog items need separate prioritization before changing product scope.
  2. If continuing in this lane, prefer small integration migrations to `IBrainzClient` only where they reduce duplicate MusicBrainz/AcoustID orchestration without changing existing API behavior.

## Update 2026-05-01 02:43:28Z

- Current task: Tiny display visualizer controls are moved out of the tile.
- Last activity:
  - removed visualizer mode/maximize controls from inside the square visual surface
  - added an external control strip under the tile with explicit spectrum, signal, Butterchurn, native WebGL2, native WebGPU, browser-window, and fullscreen buttons
  - fullscreen now requests browser fullscreen on the tile before switching layout
  - validated focused PlayerBar/Visualizer tests, frontend lint, native browser smoke, frontend production build, and touched-file whitespace checks
- Next steps:
  1. In-browser screenshot validation is the remaining UI-confidence check.
  2. Keep renderer selection explicit; do not reintroduce hidden cycling as the only route to Butterchurn/native MilkDrop.

## Update 2026-05-01 02:33:43Z

- Current task: Tile-level visualizer controls are implemented locally.
- Last activity:
  - moved compact controls to `PlayerVisualTile` so they exist for bars, signal, Butterchurn, and native MilkDrop
  - tiny tile now always shows switch visual, maximize-to-browser-window, and maximize-to-fullscreen controls
  - maximizing from bars/signal restores the saved concrete visualizer backend before opening the expanded view
  - removed duplicate nested compact controls from `Visualizer`
  - validated focused PlayerBar/Visualizer tests, frontend lint, native browser smoke, frontend production build, and touched-file whitespace checks
- Next steps:
  1. In-browser screenshot validation of the compact tile should be the next check if the visual layout still feels cramped.
  2. Keep controls that must work across analyzer and visualizer variants at the tile layer, not inside renderer-specific components.

## Update 2026-05-01 02:29:18Z

- Current task: Compact player visualizer tile controls are fixed locally.
- Last activity:
  - compact tile controls are always visible instead of hover-only
  - compact tile shows only switch-engine, next-preset, and expand controls
  - native import/editor/debug/preset-bank controls stay out of the tiny tile and remain available after expansion
  - compact fallback errors are shortened and moved to the bottom of the tile
  - validated focused PlayerBar/Visualizer tests, frontend lint, native browser smoke, frontend production build, and touched-file whitespace checks
- Next steps:
  1. Validate the square tile in-browser against the user’s exact viewport after server startup if more visual feedback is needed.
  2. Keep advanced native controls out of compact display surfaces.

## Update 2026-05-01 02:18:48Z

- Current task: Player display visualizer mode selection is fixed locally.
- Last activity:
  - display tile now cycles album art, Butterchurn, MilkDrop3 WebGL2, MilkDrop3 WebGPU, spectrum bars, and signal scope
  - eye/keyboard visualizer toggle resolves to a concrete saved backend instead of the old `milkdrop` tile token
  - Butterchurn no longer requires the native WebGL2 support preflight before engine creation
  - documented the visualizer tile-token gotcha in ADR-0001
  - validated focused PlayerBar/Visualizer tests, frontend lint, native browser smoke, frontend production build, and touched-file whitespace checks
  - full `git diff --check` is blocked by pre-existing trailing whitespace in `docs/design/sharegroups-collections-streaming-assessment.md`
- Next steps:
  1. Run the broader MilkDrop/browser smoke/build slice after any further visualizer changes.
  2. Keep tile/display modes and renderer backend modes concrete at storage boundaries.

## Update 2026-05-01 02:01:37Z

- Current task: T-938 native MilkDrop WebGPU backend selection is implemented locally.
- Last activity:
  - expanded visualizer engine storage from Butterchurn/native to Butterchurn, native WebGL2, and native WebGPU
  - added a single overlay cycle button that is present in inline, full-window, and fullscreen visualizer modes
  - wired the native engine factory to create either the WebGL2 renderer or WebGPU renderer based on the selected backend
  - kept old stored `native` values compatible by mapping them to `native-webgl2`
  - validated focused Visualizer/native/WebGPU tests, broader MilkDrop/WebGPU unit slice, frontend lint, native browser smoke, native performance sample, frontend production build, whitespace check, and repo lint through `bash ./bin/lint`
- Next steps:
  1. Use real preset-pack/device measurements to decide whether WebGPU can graduate beyond explicit opt-in.
  2. Keep adding parity gaps found by those scans to the compatibility matrix before making WebGPU the default native backend.

## Update 2026-05-01 01:58:14Z

- Current task: Guided YAML admin settings and dump smoke cleanup are implemented locally.
- Last activity:
  - added a System Integrations settings panel for Chromaprint, AcoustID, MusicBrainz, and Lidarr
  - validated the T-939 source-feed history/audit backend slice and the new Integrations UI slice
  - fixed the stale PR-06 dump endpoint smoke expectation so test environments can return `501 NotImplemented` for unsupported dump creation
  - documented the dump-test gotcha and committed the doc entry as `fd3b283c0`
  - validated focused DumpTests, full `dotnet test`, frontend production build, frontend lint, repo lint, and whitespace checks
- Next steps:
  1. Continue the feature-expansion burn-down with webhooks/scripts, identity/auth/HTTPS, transfer policy, search/network policy, or retention/storage settings if staying in the admin-surface lane.
  2. Keep live provider execution and credential validation behind explicit user-triggered actions with visible network impact.

## Update 2026-05-01 01:58:07Z

- Current task: Remaining non-visualizer front-block feature expansions are closed for this pass.
- Last activity:
  - marked T-919 Discovery Graph complete for the current substrate pass after route/atlas/evidence/export work
  - added explicit SongID forensic-matrix export/debug endpoint and Web UI copy action
  - strengthened SongID API/store tests for run creation progress shape, persistence, and forensic export
  - marked current T-917/T-918 SongID parity complete in tasks and updated `docs/dev/SONGID_INTEGRATION_MAP.md`
  - confirmed full Web UI lint passes and marked T-915 complete
  - validated focused SongID tests, focused browser SongID helper test, frontend production build, repo lint, whitespace checks, and the broader non-live .NET suite
  - validated focused SongID backend tests and browser SongID API helper tests
- Next steps:
  1. Run whitespace, repo lint, frontend build, and broader non-live .NET tests for the final batch.
  2. Leave T-938 visualizer work to the active visualizer lane; future MIR/Essentia and Unified BrainzClient are separate follow-up epics.

## Update 2026-05-01 01:44:11Z

- Current task: Guided YAML admin settings for metadata/Lidarr integrations are implemented locally.
- Last activity:
  - validated the T-939 source-feed history/audit backend slice with focused SourceFeeds tests, whitespace, and repo lint
  - added a System Integrations settings panel for Chromaprint, AcoustID, MusicBrainz, and Lidarr
  - masked existing AcoustID/Lidarr credentials while allowing explicit replacement values
  - persisted snake_case YAML for metadata/fingerprint/Lidarr settings through the existing options YAML API
  - kept the panel configuration-only with no credential tests, provider calls, peer search, peer browse, downloads, or file mutations beyond explicit YAML save
  - validated focused Integrations tests, Integrations ESLint, frontend production build, repo lint, and whitespace checks
- Next steps:
  1. Continue the feature-expansion burn-down with webhooks/scripts, identity/auth/HTTPS, transfer policy, search/network policy, or retention/storage settings if staying in the admin-surface lane.
  2. Keep live provider execution and credential validation behind explicit user-triggered actions with visible network impact.

## Update 2026-05-01 02:02:28Z

- Current task: T-917/T-918 SongID backend review/export APIs are implemented locally.
- Last activity:
  - added a SongID queue-summary model and authenticated API for queued/running counts, configured concurrency, and active run progress snapshots
  - added a SongID evidence-package model and authenticated API for capped track/album/artist candidates, segments, mix groups, plans, acquisition options, scorecard, assessments, forensic matrix, evidence strings, warnings, and artifact references
  - kept evidence packages read-only and review/export oriented; they do not start searches, browse peers, download, publish, or mutate files
  - added focused SongID service/controller coverage for queue summaries and evidence packages
  - validated focused SongID tests, whitespace, repo lint, and the full non-live test suite
- Next steps:
  1. Continue backend/middle-lane work only where it avoids active T-938/T-919 frontend and visualizer files.
  2. Keep any SongID acquisition execution or automatic search fan-out behind explicit user-triggered actions.

## Update 2026-05-01 01:25:41Z

- Current task: T-939 source-feed import history/audit backend is implemented locally.
- Last activity:
  - moved off the crowded T-934/T-936 lane and added a larger T-939 backend follow-up
  - source-feed previews now record bounded restart-safe history entries with provider/source metadata, source fingerprint, safe source preview, request options, result counts, network-request counts, skipped-row samples, and suggestion samples
  - added authenticated API endpoints to list import history and fetch a single import run
  - persisted history to an app-dir JSON file through a temp-file plus atomic replace path and reloads it on service startup
  - avoided storing per-import provider bearer tokens in history
  - validated focused SourceFeeds service/controller tests, whitespace, repo lint, and the full non-live test suite
- Next steps:
  1. Continue with middle/backend follow-ups outside the active visualizer/frontend lanes.
  2. Keep import execution/search/download automation separate from review-first source-feed previews.

## Update 2026-05-01 01:55:32Z

- Current task: Pod direct channels no longer replace deleted Soulseek DMs in Messages; deployed to `kspls0`.
- Last activity:
  - hid pod direct channels from the Messages Pod Channels list unconditionally
  - kept non-direct pod room channels visible
  - close stale workspace panels that point at pod direct channels after hydration
  - added focused regression coverage for deleting a saved Soulseek DM without revealing `peer / dm`
  - rebuilt and deployed the web bundle to `/usr/lib/slskd/current/wwwroot` on `kspls0`
  - validated focused Messaging/App tests, frontend lint, production build, repo lint, and whitespace checks
- Next steps:
  1. Hard refresh `/messages` if the browser still has an older bundle.
  2. Build any future mesh-DM surface explicitly instead of exposing pod direct channels as fallback DMs.

## Update 2026-05-01 01:46:07Z

- Current task: Unified Messages permanent delete/leave actions and dense transcript cleanup are deployed to `kspls0`.
- Last activity:
  - added confirmed permanent delete for saved DM threads from sidebar rows and chat panel headers
  - added confirmed room leave from joined-room rows and room panel headers
  - added confirmed pod leave from pod-channel rows and pod panel headers, preserving the Gold Star irreversible warning
  - moved rich user badges out of repeated chat/room message rows and into stable identity surfaces
  - rebuilt and deployed the web bundle to `/usr/lib/slskd/current/wwwroot` on `kspls0`
  - validated focused Messaging/App tests, frontend lint, production build, repo lint, and whitespace checks
- Next steps:
  1. Hard refresh `/messages` if the browser still has an older bundle.
  2. If the permanent actions should also show toast success/failure messages in Messages, add that as a small UX follow-up.

## Update 2026-05-01 01:38:27Z

- Current task: Listen Along channel scoping and compact room-broadcast controls are deployed to `kspls0`.
- Last activity:
  - removed Listen Along from pod direct-message channels in Messages and Pods
  - kept Listen Along available for pod room channels only
  - replaced the full Listen Along panel with a compact room-broadcast strip using a glowing status icon and small icon controls
  - rebuilt and deployed the web bundle to `/usr/lib/slskd/current/wwwroot` on `kspls0`
  - validated focused Messaging/App tests, frontend lint, production build, repo lint, and whitespace checks
- Next steps:
  1. Hard refresh `/messages` if the browser still has an older bundle.
  2. Treat any future mesh-wide broadcast surface as a separate global control rather than a direct-message widget.

## Update 2026-05-01 01:22:02Z

- Current task: Unified Messages duplicate pod-DM and composer/member-list follow-up is deployed to `kspls0`.
- Last activity:
  - folded pod direct channels into matching saved DMs so `peer / DM` pod channels no longer duplicate normal saved direct-message rows
  - added cleanup for stale restored bridged pod-DM panels in workspace local storage
  - made embedded chat, room, and pod message composers visibly usable inside workspace cards
  - preserved room member lists inside room workspace panels
  - rebuilt and deployed the web bundle to `/usr/lib/slskd/current/wwwroot` on `kspls0`
  - validated focused Messaging/App tests, frontend lint, production build, repo lint, and whitespace checks
- Next steps:
  1. Check the live Messages page after a browser hard refresh; service-worker/browser cache may keep the previous bundle until refreshed.
  2. Keep deeper pod-DM/Soulseek-DM history merging as a separate behavior decision if the two transports need one combined transcript.

## Update 2026-05-01 01:38:13Z

- Current task: T-919 Discovery Graph backend evidence-lane batch is implemented locally.
- Last activity:
  - added additive Discovery Graph edge evidence lanes and graph-level evidence summaries derived from score components, provenance, and evidence strings
  - extended the browser branch report to include backend-provided evidence lanes
  - kept graph evidence passive and review-only; it does not search, browse peers, download, publish, or mutate files
  - validated focused backend Discovery Graph service tests, focused browser Discovery Graph helper tests, ESLint on touched graph JS/JSX files, whitespace, repo lint, frontend production build, and the broader non-live .NET suite
- Next steps:
  1. Continue T-919 or move to T-917/T-918 once Discovery Graph’s first-class route/evidence/export surface is sufficient for this pass.
  2. Resolve or coordinate the existing dirty `playlistIntake.js` lint issue before relying on full `npm --prefix src/web run lint`.

## Update 2026-05-01 01:24:19Z

- Current task: T-919 Discovery Graph atlas branch planning batch is implemented locally.
- Last activity:
  - added browser-local graph helpers for visible node/edge filtering, branch route suggestions, nearby search seeds, and copyable branch review reports
  - added in-page Search atlas edge-family filters, suggested branch routes, pinned comparison context, copy report, and recenter actions
  - kept the atlas review local and passive except for the existing explicit Queue Nearby action
  - validated focused graph helper tests, the Search graph component test slice, ESLint on the touched graph JS/JSX files, whitespace, and the frontend production build
  - noted full web lint is blocked by an existing dirty `playlistIntake.js` `no-control-regex` issue outside this graph batch
- Next steps:
  1. Continue T-919 or the next front-of-list feature item, avoiding active visualizer and middle-lane dirty files.
  2. Resolve or coordinate the existing dirty `playlistIntake.js` lint issue before relying on full `npm --prefix src/web run lint`.

## Update 2026-05-01 01:13:23Z

- Current task: T-934 realm subject-index persistence follow-up is implemented locally.
- Last activity:
  - added restart-safe JSON persistence for accepted realm subject indexes, governance proposals, proposal reviews, and local authority decisions
  - reloads persisted subject-index state at service startup so accepted resolver data, proposal history, and disabled authority preferences survive restarts
  - writes the app-dir state file through a temp-file plus atomic replace path with deterministic ordering
  - added focused service tests for reloading accepted indexes with disabled authorities and accepted proposal review state
  - kept the persistence local and passive with no publishing, peer contact, search, browse, download, or file mutation beyond the explicit app-dir state file
  - validated focused subject-index tests, whitespace, repo lint, and the full non-live test suite
- Next steps:
  1. Continue center-lane backend work only where it avoids dirty UI and front/end-lane files.
  2. Keep actual subject-index publication/sync as a separate explicit governance/mesh contract.

## Update 2026-05-01 01:50:52Z

- Current task: T-938 native MilkDrop WebGPU readiness reporting is implemented locally.
- Last activity:
  - added WebGPU-specific compatibility matrix reporting alongside WebGL2 support
  - report entries now identify WebGPU-only shader-section blockers
  - added coverage for curated WebGPU support counts, safe WGSL named sampler/audio-helper readiness, and a WebGPU-only shader gap
  - the main remaining WebGPU promotion work is real-pack/device measurement and follow-up on measured gaps, not another obvious known parity slice
  - validated focused compatibility/shader/WebGPU tests, broader MilkDrop/Visualizer tests, and frontend lint
- Next steps:
  1. Run broader native MilkDrop performance/compatibility scans against real preset packs when available.
  2. Keep WebGL2 as the active parity renderer until those scans show WebGPU behavior is stable across devices.

## Update 2026-05-01 01:46:13Z

- Current task: T-938 native MilkDrop WebGPU named shader texture samplers are implemented locally.
- Last activity:
  - extended safe WGSL translation for named `tex`/`tex2D` sampler references
  - added four optional shader texture slots to the WebGPU feedback bind layout
  - built separate warp/comp feedback bind groups so each shader body gets its own sampler ordering
  - resolved imported texture assets through shared native alias rules with procedural fallback
  - validated focused shader/WebGPU tests, broader MilkDrop/Visualizer tests, and frontend lint
- Next steps:
  1. Continue T-938 with broader real-pack compatibility/performance measurement and any issues that measurement exposes.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 01:40:54Z

- Current task: T-938 native MilkDrop WebGPU shader audio-bin helpers are implemented locally.
- Last activity:
  - added WGSL `get_fft`, `get_fft_hz`, and `get_waveform` helpers for the safe WebGPU shader subset
  - added explicit 64-bin FFT and waveform uniform fields to avoid WebGPU uniform-array layout ambiguity
  - populated those bins from native render-frame spectrum/waveform data
  - kept named shader texture samplers as the main remaining WebGPU shader parity gap
  - validated focused shader/WebGPU tests, broader MilkDrop/Visualizer tests, and frontend lint
- Next steps:
  1. Continue T-938 with WebGPU named shader texture samplers or broader real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 01:37:08Z

- Current task: T-938 native MilkDrop WebGPU safe-subset shader execution is implemented locally.
- Last activity:
  - added WGSL generation from the existing safe MilkDrop shader parser for WebGPU feedback passes
  - wired translated WebGPU warp and comp shaders when presets fit the conservative WGSL subset
  - added WebGPU uniform coverage for color, time, bass/mid/treble, attenuated audio values, viewport size, output alpha, feedback, and q1-q64
  - kept named shader texture samplers and shader audio-bin helpers on WebGL2 while WebGPU support stays conservative
  - validated focused shader/WebGPU tests, broader MilkDrop/Visualizer tests, and frontend lint
- Next steps:
  1. Continue T-938 with WebGPU named shader texture samplers, shader audio-bin helpers, or larger real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 01:11:58Z

- Current task: T-938 native MilkDrop WebGPU textured primitive foothold is implemented locally.
- Last activity:
  - added WebGPU texture upload for procedural and imported native MilkDrop texture assets
  - reused WebGL/native texture alias matching for WebGPU shape/sprite texture lookup
  - added textured shape and sprite UV/color vertex packing plus a textured WebGPU primitive pipeline
  - padded raw WebGPU texture rows to browser validation-safe strides
  - kept WebGL2 as active while WebGPU still needs translated warp/comp shaders and broader real-pack/device measurement
  - validated focused WebGPU tests, broader MilkDrop/Visualizer tests, and frontend lint
- Next steps:
  1. Continue T-938 with WebGPU translated warp/comp shader execution or larger real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 01:03:32Z

- Current task: Front-of-list DHT candidate backoff and share-scan probe control are implemented locally.
- Last activity:
  - added progressive service-level reconnect backoff for repeatedly failing DHT-discovered overlay candidates
  - added `shares.probe_media_attributes` / CLI / environment control for skipping TagLib audio metadata extraction during share scans on slow or remote storage
  - updated example/config docs and focused tests for the new media probing fast path
  - documented both gotchas in ADR-0001 and committed the doc-only entry as `1a8ec1cd9`
  - closed the matching DHT/share-scan tasks, including the already-completed share-scan harness checkbox
- Next steps:
  1. Continue the front-of-list burn-down, avoiding active visualizer and middle-lane dirty files.
  2. Re-check the optional live-account mesh smoke when live Soulseek credentials/network are healthy; local tests pass with only that smoke excluded.

## Update 2026-05-01 01:01:48Z

- Current task: T-934 realm subject-index backend authority decisions are implemented locally.
- Last activity:
  - added local authority-decision models and service methods for accepted realm subject indexes
  - added authenticated API endpoints to list decisions and disable or re-enable a realm/index authority
  - filtered disabled authorities out of recording resolution and conflict reports while preserving accepted indexes and governance history
  - added focused service/controller tests for disable, re-enable, invalid actor/index rejection, decision listing, and API responses
  - kept decisions local and passive with no governance publication, peer contact, search, browse, download, or file mutation
  - validated focused subject-index tests, whitespace, repo lint, and the full non-live test suite
- Next steps:
  1. Keep any persistent authority-preference storage as a separate follow-up if needed.
  2. Continue center-lane backend work only where it avoids dirty UI lanes.

## Update 2026-05-01 00:47:43Z

- Current task: T-936 Quarantine Jury release-package backend is implemented locally.
- Last activity:
  - added a read-only release evidence package for locally accepted release-candidate jury decisions
  - included request evidence, selected jurors, signed verdicts, route attempts, the acceptance snapshot, current aggregate state, and drift warnings for post-acceptance aggregate changes
  - added authenticated API endpoint `GET /api/v0/quarantine-jury/requests/{requestId}/release-package`
  - kept release packages observational with no quarantine enforcement change, file movement, route dispatch, publication, search, browse, or download
  - validated focused Quarantine Jury service/controller tests, whitespace, repo lint, and the full non-live test suite
  - noted that unfiltered `dotnet test` only failed the optional live Soulseek-account overlay mesh test because the beta account did not log in
- Next steps:
  1. Keep actual quarantine release/file movement behind a separate explicit enforcement contract.
  2. Continue center-lane backend work only where it avoids dirty UI lanes.

## Update 2026-05-01 00:56:02Z

- Current task: T-938 native MilkDrop WebGPU filled-shape/sprite foothold is implemented locally.
- Last activity:
  - added triangle-fan to triangle-list conversion for WebGPU filled primitive rendering
  - added native filled custom shape rendering with existing center/edge color data
  - added fallback sprite-quad rendering with existing native sprite geometry and color helpers
  - kept WebGL2 as the active native MilkDrop compatibility renderer while WebGPU still needs textured sprite/shape sampling, translated shaders, and real-pack measurement before becoming a candidate renderer
  - validated focused WebGPU renderer tests and frontend lint
- Next steps:
  1. Continue T-938 with WebGPU texture sampling, translated shader execution, or larger real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 00:47:46Z

- Current task: T-938 native MilkDrop WebGPU screen-border foothold is implemented locally.
- Last activity:
  - added a WebGPU triangle-list primitive pipeline and colored triangle vertex conversion
  - added classic outer/inner screen-border rendering from existing `ob_*` and `ib_*` geometry/color helpers
  - draws screen borders into the WebGPU feedback texture before the display pass
  - kept WebGL2 as the active native MilkDrop compatibility renderer while WebGPU remains isolated until filled shapes, sprites, translated shaders, and real-pack parity mature
  - validated focused WebGPU renderer tests and frontend lint
- Next steps:
  1. Continue T-938 with deeper WebGPU primitive parity, translated shader execution, or larger real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 00:41:28Z

- Current task: T-938 native MilkDrop WebGPU motion-vector foothold is implemented locally.
- Last activity:
  - added WebGPU motion-vector rendering using the existing native `mv_*` vertex and color helpers
  - added a reusable colored line-list conversion helper for WebGPU primitive buffers
  - kept waveform, shape-outline, and motion-vector WebGPU draws on the same feedback-texture primitive path
  - kept WebGL2 as the active native MilkDrop compatibility renderer while WebGPU remains isolated until translated shader execution and broader real-pack parity are ready
  - validated focused WebGPU renderer tests and frontend lint
- Next steps:
  1. Continue T-938 with deeper WebGPU primitive parity, translated shader execution, or larger real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 00:34:46Z

- Current task: T-938 native MilkDrop WebGPU primitive foothold is implemented locally.
- Last activity:
  - added WebGPU waveform primitive rendering into the feedback texture using the shared native waveform vertex mapping
  - added first WebGPU shape-outline primitive rendering for enabled MilkDrop shapes
  - reused the existing native shape outline and border-color helpers so WebGPU primitive behavior stays aligned with WebGL2
  - kept WebGL2 as the active native MilkDrop compatibility renderer while WebGPU remains isolated until it has fuller primitive, shader, and real-pack parity
  - validated focused WebGPU renderer tests and frontend lint
- Next steps:
  1. Continue T-938 with deeper WebGPU primitive parity, translated shader execution, or larger real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 00:29:26Z

- Current task: Approved Discovery Inbox acquisition search handoff is implemented locally.
- Last activity:
  - added bounded acquisition-plan execution that queues backend search jobs through the selected acquisition profile
  - records queued search IDs, failed execution summaries, and skipped plans when batch limits apply
  - added Discovery Inbox Execute Ready controls with visible search-only network impact
  - kept execution operator-triggered and limited to search creation; no peer browse, result selection, download, tag write, or file movement starts from the handoff
  - validated focused Acquisition Plans and Discovery Inbox tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue beginning-lane burn-down to the next incomplete epic that does not collide with active middle/tail files.
  2. Keep actual download queueing behind search-result selection, confirmation, and existing transfer limits.

## Update 2026-05-01 00:35:31Z

- Current task: T-936 Quarantine Jury audit-report backend is implemented locally.
- Last activity:
  - added read-only audit report models for accepted, pending, stale, routed, and failed-route states
  - added service aggregation for quorum, recommendation, acceptance, route-attempt, stale, and dissent summaries
  - added authenticated API endpoint `GET /api/v0/quarantine-jury/audit`
  - kept the audit endpoint observational with no quarantine enforcement change, file movement, route dispatch, publication, search, browse, or download
  - validated focused Quarantine Jury service/controller tests, whitespace, repo lint, and full test suite
- Next steps:
  1. Keep future Quarantine Jury enforcement or file-release actions behind separate explicit confirmation contracts.
  2. Continue center-lane backend work only where it avoids dirty UI lanes.

## Update 2026-05-01 00:22:01Z

- Current task: T-935 MusicBrainz overlay persistence follow-up is implemented locally.
- Last activity:
  - added restart-safe atomic JSON persistence for signed overlay edits
  - persisted selected-peer route attempts and manual upstream export approvals
  - added explicit storage-path constructors and reload tests for edits, approvals, and route attempts
  - documented and committed the persistent-service test-storage gotcha after fixing shared app-state contamination in overlay tests
  - kept persistence passive with no publication, upstream submission, Soulseek search, peer browse, download, or cached MusicBrainz mutation
  - validated focused MusicBrainz overlay service tests, whitespace, repo lint, and full test suite
- Next steps:
  1. Continue center-lane backend follow-ups only where they avoid dirty UI lanes.
  2. Keep any future overlay federation/export actions explicit and review-first.

## Update 2026-05-01 00:24:18Z

- Current task: T-938 native MilkDrop WebGPU foothold is implemented locally.
- Last activity:
  - added a native MilkDrop WebGPU adapter probe with feature/limit/adapter metadata
  - surfaced WebGPU adapter details in the native debug snapshot and overlay
  - added an opt-in WebGPU renderer module that configures a `webgpu` canvas, maintains ping-pong feedback textures, and draws a preset-colored fullscreen display pass
  - kept WebGL2 as the active native MilkDrop compatibility renderer while WebGPU remains a scaffold for future primitives and translated shader execution
  - validated focused WebGPU renderer, native MilkDrop engine, and Visualizer tests
- Next steps:
  1. Continue T-938 with deeper WebGPU feedback-texture parity or larger real-pack compatibility/performance measurement.
  2. Keep native MilkDrop opt-in until real-pack compatibility and device performance behavior are measured across more systems.

## Update 2026-05-01 00:25:16Z

- Current task: Epic E9 Audio Verification profiles, cache, and policy review are implemented locally.
- Last activity:
  - added browser-local audio verification profiles for lossless-exact, balanced, and permissive review
  - added verification decisions with confidence, evidence, warnings, fail-open/fail-closed actions, and browser-local SHA-256 cache controls
  - wired Import Staging to show audio verification status, verify staged rows, store decisions, and explicitly apply the selected policy to Ready, Staged, or Failed review state
  - kept verification operator-controlled and local-only with no upload, import, tag write, file move, Soulseek search, peer browse, or download
  - validated focused Audio Verification, Import Staging helper/component tests, frontend lint, frontend production build, whitespace checks, `bin/lint`, and full `dotnet test`
- Next steps:
  1. Continue beginning-lane burn-down into the next feature-expansion epic after E9.
  2. Keep any future acoustic/fingerprint provider calls behind explicit enablement, rate limits, and visible Web UI state.

## Update 2026-05-01 00:11:34Z

- Current task: T-934 realm subject-index conflict-report backend is implemented locally.
- Last activity:
  - added conflict report models that preserve realm/index/revision provenance per conflicting value
  - added deterministic service reporting for external-id, recording-subject, WorkRef identity, and alias-subject conflicts
  - added authenticated read-only API endpoints for accepted indexes, recording resolutions, and conflict reports
  - kept conflict inspection passive with no publication, peer contact, Soulseek search, browse, download, or file mutation
  - validated focused Realm Subject Index service/controller tests, whitespace, repo lint, and full test suite
- Next steps:
  1. Leave the actual UI conflict display to a frontend owner unless the active UI lanes clear.
  2. Keep any future subject-index authority-disable action behind explicit user confirmation and provenance review.

## Update 2026-05-01 00:08:57Z

- Current task: Epic E8 shared Metadata Matching surface is implemented locally.
- Last activity:
  - added reusable metadata normalization and weighted candidate scoring
  - added short-title protection, version-tag awareness, identifier evidence, confidence bands, and explanation fields
  - added Import Staging manual metadata override acceptance
  - added Playlist Intake reuse for candidate scoring
  - kept matching local/deterministic with no metadata provider call, Soulseek search, peer browse, download, tag write, file move, or library mutation
  - validated focused Metadata Matcher, Import Staging, Playlist Intake, and App route tests, frontend lint, frontend production build, whitespace checks, `bin/lint`, and full `dotnet test`
- Next steps:
  1. Continue beginning-lane burn-down into Epic E9 Audio Verification.
  2. Reuse the shared metadata matcher anywhere new import/search/tagging surfaces need confidence bands or explanations.

## Update 2026-04-30 22:08:39Z

- Current task: Epic E7 provider refresh, scheduled refresh, and slskdN playlist creation are implemented locally.
- Last activity:
  - connected Playlist Intake provider refresh to the existing Source Feed Import preview API
  - added Apply Refresh for pasted rows and provider refresh application for mirrored playlist intake rows
  - added scheduled refresh enablement and Run Due Refreshes execution for mirrored playlists
  - added actual Playlist collection creation through the Collections API from matched Playlist Intake rows
  - kept refresh execution sequential and bounded by per-playlist provider limits, with no Soulseek search, peer browse, or download
  - validated focused Playlist Intake helper/component tests, App route tests, frontend lint, frontend production build, whitespace checks, `bin/lint`, and full `dotnet test`
- Next steps:
  1. Continue from the next beginning-lane epic after E7.
  2. Keep future Playlist Intake acquisition actions bounded by provider limits and explicit user controls.

## Update 2026-04-30 21:57:06Z

- Current task: Frontend Quarantine Jury review UI phase is implemented locally.
- Last activity:
  - added Quarantine Jury Web UI API client
  - added System -> Quarantine Jury request/review workspace
  - surfaced evidence, juror verdicts, dissent, route attempts, explicit route dispatch, acceptance status, and modal-gated release-candidate acceptance
  - kept local quarantine authoritative with no file movement, release broadcast, search, browse, download, or library mutation
  - documented and fixed the post-action success-message refresh gotcha
  - validated focused Quarantine Jury Web UI test, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue only after checking active lanes; Playlist Intake, MilkDrop, and MusicBrainz remain concurrently edited.
  2. Keep Quarantine Jury file-release or quarantine-state changes behind separate explicit backend contracts.

## Update 2026-04-30 21:57:55Z

- Current task: Epic E7 Playlist Intake review handoff and playlist-build previews are implemented locally.
- Last activity:
  - added changed-row detection to mirror refresh previews
  - added visible refresh cadence, cooldown, and disabled automation policy to playlist intake cards
  - added bulk Discovery Inbox handoff for non-rejected playlist rows
  - added matched-row slskdN playlist text preview without creating or mutating playlists
  - kept the batch local-only with no provider fetch, Soulseek search, peer browse, download, scheduler, slskd playlist creation, or file mutation
  - validated focused Playlist Intake helper/component tests and App route tests, frontend lint, frontend production build, whitespace checks, `bin/lint`, and full `dotnet test`
- Next steps:
  1. Continue from the next beginning-lane epic after E7 unless another Playlist Intake gap appears during integration.
  2. Defer provider-backed playlist refresh, scheduled refresh execution, and actual slskd playlist mutation until credentials, rate limits, and explicit confirmation flows are implemented.

## Update 2026-04-30 21:49:21Z

- Current task: Epic E12 Library Health safe-fix manifest export is implemented locally.
- Last activity:
  - added selected auto-fixable issue safe-fix manifest generation
  - surfaced Copy Safe-Fix Manifest in the Library Health selected-issue toolbar
  - kept manifest export review-only with no remediation job, safe-fix execution, quarantine state change, search, download, or file mutation
  - validated focused Library Health helper/component tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue tail-side work only where ownership stays clear of active Playlist Intake, MilkDrop, and MusicBrainz lanes.
  2. Keep any Library Health execution path separate from these copy/export review surfaces.

## Update 2026-04-30 21:48:49Z

- Current task: Epic E7 Playlist Intake refresh diffs and row review controls are implemented locally.
- Last activity:
  - added browser-local added/removed/unchanged refresh diff previews for mirrored playlist rows
  - added matched, unmatched, and rejected row review controls
  - added partial completion summaries for matched, unmatched, and rejected rows
  - preserved source identity and row evidence across review changes
  - kept the batch local-only with no provider fetch, Soulseek search, peer browse, download, slskd playlist creation, scheduler, or file mutation
  - validated focused Playlist Intake helper/component tests and App route tests, frontend lint, frontend production build, whitespace checks, `bin/lint`, and full `dotnet test`
- Next steps:
  1. Continue E7 with slskd playlist creation previews or Discovery Inbox bulk handoff if ownership remains clear.
  2. Defer provider-backed playlist refresh until credentials, rate limits, and per-provider preview contracts are explicit.

## Update 2026-04-30 21:45:52Z

- Current task: Epic E12 Library Health quarantine review packet export is implemented locally.
- Last activity:
  - added selected-risky-issue quarantine review packet generation
  - surfaced Copy Quarantine Packet in the Library Health selected-issue toolbar
  - kept packet export review-only with no quarantine state change, file movement, peer message, remediation job, search, download, or file mutation
  - validated focused Library Health helper/component tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Keep future Library Health handoffs explicit-review-first with no automatic quarantine/remediation.
  2. Continue tail-side work only where ownership stays clear of active Playlist Intake, MilkDrop, and MusicBrainz lanes.

## Update 2026-04-30 21:43:23Z

- Current task: Epic E12 Library Health replacement search seed export is implemented locally.
- Last activity:
  - added deduped replacement search seed generation from selected Library Health issues
  - surfaced Copy Search Seeds in the Library Health selected-issue toolbar
  - kept seed export review-only with no Search navigation, peer browse, download, quarantine, remediation job, rescan, or file mutation
  - validated focused Library Health helper/component tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue E12 only with review-first handoff surfaces that avoid automatic remediation.
  2. Keep any future replacement-search handoff behind explicit user confirmation and bounded network policy.

## Update 2026-04-30 22:04:19Z

- Current task: T-933 federated taste recommendation graph/handoff follow-up is implemented locally.
- Last activity:
  - added optional Discovery Graph evidence and proximity scoring for recommendations
  - added explicit backend handoffs to create review-only Wishlist seeds, artist release radar subscriptions, and Discovery Graph preview summaries
  - allowed MusicBrainz artist UUID external IDs in safe WorkRefs for MBID-backed radar subscriptions
  - kept service-layer k-anonymity and avoided peer/source reveal unless explicitly requested
  - kept all handoffs passive: no Soulseek search, peer browse, download, federation publish, or file-library mutation
  - validated focused `TasteRecommendationServiceTests`, `TasteRecommendationsControllerTests`, and `WorkRefTests`
- Next steps:
  1. Continue middle-lane backend work only if another non-UI federation follow-up remains clear; the remaining T-933 UI surface should stay with a UI owner.
  2. Leave concurrent System Integrations, Library Health, Playlist Intake, Quarantine Jury UI, MilkDrop/Visualizer, Discovery Inbox, Player, Watchlist, and Discovery Shelf edits untouched unless asked to integrate.

## Update 2026-04-30 21:51:38Z

- Current task: T-932 signed artist radar observation routing follow-up is implemented locally.
- Last activity:
  - added explicit selected-peer PodCore route attempts for artist-radar notifications
  - added safe opaque route metadata validation, signed local route envelopes, and route attempt API dispatch/history endpoints
  - persists route attempts with the artist radar state file so route history survives daemon restarts
  - kept routing manual-only with no automatic publication, MusicBrainz polling, Soulseek search, peer browse, download, or file-library mutation
  - validated focused `ArtistReleaseRadar` service/controller tests
- Next steps:
  1. Continue center-lane work only if another backend-only federation follow-up remains clear of active UI lanes.
  2. Leave concurrent System Integrations, Library Health, Playlist Intake, MilkDrop/Visualizer, Discovery Inbox, Player, Watchlist, and Discovery Shelf edits untouched unless asked to integrate.

## Update 2026-04-30 21:40:56Z

- Current task: Epic E7 browser-local Playlist Intake first slice is implemented locally.
- Last activity:
  - added a Playlist Intake route and navigation entry
  - added browser-local playlist intake storage and parsing for pasted text/CSV-style rows
  - retained playlist name, source identity, inferred provider, mirror-review setting, and per-row source lines
  - surfaced matched/unmatched row state and Discovery Inbox review handoff
  - kept the slice local-only with no provider fetch, Soulseek search, peer browse, download, slskd playlist creation, scheduler, or file mutation
  - validated focused Playlist Intake helper/component tests and App route tests
- Next steps:
  1. Continue E7 with refresh diff previews for mirrored playlists if ownership remains clear.
  2. Defer provider-backed playlist refresh until credentials, rate limits, and per-provider preview contracts are explicit.

## Update 2026-04-30 21:41:12Z

- Current task: T-932 artist release radar persistence follow-up is implemented locally.
- Last activity:
  - added an atomic JSON state file for radar subscriptions, muted release groups, seen-observation keys, and notifications
  - reloads persisted radar state on service startup so notification history and duplicate suppression survive daemon restarts
  - kept the feature local-only with no MusicBrainz polling, Soulseek search, peer browse, mesh routing, download, or file-library mutation
  - validated focused `ArtistReleaseRadarServiceTests`
- Next steps:
  1. Continue center-lane work with signed artist radar observation routing only if it can use explicit trusted peers/realms and avoid automatic publication.
  2. Leave concurrent System Integrations, Library Health, Playlist Intake, Discovery Inbox, Player, Watchlist, and Discovery Shelf edits untouched unless asked to integrate.

## Update 2026-04-30 21:29:48Z

- Current task: Epic E6 review-only Watchlist similar-artist expansion approval is implemented locally.
- Last activity:
  - added manually supplied similar-artist expansion candidates to browser-local watchlists
  - surfaced pending, approved, and rejected expansion evidence in Discovery Inbox watchlist rows
  - added tooltip-backed approve/reject actions for pending candidates
  - approving an expansion creates a manual Artist watchlist using the parent filter/profile policy
  - kept the slice local-only with no provider lookup, Soulseek search, peer browse, download, scheduler, automation execution, or file mutation
  - validated focused Watchlists and Discovery Inbox component tests
- Next steps:
  1. Continue beginning-lane burn-down into Epic E7 Playlist Intake only if it stays browser-local/file-first and avoids active System/Integrations work.
  2. Defer provider-backed similar-artist discovery until credentials, rate limits, and explicit expansion evidence contracts are defined.

## Update 2026-04-30 21:47Z

- Current task: T-938 native MilkDrop fragment management is implemented locally.
- Last activity:
  - added active custom shape/wave summaries to the native MilkDrop engine
  - added selected-fragment export and selected-fragment removal controls to the visualizer overlay
  - persisted edited presets back into the browser-local native preset library after shape/wave removal
  - added persisted beat-count and timed-interval settings for native automatic preset changes
  - added first native parameter editing for decay, zoom, rotation, waveform color/alpha, and full active-preset text export
  - added bounded parameter randomization, pointer-fed mouse variables, and a compact native debug snapshot overlay
  - added active native preset playlist rename support
  - added browser-local native FPS caps and a debug frame-time readout
  - added native quality presets, WebGPU capability reporting, and WebGL context loss/restore smoke coverage
  - added Chromium native render performance measurement for curated fixtures or local preset files/folders
  - added a bounded translated-shader cache for repeated native warp/comp shader bodies
  - documented and committed the DOM factory spy gotcha after fixing recursive `document.createElement` test failures
  - validated focused Visualizer, native MilkDrop engine, and preset parser tests
- Next steps:
  1. Continue T-938 with optional WebGPU renderer investigation and deeper real-pack compatibility/performance measurement.
  2. Keep native MilkDrop as opt-in until real-pack compatibility and performance behavior are measured across more devices.

## Update 2026-04-30 21:32:19Z

- Current task: T-931 Bloom-filter library diff is implemented locally.
- Last activity:
  - added serializable Bloom filter export/import support
  - added MusicBrainz library Bloom snapshot preview, inbound diff comparison, and Wishlist promotion API/service
  - compared inbound salted recording/release Bloom filters against local cached MusicBrainz candidates and local held recordings
  - kept suggestions probabilistic and review-only with no filenames, paths, file hashes, exact identifier lists, publishing, searches, browsing, or downloads
  - validated focused `LibraryBloomDiffServiceTests`
- Next steps:
  1. Continue center-lane backend work with artist radar persistence or signed radar routing if it avoids active frontend/options lanes.
  2. Leave concurrent System Integrations, OptionsOverlay, Discovery Inbox, Player, Watchlist, and Discovery Shelf edits untouched unless asked to integrate.

## Update 2026-04-30 21:20:12Z

- Current task: T-937 Discography Concierge graph-density prioritization is implemented locally.
- Last activity:
  - added optional Discovery Graph priority metadata to Discography Coverage responses
  - ranked releases by missing-track gap, graph neighborhood density, and existing HashDb/Wishlist evidence
  - kept the scoring local-only with no peer browse, Soulseek search, download, or graph publication
  - validated focused `DiscographyCoverageServiceTests`
- Next steps:
  1. Continue center-lane backend work with Bloom-filter library diff or artist radar persistence if it avoids active frontend/options lanes.
  2. Leave concurrent Discovery Inbox, Player, Watchlist, Discovery Shelf, and Options edits untouched unless asked to integrate.

## Update 2026-04-30 21:37:54Z

- Current task: Epic E12 Library Health selected action-plan previews are implemented locally.
- Last activity:
  - added selected-issue safe-fix, replacement-search, and quarantine-review preview classification
  - surfaced Copy Action Plan in the Library Health selected-issue toolbar
  - kept action plans review-only with no remediation job, replacement search, quarantine, rescan, download, or file mutation
  - validated focused Library Health report helper/component tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue E12 with queue/quarantine handoff surfaces only after explicit backend confirmation contracts exist.
  2. Keep Library Health follow-up actions review-first and separate from automatic remediation.

## Update 2026-04-30 21:30:10Z

- Current task: Epic E12 Library Health report export is implemented locally.
- Last activity:
  - added a browser-side Library Health text report builder for loaded scan data
  - surfaced Copy Report in the Library Health overview after summary/issues are loaded
  - kept report export read-only with no scan start, remediation job, replacement search, quarantine, or file mutation
  - validated focused Library Health tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue E12 with replacement-search or quarantine previews only if they stay explicit-review-first.
  2. Keep Library Health remediation/export actions separately confirmed and non-automatic.

## Update 2026-04-30 21:19:35Z

- Current task: Epic E13 Discovery Shelf policy report export is implemented locally.
- Last activity:
  - added copyable shelf policy reports with expiry window, consensus requirement, policy counts, and item-level planned actions
  - surfaced Copy Report in the player Discovery Shelf modal
  - kept report generation review-only with no apply, file mutation, queue, download, publish, or backend call
  - validated broader player-focused tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Move into Epic E12 Library Health only if the next slice can be read-only/report-first and avoid active System ownership.
  2. Keep Discovery Shelf apply actions disabled until backend preview, confirmation, and shared-owner consensus contracts exist.

## Update 2026-04-30 21:17:09Z

- Current task: Epic E6 visible Watchlist schedule and cooldown policy is implemented locally.
- Last activity:
  - added manual/daily/weekly/monthly schedule choices to watchlist creation
  - persisted bounded cooldown days and acquisition profile policy with watchlist targets
  - displayed schedule enablement, cooldown, profile policy, and no-execution network impact on each watchlist row
  - kept the slice local-only with no scheduler, metadata provider lookup, Soulseek search, peer browse, download, automation execution, or file mutation
  - validated focused Watchlists and Discovery Inbox component tests
- Next steps:
  1. Continue beginning-lane E6 with similar-artist expansion approval if it can stay review-only and avoid active middle/end lanes.
  2. Defer provider-backed release radar scans until credentials, cooldown enforcement, and network-impact policy are explicit.

## Update 2026-04-30 21:16:27Z

- Current task: Epic E13 Discovery Shelf policy previews are implemented locally.
- Last activity:
  - added local promote/archive/expiry/review/consensus-gated policy preview counts
  - added configurable unrated expiry window and shared-library consensus toggle to the player shelf modal
  - kept policy preview informational only with no backend apply or file mutation action
  - validated broader player-focused tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue E13 only if the next slice can remain explicit-preview-first and avoid DiscoveryInbox ownership.
  2. Keep apply/delete/archive disabled until backend preview, confirmation, and shared-owner consensus contracts exist.

## Update 2026-04-30 21:09:16Z

- Current task: Epic E6 browser-local Watchlist release filters are implemented locally.
- Last activity:
  - added visible release type, country, and format filter controls to the Discovery Inbox Watchlists form
  - persisted normalized filter choices with watchlist targets
  - displayed saved filter context on each watchlist row and in generated Discovery Inbox review seeds
  - kept the slice local-only with no metadata provider lookup, Soulseek search, peer browse, download, scheduled automation, or file mutation
  - validated focused Watchlists and Discovery Inbox component tests
- Next steps:
  1. Continue beginning-lane E6 with schedule/cooldown/provider-policy visibility if ownership remains clear.
  2. Defer provider-backed release radar scans until credentials, cooldowns, and network-impact policy are explicit.

## Update 2026-04-30 21:12:20Z

- Current task: Epic E13 browser-local Discovery Shelf from player ratings is implemented locally.
- Last activity:
  - added browser-local Discovery Shelf storage keyed by now-playing rating identity
  - wired player ratings into promote-preview, archive-preview, keep-reviewing, and expiry-watch shelf classifications
  - surfaced a player Discovery Shelf modal with summary counts, action previews, remove, and clear controls
  - recorded live media-server, peer search, queue, download, scrobble, and file-action handoffs as follow-up scope
  - validated broader player-focused tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue E13 with preview policy refinement only if it avoids active DiscoveryInbox ownership.
  2. Wire real promote/archive/delete only after explicit backend preview and confirmation contracts exist.

## Update 2026-04-30 21:03:57Z

- Current task: Epic E6 browser-local Watchlists panel is implemented locally.
- Last activity:
  - added local watchlist storage for artist, label, playlist, and collection targets
  - surfaced Watchlists in Discovery Inbox with add, summary, manual scan preview, and send-to-review actions
  - kept watchlists local-only with no metadata provider lookup, Soulseek search, peer browse, download, scheduled automation, or file mutation
  - validated focused watchlist, Discovery Inbox storage, Discovery Inbox component tests, and frontend lint
- Next steps:
  1. Continue beginning-lane E6 with local filter controls for release type/country/format if ownership remains clear.
  2. Later connect watchlist scans to backend radar/provider APIs only after cooldowns, credentials, and network-impact policy are explicit.

## Update 2026-04-30 21:04:16Z

- Current task: E14 Listening Intelligence browser-local media-server play-history import is implemented locally.
- Last activity:
  - added CSV/JSON import parsing and duplicate suppression for local listening history
  - added JSON/CSV local history export helpers
  - surfaced paste, local file picker, import, and export controls in the player Listening Stats modal
  - validated focused listening/player tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue tail-side burn-down with E13 only if it can stay low-overlap and avoids active Discovery Inbox ownership.
  2. Keep live media-server API import deferred until credentials, rate limits, and per-user mapping are explicit.

## Update 2026-04-30 21:06:05Z

- Current task: Realm subject-index governance proposal flow is implemented locally.
- Last activity:
  - added proposal/review models and pending/accepted/rejected status tracking for realm subject indexes
  - stored proposal and review decisions as realm governance documents when a governance client is available
  - kept pending and rejected proposals out of recording-MBID resolution, publishing only explicitly accepted revisions
  - validated focused `RealmSubjectIndexServiceTests`
- Next steps:
  1. Continue center-lane work with realm subject-index conflict display only if frontend lane ownership is clear, otherwise stay on backend-only federation follow-ups.
  2. Leave concurrent options, source-feed, transfer-test, search, wishlist, Discovery Inbox, and player edits untouched unless asked to integrate.

## Update 2026-04-30 20:59:18Z

- Current task: E14 Listening Intelligence recommendation seed handoffs are implemented locally.
- Last activity:
  - added deterministic recommendation seeds from browser-local forgotten favorites, top artists, and top genres
  - surfaced Recommendation Seeds in the player Listening Stats modal
  - used explicit Search action buttons with tooltips so no network search starts until the user chooses a seed
  - validated focused listening/player tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue tail-side burn-down only in low-overlap local player/listening surfaces unless lane ownership changes.
  2. Defer external recommendation/provider imports until credentials, rate limits, and dedupe policy are explicit.

## Update 2026-04-30 20:55:53Z

- Current task: Discovery Inbox snooze due dates are implemented locally.
- Last activity:
  - added persisted `snoozedUntil` metadata to Discovery Inbox items
  - surfaced Snoozed until/Snooze due labels and an Unsnooze action
  - added focused storage and component coverage for snoozing, clearing snooze metadata, and overdue status
  - kept the slice local-only with no scheduler, provider lookup, peer browse, search, download, automation, or file mutation
  - validated focused Discovery Inbox tests and frontend lint
- Next steps:
  1. Continue beginning-lane burn-down with Watchlists/Release Radar only if it avoids active source-feed and MusicBrainz backend ownership.
  2. Later connect snooze due dates to backend job scheduling only after explicit automation policy and rate limits exist.

## Update 2026-04-30 20:47:21Z

- Current task: Discovery Inbox impact review summary is implemented locally.
- Last activity:
  - added local impact classification for Discovery Inbox candidates
  - surfaced batch-readiness counts and per-candidate impact labels
  - kept the slice evidence-only with no provider lookup, peer browse, search, download, automation, or file mutation
  - validated focused Discovery Inbox review helper/component tests and frontend lint
- Next steps:
  1. Continue beginning-lane burn-down with Watchlists/Release Radar only if it avoids active source-feed and MusicBrainz backend ownership.
  2. Later replace text-derived impact classification with backend-provided provider capability metadata when acquisition jobs are wired.

## Update 2026-04-30 20:55:34Z

- Current task: E14 Listening Intelligence browser-local genre breakdowns are implemented locally.
- Last activity:
  - stored genre/tag metadata in local listening history entries
  - added Top Genres to the player Listening Stats modal
  - validated focused listening history, player auto-queue, radio, shortcut, rating, and PlayerBar tests
- Next steps:
  1. Continue E14 with stats-derived recommendation seed previews if they stay browser-local and explicit.
  2. Add label breakdowns only after real label metadata is present in now-playing or imported history.

## Update 2026-04-30 20:50:41Z

- Current task: E14 Listening Intelligence time-range stats and forgotten favorites are implemented locally.
- Last activity:
  - added time-range filtering to browser-local listening stats
  - added forgotten-favorite derivation from older repeat local plays
  - surfaced 7D/30D/90D/All range controls and Forgotten Favorites in the player stats modal
  - validated focused listening history, player auto-queue, radio, shortcut, rating, and PlayerBar tests
- Next steps:
  1. Continue E14 with genre/label breakdowns only if derivable from local metadata.
  2. Defer media-server import and external recommendation providers until explicit credentials, rate limits, and dedupe policy are scoped.

## Update 2026-04-30 20:45:37Z

- Current task: E14 Listening Intelligence first browser-local history/stats slice is implemented locally.
- Last activity:
  - added local play history storage with duplicate suppression
  - recorded plays at the existing scrobble threshold in PlayerBar
  - added player Listening Stats modal with total, recent, top artists, and top tracks
  - validated focused listening history, player auto-queue, radio, shortcut, rating, and PlayerBar tests
- Next steps:
  1. Continue E14 with time-range stats or forgotten-favorites derivation if it can remain browser-local.
  2. Later add media-server play import only behind explicit credentials and deduplication policy.

## Update 2026-04-30 20:41:25Z

- Current task: E15 Player Enhancements bounded similar-track auto-queue is implemented locally.
- Last activity:
  - added browser-local player auto-queue scoring from recent session history
  - wired Queue Manager Auto-fill Similar to append non-queued recent tracks
  - kept auto-queue bounded to already-known tracks with no network or file effects
  - validated focused player auto-queue, radio, shortcut, rating, and PlayerBar tests
- Next steps:
  1. Continue tail-side burn-down with documentation polish for completed E15 player enhancements or move to the next earlier untouched epic.
  2. Later connect similar-track queueing to real recommendation providers only behind explicit user action and rate limits.

## Update 2026-04-30 20:40:26Z

- Current task: Epic 4 album decision rule previews are implemented locally.
- Last activity:
  - added browser-local album decision rule preview storage
  - captured album/search identity, expected track count, format policy, warnings, and substitution tracks
  - added a Save Rule action on album candidates with tooltip-backed local-only behavior
  - validated focused album decision rule, album candidate, Search component tests, and frontend lint
- Next steps:
  1. Continue beginning-lane burn-down with the next untouched Discovery Inbox/watchlist item only if it avoids current Wishlist/source-feed ownership.
  2. Later wire album decision rules into a backend-backed album decision planner after the persistence and download handoff contract is explicit.

## Update 2026-04-30 20:57:04Z

- Current task: MusicBrainz overlay trust-scoped routing is implemented locally.
- Last activity:
  - added opt-in PodCore route attempts for stored signed MusicBrainz overlay edits
  - constrained routing to explicit safe peer IDs and safe route metadata
  - recorded route attempts with target, routed, and failed peer IDs
  - validated focused MusicBrainz overlay tests, whitespace, lint, and full `dotnet test`
- Next steps:
  1. Continue center-lane work with realm subject-index governance proposals once current frontend lanes settle.
  2. Leave concurrent options, source-feed, transfer-test, search, wishlist, Discovery Inbox, and player edits untouched unless asked to integrate.

## Update 2026-04-30 20:48:35Z

- Current task: MusicBrainz overlay manual export review API is implemented locally.
- Last activity:
  - added overlay export review packages for stored signed MusicBrainz overlay edits
  - added explicit local export approval records with safe approver validation
  - kept the flow manual-only with no upstream MusicBrainz submitter and no cache mutation
  - validated focused MusicBrainz overlay tests, whitespace, lint, and full `dotnet test`
- Next steps:
  1. Continue center-lane work with trust-scoped overlay gossip or frontend review surfaces once lane ownership is clear.
  2. Leave concurrent options, source-feed, transfer-test, search, wishlist, and player edits untouched unless asked to integrate.

## Update 2026-04-30 20:39:16Z

- Current task: Quarantine Jury manual review/acceptance API is implemented locally.
- Last activity:
  - added a backend review projection for request evidence, signed verdicts, route attempts, aggregate recommendations, and acceptance status
  - added persisted manual release-candidate acceptance decisions
  - kept acceptance constrained to release-candidate supermajorities and local decision recording only
  - split the browser Quarantine Jury review UI into a follow-up to avoid current frontend lane overlap
  - validated focused Quarantine Jury tests, whitespace, lint, and full `dotnet test`
- Next steps:
  1. Continue center-lane work with MusicBrainz overlay export review or Quarantine Jury frontend UI once frontend lane ownership is clear.
  2. Leave concurrent player, search, wishlist, source-feed, and transfer-test edits untouched unless asked to integrate.

## Update 2026-04-30 20:38:05Z

- Current task: E15 Player Enhancements queue manager is implemented locally.
- Last activity:
  - exposed player history and clear-upcoming queue action through `PlayerContext`
  - added a Queue Manager modal with current, upcoming, and recent session sections
  - added remove queued item, clear upcoming, previous, next, and close controls
  - validated focused player radio, shortcut, rating, and PlayerBar tests
- Next steps:
  1. Continue tail-side burn-down with bounded similar-track auto-queue only from already-known local/session candidates.
  2. Later revisit queue persistence only after deciding whether the queue should survive browser reloads.

## Update 2026-04-30 20:34:08Z

- Current task: Epic 4 album substitution option hints are implemented locally.
- Last activity:
  - preserved per-track visible source options inside album candidates
  - surfaced substitution hints for tracks with multiple visible peer/provider options
  - fixed and documented the expected-track-count gotcha for duplicate alternates
  - kept the slice review-only with no alternate selection, rule save, peer browse, provider query, download, stream, metadata lookup, or file mutation
  - validated focused album candidate and Search component tests plus frontend lint
- Next steps:
  1. Continue beginning-lane burn-down with local album decision rule previews only if they stay browser-local and do not alter downloads.
  2. Later wire actual substitution selection into the download planner after an explicit album decision persistence contract exists.

## Update 2026-04-30 20:34:06Z

- Current task: T-939 non-Spotify provider URL expansion is implemented locally.
- Last activity:
  - added Apple Music/iTunes lookup for Apple URLs with numeric track or album ids
  - added ListenBrainz public-listens import from user profile URLs
  - added metadata-page fallback for YouTube, Bandcamp, Last.fm, and Apple URLs
  - added the new providers to the Wishlist Import Feed source-type selector
  - documented and committed the Apple Music track-id gotcha as `e08e73774`
  - validated focused source-feed tests, backend build, Wishlist tests, and frontend lint
- Next steps:
  1. Add authenticated/API-key provider adapters only where needed for full playlist expansion, starting with YouTube Data API and Last.fm API if users configure keys.
  2. Keep provider URL imports review-first and bounded by the existing import limit before any Discovery Inbox handoff.

## Update 2026-04-30 20:34:39Z

- Current task: E15 Player Enhancements smart-radio seed handoff is implemented locally.
- Last activity:
  - added a browser-local player radio plan helper
  - surfaced Smart Radio Seed queries from current track, album, artist, and genre metadata
  - wired explicit Search handoff buttons while keeping modal open non-networking
  - documented and committed the empty metadata array fallback gotcha as `939914cc7`
  - validated focused player radio, shortcut, rating, and PlayerBar tests
- Next steps:
  1. Continue tail-side burn-down with queue management or a bounded similar-track auto-queue from already-known local session items.
  2. Later connect smart radio to a real recommendation source only behind explicit rate limits and user action.

## Update 2026-04-30 20:29:12Z

- Current task: E15 Player Enhancements keyboard-shortcut slice is implemented locally.
- Last activity:
  - added a tested browser-local player shortcut mapper
  - wired PlayerBar keyboard handling for play/pause, seek, previous/next, mute, equalizer, lyrics, and visualizer controls
  - ignored text inputs, textareas, selects, contenteditable controls, and modified browser/system chords
  - validated focused player shortcut, player rating, and PlayerBar tests
- Next steps:
  1. Continue tail-side burn-down with smart-radio/similar-track shell only if it can stay review-only and avoid other active Search work.
  2. Later connect keyboard shortcut help to existing tooltip surfaces without adding visible instructional clutter.

## Update 2026-04-30 20:29:15Z

- Current task: Quarantine Jury pod routing attempts are implemented locally.
- Last activity:
  - added selected-juror route attempts through PodCore
  - persisted route attempt history with routed and failed juror lists
  - exposed Quarantine Jury route dispatch and history endpoints
  - kept invalid route targets and unsafe route metadata from contacting peers
  - marked the T-936 trust-scoped routing follow-up complete in the feature plan
  - validated focused jury tests, whitespace, lint, and full `dotnet test`
- Next steps:
  1. Continue the center-lane burn-down with manual Quarantine Jury review UI and accept flow if it does not overlap active frontend lanes.
  2. Leave player, search-detail, source-feed, and changelog changes from other lanes untouched unless asked to integrate.

## Update 2026-04-30 20:26:18Z

- Current task: Epic 4 album candidate review details are implemented locally.
- Last activity:
  - added album candidate format mix, missing-track, duration-spread, and warning summaries
  - surfaced the review details in Search album candidates with local-only confidence warning tooltips
  - kept the slice non-networking with no peer browse, provider query, metadata lookup, download, stream, file mutation, or rule save
  - validated focused album candidate, search deduplication, Search component tests, and frontend lint
- Next steps:
  1. Continue beginning-lane burn-down with manual track substitution scaffolding only if it can stay local and avoid transfer/download code overlap.
  2. Later add backend-backed album decision rules after the review surface has an explicit persistence contract.

## Update 2026-04-30 20:25:56Z

- Current task: E15 Player Enhancements first tail-side slice is implemented locally.
- Last activity:
  - added browser-local player rating storage and summary labels
  - preserved source provider, match-confidence, and verification metadata through `PlayerContext`
  - surfaced compact source, match, verified, and star-rating controls in the now-playing display
  - documented and committed the empty now-playing helper gotcha as `534c4d34c`
  - validated focused player ratings and PlayerBar tests
- Next steps:
  1. Continue tail-side burn-down with the next untouched E15 slice, likely keyboard shortcuts or smart-radio shell if it does not overlap the other agent.
  2. Later connect local ratings to Discovery Shelf ranking once that shelf has an explicit import/sync contract.

## Update 2026-04-30 20:23:47Z

- Current task: T-939 Spotify account connection follow-up is implemented locally.
- Last activity:
  - added Spotify OAuth authorization/status/disconnect API endpoints
  - store Spotify refresh tokens through ASP.NET DataProtection under the app directory
  - refresh connected-account access tokens server-side for private source-feed imports
  - wired Wishlist Import Feed to connect/disconnect Spotify and use the connected account automatically
  - documented and committed the raw-string JavaScript brace gotcha as `5a4ed9907`
  - validated backend build, focused source-feed tests, Wishlist tests, and frontend lint
- Next steps:
  1. Configure the Spotify app redirect URI to match `/api/v0/integrations/spotify/callback` on the deployed host or set `integrations.spotify.redirect_uri` explicitly.
  2. Extend provider fetchers to Apple Music/YouTube/Bandcamp/ListenBrainz/Last.fm after each provider's auth/rate-limit policy is chosen.

## Update 2026-04-30 20:18:09Z

- Current task: Epic 3 search-result deduplication is implemented locally.
- Last activity:
  - added browser-local duplicate media candidate folding after filtering, ranking, and sorting
  - kept the highest-ranked candidate visible and attached folded provider/peer metadata
  - added a visible Search option to disable duplicate folding when users want to inspect every source separately
  - kept the slice non-networking with no provider query, peer browse, download, stream, or metadata lookup changes
  - validated focused search deduplication/ranking/filter/Search tests and frontend lint
- Next steps:
  1. Continue beginning-lane burn-down with the next untouched Epic 4 interactive album picker surface, avoiding Discovery Inbox/Import Staging overlap.
  2. Later connect deduplication to backend provider identity once multi-provider execution returns canonical content hashes.

## Update 2026-04-30 20:18:22Z

- Current task: Quarantine Jury persistence follow-up is implemented locally.
- Last activity:
  - added atomic JSON persistence for Quarantine Jury requests and signed verdicts
  - reload persisted jury state on service startup
  - added focused rehydration coverage for requests, verdicts, and aggregate recommendations
  - validated focused jury tests, lint, whitespace, and full `dotnet test`
- Next steps:
  1. Route minimal signed jury evidence through explicitly selected trust-scoped mesh channels.
  2. Add a manual review UI that keeps local quarantine authoritative until explicit user acceptance.

## Update 2026-04-30 20:13:04Z

- Current task: T-936 Quarantine Jury first slice is implemented locally.
- Last activity:
  - added local Quarantine Jury request, evidence, verdict, signature, and aggregate models
  - added safe opaque evidence validation and selected-juror enforcement
  - added signed verdict payload-hash validation and duplicate juror replacement
  - added two-thirds recommendation aggregation without changing quarantine state
  - documented and committed the jury quorum rounding gotcha as `ac77d55c9`
  - validated focused jury tests, lint, whitespace, and full `dotnet test`
- Next steps:
  1. Persist Quarantine Jury requests/verdicts across daemon restarts.
  2. Route minimal signed jury evidence through explicitly selected trust-scoped mesh channels.

## Update 2026-04-30 20:42:00Z

- Current task: T-939 Source Feed Imports is implemented locally.
- Last activity:
  - added backend source-feed parsing for CSV, pasted text, M3U/PLS, and RSS/OPML
  - added Spotify provider fetching for public playlist/album/track/artist/user playlist URLs with app credentials
  - added per-import Spotify bearer token support for liked/saved tracks, saved albums, followed artists, and current-user playlists
  - added the Wishlist Import Feed preview flow and Discovery Inbox handoff
  - documented and committed the `HttpClient.Timeout` reuse gotcha
- Next steps:
  1. Add first-class OAuth token acquisition/storage if we want users to connect Spotify without pasting a temporary bearer token.
  2. Extend provider fetchers to Apple Music/YouTube/Bandcamp/ListenBrainz/Last.fm after each provider's auth/rate-limit policy is chosen.

## Update 2026-04-30 20:13:21Z

- Current task: None. slskdN-native config compatibility map is implemented locally.
- Last activity:
  - evaluated upstream config layout changes as reference-only because this fork is license-limited to 0.24.5
  - extended the existing YAML provider compatibility pass so `transfers.upload.limits` hydrates current global upload limits
  - added `transfers.groups` compatibility while retaining top-level `groups` and group upload-limit compatibility
  - added startup warnings for accepted legacy config shapes and updated README/config examples to prefer the new layout
  - validated project build, focused YAML/config-warning tests, `bash bin/lint`, and whitespace checks
  - full `dotnet test` still has one unrelated failure in `ContentVerificationServiceTests.VerifySourcesAsync_WhenDownloadThrows_ReturnsSanitizedFailureReason`
- Next steps:
  1. Resolve the unrelated content-verification probe-budget test failure before using full-suite status as a release gate.
  2. Keep future config-shape work in the local compatibility provider unless a breaking options-model migration is deliberately planned.

## Update 2026-04-30 20:24:00Z

- Current task: T-939 Source Feed Imports is planned.
- Last activity:
  - audited Spotify/source-feed coverage
  - confirmed existing support is limited to SongID single Spotify track URLs and Wishlist CSV playlist import
  - added Source Feed Imports as Phase 8 in `docs/design/music-discovery-federation-plan.md`
  - added T-939 to `memory-bank/tasks.md`
- Next steps:
  1. Implement the first slice as dry-run/review import into Discovery Inbox from CSV, pasted tracklists, and local playlist files.
  2. Keep Spotify/provider URL fetching disabled until explicit configuration, credentials, and rate limits are added.

## Update 2026-04-30 20:28:00Z

- Current task: T-939 Source Feed Imports first slice is implemented locally.
- Last activity:
  - added backend source-feed preview parsing for local artist/title/album rows
  - dedupe suggestions by normalized evidence key and report skipped rows
  - expose provider-token requirements while keeping provider fetch count at zero
- Next steps:
  1. Wire preview suggestions into Discovery Inbox review.
  2. Add explicit provider fetchers only behind configuration, credentials, and rate limits.

## Update 2026-04-30 20:21:05Z

- Current task: E16 Automation first follow-up slice is implemented locally.
- Last activity:
  - added cooldown, max-runtime, and approval-gate metadata to Automation Center recipes
  - persisted dry-run preview reports with network/file impact and `executed: false`
  - surfaced bounded automation details on recipe cards
  - kept all automation behavior shell-only with no recipe execution
  - validated focused Automation Center tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E15 Player Enhancements if it does not overlap player/MilkDrop work.
  2. Later wire real automation execution through bounded backend jobs with cooldown enforcement and dry-run output review.

## Update 2026-04-30 20:17:39Z

- Current task: E17 Wishlist and Requests first slice is implemented locally.
- Last activity:
  - added derived Wishlist request summary logic
  - surfaced total, enabled, automatic, review-load, and quota-style remaining counts on Wishlist
  - kept request behavior read-only with no backend request portal, scheduling, approval, or download changes
  - validated focused acquisition request and Wishlist tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E16 Automation if it does not overlap existing Automation Center work.
  2. Later add real requester identity, approval roles, and enforceable per-user quotas when backend auth/request portal work is scoped.

## Update 2026-04-30 20:14:27Z

- Current task: E18 Servarr Integration first slice is implemented locally.
- Last activity:
  - added a local Servarr readiness helper
  - added a System Integrations setup checklist for base URL, API key, wanted pull, completed import, and path-map sanity
  - kept the panel diagnostic-only with no indexer/download-client registration, wanted pull, or import action
  - validated focused Servarr helper and System Integrations tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E17 Wishlist and Requests if it does not overlap current Discovery Inbox/Wishlist edits.
  2. Later wire real Servarr indexer/download-client compatibility behind explicit API endpoints and setup wizard checks.

## Update 2026-04-30 20:11:54Z

- Current task: E19 Media Server Integration first slice is implemented locally.
- Last activity:
  - added Plex, Jellyfin/Emby, and Navidrome readiness cards to System Integrations
  - added local path diagnostics for slskdN path, media-server path, and optional remote path mappings
  - kept the slice non-networking and optional; no scan, playlist sync, play-history import, or rating sync executes
  - validated focused media-server helper and System Integrations tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E18 Servarr Integration only if it does not overlap the backend source-feed/Lidarr work.
  2. Later wire media-server adapters behind explicit configured URLs/tokens and setup checks.

## Update 2026-04-30 20:08:47Z

- Current task: E20 Diagnostics first slice is implemented locally.
- Last activity:
  - added a browser-side diagnostic bundle builder with secret redaction
  - added a System Info modal to inspect/copy the redacted YAML bundle
  - wired the bundle to current app state/options without contacting the daemon
  - kept diagnostic tests on synthetic fixture usernames/hosts only
  - validated focused diagnostic bundle helper and modal tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E19 Media Server Integration or E18 Servarr Integration only if it does not overlap backend source-feed work.
  2. Later add daemon-side diagnostic bundle APIs for logs/checks with server-side redaction.

## Update 2026-04-30 20:04:02Z

- Current task: E21 Mesh Metadata Federation first slice is implemented locally.
- Last activity:
  - added browser-local Mesh Evidence Policy storage with private defaults
  - added inbound trust-tier controls and outbound publication toggles to the Mesh tab
  - kept provenance required as a non-negotiable safety invariant
  - documented the provenance sanitizer gotcha and committed it immediately as `76bc580b1`
  - validated focused mesh evidence policy and component tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E20 Diagnostics if it does not overlap the other agent.
  2. Later connect this policy to signed mesh evidence ingestion/publication APIs once backend federation is ready.

## Update 2026-04-30 19:58:08Z

- Current task: E22 Community Quality Signals first slice is implemented locally.
- Last activity:
  - added browser-local peer quality signal storage and summaries
  - wired local quality signal context into Search candidate ranking
  - added a Search result-card affordance for local suspicious-candidate reports
  - surfaced local-only quality badges for peers with stored signals
  - validated focused community quality and candidate ranking tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E21 Mesh Metadata Federation if it does not overlap the other agent.
  2. Later add a dedicated review/clear surface for accumulated local quality signals.

## Update 2026-04-30 19:53:39Z

- Current task: E23 Mobile Workflows first slice is implemented locally.
- Last activity:
  - converted Import Staging into labeled card-style review rows on narrow screens
  - made Discovery Inbox and Import Staging actions use full-width mobile touch targets
  - added mobile cell labels to staged import table cells for responsive CSS and test coverage
  - validated focused Discovery Inbox and Import Staging component tests
- Next steps:
  1. Continue tail-side feature-expansion burn-down with E22 Community Quality Signals or E21 Mesh Metadata Federation if the opposite-side agent stays on provider/search work.
  2. Run a browser screenshot sweep for the mobile review pages when the local daemon session is stable.

## Update 2026-04-30 20:08:00Z

- Current task: T-932 Per-artist release radar first slice is implemented locally.
- Last activity:
  - added a local artist-radar service/API with artist MBID subscriptions
  - require SongID-confirmed safe music WorkRefs before accepting observations
  - dedupe notifications deterministically and honor muted release groups
  - registered the service in DI without adding polling, peer browsing, searches, or downloads
  - added focused service/controller tests and follow-up tasks for persistence, trusted observation routing, and UI controls
  - validated focused radar tests, lint, and full `dotnet test`
- Next steps:
  1. Continue the center-lane burn-down with the next unclaimed middle feature once neighboring agents' latest work is visible.
  2. Wire accepted radar notifications into Discovery Inbox or Wishlist review actions when the UI agent is ready for the next middle-adjacent slice.

## Update 2026-04-30 19:48:36Z

- Current task: None. Rooms dark-text cleanup and duplicate room navigation fix are implemented locally.
- Last activity:
  - removed the redundant joined-room recovery rail so Rooms uses hydrated tabs as the single navigation surface
  - added shared dark-mode overrides for nested Semantic UI list text and reusable `UserCard` usernames
  - gave Rooms and Chat history/input/user regions distinct dark panel tones instead of matching wireframe boxes
  - documented the gotcha and committed it immediately as `747f999b6`
  - validated frontend lint, production build, static hardcoded dark-text scan, and whitespace checks
- Next steps:
  1. Run a browser screenshot sweep against the live authenticated app when a stable local daemon session is available.
  2. Keep future dark-theme fixes in shared CSS first unless a page has a specific local surface leak.

## Update 2026-04-30 19:46:09Z

- Current task: T-935 Decentralized MusicBrainz Edit Overlay first slice is implemented locally.
- Last activity:
  - added signed MusicBrainz overlay-edit models and validation
  - added deterministic local overlay storage and read-time release-graph application without mutating cached MusicBrainz data
  - added a dedicated overlay API returning original/effective graphs plus provenance
  - validated focused MusicBrainz overlay service/controller tests
- Next steps:
  1. Gossip signed overlay edits through trust-scoped mesh/realm channels.
  2. Add manual upstream MusicBrainz export/review for selected local corrections.

## Update 2026-04-30 19:36:05Z

- Current task: None. Player local-audio file explorer redesign is implemented locally.
- Last activity:
  - added a path-aware, paged `/api/v0/library/items/browser` endpoint for local/shared library browsing
  - replaced the player flat file picker with a fullscreen explorer modal with folder navigation, breadcrumbs, recursive search, duplicate collapse, copy counts, paging, and row play actions
  - documented the flat-picker gotcha and committed it immediately as `8ed962b59`
  - validated focused PlayerBar tests, focused LibraryItemsController tests, frontend lint, production build, and whitespace checks
- Next steps:
  1. Run the full repo `dotnet test` and `./bin/lint` when the surrounding dirty workspace is ready for broad validation.
  2. Consider adding backend folder-level counts from the share repository directly if very large libraries need faster root-folder summary generation.

## Update 2026-04-30 19:48:45Z

- Current task: Import Staging failed-import denylist is implemented locally.
- Last activity:
  - added browser-local failed-import denylist storage
  - key denylist entries by staged SHA-256 when available and file signature otherwise
  - rejecting staged rows now records denylist entries
  - matching re-adds are marked Failed instead of looping as fresh staged rows
  - validated focused import staging, fingerprint, and metadata matcher tests
- Next steps:
  1. Move back toward Plan P2 Decision-Ready Search if the opposite-side agent has not already covered candidate grouping/ranking.
  2. Later connect denylisted import evidence to backend import jobs once explicit library mutation exists.

## Update 2026-04-30 19:44:02Z

- Current task: Opt-in import fingerprint verification is implemented locally.
- Last activity:
  - added browser-local SHA-256 file fingerprinting for selected staging files
  - added the Import Staging Fingerprint on add toggle
  - persisted only digest metadata on staged rows
  - kept the flow local and non-mutating with no uploads, imports, tag writes, or remote fingerprint calls
  - validated focused fingerprint/import staging tests
- Next steps:
  1. Add failed-import denylist as the next P3 Safe Acquisition Pipeline story.
  2. Later add backend Chromaprint/AcoustID verification behind explicit profile settings and rate limits.

## Update 2026-04-30 19:41:00Z

- Current task: Local import metadata matcher is implemented locally.
- Last activity:
  - added browser-side filename metadata matching for Import Staging
  - surfaced confidence, parsed identity, evidence status, and warnings in staged rows
  - added row-level and bulk match controls that do not contact metadata services or mutate files
  - fixed and documented standalone filename track-number parsing as `8e24cd666`
  - validated focused metadata matcher and import staging tests
- Next steps:
  1. Add optional fingerprint verification controls as the next P3 Safe Acquisition Pipeline story.
  2. Later connect strong metadata matches to backend MusicBrainz/fingerprint services behind explicit user action and rate limits.

## Update 2026-04-30 19:35:00Z

- Current task: Import Staging first slice is implemented locally.
- Last activity:
  - added browser-local import staging persistence
  - added `/import-staging` navigation and review UI
  - added file-picker intake that records local file metadata only
  - added staged, ready, imported, rejected, and failed state handling without library mutation
  - validated focused import staging tests
- Next steps:
  1. Add a reusable metadata matcher service as the next P3 Safe Acquisition Pipeline story.
  2. Connect staged import rows to backend library scan/import APIs only after explicit review and mutation safeguards are in place.

## Update 2026-04-30 19:33:40Z

- Current task: Wishlist/request-state unification is implemented locally.
- Last activity:
  - added shared Web UI acquisition request state mapping over Wishlist and Discovery Inbox evidence
  - added Wishlist Request State labels for disabled, wanted, automatic, review, approved, snoozed, rejected, staged, imported, and failed states
  - added a Wishlist action to send a saved search to Discovery Inbox review without starting network or download work
  - validated focused acquisition request, Discovery Inbox, and Wishlist tests
- Next steps:
  1. Add import staging scan/review as the next P3 Safe Acquisition Pipeline story.
  2. Connect Approved Discovery Inbox requests to explicit backend acquisition planning once staging exists.

## Update 2026-04-30 19:28:39Z

- Current task: Discovery Inbox first slice is implemented locally.
- Last activity:
  - added browser-local Discovery Inbox persistence and review UI
  - wired `/discovery-inbox` navigation and route
  - added a Search-page action that saves the current phrase into the inbox with the selected acquisition profile
  - kept the slice review-only; no peer search, browse, or download starts from the inbox
  - fixed and documented rejected-evidence suppression as `f6d015a80`
  - validated focused Discovery Inbox/Search tests, frontend lint, and frontend production build
- Next steps:
  1. Connect Approved inbox candidates to backend-backed acquisition planning with explicit opt-in execution.
  2. Add candidate promotion from Discography Concierge, SongID, and Discovery Graph evidence sources.

## Update 2026-04-30 19:36:08Z

- Current task: T-934 Realm-Curated Subject Indexes first slice is implemented locally.
- Last activity:
  - added signed realm subject-index artifacts and validation under `Mesh/Realm/SubjectIndex`
  - added a local registry/resolver that returns realm/index/revision provenance for recording MBID matches
  - registered the service with realm DI
  - validated focused `RealmSubjectIndexServiceTests`
- Next steps:
  1. Route subject-index revisions through realm governance proposal/review/accept/reject flows.
  2. Add UI conflict display for disagreements between realm indexes, MusicBrainz, and other subscribed realms.

## Update 2026-04-30 19:27:01Z

- Current task: T-933 Federated Taste Recommendations backend is implemented locally.
- Last activity:
  - added `TasteRecommendationService`, request/result models, DI registration, and `/api/v0/taste-recommendations`
  - recommendations are computed locally from accepted inbound ActivityPub music WorkRefs and followed remote actors only
  - service-layer k-anonymity hides candidates until two trusted sources mention the same work and omits source actor IDs by default
  - validated focused `TasteRecommendationServiceTests`
- Next steps:
  1. Add a Web UI recommendation card surface with Discovery Graph/Wishlist actions.
  2. Extend ranking with explicit Discovery Graph neighborhood proximity once the UI surface is in place.

## Update 2026-04-30 19:17:59Z

- Current task: None. Web UI affordance audit/backfill is complete.
- Last activity:
  - added shared CSS affordances for hover, focus-visible, cursor, selectable-row, dropdown, checkbox, link, disabled, and reduced-motion states
  - backfilled accessible names/titles on compact player launcher buttons and chat/room/browse/user icon action controls
  - ran headless screenshots and a DOM audit across Search, Rooms, Chat, Browse, Downloads, Uploads, Wishlist, Users, and System
  - documented the icon-only affordance gotcha and committed it as `791e91e11`
  - validated frontend lint, production build, whitespace checks, and the focused headless DOM affordance audit
- Next steps:
  1. Commit the implementation and memory-bank updates with the current dirty workspace when requested.
  2. Extend the DOM audit if new dense tool pages add custom non-Semantic interactive controls.

## Update 2026-04-30 19:10:00Z

- Current task: Acquisition profile control surface is implemented locally.
- Last activity:
  - closed the local feature-expansion roadmap decisions, including visible Web UI automation enablement
  - added persisted Search-page acquisition profiles for the first download/search decision surface
  - validated focused Search tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Wire the selected acquisition profile into backend search/candidate ranking metadata.
  2. Add an Automation Center shell that lists enabled and disabled recipes visibly.

## Update 2026-04-30 19:15:00Z

- Current task: Automation Center shell is implemented locally.
- Last activity:
  - added System -> Automations with visible recipe enablement, impact labels, cadence, and dry-run checkpoints
  - low-risk local recipes default enabled; higher-impact recipes are visible but disabled until enabled
  - validated focused Automation Center/Search tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Wire acquisition profile intent into search request payloads and backend DTOs.
  2. Begin candidate-ranking/dry-run search plan support.

## Update 2026-04-30 19:08:57Z

- Current task: None. Dark theme color/contrast audit is complete.
- Last activity:
  - ran headless screenshots for Search, Rooms, Chat, Browse, Downloads, Uploads, Wishlist, Users, and System
  - ran computed contrast checks against the slskdN dark theme and fixed the remaining low-contrast stock Semantic UI green action button styling
  - confirmed the shared palette and already-present page styles now lean further into purple accents, panel separation, and shadow depth
  - documented the Semantic color-variant contrast gotcha and committed it as `a5da3f54a`
  - validated frontend lint, production build, focused contrast sweep, and screenshot capture
- Next steps:
  1. Commit the implementation and memory-bank updates with the current dirty workspace when requested.
  2. Keep any further visual polish centralized in `App.css` unless a page has a genuine local surface leak.

## Update 2026-04-30 19:26:00Z

- Current task: Native MilkDrop Phase 2 shader-side audio access is implemented locally.
- Last activity:
  - expanded translated shader FFT uniforms from 32 to 64 bins
  - added signed waveform uniform bins and `get_waveform(pos)` shader helper
  - validated shader translator/renderer tests, broader native/player tests, native browser smoke, frontend lint, and frontend build
- Next steps:
  1. Run final whitespace/status checks.
  2. Commit and push the shader audio access phase.

## Update 2026-04-30 19:22:00Z

- Current task: Native MilkDrop Phase 2 transition modes are implemented locally.
- Last activity:
  - added transition alpha mapping for crossfade, cut, fade-through-black, and overlay modes
  - added preset aliases for `transition_mode` and caller option support for transition mode overrides
  - validated focused native transition tests, broader native/player tests, native browser smoke, frontend lint, and frontend build
- Next steps:
  1. Run final whitespace/status checks.
  2. Commit and push the transition-mode phase.

## Update 2026-04-30 19:16:00Z

- Current task: Native MilkDrop Phase 2 dense wave/shape validation is implemented locally.
- Last activity:
  - added a dense 40-shape/20-wave curated MilkDrop fixture
  - made compatibility reporting and browser smoke cover the dense primitive fixture
  - included dirty app-wide affordance CSS for hover/focus/disabled/clickable row states
  - included dirty System Automation Center scaffold and missing button labels/titles in chat, rooms, users, and player browser entry points
  - validated focused automation/fixture/matrix tests, broader native/player tests, compatibility CLI, native browser smoke, frontend lint, and frontend production build
- Next steps:
  1. Run final whitespace/status checks.
  2. Commit and push this dense primitive validation pass.

## Update 2026-04-30 19:10:00Z

- Current task: Native MilkDrop Phase 2 q-register compatibility coverage is implemented locally.
- Last activity:
  - added q-register pressure metrics to the native compatibility matrix
  - added a MilkDrop3-style `.milk2` q-register fixture covering globals, waves, shapes, sprites, and translated shaders
  - included that fixture in native browser smoke coverage
  - included the dirty Search acquisition-profile selector scaffold and validated its focused test
  - validated focused fixture/matrix/search tests, broader native/player tests, compatibility CLI, frontend lint/build, and native browser smoke
- Next steps:
  1. Run final whitespace/status checks.
  2. Commit and push the q-register coverage phase.

## Update 2026-04-30 19:01:00Z

- Current task: Native MilkDrop Phase 2 `.milk2` composite controls plus dirty workspace checkpoint are validated locally.
- Last activity:
  - committed the Moq async-list test gotcha immediately as `55aab0634`
  - fixed remaining Movie/TV nullable hash/filename normalization warnings in the dirty VirtualSoulfind provider work
  - validated backend build, focused VirtualSoulfind provider tests, focused AdvancedDiscovery tests, frontend lint/build, focused MilkDrop tests, compatibility CLI, and native browser smoke
- Next steps:
  1. Run final whitespace/staged checks.
  2. Verify GitHub target, commit the remaining dirty workspace, and push to `snapetech/slskdn`.

## Update 2026-04-30 18:54:00Z

- Current task: Native MilkDrop Phase 2 `.milk2` composite controls are implemented locally.
- Last activity:
  - added secondary `.milk2` blend mode aliases for alpha, additive, screen, and multiply final compositing
  - added primary preset transition-duration aliases for imported renderer-set crossfades when the caller does not override the duration
  - plumbed composite modes into the WebGL final blit blend function
  - validated focused native/frontend tests, compatibility CLI, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Commit and push the `.milk2` composite-control phase with the dirty README change.
  2. Continue the next Phase 2 MilkDrop pass, likely additional transition modes or shader-side audio/texture coverage.

## Update 2026-04-30 18:56:00Z

- Current task: None. Fixed chrome scroll-region audit is complete.
- Last activity:
  - measured nav, player, and footer chrome into CSS variables used by the app scroll container
  - fixed player/footer safe-area double counting and nav bottom-edge under-reservation
  - documented the fixed-chrome overlap gotcha and committed it as `4e7320841`, `52fb656fb`, and `73326e5a8`
  - validated Search, Rooms, Chat, Browse, and System geometry at desktop and mobile sizes in expanded/collapsed player states
  - validated frontend lint, production build, and whitespace checks
- Next steps:
  1. Commit the implementation and memory-bank updates with the current dirty workspace when requested.
  2. Ignore unrelated dirty Mesh unit test files unless the task changes to backend mesh tests.

## Update 2026-04-30 18:45:00Z

- Current task: None. Browser storage cleanup now covers older UI paths too.
- Last activity:
  - moved remaining production direct storage calls to shared storage wrappers outside `src/web/src/lib/storage.js`
  - covered App, Search, Discovery Graph, Browse, Chat, Rooms, Users, System Network, Footer, blocked-user, and user-context persistence paths
  - verified the only production direct storage calls left are inside the storage helper itself
  - validated focused App/Search/System/player tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Browser-check storage-disabled/private-mode behavior against a target browser if needed.
  2. Ignore the unrelated dirty Mesh unit test files unless the current task changes to backend mesh tests.

## Update 2026-04-30 18:42:00Z

- Current task: Native MilkDrop Phase 2 compatibility matrix is implemented locally.
- Last activity:
  - added a compatibility matrix module over parsed preset bodies with supported counts, unsupported functions/shaders, and max shape/wave/sprite metrics
  - added a `npm run test:native-milkdrop-compatibility` CLI that scans curated fixtures by default or supplied `.milk` / `.milk2` files and folders
  - added first high-count shape/wave metric coverage to measure real-preset-pack pressure before closing renderer gaps
  - validated focused compatibility/native tests, compatibility CLI, frontend lint, frontend production build, native browser smoke, focused GoldStar and transfer backend tests, shell syntax checks, and whitespace checks
- Next steps:
  1. Commit and push the compatibility-matrix phase.
  2. Continue the next Phase 2 MilkDrop pass from measured real-pack gaps.

## Update 2026-04-30 18:38:00Z

- Current task: Browser storage hardening is implemented locally and documented.
- Last activity:
  - routed remaining Visualizer localStorage reads/writes through safe storage helpers
  - documented browser storage access throwing in privacy-locked contexts in ADR-0001
  - validated Visualizer/native focused tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Commit and push the complete dirty workspace per the current user instruction.
  2. Continue the next large MilkDrop phase, likely real-preset compatibility expansion.

## Update 2026-04-30 18:40:00Z

- Current task: None. Browser storage cleanup is implemented locally.
- Last activity:
  - added safe local/session storage helpers
  - moved ListenBrainz, token, player toggle, equalizer, and native MilkDrop preference persistence away from direct storage API calls
  - documented the storage gotcha and committed it as `fdeb67d0d`
  - validated focused player tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Consider converting remaining direct localStorage reads in older App/Search/Browse/Rooms paths to the shared helper.
  2. Browser-check with storage disabled/private mode if a target browser still misbehaves.

## Update 2026-04-30 18:37:00Z

- Current task: Native MilkDrop translated shaders accept declared temp reassignment.
- Last activity:
  - added safe `=`, `+=`, `-=`, `*=`, and `/=` handling for declared local shader temps before final `ret`
  - kept assignment to undeclared names, duplicate `ret`, and statements after `ret` rejected by the translator
  - updated shader translator coverage, changelog, design notes, and T-938 progress notes
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Commit and push the complete dirty workspace per the current user instruction.
  2. Continue the next large MilkDrop phase, likely real-preset compatibility expansion.

## Update 2026-04-30 18:36:00Z

- Current task: None. Semantic UI cleanup pass is implemented locally.
- Last activity:
  - added shared and player-local TooltipButton behavior for consistent Semantic UI button tooltips and labels
  - switched header links to active-aware navigation styling
  - added responsive table overflow handling for Semantic UI tables
  - made every Semantic UI tab non-remounting and controlled, including nested Pods/VPN tabs
  - validated `npm run lint`, `npm run build`, and `git diff --check`
- Next steps:
  1. Browser-check representative tabs and action buttons against a live dark-mode instance.
  2. Commit the code/test/memory-bank changes with the surrounding dirty workspace when ready.

## Update 2026-04-30 18:18:59Z

- Current task: Native MilkDrop shader translator unwraps safe shader_body blocks.
- Last activity:
  - translated shader parsing now unwraps `shader_body { ... }` wrappers before statement analysis
  - safe wrapped shader bodies still use the same straight-line declaration and `ret = ...` restrictions
  - curated shader fixture now exercises wrapped warp/comp shader bodies in parser, compatibility, and browser smoke paths
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, and native browser smoke
- Next steps:
  1. Run whitespace checks, commit, and push.
  2. Continue the next large MilkDrop phase, likely shader texture access or real-preset compatibility coverage.

## Update 2026-04-30 18:29:01Z

- Current task: Native MilkDrop translated shaders have first simple conditional support.
- Last activity:
  - shader translation now rewrites safe ret-only `if (...) ret = ...; else ret = ...;` bodies into ternary expressions
  - conditional branches can still use translated texture samplers and generated coordinate helpers
  - complex control flow, temp declarations inside branches, loops, and missing-else if statements remain rejected
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Commit and push the shader conditional phase.
  2. Continue the next large MilkDrop phase, likely real-preset compatibility coverage.

## Update 2026-04-30 18:26:01Z

- Current task: Native MilkDrop translated shaders have first named-texture sampler binding.
- Last activity:
  - shader translation now detects up to four non-main `tex`/`tex2D` sampler references and maps them to `shaderTexture0..3`
  - renderer binds those sampler uniforms to imported texture assets using existing alias lookup, with procedural fallback for missing assets
  - curated shader fixture now exercises a named sampler in browser smoke coverage
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, and native browser smoke
- Next steps:
  1. Run whitespace checks, commit, and push.
  2. Continue the next large MilkDrop phase, likely shader language expansion or real-preset compatibility coverage.

## Update 2026-04-30 18:22:09Z

- Current task: None. All Semantic UI tabs now preserve inactive panes locally.
- Last activity:
  - applied `renderActiveOnly={false}` to every remaining `Tab` under `src/web/src/components`
  - verified no `Tab` usages are missing the prop
  - validated `npm run lint`, `npm run build`, and `git diff --check`
- Next steps:
  1. Browser-check representative tab switches in Browse, System, Pods, and Library Health against a live instance.
  2. Commit the code/test/memory-bank changes with the surrounding dirty workspace when ready.

## Update 2026-04-30 18:19:31Z

- Current task: None. Dark-mode surface cleanup and System integrations admin tab are implemented locally.
- Last activity:
  - added central dark-mode overrides for Semantic UI nested surfaces
  - replaced light hard-coded room and port-forwarding panel colors with theme variables
  - added System -> Integrations with VPN readiness/status/config summary and Lidarr status/wanted-sync/manual-import controls
  - added focused Integrations component tests
  - documented the Semantic UI dark-surface gotcha and committed it as `c25c26de0`
  - validated `npm test -- src/components/System/Integrations/index.test.jsx`, `npm run lint`, `npm run build`, and `git diff --check`
- Next steps:
  1. Browser-check `/chat`, `/rooms`, and `/system/integrations` against a live dark-mode instance.
  2. Commit the code/test/memory-bank changes with the surrounding dirty workspace when ready.

## Update 2026-04-30 18:12:28Z

- Current task: None. Header chat/room activity dots and tab-preservation fix are implemented locally.
- Last activity:
  - added header red dots for unread chat conversations and newer joined-room messages
  - kept chat and room `Tab` panes mounted so tab changes preserve the session instead of refreshing the visible panel
  - gated chat acknowledgment and room polling to active panes to avoid hidden-tab side effects
  - documented the Semantic UI tab remount gotcha and committed it as `d0cf8a675`
  - validated `npm test -- App.test.jsx`, `npm run lint`, and `npm run build`
- Next steps:
  1. Browser-check the header dots against a live daemon with real unread chat/room activity if desired.
  2. Commit the code/test/memory-bank changes with the surrounding dirty workspace when ready.

## Update 2026-04-30 18:14:52Z

- Current task: Native MilkDrop translated shaders have viewport context.
- Last activity:
  - generated shader fragments now expose `resolution`, `pixelSize`, `aspect`, and `texsize`
  - generated shader fragments also expose MilkDrop-style per-fragment `x`, `y`, `rad`, and `ang`
  - renderer binds viewport uniforms from the current WebGL canvas before translated warp/comp passes
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, and native browser smoke
- Next steps:
  1. Run whitespace checks, commit, and push.
  2. Continue the next large MilkDrop phase, likely shader texture access or real-preset compatibility coverage.

## Update 2026-04-30 18:10:20Z

- Current task: Native MilkDrop shader translator accepts more safe straight-line bodies.
- Last activity:
  - added safe `float/float2/float3/float4` temp declaration support for translated warp/comp shaders
  - added helper aliases from common HLSL names to GLSL: `frac`, `fmod`, `rsqrt`, and `atan2`
  - compatibility scanning now accepts those straight-line shader bodies while still rejecting control flow and mutable reassignment
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, and native browser smoke
- Next steps:
  1. Run whitespace checks, commit, and push.
  2. Continue the next large MilkDrop phase, likely shader uniform/texture expansion or real-preset compatibility coverage.

## Update 2026-04-30 18:06:55Z

- Current task: Native MilkDrop has first classic waveform mode support.
- Last activity:
  - expanded built-in waveform geometry beyond the default centered trace
  - added first support for `wave_mode`, `wave_x`, `wave_y`, `wave_a`, `wave_scale`, and `wave_smoothing`
  - renderer now passes preset scope into waveform generation and blends semi-transparent waveform output
  - added focused waveform geometry and render-call coverage
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, and native browser smoke
- Next steps:
  1. Run whitespace checks, commit, and push.
  2. Continue the next large MilkDrop phase, likely richer shader translation or real-preset compatibility expansion.

## Update 2026-04-30 18:01:27Z

- Current task: Native MilkDrop has first classic screen-border rendering.
- Last activity:
  - added filled GPU ring geometry for classic MilkDrop outer and inner borders
  - renderer now draws `ob_size/ob_r/g/b/a` and `ib_size/ib_r/g/b/a` as alpha-blended screen borders
  - added focused geometry and render-call coverage for the border path
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, and native browser smoke
- Next steps:
  1. Run whitespace checks, commit, and push.
  2. Continue the next large MilkDrop phase, likely richer waveform modes or shader translation.

## Update 2026-04-30 17:56:49Z

- Current task: Native MilkDrop primitives honor common MilkDrop field aliases.
- Last activity:
  - custom waves now read native aliases such as `nSamples`, `bSpectrum`, `bUseDots`, `bDrawThick`, and `bAdditive`
  - shapes and sprites now read native aliases such as `bEnabled`, `bTextured`, `bAdditive`, `bDrawThick`, `numSides`, and `texName`
  - parser and renderer coverage locks the alias-preservation path without rewriting imported preset data
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, and native browser smoke
- Next steps:
  1. Run whitespace checks, commit, and push.
  2. Continue the next large MilkDrop phase, likely real-preset compatibility expansion or richer shader translation.

## Update 2026-04-30 17:45:25Z

- Current task: Native MilkDrop translated shaders have first shader-side FFT access.
- Last activity:
  - translated warp/comp shaders now declare a normalized 32-bin `fftBins` uniform array and `sampleRate`
  - shader-side `get_fft(pos)` and `get_fft_hz(freq)` helpers are generated into supported shader fragments
  - renderer binds FFT bins from the current analyzer spectrum before translated shader passes
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue the next large MilkDrop phase, likely richer shader translation or higher-fidelity wave modes.

## Update 2026-04-30 17:35:22Z

- Current task: Native MilkDrop translated shaders have first q/audio uniform binding.
- Last activity:
  - translated warp/comp fragment shaders now declare q1-q64 plus bass/mid/treble audio uniforms
  - native renderer binds those uniforms from the current frame scope before translated shader passes
  - compatibility tests now accept supported shader expressions that reference q-register and audio variables
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue the next large MilkDrop phase, likely shader-side FFT texture/audio access or richer shader translation.

## Update 2026-04-30 17:31:18Z

- Current task: Native MilkDrop renderer has q1-q64 frame-scope propagation.
- Last activity:
  - added explicit q1-q64 initialization to native renderer frame scope
  - added q-register extraction/merge helpers so primitive-local q writes propagate back into the render frame
  - custom waves, shapes, and sprites now pass q-register state forward while still not leaking non-q primitive-local frame/audio values into primitive base values
  - validated focused MilkDrop/native tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue the next large MilkDrop phase, likely deeper shader/audio compatibility.

## Update 2026-04-30 17:24:08Z

- Current task: Native MilkDrop has first browser-local preset playlists.
- Last activity:
  - added saved native preset playlists stored in browser local storage
  - users can save the current visible preset bank, including search/favorites filters, as a named playlist
  - users can select, clear the active scope, or delete a playlist from the visualizer overlay
  - native next/random/previous navigation now scopes through active playlist, favorites, and search together
  - validated focused native-engine/player tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue the next large MilkDrop phase, likely richer playlist editing or deeper shader/audio compatibility.

## Update 2026-04-30 17:19:40Z

- Current task: Native MilkDrop preset-bank search is implemented locally.
- Last activity:
  - added a compact native preset-bank search input to the visualizer overlay
  - search matches imported preset title/file name, persists in browser storage, and clears with an explicit affordance
  - favorites-only mode and search compose into one visible bank
  - native next/random navigation now uses the visible filtered bank and disables next when the filter has no matches
  - validated focused native-engine/player tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue the next large MilkDrop phase, likely saved playlist sequencing or deeper shader/audio compatibility.

## Update 2026-04-30 17:10:31Z

- Current task: Native MilkDrop has first browser-local preset-bank controls.
- Last activity:
  - visualizer UI now treats imported native presets as a local bank for next-preset cycling instead of falling back to built-in presets
  - added favorite/unfavorite, favorites-only selector filtering, previous manual-selection history, and random imported-preset jumps
  - persisted favorites and library filter mode in browser local storage
  - validated focused native-engine/player tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue the next large MilkDrop phase, likely search/playlists or deeper shader/audio compatibility.

## Update 2026-04-30 16:52:16Z

- Current task: Native MilkDrop has first beat and timed automatic preset changes.
- Last activity:
  - native engine now supports `off`, `beat`, and `timed` preset automation
  - beat mode derives low-frequency spectrum energy from analyzer FFT data and advances after repeated detected bass beats
  - timed mode advances native presets on an interval
  - visualizer UI adds a tooltipped automation mode button, persists the selected mode locally, and updates the displayed preset when automation advances inside the render loop
  - validated focused native-engine/player tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue Phase 2/3 MilkDrop work, likely richer random/history preset selection or fragment library management.

## Update 2026-04-30 16:44:07Z

- Current task: Native MilkDrop has first `.shape` and `.wave` fragment import/export.
- Last activity:
  - parser now handles standalone `.shape`/`.wave` fragments and prefixed fragment-style files
  - active native presets can be reserialized after merging imported shape/wave fragments
  - native engine merges fragments into the active preset, compatibility-checks before renderer replacement, and exposes fragment export
  - visualizer UI accepts `.shape`/`.wave` imports, persists merged presets into the browser-local library, and adds tooltipped shape/wave export affordances
  - validated focused parser/native-engine/player tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue Phase 2 MilkDrop3 deltas, likely beat-driven preset changes or richer fragment library management.

## Update 2026-04-30 15:58:21Z

- Current task: Native MilkDrop has first renderer-set transitions and `.milk2` composite-alpha control.
- Last activity:
  - native preset switches and imported preset loads now crossfade renderer sets instead of immediately disposing the outgoing renderer
  - transition progress uses a bounded eased curve and disposes retired renderer sets after fade-out
  - `.milk2` secondary presets can now use `blend_alpha`, `blendalpha`, or `composite_alpha` to control overlay opacity
  - updated the curated `.milk2` fixture and WebGL MilkDrop3 port plan
  - validated focused native engine/parser/fixture/player tests, frontend lint, frontend production build, native browser smoke, and whitespace checks
- Next steps:
  1. Continue Phase 2 MilkDrop3 deltas, likely `.shape`/`.wave` import/export or beat-driven preset changes.

## Update 2026-04-30 15:38:35Z

- Current task: Native MilkDrop preset packs have a folder import affordance.
- Last activity:
  - added a separate native preset-folder import button and hidden directory input
  - directory imports reuse the existing native preset import path and browser-provided relative paths
  - added component coverage for folder input attributes, button click behavior, and relative asset path persistence
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Commit and push the folder-import phase.
  2. Continue with the next larger native MilkDrop phase, likely pack ergonomics or richer transition controls.

## Update 2026-04-30 15:26:43Z

- Current task: Native MilkDrop pack imports scope image assets per preset.
- Last activity:
  - import now indexes selected image assets by browser-provided relative path when available, plus basename and stem aliases
  - each preset source is scanned for shape/sprite texture references before it is stored
  - browser-local preset entries now persist only the referenced image assets instead of the whole selected image batch
  - added component coverage for a two-preset pack where each preset keeps only its own referenced image
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Commit and push the scoped preset-pack import phase.
  2. Continue with the next larger native MilkDrop phase, likely preset pack ergonomics or richer transition controls.

## Update 2026-04-30 15:21:59Z

- Current task: Native MilkDrop has first sprite/image primitive support.
- Last activity:
  - parser now recognizes `spriteNN_` indexed primitive entries with base values and init/frame equations
  - compatibility analysis now checks sprite equations for unsupported functions before import
  - renderer evaluates sprite init/frame equations and draws enabled sprites as textured quads
  - sprite texture lookup uses the same imported asset aliases as textured shapes and falls back to the procedural checker
  - curated classic fixture now includes a sprite, and smoke continues to render all three native fixtures
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Commit and push the sprite primitive phase.
  2. Continue with the next larger renderer phase, likely richer `.milk2` transition controls, higher shape/wave counts, or preset library controls.

## Update 2026-04-30 15:04:01Z

- Current task: Native `.milk2` double presets have a first simultaneous composite renderer.
- Last activity:
  - renderer final composite output now supports explicit alpha and optional screen clearing
  - native engine creates one renderer per compatible `.milk2` preset body
  - primary preset bodies render normally and secondary bodies blend over the primary at half opacity
  - imported `.milk2` overlay titles now include both preset names when available
  - browser smoke now renders `classic-primitives`, `shader-subset`, and `milk2-double`
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Commit and push the `.milk2` composite phase.
  2. Continue with the next larger renderer feature phase, likely sprite/image handling or richer `.milk2` transition controls.

## Update 2026-04-30 14:43:28Z

- Current task: Native `.milk2` imports now compatibility-check every preset body.
- Last activity:
  - fixed native import/inspection to analyze all parsed `.milk2` preset bodies before accepting the file
  - unsupported secondary preset bodies now reject import with a preset-indexed compatibility message
  - renderer still uses the primary body until simultaneous double-preset compositing lands
  - documented the gotcha in ADR-0001 and committed it as `124f398ef`
  - validated focused native/player tests, multi-fixture native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Commit and push the `.milk2` safety slice.
  2. Start the next renderer gap after `.milk2` import safety, likely real double-preset compositing or broader sprite/image handling.

## Update 2026-04-30 14:40:57Z

- Current task: Native MilkDrop texture lookup handles common preset-pack names.
- Last activity:
  - renderer texture lookup now strips quote wrappers and normalizes Windows-style path separators
  - both imported texture asset keys and preset shape texture references expand to full path, basename, and stem aliases
  - texture disposal now deduplicates aliased texture handles
  - added renderer coverage for a quoted `textures\\cover.png` preset reference resolving against an imported `cover` asset
- Next steps:
  1. Continue fixture coverage with real-world texture-pack examples.
  2. Start the next renderer gap after texture lookup, likely broader sprite/image handling or `.milk2` simultaneous rendering.

## Update 2026-04-30 14:38:26Z

- Current task: Native MilkDrop imports now report skipped texture assets.
- Last activity:
  - native preset import now reports oversized, unreadable, or unsupported selected texture files in the visualizer overlay
  - texture assets remain capped at 1 MB and still persist only with imported browser-local preset entries
  - added component coverage for skipped texture-asset reporting
  - validated focused native/player tests, multi-fixture native browser smoke, frontend lint, and frontend production build
- Next steps:
  1. Continue expanding real-world preset fixture coverage for imported texture packs.
  2. Add richer texture lookup rules if real preset packs expose naming/layout gaps.

## Update 2026-04-30 14:31:46Z

- Current task: Native MilkDrop imports can carry local texture assets.
- Last activity:
  - native preset import now separates `.milk` / `.milk2` preset files from image files selected in the same batch
  - small image files are stored as data-URL texture assets keyed by filename, basename, and stem
  - imported preset library entries persist their texture asset maps and pass them back into the native engine on reload
  - renderer resolves parsed shape texture names against named texture assets before falling back to the procedural checker
  - added component, native engine, and renderer coverage for texture asset import and renderer plumbing
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Add user-facing feedback for skipped oversized/unsupported image assets if needed.
  2. Continue expanding real-world preset fixture coverage for imported texture packs.

## Update 2026-04-30 14:12:13Z

- Current task: Native MilkDrop browser smoke now covers multiple curated fixtures.
- Last activity:
  - native browser smoke renders both `classic-primitives` and `shader-subset`
  - smoke reports per-fixture lit-pixel and channel-total stats
  - smoke fails if either fixture renders blank, covering textured-shape and shader paths in Chromium
  - validated focused native/player tests, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue replacing the procedural texture placeholder with explicit user/preset asset loading once asset lookup rules are defined.
  2. Add more real-world fixture cases as shader and texture support expands.

## Update 2026-04-30 14:08:41Z

- Current task: Native MilkDrop has first procedural textured-shape rendering.
- Last activity:
  - added a WebGL2 textured-shape shader and dynamic UV buffer path
  - generated a small procedural checker texture for parsed shape texture references instead of loading external assets
  - textured shapes now render when `textured`, `texture`, `tex`, or `tex_name` shape fields are present
  - added renderer tests for textured shape detection, UV mapping, texture binding, tint, alpha, and draw calls
  - updated the curated classic fixture to include a textured shape
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Replace the procedural texture placeholder with explicit user/preset asset loading once asset lookup rules are defined.
  2. Expand smoke coverage to include the textured-shape fixture once browser pixel thresholds are stable for that pass.

## Update 2026-04-30 05:45:24Z

- Current task: Native MilkDrop has a first curated preset fixture pack.
- Last activity:
  - added `nativeMilkdropFixturePack` with classic primitive, supported shader, simple `.milk2`, and unsupported shader-control-flow fixtures
  - added golden parser summary tests and fixture compatibility expectation tests
  - changed the browser native MilkDrop smoke to render the shared shader fixture instead of an inline ad hoc preset
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Expand the fixture pack with real-world presets as shader/texture compatibility grows.
  2. Continue remaining Phase 1 renderer work, especially texture support.

## Update 2026-04-30 05:41:26Z

- Current task: Native MilkDrop has a first safe shader translation/execution subset.
- Last activity:
  - added a shader translator for simple `ret = ...` shader bodies
  - supported `tex2D(sampler_main, uv)`, `saturate`, and `lerp` substitutions into WebGL2 GLSL
  - renderer now compiles supported `warp_shader` bodies into the feedback pass and supported `comp_shader` bodies into the final composite pass
  - compatibility analysis now rejects only unsupported shader bodies instead of all shader-bearing presets
  - added translator, compatibility, and renderer tests for supported and unsupported shader bodies
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Expand shader compatibility with real preset fixtures and golden/smoke coverage.
  2. Add broader MilkDrop3 shader/HLSL constructs incrementally from those fixtures.

## Update 2026-04-30 05:36:58Z

- Current task: Native MilkDrop local preset library supports per-preset removal.
- Last activity:
  - selector state now tracks the active native preset id
  - added a tooltipped remove-selected button for deleting one imported preset from browser storage
  - removing the active imported preset clears the persisted active preset but keeps the rest of the library
  - added component coverage for removing one preset while retaining another
- Next steps:
  1. Run the focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks.
  2. Continue toward richer preset-pack management or shader translation after validation.

## Update 2026-04-30 05:33:38Z

- Current task: Native MilkDrop local preset library has a first cleanup affordance.
- Last activity:
  - added a tooltipped clear-library button in native visualizer mode when imported presets exist
  - clearing removes the last imported native preset and the capped local preset library from browser storage
  - added component coverage that imports a preset, clears the library, and verifies the selector disappears
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue toward richer preset-pack management or shader translation.
  2. Add per-preset removal or search/filter once the overlay has enough imported presets to justify it.

## Update 2026-04-30 05:28:58Z

- Current task: Native MilkDrop expression compatibility now includes unary and shift operators.
- Last activity:
  - added token/parser support for `!`, `~`, `<<`, and `>>`
  - extended VM coverage for shift and unary expressions alongside bitwise/logical operators
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue toward shader translation or larger preset-pack compatibility.
  2. Add richer native preset library management once more compatibility gaps are closed.

## Update 2026-04-30 05:27:23Z

- Current task: Native MilkDrop expression compatibility now accepts inline bitwise/logical operators.
- Last activity:
  - expression tokenization now recognizes `&`, `|`, `^`, `&&`, and `||`
  - parser precedence now evaluates bitwise and logical operators after arithmetic/comparison expressions
  - added VM tests for inline bitwise/logical expressions and unsupported syntax preservation
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue toward shader translation or larger preset-pack compatibility.
  2. Expand native expression compatibility based on real preset syntax.

## Update 2026-04-30 05:25:10Z

- Current task: Native MilkDrop preset imports now support small local batches.
- Last activity:
  - local native preset import accepts multiple `.milk` / `.milk2` files
  - compatible presets are added to the capped browser-local library and the last compatible preset becomes active
  - incompatible presets are skipped with a visible skipped-file summary instead of aborting the whole batch
  - added component coverage for batch import, skipped shader-bearing presets, persisted active preset, and library ordering
  - validated focused native/player tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue with shader translation or richer preset-pack management.
  2. Expand native expression compatibility based on real preset syntax.

## Update 2026-04-30 05:21:39Z

- Current task: Native MilkDrop preset imports now have a small browser-local library.
- Last activity:
  - imported `.milk` / `.milk2` presets are added to a capped local library instead of only replacing the last preset
  - native mode shows a compact preset selector in the visualizer overlay when imported presets exist
  - selecting a saved import reloads it through the same compatibility and runtime safety path as a fresh import
  - updated the WebGL MilkDrop port design note and task log for the library slice
  - validated focused player/native MilkDrop tests, native browser smoke, frontend lint, frontend production build, and whitespace checks
- Next steps:
  1. Continue with shader translation or richer native preset-pack management.
  2. Add bulk preset-pack import/search once the native compatibility report is useful for real packs.

## Update 2026-04-30 05:15:41Z

- Current task: Native MilkDrop import-time compatibility reporting is implemented locally.
- Last activity:
  - exported supported-function checks from the expression VM
  - added a compatibility analyzer that scans global, shape, and wave equations for unsupported functions
  - import now rejects presets with unsupported equation functions or pending `warp_shader` / `comp_shader` sections before replacing the active renderer
  - added focused compatibility and native adapter tests
  - validated focused MilkDrop/player tests, native browser smoke, frontend lint, and frontend production build
- Next steps:
  1. Decide whether shader-bearing presets should be rejected or loaded with shader warnings once a partial shader translator exists.
  2. Add a preset library/list UI after compatibility reporting is useful enough for real preset packs.

## Update 2026-04-30 05:12:52Z

- Current task: Native MilkDrop expression compatibility is expanded locally.
- Last activity:
  - added common NSEEL helpers/constants to the expression VM: `pi`, `e`, inverse trig, `atan2`, `tan`, logs, `exp`, `sign`, `sigmoid`, `rand`, and bitwise helper functions
  - added tests for helper behavior, clamping, integer rand bounds, and constants
  - validated focused MilkDrop/player tests, native browser smoke, frontend lint, and frontend production build
- Next steps:
  1. Build an unsupported-feature report for imported presets.
  2. Add a preset library/list UI after compatibility reporting is useful.

## Update 2026-04-30 05:09:32Z

- Current task: Native MilkDrop runtime preset errors are surfaced locally.
- Last activity:
  - native/Butterchurn visualizer render loop now catches engine render failures
  - native render errors show the underlying unsupported syntax/function detail in the overlay
  - persisted imported native preset text is cleared when render fails so bad imports do not poison future sessions
  - documented the render-loop gotcha and committed it as `bf9e51b3a`
  - validated focused MilkDrop/player tests, native browser smoke, frontend lint, and frontend production build
- Next steps:
  1. Add a preset library/list UI after compatibility reporting matures.
  2. Expand the native parser/VM support matrix with prioritized unsupported functions found from real presets.

## Update 2026-04-30 05:06:48Z

- Current task: Native MilkDrop local preset import is implemented locally.
- Last activity:
  - added native engine `loadPresetText(source, fileName)`
  - added a tooltipped `.milk` / `.milk2` import button when the native engine is active
  - imported preset text is loaded into the native renderer, displayed by name, and persisted in local storage for the next native session
  - added component coverage for switching to native mode and importing a local preset
  - validated focused MilkDrop/player tests, native browser smoke, frontend lint, and frontend production build
- Next steps:
  1. Add clearer unsupported-syntax diagnostics when imported presets fail.
  2. Add a preset library/list UI after unsupported-feature reporting is useful.

## Update 2026-04-30 05:03:40Z

- Current task: Native MilkDrop browser canvas smoke coverage is implemented locally.
- Last activity:
  - added `scripts/smoke-native-milkdrop.mjs`
  - smoke script starts a Vite server, opens Chromium, imports the real native renderer modules, renders a curated preset, and reads canvas pixels
  - smoke fails on blank output using lit-pixel and channel-total thresholds
  - exposed the smoke as `npm run test:native-milkdrop-smoke`
  - validated focused MilkDrop/player tests, native browser smoke, frontend lint, and frontend production build
- Next steps:
  1. Add user preset loading/import for the native engine.
  2. Add visualizer error surfacing for unsupported native preset syntax.

## Update 2026-04-30 04:57:05Z

- Current task: Native MilkDrop engine is player-selectable locally.
- Last activity:
  - added a `createNativeMilkdropEngine` adapter around the WebGL renderer
  - native engine reads waveform and frequency data from the shared Web Audio visualizer tap through its own analyser
  - added curated native smoke presets exercising warp grid, motion vectors, shapes, waveform, spectrum, and dots
  - added a tooltipped visualizer overlay button to switch between Butterchurn and `slskdN native`, with the choice persisted in local storage
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Add a Playwright/browser canvas smoke test for the native engine path.
  2. Add user preset loading once the native path has browser-level smoke coverage.

## Update 2026-04-30 04:53:44Z

- Current task: First motion-vector primitive rendering is implemented locally.
- Last activity:
  - added motion-vector geometry from `mv_x`, `mv_y`, `mv_dx`, `mv_dy`, and `mv_l`
  - added motion-vector color/alpha from `mv_r/g/b/a` with frame-color fallback
  - renderer draws motion vectors as alpha-blended WebGL line segments into the feedback target
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Add renderer/player selection for the native MilkDrop engine behind an explicit opt-in control.
  2. Add a browser canvas smoke test once the native path is reachable from the player UI.

## Update 2026-04-30 04:52:13Z

- Current task: First per-pixel warp-grid renderer is implemented locally.
- Last activity:
  - fixed WebGL attribute rebinding before every fullscreen, primitive, wave, shape, and warp-grid draw path
  - documented the attribute-state gotcha and committed it as `103d4c2a0`
  - added a dedicated warp-grid shader and buffers
  - presets with global `per_pixel` equations now draw a CPU-evaluated triangle grid using `x`, `y`, `rad`, `ang`, and local `dx/dy/zoom/rot` values
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Add motion-vector rendering.
  2. Add a real browser canvas smoke test for the native renderer path once it is player-selectable.

## Update 2026-04-30 04:48:19Z

- Current task: First analyzer-backed FFT expression helpers are implemented locally.
- Last activity:
  - added `get_fft(pos)` and `get_fft_hz(freq)` to the MilkDrop expression VM
  - renderer frame scope now carries frequency data and sample rate for preset equations
  - tests cover byte-frequency normalization and Hz-to-bin mapping
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Start the first translated per-pixel/per-vertex warp grid slice.
  2. Add shader-side FFT access later when translated shader execution exists.

## Update 2026-04-30 04:46:19Z

- Current task: First custom wave dot and spectrum modes are implemented locally.
- Last activity:
  - added point-size support to the primitive shader
  - custom waves with `dots` now draw as WebGL points instead of line strips
  - custom waves with `spectrum` use frame frequency data when available and normalize byte-frequency bins
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Start the first translated per-pixel/per-vertex warp grid slice.
  2. Add `get_fft` / `get_fft_hz` expression support once analyzer frequency data is passed through the native renderer path.

## Update 2026-04-30 04:43:31Z

- Current task: First custom wave equation rendering is implemented locally.
- Last activity:
  - added custom wave init/frame state evaluation with q-register persistence and no frame/audio scope leakage
  - evaluates custom wave point equations into WebGL line-strip vertices using audio samples as point inputs
  - draws enabled custom waves with color/alpha, additive blending, and thick line hints
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Add custom wave dots and spectrum modes.
  2. Start the first translated per-pixel/per-vertex warp grid slice.

## Update 2026-04-30 04:41:36Z

- Current task: First shape gradients and thick-outline hints are implemented locally.
- Last activity:
  - changed primitive rendering to use per-vertex color buffers
  - added center-to-edge custom shape fill gradients from `r2/g2/b2/a2`
  - maps `thickoutline` to a WebGL line width hint before restoring the normal width
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Add custom wave point equations and wave modes.
  2. Add textured shape support once texture loading/caching exists.

## Update 2026-04-30 04:39:15Z

- Current task: First filled/bordered custom shape rendering is implemented locally.
- Last activity:
  - added triangle-fan fill vertices for enabled custom shapes
  - added border line strips, alpha blending, and parsed `additive` blend mode handling
  - tests cover fill geometry, fill/border color clamping, normal blending, and additive blending
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Add gradient second-color and thick-outline behavior for custom shapes.
  2. Add custom wave point equations after the current shape modes are stable.

## Update 2026-04-30 04:36:34Z

- Current task: First custom shape equation evaluation is implemented locally.
- Last activity:
  - added custom shape init/frame equation evaluation to the WebGL MilkDrop renderer
  - persists shape-owned values and q-registers while keeping frame/audio globals as read-only inputs
  - renderer tests cover animated shape state, init-once behavior, q-register persistence, and no global scope leakage
  - validated focused MilkDrop/player tests, frontend lint, and frontend production build
- Next steps:
  1. Add filled/bordered/additive shape render modes.
  2. Add custom wave point equations after the shape primitive path is stable.

## Update 2026-04-30 04:32:55Z

- Current task: First parsed shape primitive pass is implemented locally.
- Last activity:
  - added `createShapeVertices` and `getShapeColor`
  - renderer draws enabled parsed shape entries as closed WebGL line strips into the feedback target
  - tests cover shape vertex closure, disabled-shape handling, color clamping/fallback, and shape draw calls
  - validated renderer tests and frontend lint
- Next steps:
  1. Add custom shape frame equations so parsed shape state can animate.
  2. Add fill/border/additive shape modes after outline geometry is stable.

## Update 2026-04-30 04:29:10Z

- Current task: First waveform primitive pass is implemented locally.
- Last activity:
  - added a second lightweight WebGL program for line-strip primitives
  - maps incoming waveform samples into clip-space vertices with `wave_scale`
  - draws waveform lines into the feedback target before blitting to screen
  - tests cover waveform vertex generation, line draw calls, and program cleanup
  - validated renderer tests and frontend lint
- Next steps:
  1. Add a shape primitive pass for parsed `shapeNN_*` entries.
  2. Teach custom wave point equations to generate waveform vertices instead of only rendering raw samples.

## Update 2026-04-30 04:25:32Z

- Current task: First preset-driven warp uniforms are implemented locally.
- Last activity:
  - feedback texture sampling now uses evaluated `zoom`, `rot`, `dx`, and `dy`
  - added warp-state normalization for defaults and invalid zoom values
  - renderer tests cover warp uniform uploads and normalized defaults
  - validated renderer tests and frontend lint
- Next steps:
  1. Add the first translated warp/comp shader support path instead of only fixed-function warp uniforms.
  2. Start WebGL waveform/shape primitive rendering once the full-screen feedback path is stable.

## Update 2026-04-30 04:22:26Z

- Current task: Native MilkDrop WebGL feedback pipeline foundation is implemented locally.
- Last activity:
  - added ping-pong feedback texture/framebuffer targets to `milkdropRenderer.js`
  - renderer now writes to feedback, blits to screen, swaps targets, resizes texture storage, and disposes GPU resources
  - tests cover feedback allocation, draw/blit calls, decay-driven feedback blend, resize storage, and cleanup
  - validated renderer tests and frontend lint
- Next steps:
  1. Add the first real warp pass over the feedback texture using parsed preset variables.
  2. Start mapping MilkDrop waveform/shape primitives to WebGL draw calls after the feedback path is stable.

## Update 2026-04-30 04:19:23Z

- Current task: Minimal WebGL2 MilkDrop renderer skeleton is implemented locally.
- Last activity:
  - added `milkdropRenderer.js` behind the native MilkDrop foundation
  - renderer compiles a WebGL2 shader program, evaluates parsed preset equations, and draws a full-screen triangle from MilkDrop color variables
  - added tests for draw calls, color clamping, resize behavior, and missing-WebGL2 failures
  - validated focused MilkDrop tests and frontend lint
- Next steps:
  1. Replace the placeholder color pass with the first real feedback texture pass.
  2. Add real browser screenshot/pixel-stat smoke coverage once the renderer is player-selectable.

## Update 2026-04-30 04:17:00Z

- Current task: Browser-native MilkDrop3-compatible visualizer parser/VM foundation is implemented locally.
- Last activity:
  - added `presetParser.js` for classic `.milk` and basic `.milk2` double-preset parsing
  - added `expressionVm.js` for deterministic first-slice MilkDrop equation evaluation
  - added focused parser/VM tests for base values, equations, shaders, custom shapes/waves, q33 preservation, and unsupported syntax
  - documented and committed the custom wave/shape equation parser gotcha as `7a6bf43ef`
  - validated parser/VM tests, player tests, frontend lint/build, and diff whitespace
- Next steps:
  1. Add a WebGL2 renderer skeleton that consumes the parsed model but starts with a deliberately tiny draw path.
  2. Add curated `.milk` fixture files and golden parse snapshots instead of embedding all fixtures inline in tests.

## Update 2026-04-30 04:08:24Z

- Current task: Browser-native MilkDrop3-compatible visualizer engine design is started.
- Last activity:
  - inspected upstream MilkDrop3 source layout and license
  - confirmed the source is BSD-3-Clause but tied to C++/Win32/Direct3D rather than directly portable to browsers
  - added `docs/design/webgl-milkdrop3-port.md`
  - promoted T-938 to a P1 active design task for a WebGL2-first in-app engine
  - kept the external visualizer launcher positioned as an interim bridge only
  - moved Butterchurn behind a visualizer engine adapter as the first Phase 0 implementation slice
  - validated focused player tests, frontend lint, and diff whitespace
- Next steps:
  1. Start the parser/VM compatibility slice with `.milk` fixtures before adding MilkDrop3 `.milk2` features.
  2. Add a headless screenshot/pixel-stat smoke test around the engine boundary.

## Update 2026-04-30 03:40:00Z

- Current task: None. MilkDrop playback visualization is fixed and browser-verified locally.
- Last activity:
  - fixed Butterchurn module resolution under Vite dynamic imports
  - connected MilkDrop to the shared Web Audio graph through a stable visualizer tap
  - kept the tap on a live silent branch so Chromium processes analyzer data while normal playback stays on the main output path
  - added cleanup for Butterchurn audio connections when the visualizer unmounts
  - documented and committed the MilkDrop gotcha as `1cd281afe`
  - validated focused player tests, frontend lint/build, repo lint, full `dotnet test`, and a Playwright canvas screenshot smoke test
- Next steps:
  1. Keep future player graph rebuilds from disconnecting `graph.visualizerInput`.
  2. Use screenshot/image statistics for MilkDrop smoke checks instead of direct default-framebuffer `gl.readPixels`.

## Update 2026-04-30 03:28:00Z

- Current task: None. Player full-width layout and Web Audio playback resume fixes are implemented locally.
- Last activity:
  - made the player display, signal/spectrum tile, and controls stretch to the full fixed bar width
  - changed playback to resume the shared Web Audio graph before `audio.play()`
  - switched the media element to direct `src` updates
  - prevented analyzer AudioContext creation before a current track exists
  - replaced an invalid Semantic UI icon name in the signal/spectrum affordance
  - validated focused tests, frontend lint/build, headless Playwright layout/console checks, and diff whitespace
- Next steps:
  1. Re-test with a real streamed track in Chrome/Edge to confirm audible output with EQ/analyzer enabled.
  2. Keep future analyzer/MilkDrop changes from initializing Web Audio before user playback starts.

## Update 2026-04-30 03:23:00Z

- Current task: None. Player duplicate-title and lyrics lookup fixes are implemented locally.
- Last activity:
  - changed the queue preview to show only upcoming tracks, not the current track
  - made lyrics lookup infer `Artist - Title.ext` filenames and avoid LRCLIB requests with placeholder artists
  - added LRCLIB search fallback and plain-lyrics rendering
  - added focused player/lyrics regression tests
  - documented and committed the player queue/lyrics gotcha as `cea5eede6`
- Next steps:
  1. Add richer persisted metadata for local library items so lyrics and now-playing services do not depend on filename inference.
  2. Do a live browser check with a real tagged song that exists in LRCLIB.

## Update 2026-04-30 03:18:00Z

- Current task: None. New streaming/player/DHT pod/listening-party security hardening is implemented and validated locally.
- Last activity:
  - replaced browser audio JWT query-string playback with short-lived stream tickets
  - required party-bound tickets and per-IP limits for listed-party radio streams
  - switched listening-party DHT records to explicit JSON byte payloads
  - failed closed on invalid pod DHT signatures and stopped signing caller-supplied pod publish/update bodies
  - bounded local stream lookup to path IDs under configured roots and reduced local library path exposure
  - tightened ListenBrainz token clearing and submit-result reporting
  - validated Release build, focused backend tests, frontend lint/build, full `dotnet test`, and repo lint via `bash ./bin/lint`
- Next steps:
  1. Browser-smoke listed-party radio ticket playback across two authenticated nodes.
  2. Keep stream tickets short-lived and content-bound for any future media-element playback URLs.

## Update 2026-04-30 02:33:09Z

- Current task: None. Stable Winget publishing is now opt-in.
- Last activity:
  - removed the automatic `winget-main` job from `build-on-tag.yml`
  - kept the manual `Publish Winget` workflow as the high-value stable release path
  - documented that main release tags regenerate checked-in Winget metadata but do not open public `microsoft/winget-pkgs` PRs automatically
- Next steps:
  1. Use `Publish Winget` manually only for stable releases that should be promoted to the public Winget catalog.
  2. Let routine/package-test releases stay in GitHub/package channels without Winget PR noise.

## Update 2026-04-30 02:20:25Z

- Current task: None. Winget singleton validation failure is fixed locally.
- Last activity:
  - confirmed `microsoft/winget-pkgs` PR #366812 cleared CLA but still failed service validation because singleton manifests are not accepted
  - changed the stable release and manual Winget workflows to stage the generated multi-file manifests under `winget-submit/manifests/s/snapetech/slskdn/<version>/`
  - tightened staging to copy only the three stable manifest filenames so dev manifests cannot leak into the stable package directory
  - corrected the Winget gotchas documentation and committed those ADR fixes separately
  - locally verified the `.202` staging shape produces version, installer, and defaultLocale manifests with no singleton or dev files
  - updated the existing `microsoft/winget-pkgs` PR branch in place with commit `25ce9d0f1cdabdb03e42bded02a49317f785cf30` instead of opening a duplicate PR
  - pushed the slskdN workflow fix to `snapetech/slskdn` as `64cb9247a`
- Next steps:
  1. Wait for Winget validation run `307341` on PR #366812 to finish.
  2. If validation passes, wait for Microsoft review/merge; if it fails, inspect the new Azure timeline before making another PR change.

## Update 2026-04-30 02:07:00Z

- Current task: None. Player and listening-party documentation is updated.
- Last activity:
  - updated README with the integrated Web Player & Listening Parties section
  - documented modal collection/file browsers, footer-safe drawer behavior, local-root streaming, player extras, and PWA/mobile behavior
  - expanded `docs/listening-party.md` with Web Player source, controls, and local audio boundaries
  - corrected `docs/advanced-features.md` and `docs/FEATURES.md` so streaming is no longer described as Pod-only
- Next steps:
  1. Improve persisted collection item display metadata so playlist rows can show friendly filenames/titles instead of raw content ids.
  2. Browser-smoke the broader Winamp controls against a real streamed track, especially Document Picture-in-Picture in Chrome/Edge.

## Update 2026-04-30 02:06:00Z

- Current task: None. Player collection/file picking now uses modal browsers instead of compact dropdowns.
- Last activity:
  - replaced the player empty-state collection dropdown with a two-pane Collections modal
  - replaced the downloaded/shared audio dropdown with a searchable local-audio browser modal
  - kept playback actions explicit with per-row play buttons and kept the full Collections management path available
  - verified focused player tests, frontend lint/build, and mobile modal geometry with no horizontal overflow
- Next steps:
  1. Improve persisted collection item display metadata so playlist rows can show friendly filenames/titles instead of raw content ids.
  2. Browser-smoke the broader Winamp controls against a real streamed track, especially Document Picture-in-Picture in Chrome/Edge.

## Update 2026-04-30 02:05:00Z

- Current task: None. Winamp-style Web UI player enhancements are implemented and validated locally.
- Last activity:
  - added shared Web Audio graph plumbing for the player, MilkDrop, EQ, analyzer, karaoke, and crossfade gain control
  - added a persisted 10-band EQ with presets
  - added lightweight spectrum/oscilloscope rendering and Document Picture-in-Picture spectrum output
  - added LRCLIB synced lyrics and ListenBrainz now-playing/scrobble submission using a browser-local token
  - rebuilt the first-pass player bar into a modern Winamp-style deck after headless visual inspection
  - documented the additions in README, feature docs, advanced walkthrough, and changelog
  - validated focused player tests, frontend lint, and frontend production build
- Next steps:
  1. Browser-smoke the new controls against a real streamed track, especially Document Picture-in-Picture in Chrome/Edge.
  2. Improve persisted collection item display metadata so playlist rows can show friendly filenames/titles instead of raw content ids.

## Update 2026-04-30 01:52:00Z

- Current task: None. The Web UI player drawer, transport controls, and local audio picker are implemented and verified.
- Last activity:
  - added collapse/expand so the player can shrink into a drawer bar above the fixed footer
  - added previous/next, rewind, fast-forward, local mute, and browser Media Session transport handlers
  - surfaced Collections and shared/downloaded local audio in the player empty state
  - made the integrated stream locator resolve allowed local share/download roots without a separate streaming service
  - verified live Commons OGG streaming, player/footer geometry, focused tests, frontend lint, and frontend build
- Next steps:
  1. Improve persisted collection item display metadata so playlist rows can show friendly filenames/titles instead of raw content ids.
  2. Exercise the global radio/listening-party flow with two real authenticated mesh nodes once live credentials are loaded.

## Update 2026-04-30 02:20:00Z

- Current task: None. Gold Star Club leave/revoke copy now states that leaving is irrevocable.
- Last activity:
  - added a browser confirmation before Gold Star Club leave
  - changed the in-page Gold Star warning to say there are no rejoins
  - updated README and Gold Star design docs to document permanent revocation
  - validated frontend lint and diff whitespace
- Next steps:
  1. Keep Gold Star membership recovery/rejoin flows intentionally absent.
  2. Review any future governance UI for the same irrevocable-leave language.

## Update 2026-04-30 01:45:00Z

- Current task: None. Pod, room, chat, and contact social recovery surfaces are integrated.
- Last activity:
  - reviewed social workflows across Pods, Rooms, Chat, and Contacts
  - added server-state rehydration affordances for saved chats and joined rooms
  - made Pods support create, discover, and local-save flows from the main page
  - added Contacts actions to jump directly into chat or shared-file browsing
  - validated frontend lint and diff whitespace
- Next steps:
  1. Exercise pod discovery/save against a live mesh node to confirm remote metadata quality.
  2. Add signed remote pod join-request UI once the membership service has a user-friendly key/signature flow.
  3. Keep social discovery scans user-triggered and conservative.

## Update 2026-04-30 02:05:00Z

- Current task: None. Discography Concierge README/docs are updated.
- Last activity:
  - documented the feature in README, `docs/FEATURES.md`, and `docs/advanced-features.md`
  - clarified the changelog entry for MusicBrainz artist coverage and manual Wishlist promotion
- Next steps:
  1. Start T-931 Bloom-filter library diff.
  2. Keep unrelated pod/listening-party dirty work separate from the Discography Concierge slice.

## Update 2026-04-30 01:55:00Z

- Current task: None. T-930 Discography Concierge first slice is implemented locally.
- Last activity:
  - added backend coverage models, service, DI, and MusicBrainz API endpoints
  - added manual missing-track Wishlist promotion without immediate searches/downloads
  - added a Search-page Discography Concierge panel with button tooltips
  - added focused backend tests for coverage and promotion
  - validated backend build, focused tests, frontend lint, frontend build, full `dotnet test`, repo lint, and diff whitespace
- Next steps:
  1. Start T-931 Bloom-filter library diff.
  2. Follow up T-937 later for Discovery Graph density prioritization and richer local-vs-mesh evidence.
  3. Keep the unrelated listening-party dirty file separate.

## Update 2026-04-30 01:30:00Z

- Current task: None. Mesh/social music discovery feature plan is documented.
- Last activity:
  - added `docs/design/music-discovery-federation-plan.md`
  - planned Discography Concierge, Bloom Diff, Release Radar, Taste Recommendations, Realm Indexes, MB edit overlays, and Quarantine Jury
  - explicitly excluded backup, file duplication, mirroring, and replica-management scope
  - added T-930 through T-936 to `memory-bank/tasks.md`
- Next steps:
  1. Start with T-930 Discography Concierge coverage API.
  2. Keep Bloom Diff (T-931) privacy boundaries tight before using it as recommendation input.
  3. Defer Quarantine Jury implementation until signed evidence and trust-scoped routing are well tested.

## Update 2026-04-30 01:15:00Z

- Current task: None. Mixed-source accelerated failover no longer dials mesh-overlay sources through Soulseek.
- Last activity:
  - filtered sequential failover candidates to `IsSoulseekPeer()` before selecting sources
  - added a regression test for mixed mesh/Soulseek source lists
  - documented the gotcha in ADR-0001 and committed it separately
  - validated backend build, focused multi-source tests, and diff whitespace
- Next steps:
  1. Keep the broader listening-party/UI dirty work separate from this transfer fix.
  2. Commit the transfer fix with its test and changelog update.

## Update 2026-04-30 01:12:43Z

- Current task: None. Layer 1 listening parties are implemented locally.
- Last activity:
  - documented the metadata-only pod listen-along protocol, opt-in global radio registry, and deferred live mic/WebRTC layer
  - added a persistent Web UI player for existing `ContentId` stream playback
  - wired browser playback into Now Playing updates
  - added collection item play controls
  - added pod listen-along host/follow controls backed by stored/routed pod messages and SignalR fan-out
  - added a mesh/DHT-backed listed-party directory and integrated slskdN radio stream endpoint gated by a Mesh Streaming toggle
  - validated backend build, frontend lint/build, repo tests, and lint via `bash ./bin/lint`
- Next steps:
  1. Review the listening-party UX in a live browser with real shared audio.
  2. Add richer playlist queue controls and explicit seek publishing if the MVP feels right.
  3. Design the separate WebRTC live mic/commentary layer before implementation.

## Update 2026-04-30 00:58:00Z

- Current task: None. Security audit hardening is implemented and validated locally.
- Last activity:
  - required auth for ActivityPub outbox publishing
  - guarded HTTP share backfill against unsafe URLs, redirects, token-bearing logs, and oversized bodies
  - fixed file listing sibling-prefix authorization
  - removed unsupported query-string API-key CSRF exemptions
  - resolved SignalR API-key promotion through the request service provider
  - documented the security boundary gotcha and committed it separately
- Next steps:
  1. Push the security hardening commits if requested.
  2. Keep the pre-existing Winget/listening-party/UI dirty work separate unless explicitly batching it.

## Update 2026-04-30 00:44:00Z

- Current task: Fix fake-green stable Winget publishing.
- Last activity:
  - confirmed the `.202` Winget job skipped real submission because `WINGETCREATE_GITHUB_TOKEN` was empty
  - changed the main Winget release job to fail loudly when the token is missing
  - added a manual `Publish Winget` workflow for retrying an existing stable release tag after credentials are configured
  - confirmed `wingetcreate update` also fails for the first package submission because `snapetech.slskdn` is not yet present in `microsoft/winget-pkgs`
  - changed the stable Winget paths to submit generated manifests directly for the initial PR
  - fixed invalid stable Winget locale YAML caused by double-indented block-scalar text
  - changed WingetCreate staging from a flat scratch directory to the repository-shaped manifest path expected by `wingetcreate submit`
  - moved stable zip portable metadata to the installer manifest root to match accepted winget-pkgs layouts
- Next steps:
  1. Commit and push the Winget portable-manifest layout fix.
  2. Re-run the manual `Publish Winget` workflow for `2026042900-slskdn.202`.

## Update 2026-04-30 00:33:57Z

- Current task: None. AUR upgrade service refresh is implemented locally.
- Last activity:
  - confirmed AUR did not restart `slskd.service` on package upgrade
  - confirmed RPM already restarts active services through its systemd macro
  - changed AUR `post_upgrade()` to `try-restart` the service only when already running
  - documented the behavior in the AUR README, changelog, task log, progress log, and gotchas ADR
  - validated the AUR install hook syntax, diff whitespace, and repository lint
- Next steps:
  1. Commit and push the package-hook fix if requested.

## Update 2026-04-30 00:21:00Z

- Current task: Prepare replacement stable release `2026042900-slskdn.202`.
- Last activity:
  - monitored `.201` through failure in the Build release gate
  - fixed manual-review SongID Discovery Graph catalog-context expansion
  - updated the `UserService` disposal regression test for the fixture-owned regex matcher listener
  - documented and committed the SongID graph expansion gotcha separately
  - validated the affected release-gate unit-test slice
- Next steps:
  1. Run broader release-gate validation locally.
  2. Commit and push the `.202` release-gate fix plus release-note update.
  3. Push tag `build-main-2026042900-slskdn.202` and monitor the replacement release workflow.

## Update 2026-04-30 00:08:00Z

- Current task: Prepare replacement stable release `2026042900-slskdn.201`.
- Last activity:
  - monitored `.200` through failure in the Build release gate
  - fixed stale unit-test compile blockers in YAML configuration and path-normalization tests
  - documented and committed the release-gate compile gotcha separately
  - validated the affected unit-test slice
- Next steps:
  1. Commit and push the test fix plus `.201` release-note update.
  2. Push tag `build-main-2026042900-slskdn.201`.
  3. Monitor the replacement release workflow.

## Update 2026-04-30 00:01:05Z

- Current task: Prepare stable release `2026042900-slskdn.200`.
- Last activity:
  - verified GitHub writes target `snapetech/slskdn`
  - promoted the current Unreleased UI chrome and transfer polish bullets into a `.200` changelog section
- Next steps:
  1. Generate release notes for `2026042900-slskdn.200`.
  2. Commit and push the `.200` release-note update.
  3. Push tag `build-main-2026042900-slskdn.200`.

## Update 2026-04-29 23:53:57Z

- Current task: None. The low-risk upstream-request integration pass is implemented locally.
- Last activity:
  - capped automatic queue-position refresh to cached batches of five every 30 seconds
  - linked transfer peer headers to Browse from Downloads and Uploads
  - fixed successful batch download delete-on-remove path resolution
  - documented advanced search filter text syntax in README and comparison rows
  - documented and committed the batch-removal path gotcha separately
  - validated focused transfer UI tests, focused transfer backend tests, frontend lint/build, and diff checks
- Next steps:
  1. Review whether the pre-existing dirty App/Footer UI files should be included with this work or split before commit.
  2. Browser playback and large browse pagination remain larger feature work that should get separate design/testing passes.

## Update 2026-04-29 23:58:00Z

- Current task: None. Header and footer chrome alignment has been fixed and deployed to `local test host`.
- Last activity:
  - split the header into primary navigation and utility rails
  - reordered utilities to Connected, Theme, System, Log Out
  - removed the permanently-highlighted Theme trigger styling
  - rebuilt the footer as brand, speed, and network/transport rails
  - validated live logged-in screenshots at desktop, mid-width, and mobile widths with no vertical overflow
  - documented and committed the chrome grouping gotcha separately
- Next steps:
  1. Commit and push the UI chrome fix when ready.

## Update 2026-04-29 23:36:10Z

- Current task: None. The principal UI pass over the README showcase surfaces is implemented locally.
- Last activity:
  - reviewed all six README screenshots for chrome, spacing, contrast, and hierarchy problems
  - compacted the desktop nav and reduced the fixed footer footprint
  - made the mobile footer a single scrollable status rail instead of a stacked block
  - rebuilt result-card identity/action layout and file-list spacing
  - tightened Discovery Graph controls and added sparse graph messaging
  - defaulted secondary Search page panels closed and persisted their state
  - deployed the rebuilt frontend to `local test host` and recaptured the README gallery
  - documented and committed the fixed-chrome screenshot validation gotcha separately
- Next steps:
  1. Commit and push the UI pass plus README screenshot changes when ready.
  2. Keep unrelated accelerated-downloads dirty work intact unless explicitly asked to include or split it.

## Update 2026-04-29 23:22:41Z

- Current task: None. README showcase dark-mode screenshots are fixed locally.
- Last activity:
  - pulled the remote README edits with rebase/autostash
  - inspected the README showcase screenshots and identified light-theme leaks in SongID, Discovery Graph, and Network
  - added explicit dark-theme styling for the affected Semantic UI variants and nested text
  - rebuilt and deployed the web bundle to `local test host` for screenshot verification
  - recaptured `songid-cc-youtube-result.png`, `songid-discovery-graph.png`, and `network-health-dashboard.png`
  - documented and committed the dark-theme Semantic UI variant gotcha separately
- Next steps:
  1. Commit and push the README screenshot/dark-theme styling changes when ready.
  2. Keep the unrelated accelerated-downloads dirty work intact unless explicitly asked to include or split it.

## Update 2026-04-29 23:20:00Z

- Current task: None. The accelerated-downloads toggle and conservative rescue wiring are implemented locally.
- Last activity:
  - added the Downloads header `Accelerated` toggle with tooltip and runtime transfer API endpoints
  - routed the underperformance detector through the toggle so slow/stalled downloads can enter rescue acceleration only when enabled
  - preserved the trust policy: normal Soulseek downloads stay single-source, raw Soulseek alternates use verified sequential failover, and true multipart remains mesh-overlay-only
  - made discovery hash probes consume the same persistent per-peer daily verification budget as verification probes
  - changed explicit swarm download requests to default to verification enabled
  - updated README, multipart-downloads, and changelog documentation for the toggle and network-health policy
  - documented and committed the discovery probe-budget gotcha separately
- Next steps:
  1. Resolve the unrelated unit-test compile errors in `YamlConfigurationSourceTests` and `ProgramPathNormalizationTests` so the new backend tests can run in a fresh build.
  2. Decide whether the accelerated-downloads toggle should be persisted to config/UI storage or remain runtime-only.
  3. Commit and push the accelerated-downloads implementation after review.

## Update 2026-04-29 22:35:00Z

- Current task: None. The post-0.25 upstream-compatibility gap plan is implemented locally.
- Last activity:
  - added dual config schema binding and startup warnings for legacy keys
  - added regex username blacklist patterns through a cached matcher
  - fixed Search Again payload mapping and reverse-proxy-safe web metadata paths
  - verified batch retry directories were already present, clamped retry max delay to 30s, covered YAML reload, and added fork guidance
  - confirmed no SignalR typed hub exception catch pattern was present to remove
- Next steps:
  1. Run full `dotnet test` and `./bin/lint`.
  2. Commit and push the compatibility implementation.
  3. Create a tag-only release only if explicitly requested.

## Update 2026-04-29 22:02:31Z

- Current task: None. The Network dashboard public-DHT notice and false no-peer diagnostics are fixed locally.
- Last activity:
  - inspected `local test host` DHT logs and confirmed the live node is not at zero peers (`nodes=155`, `discovered=37`, `activeMesh=1`)
  - made the public-DHT exposure notice a dismissable info message instead of a persistent warning/modal
  - made connectivity diagnostics consider DHT status peer counters as well as list endpoints
  - documented the warning-counter gotcha in ADR-0001 and committed that docs entry separately
  - validation passed for focused Network tests and frontend lint
- Next steps:
  1. Commit and push the Network dashboard fix.
  2. Create a tag-only release only if explicitly requested.

## Update 2026-04-29 21:43:00Z

- Current task: Prepare stable release `2026042900-slskdn.198`.
- Last activity:
  - landed the multi-source / swarm trust-aware policy split, hard floor, probe budget, and metrics
  - rewrote `docs/multipart-downloads.md` and the README multi-source section to be explicit about scope (default downloads use the standard single-source Soulseek path; multi-source is rescue / remediation / explicit-API only)
  - promoted the unreleased Chocolatey CI fix bullets and the new swarm bullets into the `.198` changelog section
- Next steps:
  1. Commit and push the `.198` release-note update.
  2. Push tag `build-main-2026042900-slskdn.198`.
  3. Watch the release workflow.

## Update 2026-04-29 18:07:00Z

- Current task: Prepare stable release `2026042900-slskdn.197`.
- Last activity:
  - promoted Web UI fix notes into the `.197` changelog section
  - generated release notes for `2026042900-slskdn.197`
  - confirmed `.197` notes include theme picker, transfer flicker, footer speed fallback, and title fixes
- Next steps:
  1. Commit and push the `.197` release-note update.
  2. Push tag `build-main-2026042900-slskdn.197`.
  3. Watch the release workflow.

## Update 2026-04-29 17:35:00Z

- Current task: None. Browser tab title is restored to `slskdN` locally.
- Last activity:
  - changed runtime `document.title` to the short slskdN brand
  - added App test coverage for the tab title
  - updated changelog, tasks, and progress notes
- Next steps:
  1. Validate, commit, and push the title fix.
  2. Continue with the requested `.197` release after the title fix lands.

## Update 2026-04-29 17:31:00Z

- Current task: None. The Web UI theme picker, transfer bulk-action flicker, and footer speed fallback are fixed locally.
- Last activity:
  - switched the theme picker to a controlled Semantic UI dropdown and added click coverage
  - made transfer polling monotonic and hid rows after accepted bulk retry/remove operations
  - made footer speed totals use active transfer bytes-over-elapsed-time when `AverageSpeed` is still zero
  - documented the UI gotchas in ADR-0001 and committed that docs entry separately
  - validation passed for frontend lint, focused frontend/backend tests, web build, and diff checks
- Next steps:
  1. Commit and push the implementation/docs updates.

## Update 2026-04-29 17:17:00Z

- Current task: Prepare stable release `2026042900-slskdn.196`.
- Last activity:
  - promoted the current Unreleased notes into a `.196` changelog section
  - generated release notes for `2026042900-slskdn.196`
  - confirmed no non-ignored dirty files were present before release prep
- Next steps:
  1. Commit and push the `.196` release-note update.
  2. Push tag `build-main-2026042900-slskdn.196`.
  3. Watch the tag-only release workflow.

## Update 2026-04-29 17:12:30Z

- Current task: None. The slskdN top status drawer has been removed and its unique stats now render in the footer.
- Last activity:
  - deleted the status drawer component, nav toggle, visibility state, and fixed top padding
  - moved DHT, mesh, hash, sequence, swarm, backfill, and karma counters into the persistent footer
  - added footer regression coverage and validated frontend lint, focused tests, build, and diff checks
- Next steps:
  1. Commit and push the Web UI footer/status cleanup.

## Update 2026-04-29 16:56:39Z

- Current task: None. The transient full-instance overlay-connect integration failure is fixed and validated.
- Last activity:
  - reproduced `OptionalLiveAccounts_CanSearchAndDownloadHostedProbeOverOverlayMesh` passing in isolation
  - changed the DHT full-instance tests to wait through transient `/api/v0/overlay/connect` `502` responses before failing
  - validated all `TwoNodeMeshFullInstanceTests`
- Next steps:
  1. Push the integration-test stability fix to `main`.

## Update 2026-04-29 16:32:00Z

- Current task: Resolve open security warnings and Dependabot bump PRs.
- Last activity:
  - identified three open Dependabot alerts and one open CodeQL cleartext-secret alert
  - applied Dependabot PR `#222`, `#220`, and `#218` dependency upgrades into `main`
  - explicitly upgraded vulnerable OpenTelemetry packages to `1.15.3` and npm `uuid` to `14.0.0`
  - changed overlay certificate handling so legacy cleartext password files are deleted without being read
  - documented the legacy password gotcha in ADR-0001 and committed that docs entry separately
- Next steps:
  1. Run focused backend/frontend validation and security checks.
  2. Commit and push the security remediation.
  3. Confirm GitHub alerts/checks after push.

## Update 2026-04-29 16:19:50Z

- Current task: Prepare stable release `2026042900-slskdn.195` for the LAN-only DHT warning fix.
- Last activity:
  - promoted the Network dashboard LAN-only warning fix into a `.195` changelog section
  - generated release notes for `2026042900-slskdn.195`
  - validation passed for release-note generation, packaging metadata, and diff checks
- Next steps:
  1. Commit and push the `.195` release-note update.
  2. Push tag `build-main-2026042900-slskdn.195`.
  3. Watch the tag-only release workflow.

## Update 2026-04-29 16:17:24Z

- Current task: None. False Network dashboard public DHT warning for LAN-only nodes is fixed locally and ready to push.
- Last activity:
  - confirmed the tester screenshot uses `dhtRendezvous.lanOnly: true`
  - traced the false warning to frontend code checking `isLanOnly` while the backend serializes `LanOnly` as `lanOnly`
  - normalized DHT status and added Network regression coverage for the backend response shape
  - documented the gotcha in ADR-0001 and committed that docs entry separately
  - validation passed for focused Network tests, frontend lint on touched files, and diff checks
- Next steps:
  1. Commit and push the LAN-only warning fix.
  2. Prepare a new tag-only release only if explicitly requested.

## Update 2026-04-29 17:10:00Z

- Current task: Prepare stable release `2026042900-slskdn.194` for the AUR source build fix.
- Last activity:
  - promoted the AUR source date-version build fix into a `.194` changelog section
  - generated release notes for `2026042900-slskdn.194`
  - validation passed for release-note generation, packaging metadata, and diff checks
- Next steps:
  1. Commit and push the `.194` release-note update.
  2. Push tag `build-main-2026042900-slskdn.194`.
  3. Watch the tag-only release workflow.

## Update 2026-04-29 16:43:00Z

- Current task: None. `slskdn` AUR source install fix is implemented, pushed, and published to AUR.
- Last activity:
  - reproduced the fatal AUR source build failure as invalid MSBuild assembly version `2026042900.193.0.0`
  - documented the date-version packaging gotcha in ADR-0001 and committed it separately
  - updated the source PKGBUILD to pass `0.0.0-slskdn.YYYYMMDDmm.NNN` to MSBuild while keeping the public informational version unchanged
  - bumped the source AUR package template to `pkgrel=2`
  - validation passed for packaging metadata, direct fixed-version `dotnet publish`, lint, and diff checks
  - pushed live AUR `slskdn` commit `b14afe2` with `pkgrel=2` and regenerated `.SRCINFO`
- Next steps:
  1. Re-run `yay -S slskdn` on an Arch host after the AUR helper refreshes package metadata.

## Update 2026-04-29 15:46:20Z

- Current task: Prepare stable release `2026042900-slskdn.193` from `main`.
- Last activity:
  - verified GitHub write target is `snapetech/slskdn`
  - confirmed `main` is clean and current with `origin/main`
  - promoted the current Unreleased attribution/theme bullets into the `.193` changelog section
- Next steps:
  1. Validate generated `.193` release notes.
  2. Commit and push the release-note update.
  3. Push tag `build-main-2026042900-slskdn.193`.

## Update 2026-04-29 16:05:00Z

- Current task: None. slskdN Web UI theme rebrand is implemented locally and ready for validation.
- Last activity:
  - confirmed the existing dark palette matches upstream `0.24.5`
  - added slskdN as the default brown/gray/purple theme
  - kept Classic Dark and Light selectable from the Theme menu
- Next steps:
  1. Run frontend lint/tests and diff checks.
  2. Commit and push the scoped web theme update if validation is clean.

## Update 2026-04-29 15:30:45Z

- Current task: Copyright and attribution audit.
- Last activity:
  - compared tracked non-generated C# files against upstream `0.24.5`
  - preserved upstream `slskd Team` AGPL headers on upstream-derived files
  - added separate `slskdN Team` copyright blocks where upstream-derived files have slskdN changes
  - normalized slskdN-only C# files to slskdN-only attribution
  - fixed and documented the stacked-comment `SA1512` header-layout gotcha
  - validation passed for the repeat attribution audit, C# diff check, `dotnet build --no-restore`, and `bash ./bin/lint`
- Next steps:
  1. Commit the attribution normalization.
  2. Leave unrelated release/packaging metadata edits unstaged.

## Update 2026-04-29 07:45:00Z

- Current task: None. Copyright and branding attribution pass is implemented locally and ready for validation.
- Last activity:
  - clarified slskdN as an unofficial fork of slskd across docs, web metadata/footer/title text, Swagger/API metadata, default Soulseek profile text, package metadata, and generated release copy
  - changed slskdN support links to target `snapetech/slskdn` while preserving upstream slskd as an attribution/reference link
  - documented compatibility names that should remain `slskd` because they affect existing installs, config, metrics, or API expectations
- Next steps:
  1. Run packaging, frontend, backend, lint, and diff validation.
  2. Commit and push the attribution pass if validation is clean.

## Update 2026-04-29 07:11:17Z

- Current task: Prepare `2026042900-slskdn.192` for the weak SongID Discovery Graph fix.
- Last activity:
  - promoted the Discovery Graph fix note into a versioned `.192` changelog section
  - left stable package metadata on `.191`; the tag workflow updates metadata after `.192` release assets and hashes exist
- Next steps:
  1. Validate generated release notes for `2026042900-slskdn.192`.
  2. Commit and push the release-note update.
  3. Push tag `build-main-2026042900-slskdn.192`.

## Update 2026-04-29 07:07:57Z

- Current task: None. Weak SongID Discovery Graph neighborhood promotion is fixed locally and ready to commit.
- Last activity:
  - traced the bogus graph neighborhoods to secondary SongID evidence being promoted into graph nodes even when the run verdict was `needs_manual_review`
  - documented the gotcha in ADR-0001 and committed that documentation as `cbe735818`
  - gated artist/album/segment/mix graph expansion on trusted identity, while still allowing exact or high-confidence track candidates through
  - added regression tests proving weak runs do not fetch MusicBrainz artist release graphs or render secondary-evidence neighborhoods
  - validation passed for build, lint, diff check, and focused Discovery Graph tests; full `dotnet test --no-build` still failed the known unrelated full-instance overlay `502` integration cases
- Next steps:
  1. Commit the implementation/docs update.
  2. Push only if requested; do not create release/build tags.

## Update 2026-04-29 06:34:00Z

- Current task: Prepare `2026042900-slskdn.191` after `.190` Docker publish failed.
- Last activity:
  - diagnosed `.190` Docker failure: `groupadd --gid 1000 slskdn` collided with an existing GID in `mcr.microsoft.com/dotnet/runtime-deps:10.0-noble`
  - documented the fixed-ID Docker gotcha in ADR-0001 and committed it as `88e29be3d`
  - changed the Dockerfile to use system-allocated placeholder user/group IDs and added packaging validation to reject fixed `1000` Docker user/group creation
- Next steps:
  1. Validate release notes and packaging metadata for `2026042900-slskdn.191`.
  2. Push `main` and tag `build-main-2026042900-slskdn.191`.
  3. Watch `.191`; purge superseded releases after a successful replacement is live.

## Update 2026-04-29 06:25:00Z

- Current task: Prepare `2026042900-slskdn.190` after upstream-alignment changes landed on `main`.
- Last activity:
  - confirmed `origin/main` advanced past `.189` with `9b31faa4d feat: Adapt upstream packaging and transfer alignment`
  - reviewed the diff: Docker runtime user handling, packaging validation, configurable transfer retry/resume and batch metadata, IPv4-mapped address normalization, and focused tests
  - promoted the Unreleased changelog notes into `2026042900-slskdn.190` while keeping the date-versioned rollback explanation explicit
- Next steps:
  1. Validate release-note generation for `2026042900-slskdn.190`.
  2. Push `build-main-2026042900-slskdn.190`.
  3. Continue watching `.189`/`.190`; purge superseded releases after the replacement release is live.

## Update 2026-04-29 06:08:44Z

- Current task: None. Upstream-alignment items `1`, `2`, and `3` are implemented locally and validated.
- Last activity:
  - added slskdN-native low-risk upstream alignment fixes for IPv4-mapped IPv6 CIDR/proxy/API handling, null-safe config diffs, and retry callbacks/backoff controls
  - added Docker packaging/runtime support for `PUID`/`PGID`, non-root `--user` operation, app-dir access validation, corrected Docker revision metadata, and packaging validator coverage
  - added configurable direct-download retry/resume behavior plus transfer batch metadata, API batch grouping, persistence migration, DTO fields, and focused regression tests
  - compared the implementation against current upstream and refactored the retry/download retry code paths so the expression is slskdN-specific rather than structurally mirroring upstream
  - documented the IPv4-mapped IPv6 gotcha in ADR-0001 and committed it as `5f80585f4`
  - validation passed: `dotnet build --no-restore`, focused unit slices, packaging metadata validation, shell syntax checks, `bash ./bin/lint`, and `git diff --check`; full `dotnet test --no-build` passed unit/non-integration projects and hit the known transient full-instance overlay `502` once, then the exact failed integration test passed on rerun
- Next steps:
  1. Review the diff, then commit the implementation if it matches the intended copyright-safe alignment scope.
  2. Do not create release/build tags unless explicitly requested.

## Update 2026-04-29 05:35:00Z

- Current task: Corrective stable release versioning after the license-compliance rollback.
- Last activity:
  - prepared `2026042900-slskdn.189` as the public stable version format requested for slskdN, avoiding any `0.26` implication while sorting newer than removed `0.25.1-slskdn.*` packages
  - updated release-note detection, CI tag scanning, tag-build publishing, and local build/publish scripts to support `YYYYMMDDmm-slskdn.###`
  - kept .NET/NuGet build inputs valid by mapping the public release version to `0.0.0-slskdn.2026042900.189` for MSBuild and keeping `InformationalVersion=2026042900-slskdn.189`
  - generated the release notes and verified a local dotnet-only build with the new public version
- Next steps:
  1. Commit and push the versioning support to `origin/main`.
  2. Push tag `build-main-2026042900-slskdn.189` and watch the tag-only release workflow.
  3. After the new release exists, delete the superseded `0.24.5-slskdn.186` release and cleanup tag so only the corrective rollback release remains.

## Update 2026-04-29 04:47:48Z

- Current task: Final validation for the `rollback/0.24.x` release-critical backport set.
- Last activity:
  - resumed Claude's partial rollback-branch state after the Soulseek.NET PR branch was pushed but cross-repo PR creation failed due token permissions
  - kept the already-applied surgical backports for release-note size guardrails, runtime YAML alias binding, directory browse timeout handling, and shutdown cancellation classification
  - added the missing selective backports for build-on-tag publish unblocking and empty cached user-group fallback
  - updated changelog/tasks/progress for the rollback backport work
  - validation passed: focused `YamlConfigurationSourceTests`/`UserServiceTests`, release-note fail-closed smoke, `git diff --check`, and `bash ./bin/lint`
  - full `dotnet test --no-restore` passed unit/non-integration projects and failed only the known transient full-instance overlay `502` cases; rerunning the exact two failed integration tests passed
- Next steps:
  1. Commit the rollback backport set.
  2. Do not create build tags unless explicitly requested.

## Update 2026-04-26 20:08:00Z

- Current task: Issue `#216` CSV playlist import is implemented and ready to commit/push.
- Last activity:
  - fetched GitHub issue `#216` from `snapetech/slskdn`: request is batch downloading music from CSV exports like TuneMyMusic
  - added a wishlist CSV import model/parser that recognizes TuneMyMusic-style track/artist/album headers, handles quoted CSV fields, generates artist/title wishlist searches, and deduplicates against existing/imported rows
  - added authenticated `POST /api/v0/wishlist/import/csv`; import creates wishlist entries without starting a large immediate Soulseek search burst, while optional `autoDownload` uses the existing wishlist scheduler/manual run path
  - added a Wishlist page CSV import modal with file/text input, filter, max results, enabled, auto-download, and album-term controls
  - validation passed: focused `WishlistControllerTests`, frontend lint, frontend production build, `dotnet build --no-restore`, `bash ./bin/lint`, `git diff --check`, full `dotnet test --no-restore` except one known transient optional live mesh setup `502`, and the exact failed integration test passed on rerun
- Next steps:
  1. Commit and push the issue `#216` work.
  2. Create `build-main-0.24.5-slskdn.181` if an immediate main build is still desired.

## Update 2026-04-26 19:33:25Z

- Current task: None. Deep code audit of Bas upload failure found a concrete listen endpoint advertisement bug and the fix is ready to commit/push.
- Last activity:
  - re-audited listener setup, runtime listener updates, inbound upload enqueue handling, upload queue processing, share resolution, and Soulseek.NET peer/transfer connection behavior
  - found that runtime `soulseek.listen_port` / `soulseek.listen_ip_address` changes can restart the local listener without forcing the Soulseek server to advertise the new endpoint
  - documented the gotcha and committed ADR-0001 entry as `d326113fc`
  - marked listen endpoint options as reconnect-required and added focused regression coverage for connected listen-port updates setting `PendingReconnect`
  - validation passed: `git diff --check` for touched files, `bash ./bin/lint`, focused `ApplicationLifecycleTests`, and rerun of the one flaky optional live mesh integration test
  - full `dotnet test` passed unit/non-integration projects and failed once only on the known optional live mesh account setup-time overlay `502`; the exact failing test passed on rerun
- Next steps:
  1. Commit and push the upload listen endpoint fix.
  2. If this needs to supersede release `.179`, tag a new build after push.

## Update 2026-04-26 19:14:31Z

- Current task: None. Bas upload troubleshooting instrumentation is implemented, validated, committed, and pushed to `snapetech/slskdn` `main`.
- Last activity:
  - added structured `[UPLOAD-DIAG]` logs when remote peers request uploads and when those requests are rejected
  - added authenticated `/api/v0/transfers/uploads/diagnostics` with configured listener details, local TCP probe, share/index state, upload counters, recent upload records, and actionable warnings
  - added focused unit coverage for the new diagnostics endpoint
  - validation passed: `git diff --check`, `bash ./bin/lint`, focused `TransfersControllerTests`, and full `dotnet test`
  - pushed commit `1fcfbcece` to `main`
- Next steps:
  1. If this needs to ship to testers immediately, create a new build tag only after explicit release/tag confirmation.
  2. Tell Bas to collect `/api/v0/transfers/uploads/diagnostics` output and `[UPLOAD-DIAG]` log lines on the next build.

## Update 2026-04-26 17:13:39Z

- Current task: Tester upload/DHT onboarding feedback is being investigated.
- Last activity:
  - confirmed the DHT public-discoverability warning used internal option names while YAML binds the public key under `dht:`
  - added `dht.lan_only` to the example config and updated the runtime warning to mention `dht.lan_only: true` / `dht.enabled: false`
  - documented the gotcha in ADR-0001 and committed that docs-only entry as `29d4d4958`
  - confirmed upload troubleshooting should collect listener bind logs, attempted remote upload logs, `/api/v0/options`, `/api/v0/application`, `/api/v0/transfers/uploads?includeRemoved=true`, and host/container port state
- Next steps:
  1. Run validation for the small DHT warning/config change.
  2. Commit and push the implementation/docs update if validation passes.
  3. Send tester-facing upload diagnostics checklist and DHT config guidance.

## Update 2026-04-24 16:10:07Z

- Current task: None. The AUR permission fix and issue `#209` early mesh-result publication fix are committed and pushed to `snapetech/slskdn` `main`.
- Last activity:
  - rebased local `main` onto remote release metadata commit `c93cf1653`
  - pushed four commits to GitHub through `00742f9cd`: AUR permission gotcha, mesh publication-delay gotcha, AUR permission fix, and early mesh-result publication fix
  - verified local and remote `main` both resolve to `00742f9cd4f17aca09e293736be8a7f47c77c467`
  - checked Actions: no current release/tag workflow is running; only CodeQL and dependency-submission checks started from the `main` push
  - confirmed published release `0.24.5-slskdn.177` / build tag `build-main-0.24.5-slskdn.177` does not contain these fixes because the build tag points at `92fa389c`
- Next steps:
  1. If these fixes should ship immediately, create a new build tag after explicit user confirmation, likely `build-main-0.24.5-slskdn.178`.
  2. Do not cancel anything unless a new release/tag workflow is found running.

## Update 2026-04-24 15:47:21Z

- Current task: Issue `#209` mesh-result UX follow-up is implemented and locally validated.
- Last activity:
  - inspected the latest tester follow-up and confirmed the current symptom is not peer acquisition delay: DHT/overlay were ready with `activeMesh=8`, mesh search returned `beatles` results at `09:22:39`, and the combined search completed at `09:22:54` after the normal 15-second Soulseek timeout
  - documented the gotcha in ADR-0001 and committed the docs-only entry as `713fe1fcf`
  - changed `SearchService` to persist and broadcast merged mesh/pod responses as soon as the mesh overlay search returns, while preserving final Soulseek+mesh merging at search completion
  - changed the search detail page to refetch responses when early result counts appear, not only when `isComplete` flips
  - validation passed: focused backend `SearchServiceLifecycleTests`, focused frontend search tests, frontend lint, `git diff --check`, and `bash ./bin/lint`
  - full `dotnet test --no-restore` passed the unit and non-integration projects, then failed one integration case (`TwoFullInstances_CanFormOverlayMeshConnection`) with setup-time HTTP 502; rerunning that exact integration test by itself passed
- Next steps:
  1. Reply on issue `#209` after this change is pushed/released, explaining that mesh results should now appear before slow/empty Soulseek searches finish.
  2. Do not create build tags unless explicitly requested.

## Update 2026-04-24 15:43:56Z

- Current task: None. The AUR release payload permission fix is implemented, locally validated, committed, and published to the live `slskdn-bin` AUR package.
- Last activity:
  - investigated AUR user feedback for `slskdn-bin 0.24.5.slskdn.177-1`, where `/usr/lib/slskd/releases/0.24.5.slskdn.177/` installed as `drwx------ root root` and blocked systemd/non-root startup
  - identified the staging-mode leak from `mktemp -d` plus archive-preserving copy into the release root
  - documented the gotcha in ADR-0001 and committed the docs entry as `a75d5783f`
  - updated `PKGBUILD`, `PKGBUILD-bin`, and `PKGBUILD-dev` to normalize release payload permissions with `chmod -R u=rwX,go=rX "${release_root}"` and apphost mode `755`
  - updated packaging metadata validation and the changelog/memory-bank records
  - validation passed: `bash packaging/scripts/validate-packaging-metadata.sh`, `bash ./bin/lint`, `git diff --check`, and local package-function smokes proving source/binary/dev release roots install as `0755`
  - published `slskdn-bin 0.24.5.slskdn.177-2` to AUR with commit `6766f22`
- Next steps:
  1. Push the local repository commits when ready.
  2. Tell affected users to update to `slskdn-bin 0.24.5.slskdn.177-2`; if their helper reuses stale metadata, clear the helper cache and rebuild.

## Update 2026-04-23 00:00:00Z

- Current task: None. The AUR binary packaging hardening for the reported `0.24.5.slskdn.175-1` Manjaro missing-DLL failure is implemented and locally validated.
- Last activity:
  - confirmed the live `0.24.5-slskdn.175` Linux x64 release zip is self-contained and includes `Microsoft.AspNetCore.Diagnostics.Abstractions.dll`, so the issue was not an upstream/slskd or GitHub release-asset regression
  - documented the AUR binary zip-staging gotcha in ADR-0001 and committed that docs entry as `7cd2cb9e1`
  - changed `PKGBUILD-bin` and `PKGBUILD-dev` to use `noextract`, unzip the downloaded archive during `package()`, and fail if the staged bundle is missing `slskd`, `slskd.deps.json`, or `Microsoft.AspNetCore.Diagnostics.Abstractions.dll`
  - updated the AUR README, changelog, and packaging metadata validator to match the new binary staging path
  - validation passed: packaging metadata script, `git diff --check`, direct release-zip smoke, and `bash ./bin/lint`; repo-wide `dotnet test` still has unrelated DNS/wildcard-sensitive failures in existing unit tests
- Next steps:
  1. Push the AUR packaging fix when ready and let the next stable/dev AUR publish carry it.
  2. If the Manjaro reporter is still blocked before the next release, reply with a temporary workaround focused on clearing the `yay` cache and rebuilding `slskdn-bin`.

## Update 2026-04-22 18:57:24Z

- Current task: Issue `#209` live mesh proof is complete for the two-account sandbox path.
- Last activity:
  - generated two fresh short alphanumeric Soulseek test accounts and recorded them in the gitignored live mesh env file
  - stored the same credential set in OpenBao at `secret/slskdn/mesh-live-test-accounts`
  - fixed live full-instance harness config/auth gaps: explicit Soulseek server endpoint, unique child listen ports, slower login polling with 429 tolerance, and API-key auth for mutating setup endpoints
  - proved the public-network path: the live-account smoke logged both accounts into Soulseek, hosted a generated probe file on beta, mesh-searched it from alpha, downloaded it through the pod path, and byte-compared the transfer
  - validation passed: standalone live-account smoke and full `TwoNodeMeshFullInstanceTests` class (`3` tests)
- Next steps:
  1. Run lint/diff checks and commit the live harness fixes plus memory/changelog updates.
  2. Do not create a build tag unless explicitly requested.

## Update 2026-04-22 18:28:38Z

- Current task: Issue `#209` mesh proof is locally strengthened with an optional live-account smoke; public logged-in sandbox validation is pending credentials.
- Last activity:
  - confirmed the deterministic two-full-instance mesh test already proves search result discovery plus pod transfer and byte comparison over the real overlay stack
  - added `OptionalLiveAccounts_CanSearchAndDownloadHostedProbeOverOverlayMesh`, which starts two full slskdN instances with configured Soulseek test credentials, waits for both to log in, hosts a generated probe on beta, mesh-searches from alpha, downloads it, and byte-verifies the file
  - validation passed: focused `TwoNodeMeshFullInstanceTests` (`3` tests), `bash ./bin/lint`, and `git diff --check`
  - no gitignored `tests/slskd.Tests.Integration/local-mesh-accounts.env` file is present here, so the public-network live-account branch is ready but not yet exercised
- Next steps:
  1. Populate `tests/slskd.Tests.Integration/local-mesh-accounts.env` with the two test credentials or export the matching environment variables.
  2. Run the optional live-account smoke to prove public logged-in mesh search and transfer end to end.
  3. Do not create a build tag unless explicitly requested.

## Update 2026-04-22 17:25:51Z

- Current task: Issue `#209` troubleshooting has a live answer from `local test host`: normal Soulseek search works after removing auto-replace budget contention; mesh search still has no active peers because discovered overlay endpoints are unreachable.
- Last activity:
  - deployed `0.24.5-slskdn.174+manual.0214ccc8b` to `/usr/lib/slskd/releases/manual-0214ccc8b`; API reports the matching executable/version and systemd is active with `NRestarts=0`
  - verified source-separated logs: auto-replace spends `source=auto-replace`, user/API searches spend `source=user`
  - reran user/API searches with the correct millisecond timeout (`10000`): `radiohead` returned `2` responses / `518` files, `pink floyd` returned `6` / `628`, `nirvana` returned `3` / `1148`, and `beatles` timed out with zero
  - confirmed the earlier API zero-result repro used `searchTimeout: 10`, which the API DTO documents as seconds but the underlying Soulseek option treats as milliseconds; documented the gotcha as `47211c67`, patched API/discovery timeout conversion, and verified live that `searchTimeout: 10` now returns `radiohead` results over a real user search
  - sampled DHT/overlay after startup: DHT running with `7` discovered peers, but `0` active mesh connections and `0` successful overlay connections; failures are connect timeouts/no-route
- Next steps:
  1. Continue mesh reachability work from the overlay side, not the normal Soulseek search path.
  2. Push the local commits when ready; do not create a build tag unless explicitly requested.

## Update 2026-04-22 16:55:30Z

- Current task: Issue `#209` troubleshooting found and fixed a second local app issue: background auto-replace was sharing the user/API search safety limiter bucket. Implementation is validated locally and ready to commit/push.
- Last activity:
  - confirmed the earlier `local test host` zero-result manual searches were not a clean population test because auto-replace was repeatedly charging `SafetyLimiter.TryConsumeSearch("user")`
  - documented the background-search budget gotcha in ADR-0001 and committed it as `1a9cb7dc2`
  - added an explicit search safety source overload, preserving `user` for UI/API callers and charging auto-replace to `auto-replace`
  - added source-aware search rejection logs and completion summaries with Soulseek/mesh/merged response counts, file count, and duration
  - validation passed: focused unit search/auto-replace/API slice (`13` tests), Release app build, and Release integration test project build
- Next steps:
  1. Run lint and diff checks.
  2. Commit the implementation/docs update.
  3. Install or otherwise deploy the fixed build on `local test host`, then rerun clean manual searches while watching the new source-aware logs.

## Update 2026-04-22 16:55:00Z

- Current task: Issue `#209` triage on `local test host` is complete locally; two app-side fixes are implemented and validated, but the code changes still need commit/push if they should ship.
- Last activity:
  - confirmed `local test host` is already running `slskdn-bin 0.24.5.slskdn.174-1`, systemd `active/running`, `NRestarts=0`, Soulseek logged in, shares ready, DHT ready with `250` nodes, and overlay TCP listening on `50305`
  - sampled DHT/overlay diagnostics showing a thin verified overlay (`activeMesh=1`, `successes=1`) with most public candidates failing by timeout/no-route/TLS EOF; this supports remote endpoint quality/population as the mesh-connectivity limit, not a local DHT bootstrap failure
  - reproduced zero-result normal API searches for `radiohead` and `beatles`, but auto-replace was repeatedly exhausting the search safety budget, so persistent Soulseek zero-result behavior needs a separate focused pass
  - fixed fake fatal unobserved task noise for normal remote transfer rejections (`Too many megabytes`, `Too many files`)
  - stopped circuit maintenance from automatically running placeholder circuit-building probes against live peers
  - documented both gotchas in ADR-0001 and committed that docs entry as `95702a2dd`
  - validation passed: focused unit slice (`42` tests), Release build, `bash ./bin/lint`, and `git diff --check`
- Next steps:
  1. Commit and push the code/docs/changelog/memory updates if approved.
  2. If search zero-results remain important, pause or disable auto-replace search consumption and run a clean Soulseek search comparison.

## Update 2026-04-21 08:21:42Z

- Current task: SongID UX cleanup and the prior `173` release-gate unit failure are fixed and validated; next step is committing, pushing, and tagging the requested release.
- Last activity:
  - audited the SongID page headlessly against the `local test host` backend with a YouTube URL
  - fixed duplicate result actions/candidates, noisy diagnostic exposure, and the duplicate graph/atlas action in `SongIDPanel`
  - isolated static event subscriber-count tests in a non-parallel xUnit collection after the `173` release-gate failure
  - validated with frontend lint, focused Search UI tests, production web build, headless render pass with no HTTP 4xx/5xx responses, full Release unit suite, repo-wide `dotnet test`, and `bash ./bin/lint`
- Next steps:
  1. Commit and push the validated changes.
  2. Create the next requested build tag and watch the release gate.

## Update 2026-04-21 06:45:00Z

- Current task: Validating the `0.24.5-slskdn.171` yay install on `local test host` and fixing actionable live noise.
- Last activity:
  - confirmed `slskdn-bin 0.24.5.slskdn.171-1` was installed, but the service was still running old PID `2151618` from `/usr/lib/slskd/releases/0.24.5.slskdn.170`
  - restarted `slskd`; the service is now running PID `2269037` from `/usr/lib/slskd/releases/0.24.5.slskdn.171`, `active/running`, `NRestarts=0`, with API `/api/v0/application` reporting `0.24.5-slskdn.171`
  - verified the duplicate MeshDHT self-descriptor publish fix holds on actual `171` startup: one `[MeshDHT] No configured endpoints...` and one `[MeshDHT] Published self descriptor...`, not the previous duplicate pair
  - verified web/API, Soulseek login, shares, and overlay TCP listener are up; DHT was still in the bootstrap window on the first post-restart API sample
  - found a remaining fake fatal from the old `170` PID before restart: `Soulseek.ConnectionReadException` with inner `IOException`/`SocketException` timeout from `Soulseek.Network.MessageConnection.ReadContinuouslyAsync`
  - documented that classifier gotcha in ADR-0001 and committed it as `2d5ee08bc`
  - patched `Program.IsExpectedSoulseekNetworkException(...)` so `Connection timed out` and `Unable to read data from the transport connection` inner exceptions match expected Soulseek network churn
  - validation passed so far: focused `ProgramPathNormalizationTests` (`29` tests), Release build, `bash ./bin/lint`, and `git diff --check`
- Next steps:
  1. Re-sample `local test host` after DHT bootstrap completes and watch for fresh current-PID fatal/error/coredump/overlay cooldown noise.
  2. Commit and push the read-timeout classifier fix after changelog/memory updates.
  3. Do not create another build tag unless explicitly requested.

## Update 2026-04-21 06:22:00Z

- Current task: None. The `local test host` log-polish findings are fixed in `main` and pushed; installed `0.24.5-slskdn.170` is still running until the next requested release/package update.
- Last activity:
  - confirmed the live yay package and API still report `0.24.5-slskdn.170`, systemd is `active/running`, `NRestarts=0`, Soulseek is `Connected, LoggedIn`, shares are ready, DHT is running, and overlay TCP is listening on `50305`
  - sampled current-process logs and coredumps; no fresh fatal/error/exception/502/coredump/bind/protocol noise appeared
  - found the remaining actionable noise was normal remote overlay endpoint cooldown streaks logging one information line per endpoint even though the aggregate DHT/overlay summaries and `/api/v0/overlay/stats` already expose the degraded endpoint state
  - documented the logging gotcha in ADR-0001 and committed it as `0018e6b90`
  - changed `MeshOverlayConnector.RecordFailure` so per-endpoint cooldown streak detail logs at debug while the aggregate summaries remain information-level
  - validation passed: focused DHT/rendezvous unit slice (`105` tests), Release build, `bash ./bin/lint`, changelog validation, and `git diff --check`
  - pushed the fix as `3f901d944`
  - final API check against the real web listener on `5030` returned quickly for `/api/v0/application`, `/api/v0/dht/status`, and `/api/v0/overlay/stats`; earlier timeouts were from probing the Soulseek listener on `50300`
  - installed `170` still logs the duplicate descriptor publish and per-endpoint cooldown detail because those fixes landed after the package was built
- Next steps:
  1. Keep `local test host` on installed build `170`; do not create another build tag unless explicitly requested.
  2. Validate the duplicate descriptor and cooldown log cleanup after the next release/package install.

## Update 2026-04-21 05:59:00Z

- Current task: Removing Snap publishing from release workflows after `0.24.5-slskdn.170` got stuck waiting on Snap Store publication.
- Last activity:
  - confirmed `local test host` is still running `slskdn-bin 0.24.5.slskdn.170-1` from systemd PID `2151618`, `active/running`, `NRestarts=0`, `ExecMainStatus=0`
  - verified authenticated API state: `/api/v0/application` reports `0.24.5-slskdn.170`, Soulseek is `Connected, LoggedIn`, shares are ready, DHT is running, and the TCP overlay listener is active on `50305`
  - monitored current-process journal after the auto-replace cycle started; no fresh fatal/error/exception/502/coredump/rate-limit/search-budget noise appeared, and no new `slskd` coredumps were present
  - found one fixable startup polish issue: `MeshBootstrapService` and `PeerDescriptorRefreshService` both published the same self descriptor at startup, producing duplicate `[MeshDHT] No configured endpoints...` and `[MeshDHT] Published self descriptor...` lines
  - documented the duplicate-publish gotcha in ADR-0001 and committed it as `a4e516468`
  - changed `PeerDescriptorRefreshService` so periodic refresh scheduling starts from current time, leaving the bootstrap service as the startup publisher while still allowing IP-change-triggered refreshes
  - validation passed: focused `PeerDescriptorRefreshServiceTests`, full unit suite (`3553` tests), Release build, `bash ./bin/lint`, and `git diff --check`
  - pushed the duplicate self-descriptor publish cleanup as `a1f105521`; CodeQL and dependency submission for that push both passed
  - removed Snap publishing from the tag workflow, converted the manual dev helper workflow to Docker-only, and stopped release metadata scripts/validators from requiring Snap manifest updates
  - validation for the Snap purge passed: YAML parse for touched workflows, packaging metadata validation, changelog validation, and `git diff --check`
- Next steps:
  1. Commit and push the Snap workflow purge.
  2. Cancel the still-running `build-main-0.24.5-slskdn.170` workflow if only Snap remains in progress.
  3. Do not create another build tag unless explicitly requested.

## Update 2026-04-21 05:06:00Z

- Current task: Validating the `0.24.5-slskdn.169` yay install on `local test host` and fixing actionable live noise.
- Last activity:
  - confirmed `local test host` is running `slskdn-bin 0.24.5.slskdn.169-1` from systemd PID `2045334`, `active/running`, `NRestarts=0`, with `ExecMainStatus=0`
  - verified the installed binary reports `0.24.5-slskdn.169`; current-process startup bound the expected web, Soulseek, DHT, and TCP overlay listeners
  - ran the bounded Playwright route/tab sweep: `/tmp/local test host-route-tab-sweep-2026-04-21T04-55-23-627Z.md`, `307` visits, `1691` same-origin responses, status summary `{"200":1687,"204":4}`, `0` issues, and no 4xx/5xx/502 responses
  - triaged the sweep warnings: most are scanner false positives from `/system/options` literal `false`/disabled config text, but `/system/logs` exposed a real current-process fatal unobserved `NullReferenceException` from `Soulseek.Extensions.Reset(Timer timer)` in a Soulseek.NET write path
  - documented and patched the live stack-signature miss so expected Soulseek timer-reset read/write races match `Reset(` instead of the synthetic exact `Reset(Timer)` string
  - also quieted clean shutdown telemetry so normal SIGTERM/systemd restart paths no longer print warning/abnormal `app.Run()`/duplicate stderr lines
  - validation so far: focused `ProgramPathNormalizationTests`/expected-network unit slice passed (`30` tests)
- Next steps:
  1. Run Release build, lint, and diff checks.
  2. Commit and push the shutdown/log-noise fixes after GitHub target verification.
  3. Do not create another build tag unless explicitly requested.

## Update 2026-04-21 04:24:00Z

- Current task: Validated the `0.24.5-slskdn.168` yay install on `local test host` and fixed the actionable issues found locally.
- Last activity:
  - confirmed `pacman`, `/usr/bin/slskd --version`, the active release symlink, and the authenticated API all report `0.24.5-slskdn.168`
  - verified the service is active, Soulseek is `Connected, LoggedIn`, shares are ready, DHT is running, and the expected TCP/UDP listeners are present after a clean restart
  - found the initial package start had lost the TCP overlay listener because `50305` hit a transient `Address already in use`; fixed rendezvous startup to retry transient overlay bind failures and added focused unit coverage
  - reduced remaining live startup journal noise by demoting method-entry/constructor/security-pipeline probe logs to debug and keeping stdout/stderr probes behind explicit E2E trace environment variables
  - investigated the red `build-main-0.24.5-slskdn.168` Actions run and found the release artifacts were published; only the final Matrix announcement failed with HTTP 504, so the announcement webhooks now retry and degrade to warnings
  - validation passed: YAML parse, focused DHT rendezvous tests, full unit suite, Release build, `./bin/lint`, and `git diff --check`
- Next steps:
  1. Commit and push the current fix set after the final `local test host` log sample.
  2. Do not create another release/build tag unless explicitly requested.

## Update 2026-04-21 04:08:00Z

- Current task: Completed the GitHub Releases page audit/rewrite.
- Last activity:
  - edited every published GitHub release body currently on `snapetech/slskdn`, from `0.24.5-slskdn.123` through `0.24.5-slskdn.168`
  - each release now describes the delta from the previous published release, links the compare range, groups changes by type, and lists exact commits with direct links
  - waited for the new `0.24.5-slskdn.168` release object to appear, then rewrote that body too with the `0.24.5-slskdn.167...0.24.5-slskdn.168` delta
  - verified `37` release bodies through GitHub after editing; no required release-note sections were missing
- Next steps:
  1. Let the `build-main-0.24.5-slskdn.168` workflow finish its remaining package jobs.
  2. If the release-notes generator should keep this stricter all-commit format automatically, update `scripts/generate-release-notes.sh` in a separate code change.

## Update 2026-04-21 03:58:00Z

- Current task: Completed the requested release, push, manual `local test host` redeploy, monitoring pass, and fixable-noise cleanup.
- Last activity:
  - created and pushed `build-main-0.24.5-slskdn.167`; release run `24702224025` produced the main release artifacts and updated stable metadata
  - fixed scheduled E2E/test target-framework drift by discovering the app target framework from `src/slskd/slskd.csproj`; gotcha documented as `a50e34bf6`, fix pushed as `2e4cc934c`
  - deployed `0.24.5-slskdn.167+manual.2e4cc934c` to `local test host`, then used the route/tab sweep to isolate the remaining fixable browser-visible noise to optional user-info badge 404s for offline historical downloads
  - added quiet optional user-info lookups so badge misses return `204 No Content` without changing default API semantics; gotcha documented as `2f52e3bed`, fix pushed as `9c1d3f14d`
  - deployed `0.24.5-slskdn.167+manual.9c1d3f14d` to `/usr/lib/slskd/releases/manual-9c1d3f14d`; service is `active/running`, PID `1887195`, `NRestarts=0`, Soulseek `Connected, LoggedIn`, shares ready
  - final Playwright route/tab sweep report `/tmp/local test host-route-tab-sweep-2026-04-21T03-44-45-634Z.md`: `307` visits, `1683` same-origin responses, `{"200":1680,"204":3}`, `0` issues, `0` 4xx/5xx/502 responses
  - fresh logs/coredumps after the final sweep show no new `slskd` coredumps and no actionable current-process fatal/error/exception/502/bind/protocol noise; remaining warnings are `/system/options` scanner false positives and expected DHT churn summaries
- Next steps:
  1. Continue passive monitoring if requested.
  2. Check any still-running downstream package jobs from release run `24702224025` if release packaging status is needed.
  3. Do not create another build tag unless explicitly requested.

## Update 2026-04-21 03:12:00Z

- Current task: Completed `local test host` manual-build testing/fixing pass.
- Last activity:
  - pushed final commits through `15ba2a423` to `snapetech/slskdn`
  - deployed `0.24.5-slskdn.165+manual.15ba2a423` to `local test host`
  - verified service health: `active/running`, `NRestarts=0`, no new `slskd` coredumps, correct version from `/api/v0/application`
  - verified QUIC remains opt-in by default: `slskd` listens on UDP `50306/50400` and TCP `5030/5031/50300/50305`, with no `slskd` listener on `50401/50402`
  - ran the full bounded Playwright route/tab sweep: `/tmp/local test host-route-tab-sweep-2026-04-21T03-02-32-124Z.md`, `307` visits, `1680` responses, `0` issues, `0` HTTP 5xx/502s, three expected offline user-info `404`s
  - fresh logs show only known operational warnings and normal DHT/overlay churn; no `[DI]`, SPA fallback, CSRF, QUIC, user-info stack, fatal, coredump, or fresh search-cancellation error noise from the final build
- Next steps:
  1. Continue passive monitoring if requested.
  2. Do not tag or trigger a release build unless explicitly asked.

## Update 2026-04-21 02:20:00Z

- Current task: None. The `local test host` user-info 500 fix is committed, pushed, deployed, and retested; the route/tab Playwright pass is clean for current 5xx/502/page-error signals.
- Last activity:
  - controlled Playwright crawling against `local test host` found no real HTTP 502 responses, but did find `/api/v0/users/{username}/info` returning HTTP 500 for expected Soulseek peer connection failures and timeouts
  - documented the peer-info gotcha in ADR-0001 and committed the docs-only entry as `1699cf7b5`
  - updated `UsersController.Info` so explicit offline users remain 404, while peer connection failures and info timeouts return generic 503 responses without exception-object stack noise
  - added focused controller coverage for connection failure, direct timeout, and wrapped timeout cases; `UsersControllerTests`, Release build, lint, diff check, and GitHub target verification passed
  - committed and pushed the fix as `5bd0e0b88`
  - published and deployed `0.24.5-slskdn.165+manual.5bd0e0b88` to `local test host`; corrected live launcher drift so `/usr/lib/slskd/slskd` execs `/usr/lib/slskd/current/slskd`, documented as ADR-0001 gotcha `deafb040b`
  - verified `/api/v0/application` reports the new payload, user-info peer failures now return controlled `503`, and the current process has no 500/502/fatal/protocol/bind/coredump noise
  - ran Playwright route/tab sweep report `/tmp/local test host-route-tab-sweep-2026-04-21T02-09-22-685Z.md`: all top-level routes and System tabs were exercised, with only one expected user-info 404 and no 5xx responses
- Next steps:
  1. Keep `local test host` soaking on PID `1642135` and resample later for long-run mesh/download noise.
  2. Treat the remaining broad dynamic `/searches/{id}` link corpus separately if exhaustive historical search-detail crawling is still desired; the bounded product route/tab pass did not show current 5xx or route-miss failures.
  3. Do not create a release/build tag unless explicitly requested.

## Update 2026-04-21 00:11:00Z

- Current task: Finish, push, deploy, and monitor the auto-replace search-budget fix found during `local test host` manual-build soak.
- Last activity:
  - live monitoring found auto-replace issuing a large stuck-download batch until `Search rate limit exceeded` triggered, then logging repeated per-track stack traces and marking the cycle as failed
  - documented the gotcha in ADR-0001 and committed that docs-only entry as `138f3a6c0`
  - updated `AutoReplaceService` so alternative searches are paced by `Soulseek.Safety.MaxSearchesPerMinute`, safety-budget rejection defers the current item, and the cycle stops early instead of logging one error per remaining track
  - added unit coverage for the budget-exhaustion path and confirmed the focused auto-replace/program test slice, Release project build, lint, full `dotnet test`, and `git diff --check` pass
  - found generated `src/slskd/dist` output being included in the next publish artifact, documented it in ADR-0001 as `fe0ab5ea9`, and excluded `dist/**` from the app project's default items
  - confirmed the live paced cycle no longer hits the search limiter, then documented and fixed routine per-track no-result log noise by moving it to `Debug`
  - found restart/re-enqueue stack traces for expected remote-offline download failures, documented the gotcha as `d3bfa41cb`, and changed those failures to warning summaries without masking transfer failure state
  - found deploy-time auto-replace shutdown cancellation being logged as search errors from the previous PID, documented the gotcha as `5a10e6cdc`, and changed caller-token cancellation to stop the hosted service cleanly
  - found the next live cycle still produced routine shared search progress at `Information`, documented the gotcha as `f4191def3`, and moved per-search completion, mesh-search fallback/fanout, and passive HashDb discovery progress to `Debug`
  - confirmed the auto-replace shutdown path was fixed, then found the remaining handled Soulseek disconnect race still emitted a stack because the catch logged the exception object; documented the gotcha as `6dd4690e7` and changed it to a debug summary
  - found a current-process fatal unobserved task from a Soulseek.NET TCP double-disconnect read-loop race, documented the gotcha as `70b26eff5`, and added it to the expected network exception classifier
  - started the requested Playwright UI sweep and found `/system/network` correctly gates public DHT exposure behind a consent modal, but its inline code copy rendered `dht.lan_only=truein`; documented the gotcha as `bff3fa0fd` and fixed the spacing
- Next steps:
  1. Validate, commit, push, and redeploy the DHT exposure modal copy fix to `local test host`.
  2. Continue the controlled Playwright UI pass against the updated manual build and document real endpoint/UI issues separately from route-change request aborts.

## Update 2026-04-20 23:55:00Z

- Current task: None. The current `local test host` manual build is stable in the latest sample, and the local formatter/compile cleanup from the validation pass is complete.
- Last activity:
  - resampled the current `local test host` service process (`PID 1335511`, active since `2026-04-20 17:37:10 CST`) and confirmed it remains active with `NRestarts=0` and `ExecMainStatus=0`
  - found no current-process journal matches for fatal unobserved exceptions, the Soulseek timer-reset classifier, native crash strings, disposed-object shutdown noise, listener bind failures, protocol violations, or invalid-frame logs
  - confirmed no new `slskd` coredumps since the current process start; the noisy timer-reset/fake-fatal entries were from older PIDs
  - fixed the local lint/formatter verification loop and cleaned compile/nullability issues in OpenAPI response mutation, QUIC task cleanup, relay pin validation, and cookie header stripping
  - documented the OpenAPI `IOpenApiResponse.Content` interface gotcha in ADR-0001 and committed that docs-only entry as `04d071597`
  - revalidated locally with Release build, focused unit slice (`31` tests), `bash ./bin/lint`, and `git diff --check`
- Next steps:
  1. Keep soaking `local test host` and only redeploy if the current process shows fresh crash/log evidence or the remaining local changes need to be promoted.
  2. Treat older pre-current-PID fatal/timer-reset logs as historical unless they recur on the current PID.
  3. Do not create a build tag unless explicitly requested.

## Update 2026-04-20 15:55:00Z

- Current task: None. The live `local test host` redeploy validated the timer-reset log-noise fix and exposed native crash recurrence as the next real host issue.
- Last activity:
  - confirmed the historical `#201` signatures are still not the current live failure mode: current `main` contains the startup-listener fix, `/system/info` renders locally, and `local test host` remains reachable/listening after deploy
  - committed and deployed `ffacda09e`, publishing `0.24.5-slskdn.159+manual.ffacda09e` to `local test host`
  - verified post-deploy health: service active, Soulseek connected/logged in, DHT ready, overlay and QUIC listeners bound, and one active mesh connection after restart recovery
  - sampled the live journal and confirmed the targeted fix held: no new `[FATAL] Unobserved task exception` entries and no `Soulseek.Extensions.Reset(Timer)` teardown noise in the observed window
  - found a different live blocker during soak: the process segfaulted once (`SIGSEGV`) and systemd restarted it automatically; `coredumpctl` shows similar recent native crashes on earlier manual builds too
  - observed the new DHT/overlay summary logging working as intended on the recovered process, with explicit failure mix and degraded endpoint rollups
- Next steps:
  1. Investigate the native `SIGSEGV` path on `local test host`, starting from the recent `coredumpctl` history and any commonality with active `libmsquic` worker threads.
  2. Decide whether to add crash-oriented symbolization or host-side runtime diagnostics before another manual/live rollout.
  3. Separately decide whether `AutoReplace` search-cap noise should be tuned, but treat it as secondary to the native crash path.

## Update 2026-04-19 20:30:00Z

- Current task: None. DHT/overlay observability and bad-candidate cooldown follow-up is implemented locally and validated.
- Last activity:
  - added bounded endpoint cooldowns and top-problem endpoint stats to `MeshOverlayConnector` so repeatedly bad DHT-discovered overlay addresses stop getting hammered
  - added periodic DHT/overlay summary logging and explicit candidate rollup counters in `DhtRendezvousService`
  - added inbound/outbound mesh session-end summary logs with connection age and disconnect reason
  - exposed the new diagnostics through the existing DHT/overlay status API and covered them with focused unit tests
  - ran the local cycle: focused DHT/overlay unit tests passed, `dotnet build src/slskd/slskd.csproj --no-restore -v minimal` passed, `bash ./bin/lint` passed, and `git diff --check` passed
  - ran `./bin/build`; the build path itself succeeded through web/release compilation but the full Release unit-test phase hit a single failure in `slskd.Tests.Unit.Transfers.Downloads.DownloadServiceTests.ShutdownAsync_WaitsForCancelledDownloadsToDrain`, which then passed when rerun in isolation
- Next steps:
  1. Decide whether to treat the Release full-suite `DownloadServiceTests.ShutdownAsync_WaitsForCancelledDownloadsToDrain` failure as an existing flaky race or to debug it before the next release build.
  2. Deploy the new DHT/overlay diagnostics to `local test host` and sample whether the summary lines clearly distinguish bad remote endpoints (`no-route`, `tls-eof`, etc.) from local capacity/backoff behavior.
  3. If remote candidate churn remains dominant, add endpoint deprioritization on top of the current cooldowns rather than increasing automatic probe volume.

## Update 2026-04-20 02:30:00Z

- Current task: None. The failed `build-main-0.24.5-slskdn.160` CI release-smoke regression is fixed locally and validated.
- Last activity:
  - traced the failed tag build to `Release Gate` compile-time integration smoke, not runtime failures
  - found that `tests/slskd.Tests.Integration/StubWebApplicationFactory.cs` still implemented the old `IDownloadService` surface after `ShutdownAsync(CancellationToken)` was added for shutdown drain sequencing
  - added the missing `StubDownloadService.ShutdownAsync` no-op implementation
  - documented the interface/test-double drift gotcha in ADR-0001 and committed that doc entry as `58c184c7f`
  - reran the exact release-smoke validation path locally: Release unit smoke passed, Release integration smoke passed, `packaging/scripts/run-release-integration-smoke.sh` passed, `bash ./bin/lint` passed, and `git diff --check` passed
- Next steps:
  1. Commit the remaining code/doc changes for the DHT/overlay diagnostics pass plus the CI smoke fix.
  2. Push `main` when desired.
  3. Create a replacement build tag only if the user explicitly wants a new release build.

## Update 2026-04-20 02:55:00Z

- Current task: None. The local release gate is green again; one non-gate full-integration interference remains in the heavier `./bin/build` pass.
- Last activity:
  - continued the local release-candidate cycle with `run-release-gate.sh` and `./bin/build`
  - fixed two release-suite test fragilities: `PortForwardingControllerTests` no longer hardcode fixed local ports, and `DownloadServiceTests.ShutdownAsync_WaitsForCancelledDownloadsToDrain` now waits for explicit tracked-work completion instead of relying on a fixed delay
  - documented the test-flakiness pattern in ADR-0001 and committed it as `c1d21e8b4`
  - reran the release bar successfully: focused Release unit regressions passed, `bash packaging/scripts/run-release-gate.sh` passed end to end, `bash ./bin/lint` passed, and `git diff --check` passed
  - identified one remaining heavier-suite issue outside the release gate: `./bin/build` still hit a single full-instance integration failure (`TwoNodeMeshFullInstanceTests.TwoFullInstances_CanFormOverlayMeshConnection` returning `502 Bad Gateway` on the initial overlay-connect call), but that exact test passed immediately when rerun in isolation
- Next steps:
  1. Commit the remaining test-hardening changes.
  2. Decide whether the isolated `TwoNodeMeshFullInstanceTests` full-suite interference should block the next release candidate, given that the documented local release bar is green.
  3. If you want a stricter candidate, debug the full `./bin/build` integration-suite interference next before tagging.

## Update 2026-04-20 03:20:00Z

- Current task: None. The local next-release-candidate cycle is clean again, including the heavier full `./bin/build` path.
- Last activity:
  - traced the remaining full-suite-only `TwoNodeMeshFullInstanceTests.TwoFullInstances_CanFormOverlayMeshConnection` `502 Bad Gateway` failure to `SlskdnFullInstanceRunner` marking instances ready after API health alone
  - hardened the full-instance harness to wait for the configured overlay TCP listener before tests issue `/api/v0/overlay/connect`, and reused the same helper shape already used for bridge readiness
  - documented the startup-readiness gotcha in ADR-0001 and committed that doc-only entry as `e26b30713`
  - reran the focused full-instance mesh test successfully, then reran the heavy local path successfully: `./bin/build` passed end to end; `bash ./bin/lint` and `git diff --check` also passed
- Next steps:
  1. Commit the remaining harness/doc updates and push `main` if you want this clean local RC state reflected on origin.
  2. Cut the next official release-candidate/build tag only if explicitly requested.
  3. After the next tag, watch CI/package jobs rather than local release-gate coverage; the local manual bar is currently green.

## Update 2026-04-20 03:45:00Z

- Current task: None. The UI/admin audit findings are fixed locally and revalidated.
- Last activity:
  - fixed `/system/mediacore` by restoring the missing `Checkbox` import
  - fixed `/pods` by putting `PodsController` on the explicit `api/v{version:apiVersion}/pods` route with `[ApiVersion("0")]` and added a unit contract test for that route/version pair
  - redirected `/` straight to `/searches`, removed the unconditional per-render `session.check()` call, and quieted the expected unauthenticated/session-expiry path in `session.js`
  - exempted authenticated requests and non-API web shell/static requests from the coarse fixed-window IP limiter so fast admin sweeps no longer self-trigger `429`
  - changed first-run share bootstrap to log a recreate/scan path instead of throwing a corruption-looking exception before recovery
  - revalidated with focused tests, `bash ./bin/lint`, `git diff --check`, and a disposable manual browser/api sweep over `/`, `/searches`, `/pods`, `/system/info`, and `/system/mediacore`; all passed with no page errors and no reproduced `429`
- Next steps:
  1. Commit and push the remaining code/test changes if you want the admin-audit fixes on `origin/main`.
  2. If you want another broad product sweep, rerun the full top-level/admin-panel Playwright crawl against this build and then triage any deeper workflow bugs that remain beyond the original hard failures.

## Update 2026-04-20 04:05:00Z

- Current task: None. The failed `build-main-0.24.5-slskdn.161` tag regression is fixed locally and the release gate is green again.
- Last activity:
  - pulled the raw `Build on Tag #228` job logs and confirmed the failure was the Release-only `DownloadServiceTests.ShutdownAsync_WaitsForCancelledDownloadsToDrain` timing out again in CI, not a packaging or product regression
  - documented the recurring startup/cancellation race in ADR-0001 and committed that docs-only entry as `22df366c6`
  - hardened the shutdown-drain test so it waits for the mocked download worker to actually start before invoking shutdown, verifies shutdown stays blocked until drain completion is permitted, then awaits shutdown completion directly
  - revalidated with the exact local release gate: the targeted Release test passed, a `5`-run Release loop of that exact test passed, `bash packaging/scripts/run-release-gate.sh` passed, and `bash ./bin/lint` passed
- Next steps:
  1. Commit and push the remaining test/doc updates.
  2. Move the failed `build-main-0.24.5-slskdn.161` tag to the fixed commit or cut `build-main-0.24.5-slskdn.162` if you want the cleanest retry path.
  3. Watch the next tag build specifically for the `Release Gate` job; that was the only failing segment on `#228`.

## Update 2026-04-20 01:52:00Z

- Current task: None. The latest `local test host` live-debug pass is implemented, committed, deployed, and host-validated.
- Last activity:
  - kept QUIC enabled and healthy on `local test host`, with overlay/data listeners active and mesh reconnecting after each deliberate restart
  - fixed `DownloadService` shutdown cleanup ordering by draining in-flight enqueue/download work before the shared Soulseek client is disconnected or disposed
  - fixed the remaining false-fatal shutdown edge by tolerating the third-party `SoulseekClient.Disconnect()` `Sequence contains no elements` race during expected host shutdown
  - deployed `0.24.5-slskdn.159+manual.1475cd068` to `local test host` and validated deliberate restarts with active downloads: no global download semaphore warnings, no transfer cleanup `ObjectDisposedException`, no false fatal shutdown log, restart count `0`, DHT healthy, and one mesh peer connected immediately after restart
- Next steps:
  1. Keep sampling `local test host` for longer-run QUIC and mesh stability, especially whether the single live peer stays connected past the keepalive windows on this final manual build.
  2. If more mesh issues surface, focus on remote candidate quality and connector failure mix (`timeout`, `no-route`, `tls-eof`) rather than the now-fixed local shutdown and framing paths.
  3. Push `main` when desired, and only create a build tag if the user explicitly wants a release build.

## Update 2026-04-17 23:05:00Z

- Current task: Package/release pipeline regressions from `build-main-0.24.5-slskdn.135` are fixed locally and validated; ready to commit, push, and retag.
- Last activity:
  - fixed the Docker release failure by moving `Dockerfile` onto real `.NET 10 noble` SDK/runtime-deps images and validating a full local Docker build
  - realigned stable package metadata, COPR/RPM inputs, and the metadata refresh script with the published `linux-glibc-*` release assets on `0.24.5-slskdn.135`
  - revalidated packaging metadata, a full Nix flake smoke build, and the Docker image path locally
- Next steps:
  1. Commit the packaging/workflow/docs fix set and push `main`.
  2. Cut a fresh stable tag to replace the failed `build-main-0.24.5-slskdn.135` run.
  3. Monitor the replacement tag for COPR, Docker, and metadata-update success before closing out the release.

## Update 2026-04-18 01:18:23Z

- Current task: None. The next round of issue `#209` follow-up fixes are implemented locally and validated.
- Last activity:
  - inspected the newer `#209` tester logs after DHT bootstrap started succeeding and separated remaining failures from misleading noise
  - fixed the real `GET /api/v0/users/notes` regression by advertising both API versions `0` and `1` on `UserNotesController`
  - removed the mesh overlay connector's bogus UDP hole-punch preflight against DHT-discovered TCP overlay endpoints, which was producing guaranteed `FAILED` logs with ephemeral local UDP ports that looked like randomized listeners
  - clarified hole-punch completion logs so operators can see the reported local port is an ephemeral UDP socket, not a configured listener port
  - added focused versioned-route integration coverage for `/api/v0/users/notes`
- Next steps:
  1. Redeploy the latest `Program` classifier fix to `local test host` and verify that remote-declared transfer failures no longer emit fake `[FATAL] Unobserved task exception` noise on the new process.
  2. Keep sampling multi-peer downloads on `local test host` to see whether any remaining post-`InProgress` failures cluster around one transport/peer pattern or are just normal remote-side churn.

# Active Context

> What is currently being worked on in this repository.  
> Update this file when starting or finishing work.

---

## ⚠️ WORK DIRECTORY (do not use <workspace>/cursor)

**Project root: `<repo-root>`**

All `git`, `dotnet`, and file paths in this repo are under this directory. Do not use the `cursor` folder under `~/Code/` for slskdn work — it is a separate project.

---

## 🚨 Before Ending Your Session

**Did you fix any bugs? Document them in `adr-0001-known-gotchas.md` NOW.**

This is the #1 most important thing to do before ending a session. Future AI agents (and humans) will thank you.

---

## Current Session

- **Current Task**: Investigating the recurring native `SIGSEGV` on `local test host`; the latest pass narrowed it to the old manual single-file/ReadyToRun publish shape and aligned `bin/publish` with the tagged release profile now running live on the host.
- **Branch**: `main`
- **Environment**: Local dev on `snapetech/slskdn`; live validation on `local test host` currently running `0.24.5-slskdn.159+manual.ffacda09e`; no release tags were created.
- **Last Activity**:
  - Kept QUIC enabled by installing Microsoft MsQuic `v2.5.7` on `local test host`; QUIC overlay/data listeners bind on `50402/50401` with no temporary systemd disable override.
  - Fixed live mesh compatibility with unframed JSON overlay frames; `local test host` connected to `m***7` and held past the 2-minute keepalive threshold without `Protocol violation`, `Invalid message length`, `Unregistered`, or disconnect logs.
  - Fixed DHT rendezvous accounting so connector-capacity deferrals are not counted as real attempts or pushed into retry backoff.
  - Fixed user directory browse API handling so expected remote peer connection failures return controlled 503 responses instead of unhandled middleware stack traces.
  - Fixed service SIGTERM handling so normal `systemctl restart` stops the host cleanly; validated on `local test host` that a deliberate restart logs expected shutdown, not status 1/failure.
  - Fixed transfer shutdown cleanup ordering so active downloads drain before Soulseek client disposal, removing restart-time semaphore/disposed-object noise.
  - Fixed the remaining third-party `SoulseekClient.Disconnect()` shutdown race so clean restarts do not emit false fatal `Sequence contains no elements` logs.
  - Deployed `0.24.5-slskdn.159+manual.ffacda09e` to `local test host` and confirmed the targeted fake-fatal Soulseek timer-reset teardown noise no longer appeared in the observed journal window.
  - During soak, observed one native `SIGSEGV` on the deployed process; systemd restarted the service cleanly and the recovered process rejoined the mesh.
  - `coredumpctl` on `local test host` shows similar recent native crashes on earlier manual builds, so the crash path predates `ffacda09e` and is now the highest-priority live issue.
  - Investigated the crash producer and found concrete QUIC connection-lifecycle gaps: cached/orphaned `QuicConnection` instances were not always disposed, and QUIC hosted servers were not explicitly closing active connections or draining active connection tasks on stop.
  - Documented that QUIC lifecycle gotcha in ADR-0001 (`06ffdca5f`), then hardened `QuicOverlayClient`, `QuicDataClient`, `QuicOverlayServer`, and `QuicDataServer` with explicit connection gates, disposal, close, and stop-drain behavior.
  - During the first manual redeploy of that hardening, found a separate restart-time bug: `MeshOverlayServer` could fail to rebind port `50305` with `Address already in use` even though no live listener remained.
  - Documented that listener-reuse gotcha in ADR-0001 (`7a6eca0dd`), then fixed `MeshOverlayServer` to use `ReuseAddress` and to clear/dispose stop state fully on shutdown.
  - Published and manually deployed `0.24.5-slskdn.159+manual.quicfix2` to `local test host`; a deliberate restart on the recovered process now rebinds overlay TCP `50305` cleanly and restores overlay/DHT/QUIC listeners as expected.
  - `coredumpctl` still captured a new startup-time native `SIGSEGV` on PID `572060` during that rollout, but the immediate systemd retry recovered to a healthy process (`572286`) and a later deliberate restart stayed clean.
  - Pulled deeper kernel/coredump evidence and found the startup crashes were native `general protection fault` events in `.NET Server GC`, while `bin/publish` was still producing a self-contained single-file `ReadyToRun` artifact that does not match the tagged release workflows.
  - Documented that manual-publish drift gotcha in ADR-0001 (`975c754d2`), then aligned `bin/publish` with the tagged release profile by removing the single-file/native-self-extract path and explicitly disabling `PublishReadyToRun`.
  - Built and manually deployed `0.24.5-slskdn.159+manual.nor2r` to `local test host`; three consecutive startup paths (initial deploy plus two deliberate restarts) came up clean with no new `coredumpctl` entries and no new `/var/lib/slskd/.net/slskd/*` extraction directory.
  - Rechecked the earlier live journal and found one remaining app-side noise path: `Soulseek.Extensions.Reset(Timer)` could still surface as a fake fatal unobserved-task exception when it happened from `Soulseek.Network.Tcp.Connection.WriteInternalAsync(...)`, not just the already-classified read loop.
  - Documented that write-path gotcha in ADR-0001 (`b121d5da3`), extended `Program.IsExpectedSoulseekNetworkException(...)` to cover the write-loop stack shape too, and added a focused `ProgramPathNormalizationTests` regression for the exact live stack.
  - The host is still logging repeated `EXT4-fs ... checksum invalid` errors on `dm-0` from `containerd`, so host filesystem health remains a separate operational concern even though the CI-aligned publish shape is currently stable.
  - Validation passed: `dotnet build src/slskd/slskd.csproj --no-restore -v minimal`, focused QUIC/transport/program unit slice (`30` passed), `bash ./bin/lint`, `git diff --check`, manual publish/install to `local test host`, live journal sampling, listener/socket checks, and coredump inspection.
- **Next Steps**:
  1. Commit and deploy the `Program` write-loop classifier fix on top of `manual-nor2r`, then verify the host still starts cleanly and that no fresh fake fatal timer-reset logs appear.
  2. Keep the host on the CI-shaped publish profile while sampling for longer-run stability.
  3. If native crashes reappear on the CI-aligned publish shape, continue symbolization/runtime triage from the new narrower baseline instead of the old divergent manual artifact.
  4. Separately flag the repeated `EXT4-fs` checksum errors on `local test host` as an operational host-health risk outside slskd itself.

## Recent Context

### Last Session Summary
- Completed Phase 1 (MusicBrainz/Chromaprint integration) T-300 through T-313
- Implemented Phase 2A tasks T-400 through T-403 (AudioVariant, canonical scoring, library health scaffolding)
- **Extended planning with critical additions**:
  - **Phase 2-Extended**: Codec-specific fingerprinting & quality heuristics (T-420 to T-430)
    - FLAC 42-byte streaminfo hash + PCM MD5
    - MP3 tag-stripped stream hash + encoder detection
    - Opus/AAC stream hashes + spectral analysis
    - Cross-codec deduplication via audio_sketch_hash
    - Heuristic versioning & recomputation system
  - **Phase 7**: Testing strategy with Soulfind & mesh simulator (T-900 to T-915)
    - L0/L1/L2/L3 test layers
    - Soulfind test harness (dev-only, never production)
    - Multi-client integration tests (Alice/Bob/Carol topology)
    - Mesh simulator for disaster mode testing
    - CI/CD integration with test categorization
- **Previously completed comprehensive planning for ALL phases (2-6)**:
  - Phase 2: 6 documents (~25,000 lines) - Canonical scoring, Library health, Swarm scheduling, Rescue mode, Codec-specific fingerprinting
  - Phase 3: 1 document (4,100 lines) - Discovery, Reputation, Fairness
  - Phase 4: 1 document (3,200 lines) - Manifests, Session traces, Advanced features
  - Phase 5: 1 document (2,800 lines) - Soulbeet integration
  - Phase 6: 4 documents (11,200 lines) - Virtual Soulfind mesh, disaster mode, compatibility bridge
  - Phase 7: 1 document (6,500 lines) - Testing strategy
- **Total: 21 planning documents, ~57,000 lines of production-ready specifications**
- **Total tasks: 127 (T-300 to T-915, plus misc)**

### Blocking Issues
- No active blocker from the old `.NET 10` test-tail issue; the previously hanging full-solution test run now exits after the integration harness/test fixes.

### Current focus (the rest)
- **40-fixes plan (PR-00–PR-14):** Done. slskd.Tests 46, slskd.Tests.Unit 2257 pass; Epic implemented. Deferred table: status only.
- **T-404+:** Done. t410-backfill-wire (RescueMode underperformance detector → RescueService); codec/fingerprint (T-420–T-430) done per dashboard.
- **New product work**: As prioritized.

**Research (9) implementation:** ✅ Complete. T-901–T-913 all done per `memory-bank/tasks.md`.

### Next Steps
1. If `#209` still reports failures after this, reproduce the next symptom from live logs before changing anything else.
2. Validate whether circuit usage beyond the current placeholder builder needs to move onto the overlay connection path instead of raw transport streams.
3. Push and tag only after this direct-mode fix is committed and the user wants another release.

4. **Recent completions** (2026-01-27):
   - ✅ Backfill for shared collections (API + UI, supports HTTP and Soulseek)
   - ✅ Persistent tabbed interface for Chat (Rooms already had tabs)
   - ✅ E2E test completion (policy, streaming, library, search)
   - ✅ Code cleanup: TODO comments updated to reference triage document
   - ✅ Soulfind integration: CI and local build workflows integrated
   - ✅ Soulbeet compatibility tests: Fixed 2 failing tests (JSON property names, Directories config)
   - ✅ Phase 2 Multi-Swarm: Implemented Phase 2B deep library health scanning (T-403), verified Phase 2A/2C/2D complete
   - ✅ Phase 3 Multi-Swarm: Verified all 11 tasks (T-500 to T-510) complete
   - ✅ Phase 4A-4C Multi-Swarm: Verified 9 of 12 tasks (T-600 to T-608) complete
   - ✅ Phase 4D Multi-Swarm: **COMPLETED** (T-609 to T-611) - Full playback-aware chunk priority integration
   - ✅ Phase 5 Multi-Swarm: Verified all 13 tasks (T-700 to T-712) complete

**Multi-Swarm Status**: 62 of 62 tasks complete (100%). All Phases 1-5 fully implemented and verified.

5. ~~**High Priority Tasks Available** (obsolete):~~
   - **Packaging**: T-010 to T-013 (NAS/docker packaging - 4 tasks)
   - **Medium Priority**: T-003 (Download Queue Position Polling), T-004 (Visual Group Indicators)
   - ~~T-404+~~ (done)

5. ~~**Implementation Timeline**~~ (archived; Phase 14 and T-404+ done.)

6. ~~**Branch Strategy**: Phase 14 `experimental/pod-vpn`~~ (Phase 14 done.)

---

## Environment Notes

- **Backend Port**: 5030 (default)
- **Frontend Dev Port**: 3000 (CRA default)
- **.NET Version**: 10.0
- **Node Version**: Check `package.json` engines

---

## Quick Commands

```bash
# Start backend (watch mode)
./bin/watch

# Start frontend dev server
cd src/web && npm start

# Run all tests
dotnet test

# Build release
./bin/build
```

## Update 2026-04-17 20:25:00Z

- Current task: None. The `#209` root-cause follow-up is implemented locally and validated.
- Last activity:
  - proved the failure was in the pinned upstream DHT library path, not just local logging/config wording
  - upgraded `MonoTorrent` from `3.0.2` to `3.0.3-alpha.unstable.rev0049`
  - added explicit `dht.bootstrap_routers` handling and validation so slskdn no longer depends on one hidden upstream bootstrap router
- Next steps:
  1. Run `bash ./bin/lint` and `git diff --check`, then commit the code/config/test follow-up.
  2. Push `main` and trigger a fresh stable build once the user wants the fix released.
  3. Update issue `#209` only after the fixed build is available for retest.

## Update 2026-04-17 21:59:58Z

- Current task: Stable package metadata drift fixed locally; ready to push and clean up failed tag `build-main-0.24.5-slskdn.133`.
- Last activity:
  - traced the `Nix Package Smoke` failure to stable metadata pointing at unreleased `slskdn-main-linux-glibc-*` assets while the latest published stable release (`0.24.5-slskdn.131`) still publishes `slskdn-main-linux-x64.zip` / `slskdn-main-linux-arm64.zip`
  - reverted the stable metadata consumers and the metadata updater script to the currently published stable asset names
  - validated packaging metadata successfully; local Nix smoke remains unexercised here because `nix` is not installed on this machine
- Next steps:
  1. Commit and push the stable metadata fix.
  2. Delete the stale `build-main-0.24.5-slskdn.133` tag locally and on origin.
  3. Re-run a fresh stable tag build only after the metadata fix is on `main`.


## Update 2026-04-18 03:45:00Z

- Current task: None. Issue `#209` circuit peer sync follow-up is implemented locally and validated.
- Last activity:
  - traced the latest `#209` report past DHT bootstrap into a split-brain state between `MeshNeighborRegistry` and `IMeshPeerManager`
  - reproduced the old failure in unit tests: live overlay neighbors alone left circuit peer stats at zero
  - added `MeshNeighborPeerSyncService` so DHT overlay neighbor add/remove events populate and prune the circuit peer inventory used by `CircuitMaintenanceService` and `MeshCircuitBuilder`
  - added focused unit tests proving the old empty-peer state without the sync service and the corrected populated-peer state with it
- Next steps:
  1. Commit the code/test/docs fix set if lint and diff checks stay green.
  2. If `#209` still reports zero circuits after this, inspect actual outbound overlay connection success/failure rates and remote peer feature compatibility rather than local peer inventory wiring.


## Update 2026-04-18 09:05:00Z

- Current task: None. The latest `local test host` live transfer and DHT follow-up is implemented locally and validated on-host.
- Last activity:
  - proved that successful transfers on `local test host` were still emitting fake fatal `Transfer failed: Transfer complete` unobserved-task noise and fixed the classifier in `Program`
  - opened `50305/tcp` and `50306/udp` in the `local test host` host firewall, then verified DHT eventually reaches `Ready` on the live host once both router and host firewall paths are open
  - proved the old 30-second DHT bootstrap warning was too aggressive on a healthy host and extended the bootstrap grace period to 120 seconds
- Next steps:
  1. Commit and push the current `Program` / DHT bootstrap follow-up if the worktree stays clean.
  2. Watch the next `local test host` runtime window for a successful transfer on the patched process and confirm the `Transfer complete` fatal log never returns.
  3. If DHT still shows slow `Initialising` windows on other hosts, consider making the bootstrap diagnostics adaptive instead of static.


## Update 2026-04-18 09:45:00Z

- Current task: None. The Docker and raw Linux release-installer smoke pass is complete locally.
- Last activity:
  - built the current Docker image locally and verified the shipped container reports `0.24.5-slskdn.141` when running the embedded `slskd` binary
  - smoke-tested `packaging/linux/install-from-release.sh` against the latest published stable release on a clean `ubuntu:24.04` container and found a real cleanup bug in the installer's `EXIT` trap
  - fixed the installer trap so cleanup no longer references an out-of-scope function-local `work_dir`, then reran the published-release smoke successfully and verified `/usr/bin/dotnet /opt/slskdn/slskd.dll --version` plus the generated `slskd.service`
  - confirmed this machine still lacks `flatpak-builder`, `snapcraft`, and `brew`, so those package-manager paths remain un-smoked here
- Next steps:
  1. Push the installer trap fix if you want it included in the next release.
  2. Use a machine with `flatpak-builder`, `snapcraft`, or `brew` available to add real install-smokes for those remaining ship methods.


## Update 2026-04-18 14:55:00Z

- Current task: None. The Jammy PPA / standalone packaging drift fix is implemented locally and validated.
- Last activity:
  - pulled the real Launchpad Jammy build log for `0.24.5.slskdn.144` and confirmed the build was failing in `debian/rules` because the standalone PPA path had drifted behind the main release flow: stale `.NET 8` workflow pin plus a hard-coded `libcoreclrtraceptprovider.so` patch path
  - updated `release-ppa.yml`, `release-copr.yml`, and `release-linux.yml` to use `.NET 10` and added publish-output verification steps so those workflows validate the staged Linux app bundle before packaging it
  - hardened the DEB/RPM `liblttng-ust` SONAME patching to discover `libcoreclrtraceptprovider.so` dynamically inside the staged package tree instead of assuming one flat path
  - validated the Debian staging logic locally with a real self-contained publish and confirmed the staged trace-provider library now depends on `liblttng-ust.so.1`
- Next steps:
  1. Push the packaging/workflow fix and cut a new stable build so Launchpad retries with the corrected PPA path.
  2. Watch the next Jammy build specifically; if it still fails, the next problem will be in the PPA source-package assembly or Launchpad environment rather than this runtime-path drift.

## Update 2026-04-21 08:00:00Z

- Current task: None. The current `local test host` 172 log/UI sweep is triaged and the remaining fixable warning noise is patched locally.
- Last activity:
  - confirmed `local test host` is still running `0.24.5-slskdn.172`, systemd is active with zero restarts, Soulseek is logged in, DHT is running, overlay TCP is listening on `50305`, and no fresh fatal/error/exception/502/coredump/protocol noise appeared
  - reran focused Playwright checks with the corrected Web UI credentials; Library Health and Files nested tabs work, and a steady `/searches` hold produced no SongID hub console errors
  - documented and fixed the repeated entropy false-warning gotcha by increasing the RNG sample size to reduce finite-sample bias, and moved expected auto-replace no-result searches down to debug
  - fixed the flaky hosted-service unit test wait exposed by the full unit suite
  - validation passed so far: focused security event tests, focused circuit maintenance test, full unit suite, Release build, lint, changelog validation, and diff check
- Next steps:
  1. Commit and push the log-polish/test fix set.
  2. Do not create another build tag unless explicitly requested.

## Update 2026-04-22 18:20:00Z

- Current task: None. The latest issue `#209` live follow-up is implemented, committed locally, and deployed to `local test host` for validation.
- Last activity:
  - confirmed the current `local test host` build is `0.24.5-slskdn.174+manual.6fce6575c` from `/usr/lib/slskd/releases/manual-6fce6575c`, with systemd active and `NRestarts=0`
  - fixed mesh self-descriptor endpoint publication so automatic detection only advertises public-routable interfaces and configured self endpoints are not silently supplemented with private/container/VPN addresses
  - added info-level mesh-search fanout diagnostics when active overlay peers are queried
  - live validation showed DHT discovery and overlay connectivity are not stuck at zero: the host reconnected to one outbound `mesh_search` peer (`minimus7`), and a live `radiohead` search logged `peers=1 peersWithResults=0 emptyPeers=1 failedPeers=0` while core Soulseek returned `252` responses / `16686` files
- Next steps:
  1. Push the local issue `#209` commits if you want them on `origin/main`.
  2. If testers still expect non-zero mesh search results, collect runs with more than one active mesh peer or a known shared probe query; the current single connected peer is reachable but simply returned no files for `radiohead`.

## Update 2026-04-18 11:20:00Z

- Current task: None. The latest issue `#209` root-cause follow-up is implemented locally and validated.
- Last activity:
  - traced the newest `#209` tester state past successful DHT bootstrap into a real inventory split: `DhtRendezvousService` discovered peers and attempted overlay connects, but never populated `IMeshPeerManager`, leaving `CircuitMaintenanceService` and `MeshCircuitBuilder` at `0 total peers` even when DHT had already found candidates
  - fixed that split by publishing DHT-discovered overlay endpoints into `IMeshPeerManager` immediately as onion-capable peer candidates and recording connection success/failure back onto the same peer records
  - broadened stale antiforgery token recovery so key-ring/decryption mismatches surfaced as raw `CryptographicException` (or other wrapped exception shapes) still clear the known cookies and retry token minting once
  - added focused unit coverage for both regressions and reran the DHT/circuit/hosted-service/security slices plus `./bin/lint`
- Next steps:
  1. Push the current fix set and cut a new build if you want the latest `#209` root fix in a tester-facing release.
  2. If the tester still reports trouble after this build, inspect live overlay connection success/failure rates and remote peer compatibility rather than DHT bootstrap or peer inventory wiring.

## Update 2026-04-18 10:05:00Z

- Current task: None. The Jammy PPA packaging regression is fixed locally and validated.
- Last activity:
  - inspected the Launchpad Jammy build log for `slskdn 0.24.5.slskdn.141-1ppa...` and confirmed the failure was `patchelf: No such file or directory` during `override_dh_auto_install`
  - added the missing Debian source-package dependency (`patchelf`) to `packaging/debian/control` so Launchpad installs the tool required by `debian/rules`
  - reproduced the PPA source-package build locally in a clean `ubuntu:22.04` container using the same source-tree shape as `release-ppa.yml`, and verified `dpkg-buildpackage -b` now completes successfully
- Next steps:
  1. Push the Debian packaging fix if you want the next PPA/release build to pick it up.
  2. Monitor the next Jammy PPA build for any second-stage Launchpad-only issues after this missing `Build-Depends` fix.


## Update 2026-04-18 17:35:00Z

- Current task: None. The next issue `#209` root fix is implemented locally and validated on `local test host`.
- Last activity:
  - stepped back from the earlier tester reports and revalidated the live overlay path on `local test host` instead of trusting the synthetic release gates
  - proved the current blocker was stale overlay TOFU pinning rather than version-locking: `minimus7` was a real reachable slskdn overlay peer, but a stale stored thumbprint caused our side to self-partition
  - changed inbound and outbound overlay handshakes to rotate stored certificate pins on mismatch instead of auto-blocking the peer, added focused `CertificatePinStoreTests`, and validated on `local test host` that the host now logs the mismatch, rotates the pin, and still registers/connects the neighbor in the same DHT cycle
- Next steps:
  1. Commit the pin-rotation fix set if the worktree stays clean.
  2. If another `#209` symptom appears, reproduce it on `local test host` first and add the missing host-backed smoke before cutting another build.


## Update 2026-04-18 17:55:00Z

- Current task: None. The next issue `#209` cleanup pass is implemented locally and validated on `local test host`.
- Last activity:
  - proved the live node was still overstating peer health after DHT discovery by counting raw DHT endpoints as onion-capable peers before overlay verification
  - changed DHT rendezvous publishing so discovered endpoints stay as `dht-discovered` candidates until a real overlay connect succeeds, and updated focused DHT rendezvous tests to cover candidate, failed-connect, and verified-connect states
  - redeployed the cleanup build to `local test host` and verified that `/api/v0/security/peers/stats` now reports `onionRoutingPeers: 0` while `/api/v0/dht/status` still shows the raw discovered candidate count separately
- Next steps:
  1. Push this peer-stats cleanup if you want it in the next release.
  2. If `#209` continues, the next investigation should focus on why the discovered candidates are not handshaking, not on inflated peer counters.

## Update 2026-04-18 18:10:00Z

- Current task: None. The latest DHT/overlay investigation and concurrent security hardening are committed locally and ready to push.
- Last activity:
  - continued the live `#209` investigation past the peer-count cleanup and confirmed the next real gap is handshake success for raw DHT candidates, not inflated counters
  - folded in the concurrent `CertificatePinStore` durability hardening so pin-store writes are now atomic and cannot silently corrupt `cert_pins.json` on partial write
  - added a DHT / mesh overlay audit note under `docs/security/` capturing the current threat-surface review and security decisions
- Next steps:
  1. Push the latest commits if you want the peer-stat cleanup and pin-store durability fix on `origin/main`.
  2. If `#209` still persists after that, focus on why the discovered candidates fail TLS/HELLO rather than on DHT discovery, pin rotation, or peer-count reporting.


## Update 2026-04-18 19:05:00Z

- Current task: None. The live DHT rendezvous retry/backoff fix is implemented and host-validated on `local test host`.
- Last activity:
  - traced the active mesh issue back to `DhtRendezvousService` using `_discoveredPeers.TryAdd(...)` as a once-ever outbound connect trigger, which meant the host never retried already-seen endpoints after a single timeout/refusal
  - split discovery caching from retry scheduling by adding explicit attempt timestamps and in-flight tracking with a 5-minute backoff for unverified peers
  - validated the change on `local test host` by forcing a post-backoff discovery cycle and observing `totalConnectionsAttempted` rise from `26` to `31` while `discoveredPeerCount` stayed at `26`, proving rediscovered candidates now re-enter the connector instead of staying stranded
- Next steps:
  1. Commit/push the retry/backoff fix if the worktree stays clean.
  2. Continue narrowing the live peer pool by filtering or deprioritizing clearly bad/non-overlay endpoints before they dominate mesh retries.

## Update 2026-04-29 22:35:00Z

- Current task: README screenshot showcase gallery is implemented locally.
- Last activity:
  - captured a varied open-license screenshot set and copied selected PNGs into `docs/assets/readme-showcase/`
  - added a clickable thumbnail gallery to `README.md`
  - patched Discovery Graph candidate filtering so manual-review SongID runs can render useful track-candidate atlas neighborhoods instead of collapsing to a single node
  - fixed the dirty `RegexUsernameMatcher` options type reference enough for the main project to compile past that file
- Next steps:
  1. Resolve the existing dirty `YamlConfigurationSourceTests` / `MusicBrainz.Enabled` compile mismatch before relying on full unit-test status.
  2. Review the README gallery layout in GitHub-rendered markdown and adjust captions/order if desired.

## Update 2026-04-29 22:54:04Z

- Current task: Web UI footer redesign is implemented locally.
- Last activity:
  - reorganized the footer into clearer brand/support, transfer speed, network/index, transport-health, and fork-note groups
  - added responsive footer behavior so lower-priority labels and the quote do not crowd the operational counters on narrower screens
  - browser-checked mocked logged-in footer states at `1920`, `1366`, and `390` pixel widths with no detected footer overflow
- Next steps:
  1. Review the footer against a real logged-in instance if you want to tune exact spacing/colors further.
  2. Commit/push the footer redesign once the surrounding dirty worktree state is ready.

## Update 2026-04-29 23:01:14Z

- Current task: Web UI footer live-rendering correction is implemented locally.
- Last activity:
  - inspected the live `local test host` footer and confirmed the first redesign still had brittle spacing in real light-theme rendering
  - changed the footer from rigid grid columns to a flex dock with wrapping network/status content and tighter icon alignment
  - injected the fixed CSS into the live `local test host` page and rechecked desktop, mid-width, and mobile footer captures with no visible-element overflow
- Next steps:
  1. Commit/push the footer CSS/JSX and memory-bank updates when ready.
  2. Deploy or release so `local test host` gets the fixed CSS without local injection.

## Update 2026-04-29 23:39:14Z

- Current task: Search page collapsible section persistence is implemented locally.
- Last activity:
  - made SongID, MusicBrainz Lookup, Discovery Graph Atlas, and Album Completion default collapsed
  - added stable local-storage keys so Search page panels remember the user's last open/collapsed state
  - added focused Search page tests for default collapsed secondary panels and stored collapsed primary-panel state
- Next steps:
  1. Commit and push the full workspace.
  2. Release/deploy when the UI behavior should land on `local test host`.

## Update 2026-04-30 01:40:00Z

- Current task: Player/listening-party smoke test with an open Commons file is complete locally.
- Last activity:
  - used Wikimedia Commons `Sample2.ogg` as the audio fixture and did not use Soulseek network credentials
  - verified the integrated stream endpoint returns ranged `audio/ogg` responses for the local `sha256:` content id
  - verified browser playback through two Vite dev servers, `localhost:3001` and `localhost:3002`
  - fixed and documented Gold Star Club pod/channel validation startup crashes found while bringing up the isolated backend
  - added the player smoke screenshot to the README showcase
- Next steps:
  1. Decide whether to make `library/items` automatically register local `sha256:` ids as advertisable stream mappings instead of needing a content-items seed.
  2. If desired, improve collection item display metadata so playlist rows show filenames/titles instead of raw content ids.

## Update 2026-04-30 01:47:00Z

- Current task: Browser-local player mute and mobile/PWA player support are implemented locally.
- Last activity:
  - added a persisted local mute toggle to the Web UI player that mutes only the current browser/PWA audio element
  - kept stream playback browser-owned via the existing integrated `/api/v0/streams/{contentId}` endpoint
  - added mobile/PWA player safe-area handling, inline audio playback, metadata preload, and larger touch targets
  - validated the player in a 390px mobile browser viewport against the live dev instance
- Next steps:
  1. Consider adding richer lock-screen media-session metadata once collection items resolve titles/artists reliably.
  2. Keep the content-id stream registration follow-up open so local library rows are consistently playable without manual seeding.

## Update 2026-04-30 04:10:00Z

- Current task: `2026042900-slskdn.204` release handoff is ready.
- Last activity:
  - verified GitHub writes target `snapetech/slskdn`
  - promoted the release notes out of `Unreleased`
  - added release prep notes for the external visualizer launcher and player/streaming work
  - made Winget release-version metadata validation opt-in because Winget is not part of this release
- Next steps:
  1. Monitor the `build-main-2026042900-slskdn.204` tag workflow after push.
  2. Triage any release workflow failure before cutting another tag.

## Update 2026-04-30 19:20:03Z

- Current task: Feature-expansion burn-down is continuing.
- Last activity:
  - wired the Search page acquisition profile selector into search-create API requests
  - added backend DTO/controller trimming and validation for known acquisition profile ids
  - covered default, selected, and invalid acquisition-profile request behavior with focused frontend/backend tests
- Next steps:
  1. Implement the profile policy layer that maps acquisition profiles to ranking, source selection, retry, and network-impact defaults.
  2. Promote more Automation Center recipes from visible local toggles into backend-backed job definitions.

## Update 2026-04-30 19:25:59Z

- Current task: Feature-expansion burn-down is continuing.
- Last activity:
  - mapped known acquisition profiles to bounded backend search option defaults
  - kept explicit request options as overrides for all mapped profile values
  - added controller coverage for profile default application and explicit override behavior
- Next steps:
  1. Extend profile policy beyond `SearchOptions` into result ranking and source preference.
  2. Move Automation Center recipe state from browser storage into backend-backed configuration/jobs.

## Update 2026-04-30 19:36:09Z

- Current task: Feature-expansion burn-down is continuing from the top of the list.
- Last activity:
  - added a read-only native source-provider catalog API
  - added System -> Source Providers with provider enablement, registration, capabilities, risk, network policy, and disabled reasons
  - kept the provider catalog non-invasive with no peer contact or credential checks
- Next steps:
  1. Continue down the beginning of the expansion list into provider ordering/profile policy.
  2. Avoid Discovery Inbox, Taste Recommendations, and MilkDrop files while parallel agents are actively editing them.

## Update 2026-04-30 19:41:54Z

- Current task: Feature-expansion burn-down is continuing from the top of the list.
- Last activity:
  - added per-profile provider priority policies to the source-provider catalog
  - surfaced those routing policies in System -> Source Providers
  - kept every profile policy manual-only with auto-download disabled
- Next steps:
  1. Continue into candidate ranking/explanation surfaces from the start of the feature-expansion list.
  2. Keep backend execution changes explicit, rate-limited, and review-first.

## Update 2026-04-30 19:50:33Z

- Current task: Feature-expansion burn-down is continuing from the top of the list.
- Last activity:
  - added reusable browser-side ranking for returned search candidates
  - wired Search detail smart ranking to acquisition-profile-aware candidate scores
  - surfaced score reasons in result-card tooltips without adding new network activity
- Next steps:
  1. Continue from the beginning into richer candidate actions, such as shortlist/review flows or album-picker integration.
  2. Avoid Import Staging, Discovery Inbox, Transfers, and MilkDrop files while parallel agents have active edits there.

## Update 2026-04-30 19:58:42Z

- Current task: Feature-expansion burn-down is continuing from the top of the list.
- Last activity:
  - added local album candidate grouping for already-returned search results
  - surfaced Album candidates in Search detail with score, completeness, source count, evidence labels, and representative paths
  - kept the only action as a local result-filter focus with no peer or metadata activity
- Next steps:
  1. Continue from the beginning into search-result shortlist/review actions or the next album-picker refinement.
  2. Avoid Import Staging, Discovery Inbox, MusicBrainz radar, Transfers, Footer, and MilkDrop files while parallel agents have active edits there.

## Update 2026-04-30 20:08:00Z

- Current task: Documentation overstatement cleanup is complete locally.
- Last activity:
  - replaced blanket production-ready language in the README with per-feature maturity guidance
  - downgraded DHT, security hardening, multi-source, MusicBrainz, and Service Fabric status wording where appropriate
  - marked stale January status/test reports as historical snapshots
  - changed universal SSRF, guarantee, and prevention wording to scoped guardrail wording
  - recorded the findings in `docs/archive/dev-audits/documentation-audit-2026-04-30.md`
- Next steps:
  1. Review the documentation diff for release-copy tone.
  2. Continue feature-expansion work only after deciding whether these documentation changes should be committed separately.

## Update 2026-04-30 20:02:58Z

- Current task: Feature-expansion burn-down is continuing from the top of the list.
- Last activity:
  - added selected-file download action previews to Search results
  - exposed source, providers, file count, total size, candidate score, warnings, selected paths, and copyable text export
  - kept previews local and non-mutating with no network or transfer calls
- Next steps:
  1. Continue from the beginning into stricter/preferred file condition controls or result deduplication.
  2. Avoid Import Staging, Discovery Inbox, MusicBrainz radar, Transfers, Footer, broad docs, and MilkDrop files while parallel agents have active edits there.

## Update 2026-04-30 20:09:42Z

- Current task: Feature-expansion burn-down is continuing from the top of the list.
- Last activity:
  - added ranking-only preferred conditions for extensions, lossless files, and minimum bitrate
  - wired those preferences into Search candidate scores and reasons
  - surfaced preference controls in Advanced Search Filters without turning them into hard filters
- Next steps:
  1. Continue from the beginning into search result deduplication or richer album-picker warnings.
  2. Avoid Import Staging, Discovery Inbox, MusicBrainz radar, source-feed imports, diagnostic bundle, mesh policy, broad docs, and MilkDrop files while parallel agents have active edits there.

## Update 2026-04-30 20:41:14Z

- Current task: Source-feed import provider expansion is complete locally.
- Last activity:
  - added optional YouTube Data API playlist expansion for source-feed imports
  - added optional Last.fm loved/recent/top track imports
  - documented the new `integrations.youtube.api_key` and `integrations.lastfm.api_key` options
- Next steps:
  1. Review and merge the source-feed provider import changes.
  2. Keep any further private-provider imports behind explicit account connection/OAuth flows.

## Update 2026-04-30 20:53:19Z

- Current task: Source-feed provider Web UI settings are complete locally.
- Last activity:
  - added runtime overlay support for source-feed integration settings
  - added YouTube and Last.fm enabled toggles alongside their API keys
  - added System Integrations controls for Spotify, YouTube, and Last.fm settings with masked secrets, warnings, and tooltip-backed runtime apply, YAML save, and reset actions
- Next steps:
  1. Review and merge the source-feed provider settings changes.
  2. Keep private-library provider imports behind explicit OAuth/account connection work.

## Update 2026-04-30 21:06:41Z

- Current task: Web UI surface audit and first Messages redesign slice are implemented locally.
- Last activity:
  - added `docs/dev/webui-surface-audit-2026-04-30.md` with admin/general UI gaps and recommended follow-up order
  - added a unified Messages workspace for chats and rooms
  - kept `/chat` and `/rooms` as compatibility entry points into the new workspace
  - added multi-panel message windows, compact workspace layout, collapse-all, per-panel collapse, close, and restore dock affordances
- Next steps:
  1. Validate focused Messages/App tests, frontend lint/build, whitespace, and relevant backend gates.
  2. Start the next admin settings surface with Notifications or Transfer Policy.

## Update 2026-04-30 21:20:39Z

- Current task: Web UI notification provider settings are implemented locally.
- Last activity:
  - added System Integrations controls for Pushbullet, Ntfy, and Pushover
  - exposed enable toggles, private-message and room-mention trigger toggles, masked secret replacement, validation warnings, runtime apply, YAML save, and reset actions
  - extended `OptionsOverlay` so notification provider settings can be applied at runtime when remote configuration is enabled
  - updated `config/slskd.example.yml`, changelog, the Web UI audit, task list, and progress notes
- Next steps:
  1. Continue the audit burn-down into webhooks/scripts/FTP or Transfer Policy.
  2. Keep additional admin settings surfaces redacted, tooltip-backed, and explicit about runtime versus YAML persistence.

## Update 2026-04-30 21:27:05Z

- Current task: Web UI FTP integration settings are implemented locally.
- Last activity:
  - added System Integrations controls for FTP completed-download uploads
  - exposed enablement, address, port, username/password replacement, remote path, encryption mode, certificate handling, overwrite policy, timeout, retry attempts, runtime apply, YAML save, and reset actions
  - extended `OptionsOverlay` so FTP integration settings can be applied at runtime when remote configuration is enabled
  - updated changelog, the Web UI audit, task list, and progress notes
- Next steps:
  1. Continue the audit burn-down into webhooks/scripts or Transfer Policy.
  2. Keep runtime-editable surfaces paired with YAML persistence and clear validation warnings.

## Update 2026-04-30 22:04:33Z

- Current task: Subpath Web UI release-regression hardening is complete locally.
- Last activity:
  - changed the Vite build to emit relative asset references
  - tightened build-output verification against root-relative `/assets/...` regressions
  - expanded the subpath smoke script to serve `/slskd/system/info`, inject a mounted base href, and fetch JS/CSS assets from the mounted path
  - updated backend HTML rewrite rules and focused unit coverage for non-root `web.url_base` base-tag injection
  - closed the tracked frontend Vite/Vitest peer-alignment follow-up
- Next steps:
  1. Continue burning down the expansion list with another self-contained phase that avoids the concurrent MusicBrainz radar, Playlist Intake, and MilkDrop/native visualizer lanes.
  2. Prefer changes with deterministic smoke coverage so future release regressions fail before tagging.

## Update 2026-04-30 22:08:00Z

- Current task: Mobile setup-health diagnostics are complete locally.
- Last activity:
  - added a browser-local setup health evaluator and copyable report formatter
  - added System Info setup-health modal cards for connection, identity, shares, downloads, restart, URL base, and remote-configuration readiness
  - styled the modal for narrow screens and dark mode while keeping actions tooltip-backed
  - kept the feature observational with no peer contact, credential validation, scans, config writes, downloads, or file mutation
- Next steps:
  1. Continue tail-side feature-expansion work into more E23 mobile workflows or E22/E21 evidence surfaces, avoiding concurrent MusicBrainz radar, Playlist Intake, and MilkDrop/native visualizer files.
  2. Add deterministic focused tests for each slice before moving on.

## Update 2026-04-30 22:55:36Z

- Current task: App-facing slskd branding rename is implemented locally.
- Last activity:
  - audited remaining `slskd` references and avoided blanket replacement
  - updated visible Web UI labels/tooltips/errors to use `slskdN`
  - updated notification default prefixes, config examples, docs, and related tests
  - intentionally left compatibility-sensitive identifiers and upstream attribution unchanged
- Next steps:
  1. Run focused frontend tests and diff/grep validation.
  2. Keep future branding edits scoped to user-visible slskdN-owned copy unless compatibility migration is explicitly planned.

## Update 2026-05-01 00:11:29Z

- Current task: Tail-side E23/E22 feature-expansion slice is complete locally.
- Last activity:
  - added a Discovery Inbox mobile review tray with previous/next navigation and local approve, snooze, and reject actions
  - added per-card handoff into the mobile review tray
  - added browser-local community quality trust/caution/ignore overrides with reviewer notes
  - wired quality overrides into candidate ranking reasons and action-preview warnings while retaining the underlying evidence
  - avoided concurrent Import Staging, native MilkDrop, SubjectIndex, and Playlist Intake dirty lanes
- Next steps:
  1. Continue tail-side work into more E23 mobile review surfaces or E21/E22 evidence controls that do not overlap active dirty files.
  2. Keep each slice local/review-first unless backend confirmation, rate limit, and mutation contracts already exist.

## Update 2026-05-01 00:15:13Z

- Current task: Tail-side E21 mesh evidence review sandbox is complete locally.
- Last activity:
  - added mesh evidence confidence and witness/k-anonymity review thresholds
  - added local evidence parsing/evaluation/report formatting for provenance, trust tier, privacy blockers, confidence, and witnesses
  - added a System -> Mesh Evidence Review Sandbox with sample loading, local review, accepted/rejected results, and copyable report
  - kept the feature review-only with no peer query, backend submit, publication, ranking mutation, or file/network action
- Next steps:
  1. Continue tail-side work into E20 diagnostics or remaining E23 mobile workflows that avoid current dirty Import Staging, Playlist Intake, SubjectIndex, and native MilkDrop lanes.
  2. Keep mesh evidence publication disabled by default and require explicit backend contracts before any outbound action exists.

## Update 2026-05-01 00:21:03Z

- Current task: Tail-side E20 diagnostic wizard expansion is complete locally.
- Last activity:
  - expanded setup health with readiness scoring, grouped checks, group filters, and top next steps
  - added checks for API access, provider credential gaps, queue pressure, failed jobs, and automation visibility
  - embedded setup-health readiness into the redacted diagnostic bundle
  - kept diagnostics browser-local and observational with no peer contact, credential validation, scans, retries, config writes, or server calls for bundle generation
- Next steps:
  1. Continue tail-side work into remaining E20 operational visibility or E23 mobile workflows that avoid current dirty Import Staging, Playlist Intake, SubjectIndex, audio verification, and native MilkDrop lanes.
  2. Prefer review/report surfaces until backend mutation and rate-limit contracts are explicit.

## Update 2026-05-01 00:26:06Z

- Current task: Tail-side E20 Network Health visibility slice is complete locally.
- Last activity:
  - added browser-local DHT, mesh, discovered-peer, HashDb, backfill, swarm, and mesh security-signal scoring
  - added a System -> Network health panel with score, status, findings, action guidance, and a tooltip-backed copy report action
  - kept the panel observational, using already-loaded local state only with no peer contact, discovery start, publication, search, browse, download, or file mutation
- Next steps:
  1. Continue burning down the tail-side expansion list in larger phases while avoiding active dirty Import Staging, Playlist Intake, SubjectIndex, audio verification, and native MilkDrop lanes.
  2. Prefer operational review/reporting surfaces until backend mutation, rate-limit, and trust contracts are explicit.

## Update 2026-05-01 00:32:18Z

- Current task: Tail-side E19 media-server sync review planning is complete locally.
- Last activity:
  - added browser-local Plex, Jellyfin/Emby, and Navidrome sync readiness previews
  - added explicit adapter review actions, base URL/token/path-map checks, and copyable media-server sync reports in System Integrations
  - kept the planner non-mutating with no media-server call, scan trigger, playlist sync, play-history import, rating write, Soulseek search, peer browse, download, or file move
- Next steps:
  1. Continue burning down tail-side E18/E17/E16 follow-ups in larger self-contained phases where they avoid active dirty Import Staging, Playlist Intake, SubjectIndex, audio verification, and native MilkDrop lanes.
  2. Keep live external integrations behind explicit configuration, rate limits, and visible operator confirmation.

## Update 2026-05-01 00:38:24Z

- Current task: Front-of-list listening/radar/federated taste UI expansion batch is complete locally.
- Last activity:
  - added Listening Stats recommendation seed handoffs into Discovery Inbox with mesh-preferred acquisition review metadata
  - added Search-page Artist Release Radar controls for subscriptions, notification review, Discovery Inbox handoff, and explicit route attempts
  - added Search-page Federated Taste recommendation review with source reveal controls, Discovery Inbox, Wishlist, Release Radar, and graph preview handoffs
  - kept the batch review-first with no automatic search, peer browse, download, scrobble, media-server call, graph mutation, or file mutation
- Next steps:
  1. Continue from the next unchecked front-of-list feature-expansion item that avoids the active middle/end dirty lanes.
  2. Implement live listening/media-server execution only after backend credential, rate-limit, dedupe, and confirmation contracts are explicit and visible in the Web UI.

## Update 2026-05-01 00:53:49Z

- Current task: Front-of-list media-server contract and realm subject-index UI batch is complete locally.
- Last activity:
  - added visible live media-server execution contracts in System Integrations for play-history import, scrobble/rating export, acquisition queue handoff, completed-file scan, and confirmed file actions
  - exposed adapter readiness, user mapping, confirmation, rate limit, dedupe, blockers, and copyable contract reporting without calling external media servers or mutating files
  - added System -> Mesh realm subject-index conflict review with provenance, local authority disable/re-enable state, and report export
  - verified the stale broad `dotnet test -v minimal` hang no longer reproduces in this workspace
- Next steps:
  1. Continue front-of-list burn-down after the active visualizer lane, or take the next bug only if it avoids the DHT/share-scan transfer lanes other agents may be editing.
  2. Keep live media-server execution disabled until a backend adapter explicitly consumes the visible contract and its rate-limit/confirmation gates.

## Update 2026-05-01 00:39:04Z

- Current task: Tail-side E18/E17/E16 review/reporting batch is complete locally.
- Last activity:
  - added Servarr compatibility review reports for wanted-pull and completed-import readiness
  - added Wishlist request review packets for quota, state, enabled/manual/automatic posture, and Discovery Inbox decision review
  - added Automation Center history reports for enabled recipes and dry-run checkpoints
  - kept the batch non-mutating with no Lidarr call, download-client creation, wanted pull, import trigger, automation execution, Soulseek search, peer browse, download, or file move
- Next steps:
  1. Continue tail-side burn-down into E15/E14 follow-ups or another self-contained E18/E17/E16 phase that avoids active dirty Import Staging, Playlist Intake, SubjectIndex, audio verification, native MilkDrop, and front-of-list lanes.
  2. Keep any live external integration or automation execution behind explicit configuration, rate limits, and visible operator confirmation.

## Update 2026-05-01 00:43:07Z

- Current task: Tail-side Servarr/Wishlist live execution follow-up is complete locally.
- Last activity:
  - added Servarr compatibility Run Ready to call the configured Lidarr wanted-sync endpoint when wanted pull is ready
  - added Wishlist Run Enabled to run up to three enabled backend Wishlist searches from the request portal summary
  - kept both live actions explicit and bounded with no auto result selection, direct peer browse, download, completed import trigger, automation recipe execution, or policy bypass
- Next steps:
  1. Continue tail-side burn-down into E15/E14 live follow-ups or another self-contained E18/E17/E16 execution phase where backend APIs already exist.
  2. Do not fake live automation/media-server features client-side; wire only real APIs with visible confirmation and limits.

## Update 2026-05-01 00:49:40Z

- Current task: Port migration banner correction is complete and deployed to `kspls0`.
- Last activity:
  - replaced raw VPN/endpoint copy with a slskdN ingress-port reduction reminder
  - rendered each current mapping as service purpose, protocol/public endpoint, local destination, and related config option
  - bumped the dismissal storage key so the corrected reminder appears even if the previous unclear banner was dismissed
  - documented the gotcha in ADR-0001 and committed that doc-only entry as `d3c2dd923`
- Next steps:
  1. Add a real backend reachability probe before showing open/closed status in this banner; do not infer public reachability from VPN assignment alone.
  2. Hard-refresh `kspls0:5030` if a browser still has the older service-worker asset cached.

## Update 2026-05-01 00:55:00Z

- Current task: Pods channel chat visibility fix is complete and deployed to `kspls0`.
- Last activity:
  - clarified that `dm` is a pod channel label, not a separate unread user-message inbox
  - wired pod channel tab selection to `activeChannelId`, URL navigation, and message refresh
  - added a visible channel chat panel with history, message count, and composer below Listen Along
  - documented the gotcha in ADR-0001 and committed that doc-only entry as `9d0d66b2d`
- Next steps:
  1. Consider renaming generated/default direct pod channels from terse `dm` to `DM` or `Direct` at creation/display time.
  2. Add focused Pods component tests when this area gets another behavior change; current validation was lint/build plus live asset deployment.

## Update 2026-05-01 01:05:17Z

- Current task: Unified Messages/Pods messaging surface is complete and deployed to `kspls0`.
- Last activity:
  - added Pod Channels to the Messages sidebar
  - added pod-channel message panels with Listen Along, message history, composer, and send action
  - removed the top-level Pods nav item and routed `/pods` into Messages pod-channel mode
  - added focused Messaging test coverage for opening a pod channel in the unified workspace
- Next steps:
  1. If pod administration is still needed, expose it as a secondary panel inside Messages/System instead of a top-level Pods tab.
  2. Consider renaming `dm` display labels to `DM` or `Direct` for clarity.

## Update 2026-05-01 00:51:11Z

- Current task: Tail-side Automation Center live execution follow-up is complete locally.
- Last activity:
  - added executable Automation Center actions for Wishlist Retry and Library Health Scan
  - Wishlist Retry runs up to three enabled backend Wishlist searches and records execution history
  - Library Health Scan requires an explicit operator-entered path and starts the real read-only scan
  - unsupported recipes remain visible but disabled for execution instead of simulating backend work
- Next steps:
  1. Continue tail-side burn-down into the next feature-expansion phase with real backend actions where APIs already exist.
  2. Keep media-server and remaining automation execution gated behind explicit credentials/configuration, limits, and operator confirmation before wiring live calls.

## Update 2026-05-01 00:59:01Z

- Current task: Tail-side Library Health and Discovery Shelf handoff phase is complete locally.
- Last activity:
  - added selected Library Health replacement searches through the real Search batch API, capped at three deduped queries
  - constrained selected remediation jobs to auto-fixable issue IDs only
  - added Library Health quarantine-review handoff into Discovery Inbox
  - added Discovery Shelf promote-preview handoffs into Discovery Inbox at row and batch levels
- Next steps:
  1. Continue tail-side burn-down into the next E14/E13/E12 live handoff with existing backend APIs only.
  2. Leave destructive archive/expiry/quarantine/file actions review-gated until backend preview/confirmation contracts exist.

## Update 2026-05-01 01:04:11Z

- Current task: Tail-side Listening Stats live handoff phase is complete locally.
- Last activity:
  - added top-track recommendation seeds to listening intelligence
  - added bounded live Search API execution from listening seeds
  - added enabled manual Wishlist creation from listening seeds with auto-download disabled
  - added ListenBrainz recent-history submission for up to ten browser-local plays when a token is saved
- Next steps:
  1. Continue tail-side burn-down into the next phase that can use existing APIs without implicit background work.
  2. Keep direct downloads, peer browse, media-server calls, and destructive file actions behind explicit preview/confirmation contracts.

## Update 2026-05-01 01:09:07Z

- Current task: Tail-side E15 Smart Radio and queue handoff phase is complete locally.
- Last activity:
  - added Smart Radio batch search, Wishlist, and Discovery Inbox actions
  - added Playback Queue similar-candidate search and Wishlist actions
  - kept these actions bounded and explicit, with Wishlist auto-download disabled
  - preserved local queue behavior while adding acquisition handoffs
- Next steps:
  1. Continue tail-side burn-down into remaining E15/E14 gaps or move earlier only where existing APIs can be wired directly.
  2. Keep direct download/browse/file actions out until the UI has explicit preview and confirmation contracts.

## Update 2026-05-01 01:19:43Z

- Current task: Discovery Inbox acquisition-plan Wishlist handoff phase is complete locally.
- Last activity:
  - added a `Wishlist Ready` action beside `Execute Ready` in Acquisition Plans
  - creates bounded manual Wishlist requests with auto-download disabled
  - records the Wishlist request id on the plan and displays it in the plan card
  - skips plans that already have a Wishlist request so repeated clicks do not duplicate entries
- Next steps:
  1. Continue tail-side burn-down into adjacent acquisition/import phases, but avoid Import Staging files while another lane has active dirty edits.
  2. Keep direct download, peer browse, and file mutation behind explicit preview and confirmation contracts.

## Update 2026-05-01 01:24:58Z

- Current task: E11 Playlist Intake tag/organization dry-run phase is complete locally.
- Last activity:
  - added organization template, album title, multi-artist, cover-art, and ReplayGain controls to Playlist Intake
  - generated persisted dry-run plans with changed fields, destination paths, skipped rows, and explicit no-write impact
  - kept this phase review-only because the safe tag-write/file-move backend contract is not wired here
- Next steps:
  1. Continue into remaining E11/E10/E9 work only where it can avoid active dirty Import Staging/audio-verification lanes or where those lanes have settled.
  2. Do not add tag writes, file moves, cover writes, or ReplayGain execution until there is an explicit backend preview/confirmation and rollback contract.

## Update 2026-05-01 03:08:11Z

- Current task: Remaining middle/backlog completion pass is complete locally.
- Last activity:
  - reconciled completed QR invite display/scanning, DHT adaptive bootstrap diagnostics, native MilkDrop3 T-938, SongID parity notes, analyzer cleanup, and production placeholder burn-down tasks
  - confirmed the old dev package channel was stale; the later cleanup removed it from active release automation
  - replaced production placeholder/TODO wording with explicit capability-gate or unavailable-path language and updated the placeholder completion plan
  - verified focused Contacts QR tests, focused DHT tests, touched Contacts ESLint, and `dotnet format --verify-no-changes --no-restore --verbosity minimal`
- Next steps:
  1. Continue normal validation before merge because concurrent agents have been committing adjacent player and docs work.

## Update 2026-05-01 02:52:32Z

- Current task: MilkDrop/Butterchurn player tile deployment to `kspls0` is complete.
- Last activity:
  - moved the small tile mode/fullscreen controls out of the visualizer canvas area in the current frontend bundle
  - kept explicit player tile modes for spectrum, signal scope, Butterchurn, MilkDrop3 WebGL2, and MilkDrop3 WebGPU
  - synced the rebuilt `wwwroot` to `/usr/lib/slskd/current/wwwroot` on `kspls0` and restarted the live `slskd` service
  - verified `http://kspls0:5030` serves `index-CaQ2HqYt.js` and `index-6lV9-732.css`
  - fixed the bad restart state where the process briefly ran without `/etc/slskd/slskd.yml` and only listened on `127.0.0.1:5030`; verified it now listens on `0.0.0.0:5030`
- Next steps:
  1. If the browser still shows the old visualizer tile, hard-refresh once or unregister the old service worker; the server endpoint is now serving the new bundle.
  2. Continue debugging any runtime browser-console errors from Butterchurn or native MilkDrop after confirming the new bundle is loaded.

## Update 2026-05-01 03:02:00Z

- Current task: Player visual tile one-row controls and native visualizer retry fix are complete and deployed to `kspls0`.
- Last activity:
  - changed the visual tile buttons from wrapping flex controls to a fixed seven-column icon row
  - remount the compact visualizer when a tile visualizer mode button is clicked, including same-mode retry clicks
  - key the visualizer canvas by engine type so WebGPU/WebGL2 context changes get a fresh canvas
  - dispose partially initialized native engines when setup fails
  - verified `kspls0:5030` serves `index-BGGUa318.js` and `index-DwvOKuMB.css`
- Next steps:
  1. WebGPU on `http://kspls0:5030` may still be browser-blocked if the browser requires a secure context for `navigator.gpu`; WebGL2 should now be retryable after switching away and back.
  2. If WebGL2 still fails, capture the browser console error from the compact tile after loading `index-BGGUa318.js`.

## Update 2026-05-01 09:18:00Z

- Current task: PPA upload retry investigation is in progress.
- Last activity:
  - confirmed the `.215` PPA source package built and signed correctly before upload
  - confirmed historical `.197` PPA success used anonymous FTP with passive mode, not SFTP
  - reverted the incorrect SFTP/Launchpad SSH secret change
  - fixed standalone PPA web asset staging after the manual retry failed on a missing `publish-linux-x64/wwwroot`
- Next steps:
  1. Retry the original tag workflow's failed PPA job with the known-good FTP configuration.
  2. If FTP still fails, inspect whether this is a transient GitHub/Launchpad network issue or a changed runner/network policy.

## Update 2026-05-05 01:57:00Z

- Current task: Manual deployment follow-up and live mesh login diagnosis.
- Last activity:
  - deployed the dirty manual build to `kspls0` and verified the service is active, reporting the new build info, and logged in to Soulseek as the configured live service account
  - investigated the failed publish integration smoke; it used local Debug child processes without VPN/proxy egress
  - confirmed a single-account local probe can connect TCP to Soulseek but is closed/reset before login completes, so the failure is the local egress/server path rather than the deployed service
  - added ignored local account-pool support for the optional live mesh smoke and loaded four extra local test accounts into the private pool file
- Next steps:
  1. If live mesh smoke should remain in publish validation, route its two spawned processes through documented VPN/proxy egress or make it opt-in so external Soulseek policy does not fail local builds.
  2. Preserve child process logs in `SlskdnFullInstanceRunner` failure output so future live-account failures include the actual disconnect reason.

## Update 2026-05-05 02:47:00Z

- Current task: VPN-backed live mesh login matrix.
- Last activity:
  - found and used the four local Proton WireGuard configs from the private credential pool
  - added an opt-in full-instance VPN wrapper path for integration children
  - verified Proton config 1 fails Soulseek login for accounts A-D while configs 2, 3, and 4 all log in
  - confirmed the full live mesh smoke gets past alpha/beta login when using configs 2 and 3, then stalls later with no overlay peers
- Next steps:
  1. Exclude Proton config 1 from live Soulseek test runs unless it recovers.
  2. Debug the post-login live mesh stall separately from login/IP throttling; login is no longer the blocker on configs 2/3/4.

## Update 2026-05-05 03:12:00Z

- Current task: VPN-backed overlay peer fix.
- Last activity:
  - changed overlay connect tests to use the target runner's reachable overlay address instead of `127.0.0.1`
  - routed wrapped VPN test namespaces across the local veth test fabric so alpha can reach beta's overlay listener while retaining VPN default egress
  - confirmed live alpha/beta overlay peers now appear on both nodes with Proton configs 2 and 3
  - found the next blocker: the probe file reaches alpha's incomplete directory, but the download API call did not complete
- Next steps:
  1. Debug live mesh download finalization after successful overlay/search.
  2. Keep Proton config 1 out of live smoke runs; configs 2/3/4 are viable for login.

## Update 2026-05-05 03:37:00Z

- Current task: VPN-backed live mesh download finalization fix.
- Last activity:
  - narrowed the VPN namespace test-fabric route to `10.224.0.0/11` so peer namespaces can reach each other without capturing Proton/private DNS routes
  - host-resolved the Soulseek server address for VPN-wrapped full-instance children so namespace DNS does not block login
  - preserved full child stdout/stderr logs under each temp app directory for failed full-instance runs
  - fixed wrapper cleanup to kill the whole process tree and remove the VPN namespace explicitly
  - confirmed the live smoke now completes login, overlay, search, pod download, HTTP response, and cleanup
- Next steps:
  1. Keep Proton config 1 out of live smoke runs unless it recovers.
  2. Use configs 2/3/4 for optional live-account mesh validation.

## Update 2026-05-05 05:49:00Z

- Current task: Adjacent rewrite evaluation for portable Web UI and integration fixes is complete.
- Last activity:
  - ported the applicable Spotify redirect URI panel guidance and authorization navigation behavior
  - changed media-server and Servarr readiness reports from `TODO` to `PENDING` for blocked checks
  - added focused Spotify integration helper tests and updated media-server report tests
  - verified existing slskdN backend controllers already cover the comparable Spotify, Lidarr, and library-health API surfaces
- Next steps:
  1. Commit these frontend integration polish changes when ready.
  2. If broader endpoint parity is desired, run a dedicated route inventory comparison against the Web UI API client calls rather than importing placeholder/stub endpoints.

## Update 2026-05-05 06:54:55Z

- Current task: Web UI logo replacement from local PNG handoff is complete.
- Last activity:
  - selected the dark square lockup/cube assets from the local PNG batch and ignored flattened production-sheet/checkerboard mockups
  - generated favicon/PWA PNGs plus importable React assets under `src/web/src/assets/brand/`
  - replaced the login ASCII mark with the lockup image and added a footer GitHub logo link
  - verified focused login/footer tests, web lint, and web production build
- Next steps:
  1. Push the branding change after final repository lint passes.
  2. For future refinements, request a transparent-background icon/lockup export if the footer or light-mode treatment needs less dark-box framing.

## Update 2026-05-05 07:00:33Z

- Current task: Replacement release after the stuck tag gate is in progress.
- Last activity:
  - cancelled the stuck `.222` GitHub Actions release run
  - documented the gotcha for opaque release-gate hangs
  - added per-command release-gate timeouts and workflow-level build job timeouts
  - confirmed the release-gate shell script parses cleanly and the full bounded release gate passes locally
- Next steps:
  1. Commit/push the logo and release-gate changes.
  2. Push a new build-main tag and monitor the replacement release run.

## Update 2026-05-05 07:37:55Z

- Current task: Logo asset correction and kspls0 redeploy is in progress.
- Last activity:
  - replaced the dark square app-icon derivatives with transparent icon assets
  - added a wire-style transparent icon for favicon/footer use
  - enlarged the login lockup and widened the login column
  - documented the branding placement gotcha separately
- Next steps:
  1. Re-run web tests/lint/build.
  2. Commit and push the branding correction.
  3. Publish and redeploy the corrected manual build to `kspls0`.

## Update 2026-05-05 21:55:00Z

- Current task: Broad bug council burn-down cycle is validated and ready to push.
- Last activity:
  - fixed MediaCore primitive timestamp request bodies and pod/channel/content route encoding
  - restored descriptor verification result rendering to the descriptor panel
  - guarded remaining nested list render paths in Shares, Search, Network, and MediaCore
  - included vendor runtime RT-058 DTO collection validation work
- Next steps:
  1. Commit implementation/docs batch.
  2. Verify GitHub target and push `main`.

## Update 2026-05-05 22:05:00Z

- Current task: Broad route-boundary and MediaCore result-panel burn-down is validated and ready to push.
- Last activity:
  - encoded dynamic route segments across slskdN, library-health, collections/share grants, identity, wishlist, and bridge helpers
  - added focused helper tests for slash-bearing IDs
  - guarded additional MediaCore result-panel list counts and renders
- Next steps:
  1. Commit implementation/docs batch.
  2. Verify GitHub target and push `main`.

## Update 2026-05-05 22:58:00Z

- Current task: Bug council loop fix and second scanner pass are in validation.
- Last activity:
  - re-ran the runtime council scanner
  - recorded 28/28 constructor candidates, 221/221 protocol count/length candidates, and countable protocol loop/length/compression subgroups as classified in sweep registers
  - fixed the accepted rotated-obfuscation null-input allocation candidate
  - added the remaining candidate class queue and baseline assertions for whole-section closure
- Next steps:
  1. Run remediation baseline and focused runtime tests.
  2. Commit and push the council-loop follow-up.

## Update 2026-05-05 23:24:00Z

- Current task: Runtime protocol scalar emission sweep is in validation.
- Last activity:
  - classified 145/145 protocol scalar emission candidates
  - fixed accepted outbound token/id/version/day-count constructor guards before `WriteInteger`
  - added focused scalar-emission regression tests and ledger row `RT-071`
- Next steps:
  1. Run runtime remediation baseline, focused runtime tests, and repo checks.
  2. Commit and push the scalar-emission sweep.

## Update 2026-05-06 02:24:55Z

- Current task: Stable release `2026050600-slskdn.227` is published.
- Last activity:
  - created and pushed replacement tag `build-main-2026050600-slskdn.227`
  - verified GitHub release publication with 13 release assets
  - watched the release workflow to completion; Docker, PPA, COPR, AUR, Nix metadata/smoke, Homebrew, and announcements completed
  - noted Chocolatey push as a non-blocking external 504 timeout after successful package creation
- Next steps:
  1. Optionally rerun or manually retry only the Chocolatey publish for `slskdn.2026050600.0.0-slskdn.227.nupkg`.
  2. Address warning backlog for nullable annotations and CA2000 outbound handler analyzer warnings in a normal bug cycle.

## Update 2026-05-06 16:49:51Z

- Current task: Non-runtime council scan follow-up is in broad validation.
- Last activity:
  - skipped runtime-specific scanning/fixes because another agent owns that lane
  - recorded `BUG-20260506-014` through `BUG-20260506-022`
  - hardened ActivityPub/share backfill outbound clients, share-grant sender binding, recursive file listing reparse-point handling, Discovery Graph query refresh, SearchHub list payloads, Messaging panel persistence, and Web media/search map/list guards
  - focused backend tests, focused Web tests, outbound/path/Web-list scanners, packaging metadata, and release-copy checks passed
- Next steps:
  1. Run broad remediation/lint/diff gates.
  2. Commit and push the non-runtime council follow-up.

## Update 2026-05-06 17:21:40Z

- Current task: Attribution audit is complete.
- Last activity:
  - audited fork-only files and upstream-derived changed files against `upstream/master`
  - treated vendored Soulseek.NET runtime files as third-party exceptions that should retain JP Dillingham attribution
  - fixed the missing slskdN co-attribution block on `Z12282025_AdditionalTransferIndexesMigration.cs`
  - focused attribution checks, `git diff --check`, and `dotnet format --verify-no-changes --no-restore --severity error src/slskd/slskd.csproj` passed
- Next steps:
  1. Keep the same attribution rule for future files: fork-only slskdN code uses slskdN attribution; upstream-derived changed code preserves upstream and adds slskdN co-attribution.

## 2026-05-07 02:39:03Z

Current task: kspls0 Messages V2 resource/flicker hotfix deployed.
Next steps: User live-tests Messages pane sizing, flicker, and resource use; investigate any remaining panel-specific churn with browser profiler if visible.

## 2026-05-07 02:56:47Z

Current task: Downloads-to-Browse empty tab hotfix deployed.
Next steps: User validates live kspls0 by clicking the new folder browse button from Downloads; investigate only if a real peer browse returns an actual empty share response.

## 2026-05-07T03:10:33Z Session update

Completed live hotfix for Messaging V2 room/pod controls, Browse tree performance, and distributed-token log spam. Deployed custom build to kspls0. Next steps: user live-tests room/pod controls and large Browse behavior.

## 2026-05-07T03:15:46Z Session update

Completed Browse frontend performance follow-up. Worker tree construction and flattened/capped visible directory rendering are deployed to kspls0 for live testing.

## 2026-05-07T03:18:52Z Session update

Completed Browse folder action fix. Checkbox descendant selection was removed because it froze large browse tabs; folder rows now expose direct download buttons and keep drilldown/preview behavior.

## 2026-05-07T03:21:15Z Session update

Completed Browse UI cleanup after live feedback. Root/user tree row no longer has a download button; selected folder files remain available through the integrated file panel.

## 2026-05-07T19:37:00Z Session update

Completed kspls0 mesh/VPN diagnosis and hotfix. DHT now announces the VPN-assigned public overlay port separately from the local listener, Soulseek rendezvous is enabled, and kspls0 has a verified active mesh overlay peer.

Next steps: package the mesh advertised-port fix in the next normal release; remove the live hotfix directory after kspls0 is upgraded to a release containing this change.

## 2026-05-12T23:49:22Z Session update

Completed the next feature-coherence hardening slice on `main`: wired real web bind exposure into startup hardening, and made the unavailable hash-from-audio option hidden from public CLI/env toggles and fail-fast if enabled.

## 2026-05-12T23:57:46Z Session update

Completed the hardening startup matrix at the validator/analyzer boundary for loopback, localhost, Unix socket, wildcard, private IPv4, IPv6 wildcard, unknown bind address, and remote no-auth CIDR behavior.

## 2026-05-13T00:04:31Z Session update

Added the initial runtime feature-gate foundation and gated SongID, mesh, DHT, pods, social federation, VirtualSoulfind, and multi-source APIs behind enabled-by-default `feature.*` switches.

## 2026-05-13T00:17:13Z Session update

Updated dependency ownership inventory with concrete call-site classifications and explicit prune/relocation follow-ups for `dotNetRDF`, `MathNet.Numerics`, Microsoft.Build, and Microsoft.CodeAnalysis packages.

## 2026-05-13T00:28:00Z Session update

Completed the analyzer suppression audit slice. `docs/analyzer-suppressions.md` now matches the app-project `NoWarn` set and records the remaining CA2000 transport warnings as code follow-up, while feature gates remain enabled by default.

## 2026-05-13T00:42:00Z Session update

Completed the build-task relocation slice. The runtime app no longer compiles `CodeAnalysisBuildTask`, `TestCoverageBuildTask`, or `RegressionBuildTask`, and no longer references `Microsoft.Build.*`; those tasks now build from linked CodeQuality sources in `tools/slskd.BuildTasks`.

## 2026-05-13T00:50:00Z Session update

Cleaned up the remaining app build warnings with local CA2000 scopes in the two guarded transport constructors. The app project now builds cleanly with zero warnings.

## 2026-05-13T01:05:00Z Session update

Added the next DownloadService regression slice covering in-progress duplicate protection, completed-transfer supersession, and terminal failed cleanup when background download startup fails. Focused DownloadService unit tests passed.

## 2026-05-13T01:24:00Z Session update

Added SongID runtime capability reporting and the `/api/v0/songid/capabilities` endpoint. Optional recognition/provider lanes now report based on runtime config and tool availability, and `HashFromAudioFileEnabled` is explicitly broken/unavailable.

## 2026-05-13T01:35:00Z Session update

Added DownloadService per-user semaphore regression coverage. Same-user enqueue requests serialize at the critical section, while different users can enter concurrently.

## 2026-05-13T01:43:00Z Session update

Expanded Program.cs service-module decomposition by moving top-level runtime service composition into `Bootstrap/RuntimeServiceCollectionExtensions`, ASP.NET service registration into `Bootstrap/WebServiceCollectionExtensions`, ASP.NET request-pipeline registration into `Bootstrap/WebApplicationPipelineExtensions`, application-host wiring into `Bootstrap/ApplicationHostServiceCollectionExtensions`, core app services into `Bootstrap/CoreApplicationServiceCollectionExtensions`, integration/media registrations into `Bootstrap/IntegrationAndMediaServiceCollectionExtensions`, the large experimental feature graph into `Bootstrap/ExperimentalFeatureGraphServiceCollectionExtensions`, and user-data persistence registration into `Bootstrap/UserDataServiceCollectionExtensions` after the earlier SongID bootstrap extraction.

Next steps: subdivide `ExperimentalFeatureGraphServiceCollectionExtensions` into smaller bounded modules, continue remaining DownloadService CTS cleanup cases, and resolve the runtime-vs-tooling decision for Roslyn CodeQuality helpers.

## 2026-05-13T16:38:00Z Session update

Completed the Arch build/download-timeout/Snap hotfix and kspls0 manual deployment. Current live build on kspls0 is `0.0.0-manual.20260513163650.c07c237919e0`; service is active/running, web root returns 200, application API reports connected/logged-in, shares are loaded, and fresh logs after the corrected deployment show no timeout-related `ERR` lines. No release tag was created.

Next steps: create the requested release tag only after explicit tag/release confirmation; continue monitoring live download behavior for remote-peer failures that are separate from the aggregate timeout logging bug.

## 2026-05-13T16:44:00Z Session update

Completed post-deploy test pass. Full `dotnet test --no-restore` passed across smoke, unit, and integration suites, and `./bin/lint` passed. Live kspls0 reproduced the tester timeout signature only as warning/`Completed, TimedOut`; remaining download failures appear to be peer-side rejection, remote size mismatch, offline peers, and connection failures rather than the aggregate timeout bug.

## 2026-05-13T17:20:00Z Session update

Continued Program.cs decomposition after the production wrapper unwiring. Focused tests now call extracted startup/path/network helpers directly, and redundant test-only Program wrappers were removed. `dotnet build src/slskd/slskd.csproj --no-restore` and focused Program/ProfileService tests passed.

Next steps: keep reducing Program static startup state where it still owns private runtime configuration, then continue the remaining MediaCore reconciliation forms.

## 2026-05-13T17:35:00Z Session update

Continued Program.cs decomposition by removing stale Program dead code left by earlier helper extractions while keeping command-line argument population in `Program` for correct `[Argument]` binding. Build and focused Program/controller/log tests passed.

Next steps: run lint, commit the startup-input cleanup, then continue either the remaining Program static-state split or the MediaCore reconciliation forms.

## 2026-05-13T17:48:00Z Session update

Continued Program.cs decomposition by moving startup directory preparation, single-instance mutex acquisition, configuration-file defaulting, and default directory validation into `Bootstrap/StartupApplicationDirectories`. `Program.cs` is now 466 lines. Build and focused Program/application-controller tests passed.

Next steps: run lint, commit missing status docs, then continue with the remaining Program runtime-state wrappers or the MediaCore reconciliation forms.

## 2026-05-13T17:56:00Z Session update

Removed the remaining antiforgery Program wrappers. The MVC CSRF filter now gets `OptionsAtStartup` from request services and calls `AntiforgeryCookieRecovery` directly, and focused tests call the helper directly with an explicit port. `Program.cs` is now 449 lines. Build and focused Program/CSRF/controller tests passed.

Next steps: run lint and commit this wrapper cleanup, then continue reducing remaining Program startup/logging wrappers or move back to the MediaCore reconciliation forms.

## 2026-05-13T18:05:00Z Session update

Moved startup configuration load/validation exception handling into `Bootstrap/StartupConfiguration.TryLoadAndValidate`, leaving `Program` to publish the resulting configuration state. `Program.cs` is now 445 lines. Build and focused Program/config/hardening tests passed.

Next steps: run lint and commit this configuration split, then continue with remaining Program startup/logging wrappers or the MediaCore reconciliation forms.

## 2026-05-13T18:15:00Z Session update

Continued the reconciliation plan in MediaCore by keeping similarity review/statistics as the default perceptual-hash path and grouping raw audio/image hash computation behind advanced disclosure. Focused MediaCore component tests and focused Web lint passed.

Next steps: commit the MediaCore perceptual-hash cleanup, then continue with remaining non-pod MediaCore forms or another small Program startup wrapper split.

## 2026-05-13T18:25:00Z Session update

Continued Program.cs decomposition by moving startup command-mode console output, certificate generation, and startup logo rendering calls into extracted bootstrap helpers directly. `Program.cs` is now 424 lines. Build and focused Program/log lifecycle tests passed.

Next steps: run lint, commit the console/certificate wrapper cleanup, then continue with remaining Program startup/logging wrappers or non-pod MediaCore forms.

## 2026-05-13T18:35:00Z Session update

Continued Program.cs decomposition by moving startup SQLite initialization and missing-configuration recreation calls to extracted bootstrap helpers directly. `Program.cs` is now 414 lines. Build and focused Program/log lifecycle tests passed.

Next steps: run lint, commit this startup helper wrapper cleanup, then continue with the last Program logging/shutdown wrapper cluster or non-pod MediaCore forms.

## 2026-05-13T17:50:33Z Session update

Continued Program.cs decomposition by moving startup logging configuration and shutdown telemetry installation calls to extracted bootstrap helpers directly. `Program.cs` is now 404 lines. Build and focused Program/log lifecycle tests passed.

Next steps: run lint, commit this logging/shutdown wrapper cleanup, then re-scan for any remaining Program-owned startup wrappers or continue the remaining non-pod MediaCore reconciliation forms.

## 2026-05-13T17:54:03Z Session update

Continued the parity/reconciliation list by fixing MediaCore ContentID examples after the advanced registration split. Example buttons now fill read-first resolve/validation fields and the advanced registration fields, and the focused component test covers that path. Focused MediaCore component tests and focused Web lint passed.

Next steps: commit the MediaCore example cleanup, then re-scan remaining non-pod MediaCore forms or reassess whether Program.cs has any remaining practical decomposition beyond static state/orchestration.

## 2026-05-13T18:25:58Z Session update

Reconciled the remaining stale parity plan wording. Soulseek mesh rendezvous is now described as having System UI, privacy language, a disabled-by-default gate, and tests; security/privacy now points at the completed route audit artifacts while preserving future threat-model upkeep.

Next steps: commit this doc cleanup, then leave the remaining reconciliation scope to release coordination, live/E2E checks for the target release, adverse-network validation, and any prioritized G5 guided-flow work.

## 2026-05-13T18:33:19Z Session update

Added top-level Web route smoke coverage in `App.test.jsx` for the G4 stale/orphan UI reconciliation item. The table covers the primary routes plus parameterized search, pod, channel, and System paths using page mocks so route wiring is checked without loading every page's data dependencies.

Next steps: run focused Web lint, commit this route-smoke reconciliation slice, then continue only with broader G5 guided-flow work or release/live validation if requested.

## 2026-05-13T18:34:38Z Session update

Strengthened System label validation so the test now verifies each expected admin and experimental panel carries the correct navigation label instead of only checking that some labels exist.

Next steps: commit this label-validation slice, then the remaining local reconciliation scope is broader G5 guided-flow productization. Release/live validation still waits on push/release coordination.

## 2026-05-13T18:36:36Z Session update

Mapped the G5 guided-flow tracks to current UI entry points in `docs/system-surfaces.md`: Network, Swarm Analytics, Source Providers, Search/Wishlist/Discovery Graph, Collections, Shares, Library Health, Messages/pods, and the player. The feature parity plan now treats future G5 work as new workflow pages only when current surfaces are insufficient.

Next steps: commit this doc reconciliation slice. Remaining reconciliation work is release/live validation after push coordination, adverse-network testing, and any explicitly prioritized new guided-flow build.

## 2026-05-13T18:37:49Z Session update

Closed the local G4/G5 reconciliation wording: stale/orphan UI and advanced-feature UX are complete for this pass, while future work is limited to periodic compatibility/experimental visibility review or explicitly prioritized new guided-flow pages.

Next steps: commit this closeout wording. Remaining items are release/live validation after push coordination and adverse-network testing.

## 2026-05-13T18:42:45Z Session update

Ran the remaining locally actionable validation slice. `npm run check:soulseek-network-health` passed. The first focused E2E run exposed only a missing Playwright Chromium cache, so `npx playwright install chromium` was run; then `SLSKDN_TEST_NO_CONNECT=true npm run test:e2e:ci -- e2e/core-pages.spec.ts` passed all 4 tests against an isolated harness node.

Next steps: commit the validation documentation. Remaining reconciliation scope is push/release coordination, target-specific live/E2E validation, live/stub Soulseek interop, and adverse-network mesh/DHT testing.

## 2026-05-13T18:44:14Z Session update

Ran the focused deterministic mesh/adverse integration slice: `MeshSearchLoopbackTests` and `MeshOnlyTests` passed (`5/5`). This covers mesh search loopback and mesh-only partition behavior without requiring live Soulseek credentials.

Next steps: commit the validation documentation. Remaining reconciliation scope is push/release coordination plus any target-specific live-account or external-network validation the release owner wants.

## 2026-05-13T18:47:32Z Session update

Ran the optional live Soulseek-account mesh smoke with `SLSKDN_RUN_LIVE_MESH_ACCOUNT_TESTS=1`; `OptionalLiveAccounts_CanSearchAndDownloadHostedProbeOverOverlayMesh` passed (`1/1`). This closes the locally runnable live interop validation gap.

Next steps: commit the validation documentation. Remaining reconciliation scope is branch sync/release coordination and any additional release-owner-specific checks.

## 2026-05-13T18:48:39Z Session update

Updated the core Soulseek risk wording so it reflects the passed optional live mesh smoke. Broader downstream compatibility checks remain release-owner scope instead of local reconciliation cleanup.

Next steps: commit this wording cleanup. Remaining reconciliation work is branch sync/release coordination and any release-owner-specific checks.

## 2026-05-13T19:05:00Z Session update

Answered the live-test coverage gap by adding an optional slskdN/Soulseek.NET runtime native transfer smoke. The test starts a full slskdN instance, exposes its Soulseek listen port to the harness, uses a deterministic test endpoint override for the raw runtime client, then verifies raw `SoulseekClient` browse and download against a tiny slskdN-hosted probe. Both the flag-unset and live-flag test runs passed.

Next steps: upstream `slskd/slskd` daemon compatibility still needs an external upstream binary/container harness if release owners want that specific app-to-app check; local reconciliation coverage now includes slskdN mesh live transfer and raw Soulseek.NET runtime native transfer.

## 2026-05-13T19:15:00Z Session update

Added the upstream `slskd/slskd` compatibility harness. The runner can use `SLSKDN_UPSTREAM_SLSKD_BINARY_PATH` or clone/build upstream into `/tmp/slskdn-upstream-compat/slskd`; the test starts upstream plus slskdN, scans an upstream-hosted probe, and has slskdN enqueue the native Soulseek download. Upstream built successfully, and the default opt-in-off integration path passed.

Live same-host validation currently reaches login, share scan, enqueue, and upstream upload request, then fails because upstream tries to connect back to slskdN through the public Soulseek endpoint reported by the server. That local endpoint is not routable here. The test is useful for a two-host or port-forwarded release environment; same-host NAT/firewall runs need routable listen ports or they will fail at the upstream upload callback stage.

## 2026-05-13T19:30:03Z Session update

Extended the upstream compatibility harness for per-credential VPN isolation. Full-instance runners now lease VPN configs through a shared allocator, so an upstream daemon and a slskdN daemon can run through different WireGuard namespaces in the same test. The runner also has an opt-in namespace entrypoint that runs `natpmpc` before daemon startup to claim the daemon's Soulseek listen port when the provider supports it.

Validation: the inert `SlskdnSoulseekRuntimeInteropTests` integration filter passed (`2/2`). Live VPN runs with Proton configs 2/3 reached login, share scan, API control, and slskdN enqueue across separate namespaces, but transfer completion still requires inbound VPN port forwarding. With NAT-PMP claiming enabled, the provider gateway `10.2.0.1` returned `the gateway does not support nat-pmp`, so this local VPN stack cannot complete upstream app-to-app native transfers until working forwarded-port configs/static forwards are available.

Correction: config 2 is marked `NAT-PMP (Port Forwarding) = off`; configs 3 and 4 are NAT-PMP capable. The harness now skips NAT-PMP-off configs when port-forward claiming is enabled. It also follows the Proton behavior documented and used by slskR: accept a random public port, rewrite `soulseek.listen_port` before daemon login, and redirect the provider-mapped private port to the rewritten listener inside the namespace. Live upstream `slskd` -> slskdN native transfer passed with separate Proton configs 3/4.

## 2026-05-13T20:10:00Z Session update

Implemented the upstream PR idea pass in slskdN style without copying upstream code. Current changes include group overlap validation, safe completed-download destination routing with migration, derived download batch summaries, named browser-local search filters, shift-range file selection, configured native Soulseek liked/disliked interest publishing, and Lidarr/Wishlist acquisition hardening.

Validation so far: focused backend unit tests passed (`36/36`), `dotnet build src/slskd/slskd.csproj -c Release --no-restore` passed, `cd src/web && npm run lint` passed, and `cd src/web && npm run build` passed.

Next steps: run repo lint/full backend tests after final formatting cleanup, then decide whether to deploy this build to kspls0 for live validation.

## 2026-05-13T19:58:55Z Session update

Integrated the other agent's mesh streaming work into main instead of treating it as discardable scratch. The slice adds mesh stream ticket/service/controller files, wires the services into core DI, and changes pod search stream actions to return local stream URLs for already-local content or short-lived mesh stream URLs for remote mesh content. Added focused unit coverage for mesh tickets/controller plus the updated SearchActions constructor path.

Validation: `dotnet build src/slskd/slskd.csproj --no-restore` passed with zero warnings, and focused backend unit tests passed (`36/36`) for mesh streams, peer streams, and SearchActions.

Next steps: commit and push this mesh streaming merge slice, then re-check branch cleanliness before deleting any obsolete branch refs.

## 2026-05-14T16:42:00Z Session update

Continued kspls0 log cleanup after the manual release build. Classified Soulseek `File read error` transfer rejections as expected peer denials, then reduced Lidarr wanted-sync HTTP status failures to concise one-line warnings instead of stack traces while keeping unexpected Lidarr exceptions fully logged.

Validation: focused classifier/Program path tests passed (`41/41`), focused Lidarr sync tests passed (`4/4`), `dotnet build src/slskd/slskd.csproj --no-restore` passed after rerunning serially to avoid a transient SDK file lock, and manual build `0.0.0-manual.20260514164010.60039cee8323` is running on kspls0 with `NRestarts=0`.

Next steps: keep watching PID-scoped kspls0 logs for fresh warning/error signatures. Current post-deploy sample from PID `614254` has no warnings, errors, exceptions, or fatal entries.

## 2026-05-15T21:18:36Z Session update

Added Docker optional heavyweight SongID tool installation support. The default
image now carries `install-optional-media-tools`, a root-only helper with
explicit profiles for distro prerequisites, Whisper/Demucs Python tooling, C2PA
via Cargo, Audfprint, and Panako jar download. Docker and SongID docs now show
post-start and derived-image workflows, and explain why UI feature toggles do
not auto-install heavyweight toolchains.

Validation: script syntax passed, focused `SongIdCapabilityReporterTests`
passed, packaging metadata validation passed, `git diff --check` passed, local
Docker build `slskdn:optional-tools-test` passed, container smoke confirmed the
installer, core media tools, QUIC library visibility, and setuid/setgid
stripping, and `./bin/lint` passed.

Next steps: push this packaging/docs slice, then cut a release tag only if
explicitly requested.

## 2026-05-15T22:25:53Z Session update

Completed the live Docker headless validation pass and deployed the fixed
manual all-tools image from revision `c39a604ac3`. The post-deploy crawl covered
25 top-level Web routes and 72 API probes with no route failures, browser
console errors, page errors, bad responses, or failed browser requests.

Current live logs are materially clean for the UI/API fixes: no fresh backend
errors, fatal entries, route ambiguity, API 404s, permission denials, or
read-only filesystem failures. The remaining item to burn down first is noisy
mesh-health warning output when mesh/DHT has no reachable healthy peers; the
other open follow-ups are documented in `memory-bank/tasks.md`.

## 2026-05-15T22:35:00Z Session update

Fixed the negative completed-transfer speed display found in the latest live
logs. This is ready to include in the next manual image/release pass; the live
container has not been rebuilt again for this last cosmetic log fix yet.

## 2026-05-15T22:48:00Z Session update

The live Docker host is now running the all-tools manual image from revision
`b2ebabc43e` after a verified tar reload. Docker health is healthy, restart
count is zero, optional tools are present, and the post-deploy crawl is clean
for routes and browser behavior.

Known remaining follow-ups are unchanged: admin-only debug/visualizer API
semantics, direct uncapped `/api/v0/searches`, noisy mesh-health warnings when
no mesh peers are reachable, Lidarr 500s, and the host unit bypassing the image
entrypoint so live `umask` remains `0022`.

## 2026-05-15T23:50:00Z Session update

The live Docker host is running the cached all-tools manual image from revision
`983466e9cf`. Docker health is healthy, restart count is zero, the optional
media/tooling stack is present, and authenticated headless checks confirmed the
Browse and Search routes render without stale asset failures.

Fresh logs revealed an unsafe auto-retry local HashDb candidate path: same-size
same-extension files could be treated as alternates even when their filenames
were unrelated. The fix now requires local HashDb retry alternates to have the
same leaf filename as the failed transfer, and ADR-0001 documents this gotcha.

Next steps: commit and push the HashDb auto-retry fix, then build/deploy a
manual image from current HEAD if we want this auto-retry safety patch live
before the next tagged release.

## 2026-05-16T00:05:00Z Session update

The current-HEAD all-tools manual image was deployed to the live Docker host and
reports `0.0.0-manual.20260515235056.cb9940d973`. Container health is healthy,
restart count is zero, optional tools are present, and Soulseek returned to
Healthy after startup reconnect.

The post-deploy crawl exposed that direct `/api/v0/searches` still timed out.
The cap was present, but the database still needed to sort a large search table
by `StartedAt`. A live `IDX_Searches_StartedAt` index was applied manually and
made `limit=1`, `limit=50`, and default search-list calls return in
milliseconds. Local code now has the matching EF model index and migration.

Next steps: validate, commit, push, and rebuild/deploy once more if we want the
search-index migration baked into the manual image before release.

## 2026-05-18T00:00:00Z Session update

Added the reusable post-release package-channel validation scaffold. The new
`packaging/smoke/package-smoke` driver installs from public channels, records
evidence JSON/JUnit/logs, checks requested-version reporting, and exposes
adapters for release archives, direct packages, containers, package managers,
and slskdN packaging metadata surfaces.

GitLab now has a tag-only `post_release_validate` stage after promote for
container, Ubuntu, Fedora, AUR, Snap, and metadata surfaces; GitHub has a
disabled `workflow_dispatch` workflow that reuses the same driver when the
project intentionally enables it.

Next steps: run the GitLab validation stage against the next release tag and
tighten delayed/manual channel `allow_failure` settings after observing real
channel propagation.

## 2026-05-20T22:44:00Z Session update

The live Docker host is now running manual image
`slskdn:0.0.0-manual.20260520223825.98f7c84fff0c`, built from the local rescue
cooldown and Wishlist/Search UX changes without creating a release tag. The
container reports the matching version, Docker health is healthy, restart count
is zero, and `/health` remains in the existing degraded operational state.

The rescue retry storm found in the earlier log sample is quiet after the
startup pass. A 150-second post-restart sample contained no `[RESCUE]`, warning,
error, or fatal log entries.

## 2026-05-20T23:05:00Z Session update

Implemented the next rescue/matching cleanup locally. Rescue activation now
returns a concrete outcome, allowing the detector to use different retry
cooldowns for missing recording IDs, no overlay peers, guardrail denials,
multi-source unavailability, and unexpected failures. Expected no-job rescue
outcomes no longer emit warning-level logs.

Auto-replace network alternatives now require plausible filename token overlap
before ranking, so same-extension and similar-size unrelated tracks are not
accepted as replacement candidates. This local patch has not been deployed to
the live Docker host yet.

## 2026-05-20T23:07:00Z Session update

The rescue/matching cleanup is now deployed to the live Docker host as
`slskdn:0.0.0-manual.20260520230151.febcda05dbee`. The container reports the
matching version, Docker health is healthy, restart count is zero, and `/health`
continues to report the existing degraded operational state.

Fresh post-restart logs are clean after the initial rescue trigger. The latest
90-second sample showed no rescue repeats and no warning, error, or fatal log
entries.

## 2026-06-16T20:57:24Z Session update

The first main release tag for the current live-validated build published the
GitHub release assets and most primary channels, but the overall tag workflow
failed because the omnibus testers Docker image could not compile SongRec:
`bindgen` could not find `libclang.so`. COPR also failed at upload with a
non-JSON API response, and Chocolatey failed after repeated 504 Gateway Timeout
responses; public checks did not show those downstream packages accepted the
271 version.

The actionable release-blocking Docker issue is fixed locally: the optional
media-tools installer now installs `libclang-dev`, packaging metadata validation
guards that dependency, and a full local all-tools Docker build against the
published 271 base image completed successfully. Smoke checks inside the image
found `songrec`, `c2patool`, `audfprint.py`, and `panako.jar`.

Next steps: finish repo validation, commit/push the omnibus fix, then create a
replacement main release tag. Treat COPR and Chocolatey as external publisher
failures unless a retry or service recovery succeeds.

## 2026-06-16T21:54:13Z Session update

Replacement release `build-main-2026061621-slskdn.272` is cut and the GitHub
tag workflow completed with overall `success`. Release artifacts, main Docker,
omnibus testers Docker, PPA, AUR, Nix metadata, Homebrew, and Discord
announcement succeeded. Artifact verification passed for
`build-main-2026061621-slskdn.272`, including SHA256 checks and the Linux x64
binary version check.

COPR and Chocolatey are the remaining downstream publication gaps. COPR failed
during upload with a non-JSON API response from `copr-cli`; Chocolatey failed
after five 504 Gateway Timeout responses, and the public feed did not list the
272 package at verification time.

Next steps: retry or repair COPR and Chocolatey publishing separately when
those services/credentials are ready. No code release blocker remains from the
live-log or omnibus Docker validation work.

## 2026-07-11T18:34:00Z Session update

Manual validation deploy completed. The live validation host now runs
`0.0.0-manual.20260711182800.34113e245`; service, container, VPN, and Soulseek
connectivity are healthy with zero restarts. Authenticated headless validation
passed the 7-test core suite and a 19-route application sweep with no page
errors or HTTP 5xx responses. Rapid navigation exposed client-aborted requests
being reported as server failures; the cancellation handling fix and regression
tests were committed and deployed, and the final post-test log scan was clean.
No release tag was created.

Next steps: continue operator testing on the manual build and monitor normal
workload logs; the prior service definition and image remain available for
rollback.
## Update 2026-07-15 01:00:00Z

- Current task: GitHub maintenance and dependency PR integration in progress.
- Last activity:
  - Confirmed no open CodeQL, Dependabot security, or secret-scanning alerts.
  - Repaired scheduled browse/policy E2E failures and the Servarr readiness API import.
  - Preserved production web/network identity separation while making the guarded E2E announce path explicit.
- Validation:
  - Passed focused share-announcement unit tests, Web build/lint, and concurrent browse/policy Playwright tests (`4/4`).
- Next steps:
  1. Run full repository tests and lint, then commit and push the baseline repair.
  2. Validate and integrate the open dependency PRs, then close the answered roadmap issue.
  3. Do not create a release/build tag without explicit user authorization.
## Update 2026-07-15 01:15:00Z

- Current task: GitHub security and dependency maintenance complete.
- Last activity:
  - Integrated Dependabot PRs #257, #263, and #264 with required NuGet direct-reference and analyzer compatibility corrections.
  - Confirmed zero npm/NuGet vulnerabilities and zero open GitHub security alerts.
- Validation:
  - Passed full backend tests (`4675/4675`), Web tests (`765` passed, `4` skipped), Web build/lint, repository lint, warning-free Release build, and concurrent browse/policy E2E (`4/4`).
- Next steps:
  1. No security, CodeQL, dependency PR, or roadmap-issue maintenance remains.
  2. No release/build tag was created.
## Update 2026-07-15 01:30:00Z

- Current task: GitHub/GitLab main reconciliation complete.
- Last activity:
  - Merged the duplicate GitLab libclang history without replacing newer main content.
  - Hardened Prometheus HELP/TYPE metadata parsing against malformed collector output.
- Validation:
  - Passed focused Prometheus parsing tests (`6/6`) and diff checks.
- Next steps:
  1. Push the unified main tip to GitHub and GitLab.
  2. No release/build tag was created.
