# Changelog

All notable changes to slskdN are documented here. GitHub release pages use
[`scripts/generate-release-notes.sh`](../scripts/generate-release-notes.sh),
which prefers the matching version section below and otherwise falls back to
the commit delta since the previous release tag. Tagged releases must never
publish the rolling `## [Unreleased]` section.

Feature and fix work belongs in `## [Unreleased]` when the commit lands. When a
release is cut, move only the shipped bullets into the new versioned section so
each release note reflects the delta from the previous release.

Use headings in this form:

```markdown
## [<version>] — YYYY-MM-DD
```

For dev or build tags, use the same logical version string embedded in the tag.

---

## [Unreleased]

- SongID repeated-line scoring now scans CR/LF-delimited ranges directly,
  applies the existing trim and loose-text normalization contract, and updates
  normalized occurrence counts online instead of retaining split arrays, a
  complete normalized-line list, and LINQ groups. Exact raw-line normalization
  results are reused through a method-local cache capped at 256 entries, so
  repeated lyrics avoid repeated lower/replace/regex work without adding an
  input-sized cache for unique or hostile text. For 10,000 identical lines,
  warmed allocation falls from 3,143,312 to 728 bytes (>99.97%). For 10,000
  distinct lines it falls from 2,843,392 to 2,250,568 bytes (20.9%). CR/LF
  splitting, Unicode whitespace trimming, empty/normalization-empty filtering,
  loose-text aliases, ordinal normalized grouping, and occurrence-based ratios
  remain unchanged.
- SongID transcript synthetic-cue counting now executes its existing
  case-insensitive word-boundary regex through `Regex.Count` instead of
  materializing a `MatchCollection` and one `Match` per cue. For a transcript
  containing 40,000 recognized cues, warmed allocation falls from 10,968,992
  bytes to zero measured bytes. Case-insensitive matching, word boundaries,
  underscore behavior, composite `cover by ai` and `ai-made` alternatives,
  left-to-right alternative selection, empty input, and null runtime input
  remain unchanged.
- SongID lyrics/speech token counting now reuses the direct ASCII-letter and
  apostrophe run scanner instead of materializing a regex `Match` for every
  token. The counter still scans the original transcript independently from
  invariant-lowercased n-gram analysis, preserving Unicode boundary behavior.
  On a 30,000-token transcript, warmed allocation falls from 6,764,680 bytes
  to zero measured bytes. Uppercase/lowercase ASCII letters, apostrophe-only
  runs, digit and punctuation separators, non-ASCII boundaries, empty input,
  and null runtime input retain the exact `[a-zA-Z']+` count contract.
- SongID transcript repetition scoring now scans the invariant-lowercased text
  twice—first to preserve the six-token threshold, then to aggregate overlapping
  three-token range keys directly. It no longer creates regex `Match` objects,
  token strings, joined n-gram strings, complete n-gram lists, or LINQ grouping
  buffers. A 30,000-token transcript containing three repeated tokens falls
  from 13,552,656 to 288 isolated warmed allocated bytes (>99.997%) and remains
  below 16 KiB under broad-suite runtime noise. A 10,000-distinct-token fixture
  falls from 5,804,800 to 1,748,840 bytes (69.9%), with retained state
  proportional to distinct trigrams. ASCII-letter/apostrophe tokenization,
  invariant lowercasing, overlapping windows, the minimum-six-token boundary,
  occurrence-based repeated counts, and exact ordinal collision checks remain
  unchanged.
- SongID loose-text similarity now scans normalized token ranges into one
  exact membership table instead of allocating two split arrays, per-token
  strings, and separate LINQ intersection and union sets. A representative
  10,000-comparison corpus-style batch falls from 24,400,248 to 9,760,000
  warmed allocated bytes (60.0%). Comparing two 5,000-token inputs with a
  2,500-token overlap falls from 1,480,736 to 907,208 bytes (38.7%). Existing
  lowercase/feature/ampersand/punctuation normalization, ASCII token filtering,
  duplicate-token collapse, empty and exact-match handling, and exact Jaccard
  scores remain unchanged; hash collisions still require full ordinal token
  equality.
- Discovery Graph evidence summarization now scans edge lanes directly into
  case-insensitive per-lane accumulators and sorts only the distinct summaries,
  eliminating `SelectMany`/`GroupBy` buffers and repeated group enumeration.
  For 100,000 duplicate lane observations, warmed allocation falls from
  2,098,912 to 784 bytes (>99.96%), and retained aggregation memory is
  proportional to distinct lanes rather than observations. First-seen lane
  spelling, case-insensitive grouping, average-score rounding, minimum-one
  count contribution, formatted labels, singular/plural summaries, descending
  score order, label order, and first-seen stable ties remain unchanged.
- Taste recommendations without MusicBrainz IDs now write the exact legacy
  `text:creator:title:year` canonical form into one reusable 1,024-character
  stack buffer. Lookups hash and compare that span directly; only a newly
  retained work allocates its canonical key string. Hash buckets retain exact
  ordinal-ignore-case collision chains, and oversized or culture-expanded keys
  fall back to the original allocating builder. For 100,000 observations of one
  two-source text-only work, warmed allocation falls from 64,001,712 to 1,840
  bytes (>99.997%). A 10,000-distinct-work fixture stays below 3.7 MB while
  retaining the required key/group state. Invariant Unicode lowercasing,
  Unicode edge trimming, ASCII-space collapse, internal-tab distinction,
  component-boundary colon collisions, culture-sensitive year formatting,
  candidate identity, and newest representative selection remain unchanged.
- Taste-recommendation building now aggregates observations directly into
  per-work state, creates a case-insensitive actor set only after a second
  distinct source appears, and maintains a stable best-first list bounded by
  the requested result limit. MusicBrainz identifiers are grouped in their own
  case-insensitive map without allocating a prefixed key for every observation;
  reasons, sorted source lists, and recommendation DTOs are created only for
  returned results. For 100,000 observations of one two-source work, warmed
  allocation falls from 7,701,032 to 1,696 bytes (>99.97%). For 10,000 unique
  single-source candidates rejected by the anonymity threshold, allocation
  falls from 17,515,168 to 1,924,952 bytes (89.0%). Candidate counting, filters,
  external-ID and normalized-text grouping, newest representative selection,
  first-on-equal timestamp ties, source casing/order, score ordering, stable
  ties, and result limits remain unchanged.
- WorkRef security validation now shares one process-wide sensitive-pattern
  table instead of recreating the same nine-element array for every external-ID,
  metadata, title, and creator field checked. The pattern strings, evaluation
  order, case normalization, early-return behavior, UUID exemptions, and safe/
  unsafe decisions remain unchanged. Across 10,000 warmed validations of a
  representative safe music WorkRef, measured allocation falls from 3,840,000
  bytes to zero; the improvement also applies to federation publishing, taste
  recommendations, MusicBrainz radar, and overlay callers of the validator.
- Multi-source planning now caches successful allowed/banned reputation reads
  by exact ordinal Soulseek peer ID for the lifetime of one plan. Peer IDs are
  extracted with delimiter indexing, eliminating each candidate's `Split`
  array and unused path substring; failed reads are not cached, so a later
  candidate for the same peer still retries and remains fail-closed on its own
  failure. For 10,000 candidates from one peer, warmed allocation falls from
  7,910,048 to 2,620,064 bytes (66.9%) and reputation-store reads fall from
  10,000 to one (99.99%). For 10,000 unique peers, all 10,000 required reads
  remain and allocation still falls from 7,909,568 to 7,740,064 bytes (2.1%).
  Candidate order, same-peer path retention, banned-peer exclusion, blank peer
  handling, case sensitivity, and transient-error retry behavior remain intact.
- Multi-source planning now merges registry and backend candidates through a
  first-retaining `(backend, reference)` set instead of allocating formatted
  string keys and LINQ grouping state. Null and empty references retain their
  previous equivalence, registry candidates still win cross-source duplicates,
  and unique order is unchanged. Because moderation receives the same track ID
  for every candidate, each nonempty plan now performs one content-level check
  before per-Soulseek-peer reputation filtering; blocked, quarantined, and
  failed checks remain fail-closed, while empty candidate sets make no call. For
  10,000 unique local candidates, warmed allocation falls from 7,295,712 to
  2,218,032 bytes (69.6%) and moderation calls fall from 10,000 to one (99.99%).
  A 100,000-entry duplicate fixture retains one candidate with less than 32 KiB
  of planning allocation.
- Cross-provider search aggregation now consumes result sequences directly
  instead of first copying every result, removes its unused hash dictionary,
  and deduplicates ASCII filename/size keys with allocation-free ordinal-ignore-
  case comparison. Non-ASCII keys retain the exact legacy invariant-lowercase
  and ordinal comparison path, including Unicode folding distinctions; slash
  replacement, outer trimming, first-result retention, provider/reference
  merging, preferred-source selection, input order, and invalid-result skipping
  remain unchanged. For 100,000 unique mixed-case paths, warmed allocation falls
  from the combined original 21,764,680-byte path to 12,964,632 bytes (40.4%)
  while measured aggregation time remains approximately 39 ms. A lowercase
  no-case-change fixture still falls from 13,764,736 to 12,964,632 bytes (5.8%),
  and a 100,000-result duplicate fixture stays below 32 KiB instead of using
  raw input count as exact output capacity.
- IPLD graph construction now pre-sizes its node, path, and visited collections
  from direct root fan-out, capped at 4,096 entries so duplicate or cyclic link
  lists cannot force unbounded speculative reservation. Leaf nodes reuse the
  shared empty outgoing-link array instead of allocating an empty list. On the
  covered depth-two graph with 10,000 unique direct children, warmed allocation
  falls from the previous indexed baseline of 8,958,848 to 8,199,368 bytes
  (8.5%); it is 52.8% below the original double-hydration/full-scan path. A
  100,000-link duplicate-fan-out regression retains the complete root link
  snapshot while producing only two nodes and one path below 1.1 MB. Exact
  graph contents, depth-first order, duplicate-edge suppression, cycles, shared
  targets, and incoming/outgoing link snapshots remain unchanged.
- IPLD link insertion now maintains a reverse target index ordered by each
  source's original insertion position. Inbound queries and graph-node
  hydration scan only links for the requested target instead of applying an
  `Any` predicate across every outgoing source/list. Late links from an earlier
  source are inserted back into that source-order group, so unfiltered and
  name-filtered results retain the previous order and one-result-per-source
  behavior. On the covered 10,000-child graph, warmed construction allocation
  falls from the previous single-hydration baseline of 10,398,992 to 8,958,848
  bytes (13.8%); compared with the original double-hydration/full-scan path it
  is 48.4% lower. More importantly, inbound work changes from O(all graph
  sources + links) per target to O(links for target). Exact ordinal target/name
  matching, source order, duplicate-link collapse, late earlier-source links,
  graph nodes/paths, cycles, shared targets, and link validation remain intact.
- IPLD graph construction now passes the already-hydrated root and child nodes
  into recursive expansion. It no longer recreates the root before visiting its
  links or recreates every expanded child after that same node was added to the
  output, eliminating duplicate `ContentGraphNode`, outgoing-list copy, and
  inbound-list allocations. For the covered depth-two graph with 10,000 direct
  children, warmed allocation falls from 17,359,648 to 10,398,992 bytes (40.1%)
  while retaining all required 10,001 nodes and 10,000 paths. Depth boundaries,
  depth-first node/path order, duplicate-edge suppression through the visited
  set, cycles, shared targets, incoming/outgoing link contents, and root identity
  remain unchanged.
- Music-domain variant projection now applies the existing ordinal
  `VariantId ?? FlacKey ?? ""` deduplication while scanning recent HashDb rows
  and allocates a `MediaVariant` only for the first occurrence retained in the
  result. It no longer converts every row and builds `GroupBy` lookup/group/
  iterator state before discarding duplicates. For the covered 100,000-row
  input containing 10 distinct IDs, warmed allocation falls from 17,028,440 to
  4,600 bytes (>99.97%), and retained result memory scales with distinct output
  rather than input rows. First-occurrence order and object identity, ordinal
  case sensitivity, explicit empty IDs, null-to-FlacKey fallback, positive
  limit behavior, and the single bounded HashDb read remain unchanged.
- Shadow Index descriptor projection now selects the highest-quality/largest
  variant during one input scan, structurally deduplicates hash prefixes before
  ordering, sorts only each prefix's highest-ranked representative, and writes
  lowercase hex directly into its final strings. It no longer copies and sorts
  every variant or creates an uppercase string, lowercase string, and
  `ContentHash` for every duplicate before `Distinct`. For the covered 100,000
  variants sharing 10 prefixes, warmed allocation falls from 11,061,944 to
  4,880 bytes (>99.95%), and ordering work falls from O(variants log variants)
  to O(variants + distinct hashes log distinct hashes). Highest quality then
  size selection, stable ties, `NaN` ordering, null variants/prefixes, distinct
  hash order, size/codec/confidence output, and lowercase hash values remain
  unchanged.
- Descriptor publication now generates content versions by feeding bounded
  UTF-8 chunks directly into incremental SHA-256 and formatting the timestamp,
  eight-character lowercase digest prefix, and final version in stack buffers.
  It no longer materializes the complete interpolated content payload, UTF-8
  byte array, 32-byte digest array, dashed uppercase hex string, replacement,
  substring, lowercase copy, or intermediate final components. For the covered
  100,000-character ContentID, warmed allocation falls from 562,912 bytes to
  below 2 KiB (>99.6%) and working storage remains bounded. The exact legacy
  `ContentId:Codec:SizeBytes` UTF-8 payload, current-culture nullable number
  formatting, SHA-256 prefix, lowercase version shape, surrogate-pair encoding,
  and millisecond timestamp semantics remain unchanged.
- Descriptor batch publishing now drains its materialized descriptor input
  through exactly five long-lived workers and pre-sizes its required result
  list. It no longer creates one async task and semaphore waiter per descriptor
  before publication. For the covered 10,000 genuinely asynchronous publishes,
  isolated process-wide allocation falls from 33,285,480 to 27,487,104 bytes
  (17.4%), while coordinator task/state memory becomes O(5). Total/success/
  failure/skip counters, completion-order results, version/publication behavior,
  cancellation propagation, and the conservative five-call network concurrency
  limit remain unchanged.
- Descriptor batch retrieval now normalizes/deduplicates IDs in one direct pass,
  pre-sizes required lists, and drains them through exactly 10 long-lived async
  workers. It no longer creates one async task and semaphore waiter for every
  requested ContentID before work begins. For the covered 10,000-request batch,
  warmed whole-call allocation falls from 12,743,928 to 11,418,688 bytes (10.4%)
  despite retaining 10,000 required result objects and DHT/mock calls; scheduler
  task/state memory now scales with 10 rather than request count. Trimming,
  case-insensitive first-ID deduplication, requested/found/failed counts,
  completion-order results, cancellation stop behavior, error handling, and the
  existing 10-call network concurrency cap remain unchanged.
- Descriptor cache domain queries now parse `content:<domain>:<type>:<id>` keys
  through spans, retain only the newest entry per case-insensitive descriptor
  ID, and select the bounded newest `maxResults + 1` set with a worst-first
  priority queue. They no longer allocate regex match/group strings, fully sort
  every matching cache entry, create grouping objects, or copy the result when
  reporting `hasMore`. For 10,000 matching entries and 50 returned descriptors,
  warmed allocation falls from 9,025,920 to 1,759,304 bytes (80.5%), while the
  ordering heap is capped at 51. Exact domain/type and MusicBrainz normalization,
  expired removal, case-insensitive newest-per-ID deduplication, newest-first
  ordering/stable timestamp ties, clamped limits, and `hasMore` remain unchanged.
- Descriptor cache diagnostics now clean expired entries, count/size active
  entries, estimate descriptor payloads, and account clear operations through
  direct collection/list passes. They no longer buffer every expired key, take
  repeated `ConcurrentDictionary.Values` snapshots, scan active entries twice,
  or box hash-list enumerators through LINQ `Sum`. On the covered 10,000-entry
  mixed cache, cleanup plus statistics falls from 389,872 bytes to below 4 KiB
  (>98.9%); clearing 10,000 active entries falls from 87,840 bytes to below
  8 KiB (>90.6%). Expiry removal, one-time pass timestamps, active/cleared
  counts, exact byte estimates, clear results, logging, and weakly consistent
  concurrent diagnostics remain unchanged.
- Combine-all metadata merging now aggregates every output field in one stable
  source pass. It maintains ordered-distinct hash/perceptual-hash result lists
  with membership sets and accumulates maximum size, first nonblank codec, and
  average confidence directly, instead of creating source and descriptor copies
  and running six LINQ traversals with per-source `SelectMany` state. For the
  covered 100,000-source input, warmed allocation falls from 9,602,312 bytes to
  below 8 KiB (>99.9%). First ContentID, ordered distinctness, nullable maximum,
  codec selection, confidence arithmetic/order, exact source count, null/empty
  behavior, and returned descriptor shape remain unchanged.
- Metadata export checksum generation now writes the existing ordered
  `entries`/`links` JSON object through a reusable pooled buffer directly into
  incremental SHA-256. It no longer materializes the complete JSON as a UTF-16
  string and then copies it into a second UTF-8 byte array. For the covered
  10,000-entry package, warmed allocation falls from 19,947,096 to 561,008
  bytes (97.2%) and remains below 600 KiB. The exact legacy lowercase digest,
  property order/names, default JSON serialization, input enumeration order,
  entry/link contents, and package metadata contract remain unchanged.
- Metadata merge `PreferNewer` and `PreferHigherPriority` strategies now select
  the winning source in one stable pass while counting sources for diagnostics,
  instead of copying every source into a list and applying ordered selection.
  For the covered 100,000-source inputs, warmed allocation falls from 801,040
  bytes to below 4 KiB (>99.4%), working storage becomes constant, and runtime
  remains linear without ordering overhead. First-source tie behavior, later
  maxima, exact returned descriptor identity, full enumeration/diagnostic count,
  null and empty argument boundaries, custom fallback, and `CombineAll`
  materialization remain unchanged.
- Levenshtein fuzzy scoring now locates invariant-case shared prefixes and
  suffixes before allocating lowercase comparison strings, then normalizes only
  the unmatched middle slices. For the covered mixed-case inputs with a
  20,000-character shared prefix and one differing character, warmed allocation
  falls from 80,112 bytes to below 512 bytes (>99.3%). Inputs without shared
  affixes retain the existing full normalization and two rolling distance rows.
  Case-insensitive equality, edit distance, mixed-case prefix/suffix behavior,
  empty inputs, original-length score normalization, and exact scores remain
  unchanged.
- Soundex fuzzy scoring now scans input characters directly, applies invariant
  uppercase only to recognized letters, and builds the fixed four-character
  code in a stack buffer. It stops once the code is complete instead of
  uppercasing the entire input and copying every letter through a LINQ array and
  filtered string. For the covered 100,006-character pair, warmed allocation
  falls from 1,462,432 bytes to below 256 bytes (>99.98%). Existing whitespace
  handling, non-letter removal, first-letter retention, vowel/duplicate rules,
  zero padding, lowercase and punctuation behavior, exact/partial/no-match
  scores, and invariant casing remain unchanged.
- Jaccard fuzzy scoring now tokenizes each input in one direct scan and counts
  overlap by probing the smaller token set, deriving union cardinality from the
  two existing set sizes. It no longer creates a split array, per-token trim
  parameter arrays, LINQ iterator pipeline, or extra intersection and union
  sets. Across two covered 5,000-token inputs, warmed allocation falls from
  3,172,664 to 1,335,064 bytes (57.9%). Case-insensitive token identity,
  ASCII-space splitting, boundary punctuation trimming, internal punctuation,
  duplicate collapse, Unicode invariant casing, empty inputs, exact Jaccard
  scores, and title/artist combination remain unchanged.
- PCM extraction now decodes ffmpeg's buffered signed 16-bit little-endian
  output directly into the required normalized `float[]`. It no longer copies
  the complete stream into a new `byte[]` and then again into a `short[]`; for
  one million samples, measured conversion allocation is below 4.1 MB and
  consists only of the approximately 4 MB result payload, removing another
  approximately 4 MB of transient copies. Signed boundary values, normalization,
  sample order/count, odd trailing-byte truncation, empty-output errors, and the
  synchronous/async API results remain unchanged.
- Image pHash now samples, converts, and transforms only the 32 source pixels
  that contribute to its low-frequency result, using one 256-byte stack buffer
  instead of allocating a full-image grayscale array plus 8×8 downsample, DCT,
  and low-frequency arrays. For the covered 1024×1024 RGBA image, the warmed
  end-to-end call allocates less than 512 bytes instead of over 8 MiB for the
  grayscale payload alone. Nearest-neighbor source coordinates, luminance
  coefficients and arithmetic, alternating transform signs, sorted upper-median
  thresholding, exact numeric/hex hash, empty-input handling, and malformed RGBA
  behavior remain unchanged.
- Spectral hashing now computes each RMS window directly over the virtual
  decimated sample sequence instead of allocating and filling the complete
  downsampled signal before visiting it once. A one-second 44.1 kHz input no
  longer allocates the 11,025-float intermediate (roughly 44 KiB), and the
  complete warmed hash call remains below 256 allocated bytes. Decimated length,
  `floor(index × ratio)` source selection, window partitioning, arithmetic order,
  exact hash output, and downsampling similarity remain unchanged. Chromaprint
  retains a contiguous downsample buffer because its FFT requires one.
- Chromaprint hashing now precomputes its immutable 4,096-point Hann window and
  normal 11,025 Hz FFT-bin-to-chroma map once instead of rebuilding them for
  every hash. Typical calls remove roughly 40 KiB of table arrays plus 4,096
  cosine evaluations and about 2,048 logarithmic bin calculations. The warmed
  44.1 kHz end-to-end call remains below 220 KiB including MathNet FFT workspace.
  Lower nonstandard sample rates still build the correct per-rate bin map;
  downsampling, coefficients, bins, exact hashes, and similarity remain unchanged.
- Perceptual-hash Hamming distance now uses `BitOperations.PopCount`, allowing
  the runtime to select a hardware population-count instruction where supported,
  instead of shifting and branching once per set bit (up to 64 iterations).
  Distance range, symmetry, zero/one/half/all-bit results, similarity conversion,
  controller responses, and platform-independent fallback behavior remain
  unchanged. Ten thousand deterministic 64-bit pairs match an independent
  reference counter exactly.
- Spectral and Chromaprint hashing now keep their bounded feature/median buffers
  on the stack, and Chromaprint clears/reuses one 24-bin chroma vector across
  eight frames. This removes the remaining two spectral arrays and ten
  Chromaprint arrays per hash. The covered no-downsample spectral call now
  allocates less than 256 bytes. Feature order, median selection, threshold
  comparisons, exact hashes, FFT/chroma accumulation, and similarity behavior
  remain unchanged.
- Spectral perceptual hashing now computes each of its eight RMS-energy windows
  over read-only spans instead of allocating eight array slices whose combined
  payload equals the complete input. At the covered 11,025-sample boundary,
  roughly 44 KiB of copied float payload plus eight array headers are removed
  and the complete warmed hash call allocates less than 2 KiB. Window boundaries,
  arithmetic order, median thresholding, exact hash output, downsampling, and
  similarity behavior remain unchanged.
- Both connection-fingerprint services now format the retained 12-character ID
  and privacy-preserving IP hash directly from stack buffers. Admission no
  longer creates a full GUID string plus slice, IP address byte array, SHA-256
  byte array, full hex string, hex slice, and lowercase copy. Complete warmed
  admission allocates less than 2 KiB in each service. Lowercase GUID-prefix
  shape, exact IPv4/IPv6 SHA-256 prefix, fingerprint/event objects, logging,
  retention, and security semantics remain unchanged.
- The Common.Security connection-fingerprint service now uses the same bounded
  algorithms as DHT rendezvous diagnostics: direct dictionary queries,
  single-pass statistics and oldest-entry eviction, requested-size recent-event
  tails, and exact atomic event-log sizing. At production caps, recent reads
  allocate below 8 KiB, complete eviction below 32 KiB, and 1,000-result queries
  below 48 KiB; four concurrent producers retain/report exactly 10,000 events.
  Authentication events, per-fingerprint history, all counters/filters/orders,
  clear/reset behavior, logging, and best-effort concurrency remain unchanged.
- Filtered connection-fingerprint queries now enumerate concurrent-dictionary
  entries directly instead of allocating the `.Values` snapshot before their
  required filtering and stable descending timestamp sort. At the 1,000-result
  production cap, the complete warmed query allocates less than 48 KiB. IP,
  case-insensitive username, certificate, and `since` filters, descending
  timestamp order, result cardinality, and best-effort concurrent query
  semantics remain unchanged.
- Connection-event retention now maintains an exact atomic queue-size counter
  instead of evaluating `ConcurrentQueue.Count` after every enqueue, during
  trimming, and for statistics. Each event performs one increment and, only
  above the 10,000-event cap, one dequeue/decrement. Four concurrent producers
  adding 12,000 events retain and report exactly 10,000. Event ordering, cap,
  event contents, statistics shape, and best-effort concurrent reads remain
  unchanged.
- Recent connection-event retrieval now keeps a rolling tail sized to the
  requested result count instead of `Reverse()`-buffering the complete audit
  queue before taking the newest entries. With a full 10,000-event log and the
  default 100-event request, measured warmed allocation remains below 8 KiB and
  working memory scales with 100 rather than 10,000 events. Newest-first order,
  exact count bounds, empty/non-positive requests, event objects, log retention,
  and best-effort concurrent enumeration remain unchanged.
- Connection fingerprint retention now selects the oldest entry with a stable
  direct minimum scan when the 1,000-entry cap is reached instead of allocating
  a values snapshot and fully sorting it. Cap eviction falls from O(n log n) to
  O(n) with constant working memory; the covered complete replacement operation
  allocates less than 32 KiB. Oldest timestamp selection, first-enumerated tie
  behavior, cap size, event recording, logging, and best-effort concurrency
  remain unchanged.
- Connection fingerprint statistics now aggregate active/recent connections,
  unique identities, and security-event totals in one direct concurrent-
  dictionary pass instead of allocating both a values snapshot and a list,
  then traversing them five more times. At the 1,000-fingerprint production
  cap, measured warmed allocation remains below 8 KiB. Count definitions,
  case-sensitive username uniqueness, security-event locking, event-log size,
  and best-effort concurrent diagnostic behavior remain unchanged.
- DHT rendezvous mesh-search response enrichment now consumes the share
  repository's file-content mapping iterator lazily and stops at the first
  advertisable mapping instead of buffering every row. In the covered
  1,000-row best case, mapping rows read fall from 1,000 to one (99.9% fewer).
  If no mapping is advertisable, all rows are still checked without a list
  allocation and the first mapping remains the fallback. Search result limits,
  ordering, moderation preference, response shape, and network behavior remain
  unchanged.
- Virtual Soulfind canonical selection now uses a stable single-pass maximum
  instead of sorting and materializing the complete variant list. Selection
  complexity falls from O(n log n) to O(n) with constant working memory; the
  covered 10,000-variant input allocates less than 4 KiB during selection.
  FLAC/ALAC/AAC/MP3 priority, case-insensitive codec matching, quality ordering,
  unknown-codec handling, first-occurrence tie behavior, response fields, and
  shadow-index network access remain unchanged.
- MediaCore dashboard aggregation now shares one immutable content-registry
  snapshot between its registry and IPLD sections instead of reading registry
  statistics and every domain twice. For `D` domains, registry calls fall from
  `2 + 2D` to `1 + D` (50% fewer); the covered three-domain dashboard falls
  from eight calls to four. Standalone section endpoints still obtain fresh
  snapshots, while dashboard counts, type grouping, graph statistics,
  validation, and result shapes remain unchanged.
- Levenshtein fuzzy scoring now removes shared prefixes and suffixes through
  zero-copy spans before evaluating distance, and exact case-insensitive
  matches return before lowercase normalization. In the covered 20,001-character
  near-match with a 20,000-character shared prefix, distance-cell evaluations
  fall from 400,040,001 to one and measured call allocation stays below 128
  KiB. Exact distance, normalized scores, case behavior, shared-suffix behavior,
  and worst-case quadratic complexity remain unchanged.
- Levenshtein fuzzy scoring now keeps two distance rows sized to the shorter
  input instead of allocating the complete dynamic-programming matrix. At the
  covered 2,048-by-2,048 boundary, distance storage falls from 4,198,401
  integers to 4,098 (99.90% fewer) and total measured call allocation remains
  below 128 KiB. Exact edit distance, case-insensitive normalization, empty
  input behavior, and normalized similarity scores remain unchanged; runtime
  complexity remains quadratic.
- Fuzzy content searches now reuse usable descriptors within one candidate
  pass instead of retrieving the target descriptor for every comparison. For
  100 unique candidates, descriptor-retriever calls fall from 200 to 101
  (49.5% fewer); repeated candidate IDs reuse their descriptor while retaining
  duplicate result entries. Missing and failed retrievals remain retryable,
  and perceptual/text scoring, confidence ordering, thresholds, domain checks,
  and direct pair scoring remain unchanged.
- IPLD link validation now caches ContentID registration results for one
  validation run and checks each source once for orphan detection while still
  reporting every broken or orphaned link. For `L` links with `U` unique
  targets and `S` unique sources, registry checks fall from up to `2L` to at
  most `U + S`; 1,000 repeated links from one source to one target fall from
  2,000 checks to two (99.9% fewer). Link diagnostics and domain enumeration
  remain unchanged; registration is evaluated at each ContentID's first
  encounter for the duration of that run.
- Advanced discovery peer ranking now resolves uncached local metrics through
  one service batch and bounded 500-ID HashDb reads instead of awaiting one
  storage lookup per peer. Ranking 100 uncached peers falls from 100 serialized
  reads to one (99% fewer); 501 IDs use two reads. First-occurrence peer source,
  persisted/default metrics, cache reuse, null/exception fallbacks, ranking
  scores/order, and network behavior remain unchanged.
- Multi-source canonical download skip decisions now reuse the bounded HashDb
  best-variant query instead of loading and sorting every local variant for the
  recording. With 1,003 local rows, mapped rows fall from 1,003 to one (99.9%
  fewer). The local-quality threshold, minimum proposed improvement, missing
  recording/local behavior, and skip result remain unchanged.
- MediaCore variant-ID resolution now uses one bounded HashDb best-variant
  query when the identifier matches a recording rather than loading,
  deduplicating, and sorting every variant for that recording. With 1,003 rows,
  mapped return rows fall from 1,003 to one (99.9% fewer). Per-variant identity
  deduplication, quality/seen-count ordering, latest-row selection within a
  duplicate identity, exact recording-ID matching, direct FLAC-key precedence,
  and non-music fallback remain unchanged.
- Recipient collection GET authorization now resolves active direct-user and
  group-member access through one scalar SQLite query instead of hydrating the
  user's complete accessible-grant list. The query uses the collection grant
  index and composite group-membership key and hydrates zero grants. With 1,000
  unrelated accessible grants, authorization hydration falls from 1,000 rows
  to zero. Owner access, expiry, direct/group identity, lowercase group IDs,
  membership, malformed or unrelated grants, and not-found behavior remain
  unchanged.
- Share-ticket creation and share-token streaming now authorize content through
  one exact indexed collection-membership existence query instead of hydrating
  and scanning every collection item. A `(CollectionId, ContentId)` index is
  created for fresh and existing sharing databases. With 1,000 items,
  authorization hydration falls from 1,000 rows to zero, while case-sensitive
  content identity, collection scoping, token validation, resolver ordering,
  tickets, and not-found behavior remain unchanged.
- Single-grant get, backfill, and authenticated-manifest authorization now
  resolves one active grant by ID and queries group membership only for that
  grant instead of hydrating every accessible direct and group grant. With
  1,001 direct grants for a user, authorization hydration falls from 1,001
  rows to one (99.9% fewer); the grant GET also falls from two grant reads to
  one. Expiry, direct-user, group-member, malformed-group, not-found, download
  policy, and manifest behavior remain unchanged.
- Collection item updates now resolve the requested item through one untracked
  collection/ID lookup instead of hydrating and scanning the complete ordered
  collection. With 1,000 items, update lookup hydration falls from 1,000 rows
  to one (99.9% fewer). Collection ownership checks, wrong-collection and
  missing-item results, field normalization, update persistence, and response
  data remain unchanged.
- Peer-ID share-group member removal now targets one matching SQLite `rowid`
  through a bounded delete subquery instead of reading, hydrating, and then
  deleting the entity. Existing and missing paths fall from two commands to
  one (50% fewer), and hydration falls from one entity to zero. Exactly one
  legacy duplicate remains the removal boundary, while no-op and EF/provider
  exception behavior remain unchanged.
- Collection-item append now assigns the next ordinal and returns it through
  one parameterized SQLite `INSERT ... SELECT ... RETURNING` statement instead
  of querying the maximum ordinal before a tracked insert. Each append falls
  from two commands to one (50% fewer), with ordinal selection atomic with the
  write. Zero-based and sparse next-ordinal behavior, all persisted item
  fields, returned-entity identity, and EF constraint-exception boundaries
  remain unchanged.
- Atomic share-group member admission now retains the prior EF
  `DbUpdateException` boundary for database constraint failures, with the
  provider error available as its inner exception. This restores compatibility
  with the tracked-write path while keeping the one-command conditional insert.
- Share-group user and peer admission now uses one parameterized conditional
  SQLite insert instead of a duplicate query followed by an insert. New and
  duplicate admission fall from two commands to one (50% fewer), and the
  duplicate decision is atomic with the write. User-key and peer-key duplicate
  semantics, legacy peer rows whose user ID differs, backward-compatible peer
  user IDs, and missing-group foreign-key failures remain unchanged.
- Collection reordering now applies ordinals through transactional 400-item
  SQLite updates instead of hydrating the complete collection and tracking an
  update for every requested item. A 1,000-item reorder hydrates zero entities
  instead of 1,000 and uses three bounded update commands. Exact requested
  order, last-position duplicate handling for repository callers, unknown-ID
  filtering, untouched ordinals, and empty-input behavior remain unchanged.
- Collection, collection-item, share-grant, share-group, and user-member
  deletion now uses one key-targeted SQLite command instead of a lookup plus a
  tracked delete. Each operation falls from two commands to one (50% fewer)
  and entity hydration falls from one row to zero. Existing/missing return
  values, missing-member no-ops, and database cascades remain unchanged.
- Incoming collection announcements now replace prior collection items with
  one set-based SQLite delete inside the same explicit transaction as the new
  item, collection, and grant writes. With 1,000 prior items, replacement
  hydration falls from 1,000 entities to zero and the old-item removal is one
  command. Item order and fields, collection/grant updates, sender validation,
  and all-or-nothing replacement behavior remain unchanged.
- Wishlist ignored-result creation now resolves an existing
  item/peer/directory rule through the existing unique case-insensitive
  composite index instead of hydrating every ignored rule for that Wishlist
  item and comparing them in memory. With 1,001 rules, duplicate-check rule
  hydration falls from 1,001 rows to one (99.9% fewer). Parent existence,
  username/directory case-insensitivity, directory normalization, duplicate
  return behavior, and new-rule creation remain unchanged.
- Share-group member details now resolve all distinct peer-contact nicknames
  through bounded 500-ID contact queries in one DbContext instead of one
  context/query per peer-backed member. With 100 peer-backed members, contact
  reads and contexts fall from 100 to one (99% fewer); 501 distinct peer IDs
  use two bounded reads in the same context. Exact peer-ID matching, duplicate
  member handling, missing contacts, result ordering, and non-contact members
  remain unchanged.
- Share-manifest generation no longer resolves and loads every contact before
  returning owner contact fields that must remain empty: collection ownership
  stores an application user ID, not a resolvable peer ID. Each manifest read
  removes one complete contact-table query and all associated hydration; with
  10,000 contacts, contact rows loaded fall from 10,000 to zero. Share-group
  member nickname resolution remains unchanged because those records carry
  explicit peer IDs.
- Native Jobs listing now counts and pages a lightweight union of discography
  and label-crate scalar columns in HashDb instead of synchronously loading,
  JSON-deserializing, combining, sorting, and then truncating both complete job
  tables. Created/status composite indexes support common filtered pages, and
  the API's 100-row cap is enforced at the database boundary. With 100,000
  stored jobs, returned rows fall from 100,000 to 100 (99.9% fewer), while a
  five-run database-only median fell from 0.091 seconds to 0.033 seconds
  (63.7% faster) before the removed JSON parsing and application sorting.
  Existing type/status filters, sort aliases/directions, totals, progress
  fields, offsets, and unknown-filter behavior remain unchanged.
- Search page startup now uses the SignalR connection's initial history
  snapshot as its normal data source instead of requesting the same list over
  REST in parallel. The hub snapshot uses the same 500-search cap as the REST
  endpoint, while a failed hub connection still loads REST history and direct
  older-search URLs retain their scalar fallback. Normal list database reads
  and payloads fall from two to one (50% fewer); with 100,000 retained
  searches, hub snapshot rows fall from 100,000 to 500 (99.5% fewer).
- Download-request list reads now project per-request attempt counts and
  current-attempt IDs in SQLite, then hydrate only those current transfers
  instead of loading every historical attempt. A mixed-direction
  `(RequestId, Removed, RequestedAt DESC)` index replaces the redundant
  single-column request index and supports newest-active/fallback selection
  without a temporary sort. With 100,000 attempts across 5,000 requests,
  hydrated transfer rows fall from 100,000 to 5,000 (95% fewer), and a
  five-run synthetic SQLite median fell from 0.082 seconds to 0.021 seconds
  (74.4% faster). Request/state ordering, attempt counts, active-attempt
  preference, newest-removed fallback, and empty histories remain unchanged.
- Application startup now registers and applies the previously unreachable
  ordered download auto-retry index migration. Existing installations receive
  the partial `(Direction, EndedAt, Id)` index instead of retaining a full scan
  and temporary sort on each retry-candidate page.
- Swarm analytics and scheduling peer ranking now applies the canonical
  default cost formula and requested limit in SQLite, then reuses the C# cost
  function to finalize the bounded return order. At the normal 100-peer limit
  over 100,000 stored metrics, application hydration falls from 100,000 rows to
  100 (99.9% fewer); a five-run synthetic SQLite query median fell from 0.088
  seconds for full-row streaming to 0.044 seconds for bounded selection before
  also removing the full application-side sort. Case-insensitive first-row
  deduplication, tie order, cost clamping, and empty-limit behavior remain
  unchanged.
- Warm-cache access touches now update `last_accessed` directly instead of
  reading and hydrating the complete metadata row before rewriting it. Each
  touch falls from two SQLite commands and connections to one (50% fewer),
  while identifier normalization and missing-entry no-op behavior remain
  unchanged.
- Federated recommendation promotion now resolves a duplicate Wishlist seed
  through one untracked, case-insensitive exact lookup backed by an idempotent
  SQLite index instead of loading and tracking the complete Wishlist. With
  10,000 items, hydrated entities fall from 10,000 to one (99.99% fewer), while
  newest-duplicate selection and review-only seed behavior remain unchanged.
- Music metadata fallback matching now reads one bounded best-variant row for
  each of at most 256 recent recordings instead of materializing every variant
  recording ID and issuing a second hydration query. Local reads fall from two
  to one (50% fewer), and application memory falls from full-library recording
  IDs plus variants to at most 256 variants. Recent-recording selection,
  quality/seen-count ranking, case-insensitive IDs, and fallback matching are
  unchanged.
- MediaCore music-domain variant sampling now applies recording recency,
  per-recording quality ordering, duplicate filtering, and the requested limit
  in one HashDb query instead of materializing every recording ID and reading
  variants one recording at a time. At the default 100-item limit with one
  variant per recording, reads fall from 101 to one (99.01% fewer), and
  application memory no longer scales with the complete recording-ID set.
- Warm-cache capacity enforcement now calculates total size and cumulative
  oldest-unpinned reclamation inside one SQLite windowed delete instead of
  loading every entry and deleting metadata individually. Evicting 100 entries
  falls from 102 commands and connections to one (99.02% fewer). Pinned rows,
  least-recently-used ordering, exact capacity stopping, and the no-file-delete
  boundary remain unchanged.
- Warm-cache hint ingestion now sends the complete validated request through
  one service call and persists normalized popularity increments in bounded
  400-ID SQLite upserts. At the 100-hint endpoint maximum, writes and
  transactions fall from 100 to one (99.0% fewer). Duplicate inputs retain
  their full hit count, while an empty normalized batch performs no database
  work.
- Accessible share-grant resolution now loads relevant active candidates and
  resolves every distinct group membership through one set-based read instead
  of opening a new database context per group grant. With 100 valid group
  grants, database reads fall from 101 to two (98.02% fewer) and contexts from
  101 to one (99.01% fewer). Direct grants for other users, expired grants, and
  malformed group IDs are filtered without changing accessible results.
- Virtual Soulfind upgrade and orphan scans now query the newest verified copy
  only for unresolved files in each 250-file page, using a new local-file/time
  index instead of materializing the complete verified-copy table. With 10,000
  unresolved files and one million verified-copy rows, upgrade-analysis reads
  fall from 4,082 to 121 (97.04% fewer) and orphan-scan reads from 4,042 to 81
  (98.00% fewer). Memory is page-bounded, while newest-copy selection, output
  order, and inferred-track precedence remain unchanged.
- Virtual Soulfind release-gap analysis now hydrates tracks, release groups,
  artists, and copy states in bounded batches for each 250-release page instead
  of loading all four evidence sets per release. For 10,000 releases with ten
  tracks each, catalogue reads fall from 40,041 to 361 (99.10% fewer), while
  memory remains page-bounded. Release and track order, partial-release
  filtering, copy counts, and unknown-artist fallback remain unchanged.
- Virtual Soulfind upgrade analysis now hydrates distinct track metadata
  through bounded indexed batches after filtering each 250-file page, instead
  of querying one track per eligible file. For 10,000 eligible files with no
  verified-copy rows, catalogue reads fall from 10,042 to 82 (99.18% fewer),
  while memory remains page-bounded. Suggestion order, quality thresholds,
  verified-copy track resolution, and missing-track title fallback are
  unchanged.
- Virtual Soulfind library reconciliation now loads local-file and
  verified-copy presence through one indexed, 500-track catalogue projection
  instead of querying both states for every track. A 1,000-track release falls
  from 2,001 track/copy reads to three (99.85% fewer), while a full 250-track
  missing-copy page falls from 252 catalogue reads to three (98.81% fewer).
  Inferred files, files linked only through verification, missing-track order,
  copy counts, and network behavior remain unchanged.
- Native shared-library browser directory aggregation now indexes normalized
  paths, file counts, and immediate-child counts in one pass instead of
  rescanning every directory twice per returned child. In the 10,002-directory
  regression fixture producing 10,000 root entries, full-list traversals fall
  from 20,001 to one and directory visits from 200,050,002 to 10,002 (99.995%
  fewer), while duplicate records, normalized path matching, and sorting remain
  unchanged.
- Native shared-library search and browser pages now resolve their bounded file
  set once and query exact FLAC keys through one indexed HashDb batch. At the
  100-item endpoint maximum, local database reads fall from 100 to one (99.0%
  fewer), while cache population, per-file resolution isolation, and SHA-256
  fallback for missing evidence remain unchanged.
- Audio analyzer migration now keyset-pages 500 recording IDs, batch-loads
  variants, and updates only recalculated analysis fields in transactional
  100-row commands. For 10,000 recordings with three stale variants each,
  database commands fall from 40,001 to 341 (99.1% fewer), write transactions
  fall from 30,000 to 20, and memory remains page-bounded.
- MusicBrainz discography and Library Bloom promotions now persist Wishlist
  seeds through one bounded bulk call. A 1,000-track discography promotion falls
  from 1,000 insert commands to 25 (97.5% fewer); the Bloom maximum of 250 falls
  from 250 to seven (97.2% fewer). Bloom promotion also reads Wishlist once
  instead of twice, and 250 suggestions against 10,000 existing items fall from
  up to 2.5 million prefix comparisons to 250 hash-set lookups.
- Lidarr wanted synchronization now groups each fetched page into one Wishlist
  persistence call, while Wishlist and CSV imports use 40-row SQLite inserts.
  At the default 100-item Lidarr cap, inserts fall from 100 commands and 100
  transactions to three commands and one transaction (97.0% and 99.0% fewer).
  A 100-track CSV import also falls from 100 insert commands to three.
- Canonical audio candidate ranking now reuses its loaded variants, reads all
  stored profile stats once, and batches missing-stat persistence. With 100
  missing profiles it falls from 301 SQLite commands to three (99.0% fewer).
  Full recomputation now keyset-pages 500 recording IDs and batch-loads their
  variants; 10,000 recordings with three profiles each fall from 70,001
  commands to 341 (99.5% fewer) without full-library variant materialization.
- MusicBrainz discography coverage now batches cached album targets, release
  tracks, and recording-hash evidence before assembling the unchanged response.
  A cached 100-release collection with ten tracks each falls from 1,200 SQLite
  commands to four (99.7% fewer), while cache misses remain sequential and
  Wishlist fallback uses indexed membership instead of per-track prefix scans.
- Discography and label-crate job reads now derive child aggregates in one pass
  and skip the parent upsert when totals and status are unchanged. Steady-state
  polling falls from three database operations to two (33.3% fewer) and avoids
  a write lock; a 10,000-release aggregate falls from four list passes to one.
- HashDb history backfill now flattens each retained search page into one
  existing bounded inventory/peer ingestion transaction. With one FLAC response
  per search, the default 50-search page falls from 100 database commands to
  two (98.0% fewer); the maximum 500-search page falls from 1,000 to six
  (99.4% fewer), while empty searches still advance durable progress.
- Library Health scans now retain per-file in-memory progress while persisting
  durable checkpoints every 100 files plus initial, final, and failure states.
  A 201-file scan falls from 203 status writes to four (98.0% fewer); a
  100,000-file scan falls from 100,002 writes to 1,002 (99.0% fewer).
- Discography and label-crate release status lists now normalize composite job
  keys before writing and use 100-row SQLite upserts inside their existing
  transactions. A 202-row workload falls from 202 database commands to three
  (98.5% fewer), while later duplicate statuses still win across batches.
- HashDb statistics now aggregate total/capable peers, total/known FLAC
  inventory, and stored hashes with one SQLite command and one scan per table.
  Each dashboard or mesh-hello snapshot falls from five database commands to
  one (80% fewer) and from five table scans to three (40% fewer).
- Album-target persistence now normalizes track metadata once and replaces the
  list with 100-row SQLite inserts inside its existing transaction. A typical
  12-track album falls from 14 database commands to three (78.6% fewer); a
  202-row large replacement falls from 204 commands to five (97.5% fewer),
  while zero-position fallback and later-duplicate-wins behavior remain intact.
- HashDb mesh merge now checks exact FLAC keys and variant aliases in bounded
  500-key indexed reads, preserves local conflicts and input duplicate
  semantics, and inserts new entries in transactional 100-row commands. At the
  1,000-entry sync cap, an all-new merge falls from 2,000 database commands to
  12 (99.4% fewer); an all-existing merge falls from 1,000 to two (99.8% fewer).
- HashDb peer activity and capability updates now normalize identifiers once
  and create-or-update with one atomic SQLite command. Peer search/download
  events fall from three-to-five database commands to one (66.7–80% fewer),
  while existing capabilities, versions, and backfill counters are preserved.
- Passive FLAC discovery now writes inventory in 100-row SQLite batches, while
  history backfill also inserts 500 distinct peers per command in the same
  transaction. Persisting 100 live FLAC results falls from 100 commands to one;
  100 historical one-file responses fall from 200–400 commands to two
  (99.0–99.5% fewer), without changing Soulseek traffic or probe scheduling.
- Library Health remediation now persists its job linkage, reads requested and
  active-job issues directly, and changes their state with bounded set-based
  updates. At 100 issues, job creation falls from 102 database operations to
  two and completion falls from 101 to two (98.0% fewer in both cases); linked
  jobs are no longer hidden by the generic 100-issue default page.
- Virtual Soulfind music tag matching now batches exact album-track candidates
  and the bounded 256-recording variant fallback. Across 100 albums, exact
  matches fall from up to 102 database reads to three; a full fallback miss
  falls from up to 358 reads to four (97.1% and 98.9% fewer).
- Recent Virtual Soulfind music enumeration now uses a schema-v22 album-recency
  index, hydrates at most the requested track count, and batches advertisable
  presence. The default 50-item request falls from 52–101 database queries to
  two (96.2–98.0% fewer).
- Virtual Soulfind music recording-ID resolution now uses a schema-v21
  case-insensitive album-track index instead of loading the album catalog and
  querying every release. A successful lookup across 100 albums falls from up
  to 103 database queries to two (98.1% fewer), and does not hydrate variants.
- MusicBrainz album completion now batches both release tracks and full hash
  evidence through existing SQLite indexes before assembling its unchanged
  response. A 100-album collection with ten tracks each falls from 1,101
  database queries to four (99.6% fewer).
- Library Bloom now loads album tracks through bounded indexed release batches
  and indexes held recording IDs before testing track membership. A 100-release
  operation falls from 101 database queries to two, while a 10,000-by-10,000
  membership pass falls from up to 100 million comparisons to 10,000 hash-set
  lookups.
- SignalBus now admits distinct incoming signal IDs concurrently with atomic
  cache operations while preserving duplicate, expiry, and cancellation
  behavior. A 100,000-signal burst avoids 200,000 global semaphore operations,
  and concurrent copies of one ID still produce exactly one delivery.
- Source Discovery now streams returned files into 100-row SQLite UPSERT
  commands inside the existing transaction. Persisting 100,000 results falls
  from 100,000 commands to 1,000 without changing Soulseek search traffic, and
  post-commit hash-verification failures no longer attempt an invalid rollback.
- Library Health now performs release completeness analysis once per release
  directory after the file scan and checks recording presence with one indexed
  batch query. A ten-track release containing ten files falls from 120 database
  reads to three, while retaining the existing conservative hash-presence
  semantics and scan concurrency bound.
- Scheduled incomplete/download retention now streams recursive directory
  entries and counts outcomes during the destructive pass. Each candidate is
  resolved and age-checked once instead of three times, removing 200,000
  repeated filesystem checks and a 100,000-entry filename array at that scale.
- Virtual Soulfind intent batches now claim loaded pending records atomically
  instead of fetching every same intent again. A full default batch falls from
  11 intent reads to one, concurrent/manual processors cannot duplicate a
  claim, and normal info-level cycles no longer scan the queue twice solely to
  prepare a suppressed debug message.
- Library Health scan status checks now stop while the document is hidden and
  resume against the original one-minute deadline. Slow requests remain capped
  at one in flight instead of accumulating on a fixed two-second interval, and
  a fully hidden minute produces zero status requests instead of up to 30.
- Wishlist “mark all viewed” now applies its timestamp with one predicate
  update. Database commands and managed memory no longer scale with the number
  of unread Wishlist items.
- Pod deletion now preserves its parent-existence guard and removes each child
  table with one set-based command inside the transaction. Deletion memory and
  command count no longer scale with retained message, member, or membership
  history rows.
- Share scan completion now selects advertisable content IDs with one indexed
  join per repository while retaining blocked and quarantined file filtering.
  A 100,000-file repository falls from 100,001 SQL queries plus full file
  hydration to one projected query before hint enqueueing.
- Wishlist and Auto-Replace now poll response-free search state at one- and
  two-second cadences, respectively, and hydrate responses once after
  completion. At their timeout bounds, repeated database reads fall from 40 to
  21 and from 45 to 24 while full-payload hydrations fall to one.
- Share content-peer hints retain their conservative one-second publication
  pacing but now deduplicate pending IDs and update the shared reverse index
  once per 32-ID batch. Publishing 1,000 IDs falls from 3,000 DHT operations to
  1,064 while preserving later TTL refreshes.
- Conservative FLAC backfill scheduling now loads daily counters for its
  bounded peer set in one case-insensitive query and reuses them while applying
  per-peer limits. A full ten-candidate cycle falls from 21 database queries to
  two without increasing peer traffic.
- Periodic Pod discovery refresh now queries one listed-only snapshot and
  batches the shared DHT index read/write around all successful metadata
  publications. A 100-Pod cycle falls from 101 database queries and 200 DHT
  operations to one query and 102 operations, and the index TTL is renewed even
  when membership is unchanged.
- Mesh bootstrap now performs one initial self-descriptor publication and
  completes, leaving configured periodic and IP-change refresh ownership to
  `PeerDescriptorRefreshService`. Under defaults this removes 48 duplicate DHT
  writes and up to 144 active STUN probes per day.
- Search retention, legacy age pruning, and manual completed-history clearing
  now select at most 250 response-free summaries and delete each page with one
  database command while preserving every existing live deletion notification.
  A 10,000-row cleanup falls from 10,000 delete transactions to 40.
- Automatic search retention now honors `cleanup_interval_seconds`, suppresses
  overlapping runs, and retries failures at the next five-minute evaluation.
  Under the one-day default, policy database evaluations fall from 288 per day
  to one after the immediate startup run.
- Shadow Index publishing now advances through indexed normalized recording-ID
  pages instead of loading the full library and repeatedly selecting one fixed
  newest batch. Its candidate count is clamped to the immediate DHT write
  budget, and Virtual Soulfind now honors the configured DHT operation limit.
- Download auto-retry now streams a minimal, indexed oldest-first candidate
  sequence and stops database enumeration once its bounded global/per-peer plan
  cannot change, instead of materializing every retained failed download each
  minute.
- System Bridge keeps its ten-second visible dashboard cadence but stops while
  hidden, coalesces slow requests, suppresses uptime-only rerenders, and retries
  failed initial config hydration. The synced-lyrics pane now follows native
  media and seek events without an additional 500 ms timer.
- Security dashboard statistics now use single-pass retained-set aggregation
  across event, reputation, violation, canary, network, reconnaissance,
  honeypot, consensus, verification, disclosure, and temporal collectors.
  System Security polling stops while hidden, rejects overlap, preserves
  unchanged or last-successful data, and correctly renders and switches its
  active dynamic pane.
- Mesh diagnostic reads now expose the cached NAT type without launching STUN
  probes from dashboards, Network snapshots, or health checks. System Mesh
  stats polling stops while hidden, rejects overlap, survives Strict Mode
  replay, and retains unchanged or last-successful data; the polling lifecycle
  gate now checks setup/cleanup symmetry across every covered System panel.
- Compact listen-along panels no longer request the unrendered global radio
  directory. Full panels use visible-only, non-overlapping one-minute polling,
  and the singleton backend coalesces callers onto one shared DHT directory
  hydration per minute while preserving the last successful browser result.
- Library Health dashboard hydration now uses one database-backed snapshot for
  summary, type, artist, and bounded issue details. Legacy aggregate endpoints
  also count the complete filtered set, recent pages use a recency index, and
  public page limits are explicitly bounded.
- Lidarr dashboard status polling now stops in hidden documents, refreshes on
  visibility restoration, rejects overlap, reuses a short-lived external
  status snapshot across rapid remounts, and skips unchanged state updates.
- Search download-history ranking now aggregates counts, successful bytes, and
  last-download timestamps inside SQLite instead of materializing every
  retained download. Concurrent and rapid detail loads reuse one short-lived
  client result.
- Search results now batch cached group metadata for visible users, reuse
  response-provided speed, queue, and slot fields, and defer reputation and
  opinion hydration until interaction instead of contacting every peer for
  duplicate user information during initial rendering.
- MediaCore ContentID stats and domain/type queries now read maintained
  secondary indexes instead of rescanning and reparsing all mappings. Empty
  reverse buckets are removed on remap, while the System MediaCore stats poll
  is visible-only, non-overlapping, and skips unchanged renders.
- Search progress now remains live on a bounded one-second hub cadence without
  persisting incomplete response rows. The detail route hydrates response
  payloads only at completion or when early mesh data is durable, clears reused
  route state, and preserves search source/wishlist provenance.
- Browse progress now polls once per second without overlapping requests,
  suppresses unchanged state updates, stops before starting in hidden tabs, and
  catches up immediately when the document becomes visible.
- Swarm Analytics now replaces five polling requests and four complete
  peer-ranking passes with one dashboard snapshot, preserves cached results
  across transient failures, rejects stale or overlapping work, and suspends
  polling in hidden documents.
- Port Forwarding now defers secondary-tab hydration, returns a bounded
  available-port preview with full counts, pauses non-overlapping status polling
  while hidden, and renders authoritative stream/performance statistics instead
  of fabricated values.
- Messaging V2's active room/Pod member rail now uses a non-overlapping,
  visible-only ten-second cadence and preserves cached members on transient
  failures. Pod member responses reuse authorization snapshots and aggregate
  membership timestamps in SQLite instead of materializing retained events.
- Footer transfer telemetry now reads projected active transfer fields and
  grouped byte totals rather than loading every retained transfer. Both global
  footer polling cadences stop in hidden documents and catch up immediately on
  visibility restoration.
- Jobs and swarm visualization polling now keeps two-second live status while
  reading and aggregating full trace histories every ten seconds. Unchanged,
  overlapping, hidden, and transiently failed work no longer replaces or
  rerenders the current view.
- The Downloads/Uploads page now seeds only actionable transfers, then follows
  indexed `UpdatedAt` deltas and loads successful history in stable 250-record
  pages only when users reveal or request it. Server-provided totals keep tab
  counts accurate without transferring the complete history. The page merges
  only changed or removed records, suppresses overlapping and hidden work, and
  catches up on visibility; removal events retain stable request identity.
- Private-chat polling now uses an overlapping timestamp cursor and bounded
  client cache instead of repeatedly transferring the latest 100 messages.
  Legacy chat also suppresses overlapping work and hidden-tab polling;
  server-side timeline reads use a composite username/timestamp index, unread
  totals cover the full conversation, and ISO chat/room timestamps sort
  correctly.
- The legacy Pods route now reuses complete list metadata, incrementally merges
  channel messages, polls slow metadata every sixty seconds, suspends hidden
  work, and correctly hydrates direct Pod channel URLs.
- Unified Messaging and the Rooms route now poll room messages through an
  overlapping timestamp cursor, merge stable message identities into bounded
  caches, and avoid retransferring the retained room history. Rooms membership
  uses a separate cadence; overlapping, unchanged, and hidden work is
  suppressed.
- Active Pod streams now request only messages newer than their latest retained
  cursor, merge a bounded local cache by stable message identity, pause polling
  while hidden, and prevent overlapping slow refreshes.
- Messaging V2 no longer refetches details for every saved pod during
  hydration; slow-changing pod metadata uses a separate sixty-second cadence,
  overlapping and hidden-tab polling is suppressed, and unchanged lists avoid
  rerendering the workspace.
- Conversation-list unread counts now use one indexed SQLite projection instead
  of loading every unread private-message row and rescanning it per
  conversation.
- Footer and System Network aggregate status now uses one bounded server
  snapshot per consumer instead of browser-side request fan-out. Network polling
  runs every ten seconds, pauses while hidden, rejects overlap, and correctly
  renders normalized peer and swarm-job response shapes.
- Global direct-message navigation polling now uses one indexed scalar activity
  request instead of loading and aggregating all unacknowledged message and
  active conversation rows every ten seconds.
- Global room-activity badges now use one bounded timestamp-summary request
  instead of fetching every joined room's retained message list, and navigation
  polling pauses in hidden tabs and cannot overlap a slow prior request.
- Footer polling now keeps two-second transfer-speed updates while refreshing
  aggregate network statistics every ten seconds without overlapping requests,
  and HashDb peer-capability counts use a covering index instead of a full scan.
- GitLab CI configuration now keeps the Arch package-smoke sudoers command as
  a string scalar, and performance-test dependencies align with the runtime
  graph so GitLab pipeline creation and GitHub dependency submission succeed.
- Share manifests now send reusable share tokens in a request header, and
  shared-content playback exchanges them for short-lived, content-bound stream
  tickets instead of exposing long-lived secrets in URLs.
- Request-log query enrichment now redacts authentication and other sensitive
  parameter values before they reach Serilog sinks.
- Removed obsolete upstream synchronization/release workflows under the fork's
  license rollback policy, and updated the workflow guard to keep them removed.
- Coverage baseline metadata now states that its thresholds are aspirational
  and not enforced by the current CI or release gates.
- GitLab now creates pipelines only for tags, matching the repository's
  tag-only build policy and preventing failing `main` push pipelines and their
  notification emails.
- Search-request filter documentation now follows its renamed parameter, so
  Release builds no longer emit CS1734 for the helper.
- Incoming search-request filtering now applies a per-match regex timeout, so a
  pathological configured search filter combined with a crafted peer query string
  can no longer stall the search-response resolver via catastrophic backtracking.
- Blacklist username-pattern matching now applies a per-match regex timeout so a
  pathological configured pattern combined with a crafted peer username cannot
  stall a request thread through catastrophic backtracking (ReDoS).
- Swarm performance analysis now treats an empty peer set as a neutral result
  instead of averaging an empty sequence, which was swallowed as a spurious
  "analysis failed" error on every analysis of a new or idle swarm.
- Adaptive chunk scheduling no longer throws when a peer's only recent chunk
  completions all failed; the recent-performance score now treats an empty set
  of successful transfers as the worst duration score instead of crashing the
  assignment path.
- Mesh preview producers now retain ownership of their pipe writer until a
  single final completion, so hash mismatches and peer failures return clean
  end-of-stream responses instead of intermittently leaving readers pending.
- The Gold Star Club auto-join, mesh bootstrap, and MediaCore content-publisher
  background services now contain non-cancellation errors instead of letting them
  escape ExecuteAsync, which under the default host behavior would stop the entire
  application on a transient pod-store, DHT, or publish failure.
- Prometheus metrics responses now skip malformed HELP/TYPE metadata lines
  instead of failing the entire response.
- The metrics object endpoint no longer throws on malformed Prometheus exposition
  text (a HELP line with no following TYPE line, or an over-short header line),
  skipping unparseable metric families instead of failing the whole response.
- Multi-source chunk calculation now guards against a non-positive chunk size,
  which previously could spin forever and exhaust memory instead of downloading.
- Streaming responses now forward asynchronous reads to the underlying stream
  instead of blocking a thread-pool thread per concurrent stream, and the
  content-locator fallback miss cache is bounded and evicts expired entries.
- Updated the supported Web build/test toolchain, Node type definitions, and
  nonbreaking runtime dependencies; clean npm and NuGet scans report no known
  vulnerabilities.
- Scheduled browser policy checks now isolate each node's Web root, avoid
  cross-worker asset-copy races, bind test-only share announcements to the
  authenticated recipient, and reliably exercise modal controls above the player.
- "Shared with Me" now bypasses stale browser responses for newly announced
  grants, while production Soulseek identities remain separate from web accounts.
- The Servarr readiness action now calls the imported Lidarr API client instead
  of an undefined browser symbol.
- Wishlist results can now persistently ignore one peer folder for one saved
  search without blocking the peer. Ignored folders are excluded from display,
  hit counts, album candidates, and auto-download selection, remain reversible,
  and filters support quoted phrase exclusions for recurring title collisions.
- Client-aborted HTTP requests no longer produce duplicate security-middleware
  error logs or false security events during normal browser navigation.
- Privacy batching now releases every mesh and pod caller with its matching
  payload on timeout, size, flush, or shutdown and propagates cancellation.
- Pre-validation UDP limits now aggregate spoofable endpoints by IPv4 /24 or
  IPv6 /64, cap and expire buckets, and switch to peer IDs only after signature validation.
- Mesh rate-limit, work-budget, and discovery identity caches now expire and
  cap entries, while unauthenticated claimed peer IDs share one admission bucket.
- Remote DHT writes now bind signatures to authenticated self-certifying peers,
  enforce expiring global/per-peer/namespace quotas, and reclaim expired keys.
- Mesh service descriptors now require cryptographic verification of canonical
  descriptor bytes against keys authenticated by self-certifying peer records.
- JWT logout revocations now persist atomically across service restarts and
  expire only after the original token lifetime plus validation clock skew.
- Collections, share groups, and share grants now use the authenticated web
  account as owner/access identity; daemon Soulseek and mesh identities remain
  separate network principals and cannot collapse multiple web users together.
- Direct QUIC clients now reject empty or unknown certificate pin sets, use
  descriptor and endpoint-configured SPKI pins, and never establish or rotate
  trust merely because a certificate is self-signed.
- Path containment now resolves existing symbolic-link components, and relay,
  mesh, and multi-source output files use no-follow, directory-handle-relative
  opens on Linux so symlinks cannot escape approved roots or win path-swap races.
- Pod APIs now derive peer identity from authenticated web claims, require
  active pod membership for private reads, require owner/moderator membership
  for mutations, bind signed/sent records to that identity, and reserve
  internal PodCore maintenance surfaces for administrators.
- Shared-port QUIC proxying now admits only minimum-size supported-version
  Initial packets, bounds pending sessions globally and by network prefix,
  expires unvalidated state after ten seconds, and requires return-path proof
  before granting the normal idle lifetime.
- QUIC TCP relay commands now require a shared authentication token, exact
  public-destination allowlisting, and concurrency, duration, and byte quotas;
  unauthenticated, internal, loopback, and unapproved destinations are denied.
- QUIC overlay certificate pin mismatches now reject inbound and outbound
  connections instead of replacing trust automatically; pin rotation is an
  explicit administrator-only API operation.
- ActivityPub HTTP signatures now require unique signed `(request-target)`,
  `host`, and `date` or `(created)` fields, plus `digest` for request bodies;
  signed creation timestamps receive the same five-minute freshness check.
- Friends-only ActivityPub actor discovery no longer trusts `Origin` or
  `Referer`; non-loopback access requires a verified HTTP-signature key resolved
  to an actor on an approved peer.
- Library Health now admits one scan globally with duplicate `409` rejection,
  caps file processing at eight bounded workers without task-per-file fan-out,
  and restricts scan controls and path-bearing results to administrators.
- ActivityPub inbox writes now reject oversized JSON and transactionally enforce
  30-day, 1,000-entry, and 64 MiB per-actor retention limits.
- Warm-cache hint submissions are capped at 100 bounded identifiers, processed
  through four workers and a bounded channel, and protected by a caller-aware
  request rate partition.
- The optional legacy bridge now binds to loopback and requires authentication
  by default, rejects insecure non-loopback configuration, redacts its password,
  compares credentials in constant time, and enforces per-client request and
  transfer quotas.
- Non-wildcard scoped API keys now fail closed on endpoints without an explicit
  `[RequireScope]` mapping; wildcard keys and administrator JWTs remain
  universal.
- Failed logins no longer create a global administrator-username lockout from
  rotating IPs; credential throttles are scoped to username plus source and
  lockouts are reduced from one hour to five minutes.
- MeshContent now rejects explicit and open-ended ranges above 32 MiB before
  allocating response buffers; callers continue large transfers through the
  existing bounded chunk loop.
- The Web lockfile now resolves `undici` `7.28.0` through jsdom, clearing the
  high-severity advisories affecting the previous `7.25.0` test dependency.
- SQLitePCLRaw's native SQLite bundle is pinned to `3.0.3`, replacing the
  vulnerable `2.1.11` native library in production and vendored example graphs.
- All configured webhook header values are now redacted from options responses,
  including authorization, API-key, and operator-defined headers.
- Synthetic internal event dispatch is now administrator-only, restricted to
  explicitly supported sample event types and bounded disambiguators, and has
  a dedicated rate-limit partition before authenticated API exemptions.
- Historical and live application logs now require the administrator role on
  both the REST controller and SignalR hub.
- Stored ActivityPub inbox contents are now restricted to administrators while
  signed remote inbox delivery remains publicly reachable as required by the
  federation protocol.
- Generated TLS certificate bundles are now written through mode-restricted,
  flushed sibling files and atomically renamed, so private key material is
  never created with a transient umask-derived permission window.
- Sharing token signing keys are now treated as secret options and are redacted
  from read-access configuration responses; a recursive option-schema test now
  rejects credential properties that omit the secret marker.
- VPN-required installs now package the Gluetun-compatible VPN status helper as
  a dependency of both supported app service names, preventing a healthy app
  container from staying Soulseek-offline after the helper is stopped. Message
  acknowledgement endpoints now return 503 while Soulseek is disconnected or
  logging in instead of surfacing runtime exceptions.
- The optional Docker media-tools installer now includes `libclang-dev`, and
  packaging validation guards it, so the omnibus testers image can compile
  current SongRec/bindgen dependencies during release publishing.
- Event retention pruning now deletes expired rows with a set-based database
  command instead of materializing serialized event payloads into application
  memory, avoiding startup prune `OutOfMemoryException` failures on large event
  histories. Successful second-chance transfer fallback diagnostics are again
  Info/Debug instead of warning-level noise.
- HashDb completed-download ingestion now skips non-audio sidecars such as PDF
  booklets before hashing, fingerprinting, or deriving audio variant metadata,
  keeping album extras out of audio-only warning paths.
- VPN-required Soulseek disconnect teardown now treats intentional VPN
  disconnect exceptions and expected Soulseek read-loop shutdown races as
  observed/expected instead of fatal unobserved task exceptions. Vendored
  search cleanup no longer disposes the active response lock while late peer
  search responses can still be unwinding.
- Open Dependabot NuGet/npm updates are applied directly to `main`, including
  `esbuild` `0.28.1`, `Serilog.Sinks.Grafana.Loki` `9.0.0`, `YamlDotNet`
  `18.0.0`, `react-window` `2.2.7`, `react-router-dom` `7.18.0`,
  `AWSSDK.S3` `4.0.24.5`, Microsoft package alignment on `10.0.9`, and npm
  transitive audit cleanup. The Web transfers virtual list now uses the
  `react-window` v2 API, optional Loki
  logging skips sink construction when no URI is configured, and the player
  import test fixture no longer ages out of the default 30-day stats range.
- Vendored slskNet.Runtime is synced with the standalone runtime security and
  dependency updates, including the Vite/npm Web example, CodeQL remediation,
  and legacy peer path-encoding support for Windows-1251/Cyrillic browse and
  download paths.
- MessagePack is updated to `3.1.7`, clearing the current high-severity
  advisory reported for `3.1.4`.
- Downloads realtime updates now preserve `RequestId` on transfer activity and
  progress events, preventing request-backed rows from duplicating or jumping
  between live SignalR updates and periodic REST reconcile.
- Wishlist result links and inline search-history expansion no longer
  automatically clear new-results badges, so users can inspect results, return
  to a "New results" filtered Wishlist, and edit the saved filter explicitly.
- Timed-out download enqueue attempts now cancel the underlying Soulseek
  download operation, and terminal Soulseek client transfer snapshots no longer
  block retry when slskd has no active transfer record.
- Rescue mode now skips non-audio sidecar files such as `large_cover.jpg`,
  keeping mesh/multi-source recovery work focused on audio transfers.
- Startup DHT re-announces requested before the DHT engine reaches `Ready` now
  log as deferred instead of warning-level failures.
- Lidarr wanted-sync `HttpClient.Timeout` failures now log concise
  unavailability messages instead of warning-level stack traces.
- Main release COPR publishing now prefers the configured API token before the
  legacy Fedora password+OTP fallback, preventing stale Kerberos secrets from
  shadowing the working token path.
- COPR publishing now builds SRPMs in isolated temporary RPM topdirs and uploads
  the exact expected `slskdn-<version>-1.src.rpm`, preventing stale runner
  `~/rpmbuild` state from publishing an older release.
- Standalone COPR recovery publishing now creates the Web asset destination
  before copying frontend files during rebuilds.
- COPR release publishing now installs Ubuntu's MIT Kerberos HTTPS KDC proxy
  and PKINIT packages, Kerberos development headers, and explicitly sources the
  Fedora realm drop-in before using Fedora Kerberos. It also installs
  `requests-gssapi` so `copr-cli` can authenticate to the COPR API with the
  Kerberos ticket during tag and recovery uploads. The Kerberos realm mapping
  now covers COPR's `fedorainfracloud.org` API domain as well as Fedora identity
  hosts, and GSSAPI auth now targets the kerberized `copr.fedoraproject.org`
  API alias. Standalone COPR recovery now uses the same existing-project upload
  shape as tag releases instead of trying to recreate, modify, or override
  project settings during recovery, and emits COPR's server response when
  recovery uploads fail with non-JSON API output.
- Download auto-retry now skips non-audio sidecar files such as `cover.jpg`,
  keeping automatic retry budgets focused on audio tracks.
- Completed downloads now default to the source remote folder/file layout
  instead of UUID-looking batch folders; explicit `batch_id` configuration still
  preserves the old grouping behavior.
- Release builds no longer emit the recent nullable, XML documentation, or
  StyleCop warnings in transfer activity, auto-replace, transfer models, and
  search deletion APIs.
- Audio sketch hashing now skips non-audio sidecar files such as album artwork
  before launching ffmpeg, avoiding warning noise during library scans.
- System Metrics now tolerates an empty or still-loading metric payload without
  crashing, and high-traffic pages use full-width layouts instead of narrow
  fixed-width containers.
- Top-level `/health` no longer reports `Degraded` solely because optional mesh
  peer connectivity is absent; mesh peer/routing counts remain diagnostic data
  while app/container health reflects subsystem responsiveness.
- Wishlist search completion now preserves filter edits saved while a wishlist
  search is running instead of letting stale in-flight search stats overwrite
  user-edited item settings.
- DownloadRequest entity provides stable identity across rescue/auto-replace source swaps; Transfer carries RequestId; migration Z05292026 backfills existing transfers using a temp-table + single bulk INSERT/UPDATE (seconds even on 130k+ transfer histories). New /api/v0/downloads/requests endpoints (list, get with attempts, rename, cancel). Legacy /api/v0/transfers/downloads listing endpoints marked deprecated via response header.
- Updated integration test stubs for the structured download enqueue request path so full validation covers the new request identity API.
- Wishlist now renders large libraries in pages instead of mounting every row/card at once, preventing browser stalls on installs with thousands of saved searches.
- Lidarr seeded wishlist rows, search history, collections, contacts, incoming
  shares, and share manifests now page their visible rows instead of rendering
  entire client-side datasets at once.
- Expected inbound search response timeouts now return no response instead of
  producing duplicate warning logs, and malformed overlay datagrams no longer
  pollute warning-level log scans.
- Configurable completed download path template (Global.Download.CompletedPathTemplate) with tokens {uploader}, {remote_folder}, {remote_parent}, {remote_filename}, {batch_id}, {request_name}, {date}/{date:fmt}.
- Pre-download metadata seeded from search-result attributes (bitrate, length, samplerate, bitdepth); transfer row shows secondary "Artist — Title" line.
- Wishlist hit count exposes locked-hit subtotal alongside visible.
- Frontend keys transfer rows by requestId so source swaps patch in place; RequestDetailModal exposes rename and attempt history; applyRemoved guards against stale-id events.
- Compact qBittorrent-style Downloads/Uploads UI: full-width layout with zero dead
  space, `view-transfer` CSS class, dynamic table height via ResizeObserver, 28px
  rows, scaled-down controls, and flat header.
- Column management for Downloads/Uploads: drag-to-reorder headers, resize handles
  on column edges, column chooser popup to show/hide columns, configuration
  persisted per-direction in localStorage via new `transferColumns.js` module.
- Six new transfer columns: Type (file extension), Folder (directory path),
  Elapsed (transfer duration), Remaining (bytes remaining), Added (queued time),
  Done (completion time).
- Audio metadata enrichment for downloads: Bitrate, Sample Rate, and Length
  columns populated from downloaded files via TagLib after transfer completion.
  New `BitRate`, `SampleRate`, `BitDepth`, `Length` fields on internal Transfer
  model and API response; `DownloadService.EnrichTransferMetadata()` reads
  audio properties post file-move, non-fatal on failure.
- Added `getExtension()` helper to frontend util.js.
- Reworked the uploads/downloads pages into a single qBittorrent-style
  TransferManager: one Downloads/Uploads tabbed pane backed by a flat
  `/api/v0/transfers` snapshot plus SignalR ACTIVITY/PROGRESS/REMOVED deltas
  and a slow REST reconcile, so rows patch in place (no full redraw on poll or
  auto-retry) with a virtualized, sortable, accessible grid, status/peer
  filters, zebra rows, and retry/attempt affordances. Auto-replace now emits
  REMOVED so an alternate-source swap drops the stale row immediately.
- Required tagged release publishing for build and COPR workflows and added
  secret-backed COPR Kerberos/Fedora login support for durable release auth.
- Added a default-off private-message auto response for human-check, captcha,
  anti-bot, no-share, empty-share, and no-leecher gate prompts, with shared
  Downloads/Messages toggles and per-sender cooldowns.
- Routed self-hosted Linux CI jobs to the paired `slskdn` runner pool so either
  local build host can take amd64 or arm64-cross build work.
- Fixed duplicate telemetry metrics routing so `GET /api/v0/telemetry/metrics`
  no longer throws an ambiguous-route 500.
- Allowed API-key authentication on native slskdN API controllers that
  previously required JWT-only authentication.
- Added `/api/v0/hashdb/entries` for hash database dashboard views.
- Kept explicit remote transfer rejections distinct from offline-peer failures
  in download logs.
- Clamped completed-transfer display speed to zero when immediate failed
  transfers report transient negative throughput.
- Ignored invalid optional obfuscated ports in Soulseek `ConnectToPeer`
  messages so malformed extension metadata does not drop otherwise usable
  regular peer-connection hints.
- Added transfer-path diagnostics for remote download failures, including
  regular/direct, obfuscated/direct, indirect, endpoint metadata, transfer
  tokens, remote rejection reasons, exception types, and whether the peer
  closed before or after transfer setup.
- Kept regular outbound Soulseek transfer dials first in compatibility
  obfuscation mode so second-chance downloads do not prefer obfuscated
  endpoints unless the operator explicitly selects prefer mode.
- Updated Web build-output release gates to validate root-absolute Vite asset
  paths plus server-side subpath rewriting for deep links.
- Updated service-worker retirement coverage so the release gate validates
  cache cleanup instead of the removed precache path.
- Labeled expected remote download failures as `Peer unavailable` in the
  transfer list instead of the generic `Error`.
- Returned controlled 503 responses when browse is requested before Soulseek
  has finished reconnecting after daemon startup.
- Fixed Wishlist/Search regressions from the new search history workflow:
  existing searches with NULL or blank source values are backfilled instead of
  throwing 500s when responses are opened, bridged wishlist searches preserve
  their wishlist source/item metadata, wishlist filters again support
  filename/path terms and `-term` exclusions instead of file types only, edited
  auto-disable download limits persist, and malformed wishlist dates render as
  `Never`.
- Improved Wishlist/Search follow-up UX by passing wishlist filters into
  related search result pages, adding per-item badge clearing, and showing a
  latest-search fallback when linked search history is empty.
- Reduced rescue and alternate-source noise by making rescue retry cooldowns
  outcome-aware, downgrading expected no-job rescue outcomes, and requiring
  filename-token overlap before auto-replace ranks network alternatives.
- Returned controlled 503 responses for expected peer browse connection
  failures and showed the peer-unavailable reason in the Browse UI.
- Added a root SPA base tag so direct deep links such as `/searches/{id}` load
  Vite assets from `/assets` instead of `/searches/assets`.
- Retired the web service worker and cleared stale browser caches during
  startup so upgraded clients load current SPA route code instead of old bundles.
- Avoided duplicate warning logs for expected download timeouts by leaving the
  user-visible warning in the transfer path and downgrading the observer wrapper
  to debug.
- Quieted planned-shutdown cancellation noise from wishlist search processing
  and direct download retry attempt logging.
- Preflighted incomplete-download directories before starting Soulseek transfer
  attempts so local permission failures are observed and classified without
  detached task stack-trace noise.
- Preflighted completed-download destination directories before moving finished
  files so stale directory permissions fail once on the observed transfer path.
- Captured search cancellation tokens before publishing their sources so normal
  shutdown no longer logs search finalization disposal stack traces.
- Logged missing optional AudioSketch `ffmpeg` support only once per process so
  busy Docker nodes do not bury actionable warnings under per-download optional
  tool noise.
- Bundled Docker runtime media prerequisites for the default audio/SongID paths:
  `ffmpeg`/`ffprobe`, `yt-dlp`, and Chromaprint `fpcalc`.
- Bundled Microsoft `libmsquic` in Docker images so .NET QUIC mesh transports
  work in the default Linux container instead of being disabled at startup.
- Added a Docker `install-optional-media-tools` helper for heavyweight SongID
  experiments so operators can populate OCR, Whisper/Demucs, SongRec, C2PA,
  Audfprint, and Panako support after startup or in a derived image.
- Added a cached all-tools Docker recipe for local validation builds so repeat
  optional-tool images reuse apt, Python, Rust, and Gradle downloads instead of
  fetching the heavyweight stack from scratch.
- Required local HashDb auto-retry alternates to match the failed transfer's
  leaf filename so same-size unrelated audio files are not queued as automatic
  replacements.
- Added a `Searches.StartedAt` SQLite index migration so recent-first search
  history API calls return quickly on busy nodes with large search tables.
- Quieted intentional-shutdown `ObjectDisposedException` noise from background
  search finalization and event-record writes while preserving runtime error
  visibility.
- Added a separately tagged amd64 Docker omnibus testers image that bakes in
  the heavyweight optional media/SongID prerequisites without bloating the
  default release image.
- Downgraded successful second-chance transfer fallback diagnostics so normal
  peer/client compatibility recovery no longer appears as warning-level log
  noise.
- Treated search runtime lock-disposal failures during application shutdown as
  expected cancellation noise while keeping the same failures noisy during
  normal runtime.
- Built out Pod join/leave signature enforcement with real Ed25519 payload
  verification, fresh timestamp checks, join nonce replay protection, and
  legacy Off/Warn compatibility for existing local workflows.
- Reworked the Synology SPK builder to package the real published `slskd`
  binary instead of a placeholder script, and added packaging gates to block
  placeholder SPK payloads from returning.
- Built out VirtualSoulfind disaster-mode mesh peer discovery so known hashes
  can resolve through HashDb recording IDs into shadow-index peer hints before
  scene-discovery fallback.
- Cleaned stale placeholder wording from native job API docs, mesh circuit
  comments, playback feedback, and related release-facing code comments so
  intentionally gated paths are described as such.
- Fixed release-tagged Docker builds being reported as local development
  builds, converged Unix pod database permissions to `0600`, and downgraded
  inbound mesh TLS handshake timeouts to expected handshake noise.
- Added an opt-in experimental media Docker image recipe for heavier SongID
  prerequisites, and updated capability reporting with Docker guidance when
  optional recognizer tools are missing.
- Routed HTTP LLM moderation endpoint checks through the shared DNS-aware
  outbound guard so hostnames resolving to non-public ranges are blocked before
  any request is sent.
- Logged expected Lidarr auto-import HTTP failures without stack traces so
  external connection refusals and 5xx responses stay readable in daemon logs.
- Fixed concurrent HashDb peer creation so simultaneous passive peer-tracking
  events for the same Soulseek username no longer log SQLite unique-constraint
  warnings.
- Added a release-gate check that blocks private local hostnames, local OS
  usernames, and home-directory paths from release-facing text and recent
  commit messages.
- Wired the local-identity release gate to accept a private GitHub Actions
  denylist secret and report only redacted file/line locations for matches.
- Generalized the local-identity release gate so other repositories can reuse
  the same scanner with opt-in commit-message checks.
- Added a reusable local-identity GitHub Actions workflow so the scanner can
  run independently of the release gate.
- Synced Snap packaging metadata with the latest stable release baseline so
  the release gate can validate all package channels consistently.
- Logged expected Lidarr HTTP status failures during wanted sync without stack
  traces so external 4xx/5xx responses stay readable in daemon logs.
- Capped default search-list API responses, bounded default download
  auto-retry attempts, and downgraded expected mesh/Lidarr/queue-position
  runtime noise so live Docker logs stay actionable under busy traffic.
- Classified Soulseek `File read error` transfer rejections as expected peer
  denials so detached runtime transfer cleanup does not log as fatal.
- Hid legacy duplicate job route controllers from Swagger discovery and made
  Swagger tolerate duplicate compatibility routes so OpenAPI generation no
  longer fails on repeated method/path operations.
- Classified Soulseek `MessageConnection` disposal races as expected peer
  teardown so detached runtime read-loop cleanup does not log as fatal.
- Classified completed peer connection closes as expected Soulseek network
  noise and serialized DM conversation pod creation to avoid duplicate startup
  SQLite errors under private-message bursts.
- Fixed Lidarr auto-import debounce tracking so a directory is marked in-flight
  before processing and no longer re-marked only after selected exits.
- Serialized Lidarr auto-import candidate and command calls so completed album
  bursts do not overload Lidarr's SQLite-backed API.
- Updated council guardrail artifacts for peer/mesh preview streaming, stopped
  streaming controllers from returning raw exception messages, and restored
  Wishlist searches to the shared network search scope with wishlist safety
  budget labeling.
- Added authenticated, ticketed manual peer-to-browser audio preview streams
  with bounded buffering, per-user concurrency limits, and search-result UI
  access that does not save files or trigger batch downloads.
- Preserved existing Pod search-result stream action routing while adding the
  direct Soulseek peer preview fallback.
- Added ticketed mesh peer preview streams for non-local Pod results with
  fixed-size overlay chunks, fairness checks, traffic accounting, per-user
  limits, and expected hash validation.
- Hardened peer and mesh preview stream tickets against traversal-looking
  filenames, rooted filenames, Windows drive-letter roots, and malformed mesh
  SHA-256 hash expectations.
- Added config compatibility warning coverage for legacy transfer, group,
  integration, upload-limit, and retry-delay settings.
- Kept download auto-retry enabled while making its Soulseek retry policy more
  conservative with slower scans, per-peer cooldowns, global/per-peer per-cycle
  retry budgets, and unlimited retries by default.
- Added polite alternate-source selection for auto-retry, preferring cooled-down
  local HashDb candidates and limiting network alternative searches per scan.
- Removed an unreachable auto-retry fallback path so the app build stays
  warning-clean.
- Reduced warning noise for normal peer-side download denial/failure events
  while leaving transfer failure handling intact.
- Preserved peer and mesh preview stream validation messages in ticket API
  bad-request responses.
- Observed terminal download enqueue signal faults so expected remote transfer
  rejections do not surface as process-level unobserved task exceptions.
- Observed download enqueue cleanup aggregate faults so expected remote transfer
  rejections do not surface as process-level unobserved task exceptions.
- Stopped asynchronously disposing ephemeral download enqueue throttle semaphores
  so auto-retry enqueue cleanup cannot race into disposed synchronization state.
- Reduced same-peer download enqueue burst concurrency so album-sized requests
  are less likely to trigger peer-side "overwhelmed with requests" rejections.
- Classified expected Soulseek peer transfer denials in the global unobserved
  task handler so normal "file not shared" responses are not logged as fatal.
- Logged Lidarr auto-import HTTP timeouts as concise warnings instead of
  emitting full stack traces for transient slow Lidarr responses.
- Synchronized Snap stable release metadata with the current stable package
  references so the release gate accepts new main release tags.
- Included Snap metadata in the post-release stable metadata commit so the
  updater cannot silently leave `snapcraft.yaml` stale.
- Made completed download moves tolerate concurrent incomplete-directory
  cleanup, and added direct search-detail loading so search result pages open
  reliably in new tabs.
- Archived stale working notes and removed obsolete local one-off remediation
  scripts from the active docs/scripts surface.
- Strengthened mesh preview hash validation so hash-protected content is
  verified before any bytes are emitted to the browser stream, with producer
  tests covering Soulseek and mesh preview data flow.
- Downgraded additional expected Soulseek peer transport failures from error
  stack traces to single warning records, rate-limited malformed overlay
  datagram logging, and made DHT/overlay summaries identify
  discovered-but-unreachable mesh peers.
- Gave the mesh overlay TCP listener a longer graceful-restart retry window and
  kept expected bind-retry exceptions out of error logs.
- Recorded the associated mesh/Soulseek transfer gotchas for future agents.
- Downgraded startup search attempts while Soulseek is still logging in from
  errors to deferred-search warnings.
- Improved Lidarr/Wishlist acquisition by deduping Lidarr wanted syncs by
  search plus filter, using native Soulseek wishlist search scope, processing
  older wishlist rows first, and disabling auto-download rows only after files
  are actually enqueued.
- Added explicit user-group overlap validation, safe per-request download
  destination routing, derived download batch summaries, named local search
  filters, shift-range file selection, and configured native Soulseek interest
  publishing.
- Hardened Lidarr/Wishlist auto-acquisition and transfer enqueue routing around
  these reconciliation fixes.
- Labeled System admin and experimental panels in the tab menu to reduce
  cognitive load around advanced surfaces.
- Updated System surface and route/UI parity docs to reflect the new
  admin/experimental navigation labels.
- Recorded the G6 validation pass: full frontend tests/build, full backend
  tests, repo lint, and remediation baseline results through the expected
  release branch sync guard.
- Reconciled the feature parity plan and remediation completion report with the
  completed validation results and remaining release coordination work.
- Reconciled route/UI parity rows for DHT/bootstrap/NAT visibility and
  VirtualSoulfind provider capability visibility.
- Reconciled remaining stale parity status wording for Soulseek mesh
  rendezvous UI/privacy/test coverage and security route-audit artifacts.
- Added top-level Web route smoke coverage for the reconciliation plan's stale
  route/UI visibility follow-up.
- Strengthened System navigation tests to verify every expected admin and
  experimental panel carries the correct label.
- Ported all 21 seerrng colour palettes (Aurora through Sietch) into the
  Web UI theme picker, each with 4 swatch colours. Selecting a palette
  overrides accent, warm, surface, nav, and footer CSS custom properties
  at runtime via the new `src/web/src/lib/themes.js` module using
  Tailwind-style colour scales and seerrng's colour-mixing engine. Choice
  persists across page loads via `localStorage`.
- Added full theme palette picker UI with 2-column labelled swatch grid,
  active-palette highlighting, and a Reset option.
- Added 11 test cases for palette definitions, token uniqueness, and
  Sietch colour characteristics.
- Updated Wishlist/Search UX: added Search Source tracking with
  wishlist-item linking, unseen-results badge, inline search history,
  filter presets (FLAC/MP3 etc.), table/card view toggle, bulk
  operations, auto-disable after N downloads, Mark All as Viewed, and
  search retention cleanup config in System settings.
- Fixed TransferList visual alignment issues in the transfers page.
- Added error affordances to failed transfer rows: each failure reason
  (Too many megabytes, File not shared, Size mismatch, Overwhelmed,
  Internal error, Peer offline, Connection lost) now shows a contextual
  action button with an appropriate icon (search, clock, pause, redo)
  and a tooltip explaining what the action does. Clicking retries the
  transfer or finds other sources.
- Fixed inverted condition in the wishlist schema migration
  (Z05182026_WishlistItemViewingAndDownloadLimitsMigration) so that
  LastViewedAt and MaxDownloads columns are actually added to existing
  databases instead of being skipped. The bug caused Lidarr wanted sync
  to fail every cycle with `SQLite Error 1: no such column: w.LastViewedAt`.
- Documented the inverted migration condition gotcha in
  `memory-bank/decisions/adr-0001-known-gotchas.md` as entry 0z361.

## [2026051318-slskdn.251] — 2026-05-13

- Labeled System admin and experimental panels in the tab menu to reduce
  cognitive load around advanced surfaces.
- Grouped MediaCore fuzzy candidate search behind advanced disclosure so
  pairwise perceptual/text similarity remain the default review paths.

## [2026051318-slskdn.250] — 2026-05-13

- Grouped MediaCore descriptor cache bypass and batch DHT retrieval controls
  behind advanced disclosure with network-impact guidance.
- Added MediaCore component coverage for the advanced descriptor retrieval
  grouping.
- Grouped MediaCore fuzzy candidate search behind advanced disclosure so
  pairwise perceptual/text similarity remain the default review paths.

## [2026051317-slskdn.249] — 2026-05-13

- Kept the framework antiforgery cookie HTTP-only while preserving the
  separate JS-readable CSRF request-token cookie, resolving the CodeQL
  `cs/web/cookie-httponly-not-set` alert.
- Fixed MediaCore ContentID example buttons so they populate the active
  read-first resolve and validation fields after the workflow simplification.
- Aligned the frontend root-route regression test with the current Search
  navigation label so the release gate checks the `/searches` target directly.
- Fixed Arch/AUR source builds by lowering the .NET SDK floor to the 10.0.1xx
  feature band with feature roll-forward, updated release workflow SDK pins to
  match, and refreshed Snap metadata for `2026051221-slskdn.247`.
- Fixed download timeout handling so aggregate-wrapped Soulseek timeouts and
  timeout-text transfer exceptions are recorded as `TimedOut` and logged as
  warnings instead of generic error stack traces.
- Downgraded expected remote peer download failures such as rejection,
  remote-reported failure, and remote size mismatch from error stack traces to
  warnings.
- Cleaned up now-unused `Program.cs` imports after the bootstrap decomposition.
- Removed leftover Program dead code from earlier startup helper extractions,
  while keeping command-line argument population in `Program` for correct
  `[Argument]` binding.
- Moved startup application-directory resolution, default directory validation,
  configuration-file defaulting, and single-instance mutex preparation into
  `Bootstrap/StartupApplicationDirectories`.
- Moved startup configuration load/validation exception handling into
  `Bootstrap/StartupConfiguration`.
- Moved startup command-mode console output, certificate generation, and startup
  logo rendering calls into the extracted bootstrap helpers, removing the
  remaining Program console/certificate wrappers.
- Moved startup SQLite initialization and missing-config recreation calls to
  use extracted bootstrap helpers directly, removing more private Program
  wrappers.
- Moved startup logging configuration and shutdown telemetry installation calls
  to extracted bootstrap helpers directly, removing the remaining Program
  logging/shutdown wrappers.
- Marked the Program decomposition pass complete after reducing Program to
  entrypoint orchestration, public process state, command-line binding, and the
  public log event/buffer bridge.
- Simplified MediaCore perceptual hash workflows by keeping similarity
  review/statistics as the default path and grouping raw audio/image hash
  computation behind advanced disclosure.
- Removed the remaining antiforgery `Program.cs` wrappers by making the MVC
  CSRF filter and focused tests call `AntiforgeryCookieRecovery` directly.
- Restored command-line argument population to `Program` so startup options
  such as `--config` continue to bind correctly.
- Rewired CSRF stale-cookie recovery to call the extracted helper directly
  instead of routing through `Program`.
- Removed additional `Program.cs` compatibility wrappers after tests moved to
  the extracted bootstrap/security/path helper APIs directly.
- Rewired remaining production call sites to use extracted path, Soulseek
  option, QUIC data-plane, and antiforgery helpers directly instead of routing
  through Program compatibility wrappers.
- Fixed startup hardening so no-auth exposure is based on actual web listener
  bind posture instead of port presence, and made the unavailable
  hash-from-audio option fail startup if enabled.
- Added startup hardening matrix coverage for loopback, Unix socket,
  remote-reachable, unknown bind, and remote no-auth CIDR behavior.
- Added a runtime feature-gate foundation and put the experimental SongID,
  mesh, DHT, pods, social federation, VirtualSoulfind, and multi-source API
  surfaces behind enabled-by-default `feature.*` switches.
- Fixed Messaging V2 room search so a degraded room-directory refresh cannot
  erase the last good suggestions while the user is typing.
- Fixed Messaging V2 room search/rejoin recovery when the initial room
  directory request times out and Soulseek reports an existing room join as
  "no response."
- Cached the last successful backend Soulseek room directory so transient
  room-list timeouts do not clear search suggestions.
- Polished Messaging V2 room search so the picker shows a real loading state,
  keeps the section count focused on joined rooms, and gives the search box
  enough room to be usable while empty room-directory responses are retried in
  the background after page load.
- Updated the dependency ownership inventory with call-site-backed
  classifications and explicit prune/relocation follow-ups.
- Documented the app project analyzer suppressions, including the debug-only
  binding suppressions and the current unsuppressed CA2000 transport warnings.
- Moved custom MSBuild quality tasks into `tools/slskd.BuildTasks` so the app
  project no longer loads build tasks from `slskd.dll` or carries
  `Microsoft.Build.*` runtime package references; the linked CodeQuality async
  paths now use `ConfigureAwait(false)` so the new task project builds without
  adding CA2007 warning noise.
- Scoped the CA2000 transport handler ownership suppressions locally so the app
  project now builds with zero warnings instead of carrying broad warning noise.
- Added DownloadService regression coverage for in-progress duplicate
  protection, completed-transfer supersession, and terminal failed cleanup when
  background download startup fails.
- Added DownloadService semaphore regression coverage proving same-user enqueue
  requests serialize while different users can enter concurrently.
- Added `/api/v0/songid/capabilities` so SongID reports runtime availability
  for optional providers and explicitly marks `HashFromAudioFileEnabled` broken.
- Started decomposing `Program.cs` by moving SongID service registration into a
  dedicated bootstrap extension.
- Moved the large experimental feature registration graph out of `Program.cs`
  into a bootstrap extension while preserving existing default-on feature
  behavior and registration order.
- Moved user notes, collections/sharing, identity/friends, and Solid/WebID
  registrations out of `Program.cs` into a user-data bootstrap extension.
- Moved core app service registrations, including database contexts,
  messaging/search/share/user services, transfers, and source ranking, out of
  `Program.cs` into a core bootstrap extension.
- Moved startup options, feature gates, managed state, HTTP clients, Soulseek
  client construction, and the `IApplication` hosted-service wrapper out of
  `Program.cs` into an application-host bootstrap extension.
- Moved ASP.NET service registration, including auth, controllers, SignalR,
  health checks, rate limiting, and Swagger, out of `Program.cs` into a web
  bootstrap extension.
- Moved ASP.NET request-pipeline and endpoint registration out of `Program.cs`
  into a web pipeline bootstrap extension.
- Moved the top-level runtime service composition wrapper out of `Program.cs`
  into a runtime bootstrap extension.
- Moved integration and media-adjacent registrations out of the broad
  experimental graph into a dedicated bootstrap extension.
- Moved multi-source transfer, swarm, tracing, warm-cache, playback-priority,
  and related job-manifest registrations out of the broad experimental graph
  into a dedicated bootstrap extension.
- Reconciled the feature parity plan with completed route/security remediation
  work and simplified the MediaCore pod discovery workflow by keeping read-only
  discovery actions first while grouping public registry mutation controls
  behind progressive disclosure.
- Simplified the MediaCore pod join/leave workflow by putting pending-request
  review first and grouping signed membership event publishing controls behind
  progressive disclosure.
- Simplified the MediaCore pod message signing workflow by putting verification
  and statistics first while grouping private-key signing and key generation
  behind progressive disclosure.
- Simplified the MediaCore pod channel management workflow by putting channel
  loading/review first while grouping create, edit, and delete controls behind
  progressive disclosure.
- Moved VirtualSoulfind capture, shadow-index, scene, disaster-mode, bridge,
  v2 provider/backend, reconciliation, and processing registrations out of the
  broad experimental feature graph into a dedicated bootstrap extension.
- Moved backfill, mesh hash-sync, source discovery, rescue, accelerated
  download, content verification, peer metrics, and chunk scheduler
  registrations out of the broad experimental feature graph into a dedicated
  bootstrap extension.
- Moved MediaCore/PodCore and mesh/DHT/overlay registrations out of the broad
  experimental feature graph into dedicated bootstrap extensions.
- Moved MediaCore publisher, capability bridge, and DHT rendezvous
  registrations out of the experimental feature graph coordinator into a
  dedicated bootstrap extension.
- Moved E2E hosted-service tracing and host startup timeout/concurrency options
  out of `Program.cs` into a host diagnostics bootstrap extension.
- Simplified the MediaCore pod opinion workflow by keeping opinion review and
  aggregation actions first while grouping opinion publishing and affinity
  recalculation behind progressive disclosure.
- Simplified the MediaCore pod content-linking workflow by keeping content
  search and validation first while grouping content-linked pod creation behind
  progressive disclosure after validation.
- Simplified the MediaCore pod message storage and backfill workflows by keeping
  stats, search, and timestamp review first while grouping local maintenance
  and backfill sync behind progressive disclosure.
- Moved post-build startup tasks out of `Program.cs` into a bootstrap
  extension, covering database migration, optional audio reanalysis, and forced
  construction of event-subscriber integrations.
- Simplified the MediaCore PodCore DHT publishing workflow by keeping metadata
  retrieval and publishing statistics first while grouping publish/unpublish
  controls behind progressive disclosure.
- Simplified the MediaCore pod membership management workflow by keeping
  get/verify/statistics first while grouping membership publishing, role/ban
  changes, and cleanup behind progressive disclosure.
- Simplified the MediaCore pod message routing workflow by keeping deduplication
  checks and routing statistics first while grouping message send, mark-seen,
  and cleanup controls behind progressive disclosure.
- Moved web listener/Kestrel setup out of `Program.cs` into a web host
  configuration bootstrap extension.
- Moved application run/lifecycle hooks, E2E server probes, and LAN discovery
  advertising start/stop out of `Program.cs` into a bootstrap extension.
- Moved configuration compatibility warning parsing out of `Program.cs` into a
  focused configuration helper.
- Moved expected Soulseek network exception classification out of `Program.cs`
  into a focused helper while retaining the existing Program compatibility
  wrapper for current call sites.
- Moved initial Soulseek client option construction out of `Program.cs` into a
  focused helper.
- Moved app-relative path resolution and web HTML rewrite rule construction out
  of `Program.cs` into focused helpers.
- Moved antiforgery stale-cookie recovery and stale-token classification out of
  `Program.cs` into a focused security helper.
- Moved startup configuration provider composition out of `Program.cs` into a
  focused configuration extension.
- Moved startup filesystem checks, missing configuration-file recreation, and
  generated certificate export out of `Program.cs` into a focused bootstrap
  helper.
- Moved QUIC overlay client/server construction and standalone UDP overlay
  selection out of `Program.cs` into a focused mesh helper.
- Moved global Serilog setup and shutdown/unobserved-exception telemetry wiring
  out of `Program.cs` into focused bootstrap helpers.
- Moved CLI help output, environment-variable listing, and startup logo
  rendering out of `Program.cs` into a focused bootstrap helper.
- Moved SQLite provider initialization and threading fail-fast validation out
  of `Program.cs` into a focused bootstrap helper.
- Moved runtime version/canary/development flag and executable-path calculation
  out of `Program.cs` into a focused bootstrap helper while preserving the
  public Program compatibility surface.
- Moved startup mutex-name construction and unobserved-task exception
  classification out of `Program.cs` into focused bootstrap helpers while
  preserving Program compatibility wrappers.
- Moved owned physical file provider construction out of `Program.cs` into
  `StartupFileSystem` while preserving the Program compatibility wrapper.
- Rewired web pipeline and experimental mesh bootstrap code to use extracted
  rewrite, antiforgery, file-system, and QUIC helpers directly instead of
  routing through Program compatibility wrappers.
- Moved primitive startup command-mode handling for version/help/env output,
  certificate generation, and secret generation out of `Program.cs` into
  `StartupCommandMode`.
- Moved startup application-directory default resolution and default directory
  validation out of `Program.cs` into `StartupApplicationDirectoryResolver`.
- Moved startup configuration provider loading, binding, raw security-section
  diagnostics, and validation out of `Program.cs` into `StartupConfiguration`.
- Moved configured startup identity, system, directory, compatibility-warning,
  and logging-target diagnostics out of `Program.cs` into `StartupDiagnostics`.
- Moved ASP.NET hardening validation, builder configuration, service
  registration, DI build, pipeline setup, no-start handling, and run lifecycle
  out of `Program.cs` into `StartupWebApplicationRunner`.
- Simplified MediaCore descriptor publishing by keeping retrieval/statistics as
  the default path and grouping descriptor publish, batch publish, update, and
  republish controls behind advanced disclosure.
- Simplified MediaCore ContentID and metadata portability workflows by keeping
  resolve, validate, export, and conflict-analysis paths first while grouping
  registration and import controls behind advanced disclosure.
- Simplified MediaCore retrieval and dashboard management by keeping stats
  loading first while grouping cache clearing and global stats reset controls
  behind advanced disclosure.
- Fixed DHT VPN port sync config binding so documented snake_case values such as
  `dht.vpn_port_sync: target_port` no longer crash startup, and so mesh DHT
  announcements can follow the VPN port-forward slot for the overlay listener.
- Packaged the slskdN VPN helper into release archives and Linux installers,
  including AUR, Debian, RPM, and direct release installs, so VPN split-routing
  and ingress units are available out of the box.
- Published the VPN helper for Windows and macOS release archives with
  platform split-routing docs, and fixed shared helper configuration so
  non-Linux commands do not run Linux UID discovery.
- Fixed local build/publish version normalization so legacy tags such as
  `slskdn.238` do not make MSBuild reject release smoke tests.
- Updated release packaging validation so future package changes must keep the
  VPN helper payload and Linux installer wiring intact.
- Fixed macOS VPN helper enforcement so the generated pf anchor is referenced
  from `/etc/pf.conf`, not merely loaded into an unevaluated anchor namespace.
- Added VPN helper payload checks to secondary Linux, PPA, and COPR release
  workflows, and exposed `slskdN-vpn-agent` through Homebrew and Chocolatey
  installs when the release archive contains it.
- Fixed Transfers bulk-clear optimistic state so completed filenames disappear
  from both the page body and header immediately after clearing.
- Updated Rooms join-modal regression coverage for the search-before-render room
  list behavior.
- Completed a second release-readiness pass across package-manager and
  secondary-release workflows before cutting the next tag.
- Added a release-gate branch sync check so validated local commits must be
  pushed to the tracked upstream before a release tag is cut.
- Added a guarded release-tag helper and post-publish artifact verification so
  future releases must tag the pushed aggregate, run the full gate, and confirm
  shipped archives contain key aggregate features such as the VPN helper and
  footer session-transfer marker.
- Fixed Docker release publishing after adding the VPN helper by copying the
  helper project into the Docker publish stage, and updated Snap stable metadata
  to the current stable release asset.
- Pinned release and CI workflows to the exact .NET SDK in `global.json`, with
  packaging validation coverage so future self-contained publish jobs cannot
  drift to an untested SDK feature band.
- Condensed the network endpoint ports banner to one line and made dismissal
  permanent across VPN port changes and future installs that keep browser
  storage.
- Fixed downloads from peers that advertise Windows-rooted remote Soulseek paths
  by treating the drive/root as a remote store prefix, not a local destination
  path.
- Downgraded expected rescue skips for unresolved MusicBrainz Recording IDs from
  warning to debug so log scans surface real faults more clearly.
- Stopped labeling failed terminal transfers as "completed" in the Web UI and
  made hide/clear completed affect only successful 100% transfers, keeping
  retryable failed downloads visible and protected from completed-transfer purge.
- Stopped double-logging remote queue-position lookup timeouts as app errors;
  expected peer timeouts now return a controlled `504` response.
- Tracked `global.json` in the repo and taught packaging validation to fail if
  the SDK pin is missing from tag checkouts.
- Fixed Arch, Debian, and RPM VPN helper service units so distro packages use
  `/usr/bin/slskdN-vpn-agent`, `/etc/slskd/slskd.yml`, and the packaged `slskd`
  service/user names instead of manual-install defaults.
- Fixed PPA binary builds by keeping `debian/rules` POSIX-shell compatible, and
  added a GitHub-side binary package preflight before Launchpad uploads.
- Synced Snap stable metadata with the latest stable release asset after the
  release metadata updater advanced the other package-manager manifests.
- Added Arch `.pacnew` upgrade messaging for `/etc/slskd/slskd.yml`.
- Fixed a release-gate flake in the content-verification concurrency test by
  making the mock Soulseek probe cleanup cancellation-safe.
- Fixed mesh transfer terminal status publication so polling callers cannot
  observe `Failed` before the sanitized error message is populated.
- Hardened the Messaging slash-command test so full-suite timing cannot submit
  before the controlled composer has published the `/leave` draft.
- Fixed Search, Downloads, and Uploads page-load stalls by rendering page shells
  before SignalR/API background work completes, restoring EF-translatable active
  transfer filters, and caching expensive footer transfer-session aggregates.
- Removed additional full-page initial loaders from Collections, Shared with Me,
  Share Groups, Events, Files, Metrics, Network, and Source Providers so their
  page shells render while data refreshes in the background.
- Deferred and bounded optional username metadata lookups in `UserCard`, with
  in-flight dedupe and short caching to reduce request fan-out on transfer,
  search, browse, chat, and room views.
- Split optional Search panels and System tabs into lazy-loaded chunks so
  collapsed panels and inactive admin tabs no longer inflate initial route
  bundles.
- Reduced hidden and repeated Web render costs by rendering inactive room tabs
  as lightweight shells, replacing full search-response serialization with a
  stable file signature, caching deferred search user-group metadata, and
  avoiding in-place transfer list sorting during render.
- Reduced additional hidden Web work by rendering inactive chat tabs as
  lightweight shells, avoiding shared `FileList` prop mutation while sorting,
  and preventing inactive System Files explorers from fetching directory
  listings before their tab is selected.
- Deferred optional Search Detail metadata fetches until after first paint,
  rendered Library Health tabs active-only so large inactive tables are not
  built, and fixed the System Shares contents modal to sort copied file lists
  and pass boolean modal props.
- Stopped hidden Pods Port Forwarding panes from mounting and starting polling
  intervals while the VPN Gateway pane is active.
- Rendered Contacts tabs active-only so hidden contact/nearby peer lists are
  not built while another tab is selected.
- Replaced the Messaging V2 raw available-room dump with a compact room search
  box that filters existing Soulseek rooms and joins or creates the typed room
  from the same control.
- Made the available-room API return an empty list while Soulseek reconnects
  instead of logging an unhandled 500 from optional room discovery.
- Stopped Messaging V2 from polling the full Soulseek room directory during
  general workspace hydration; available rooms now load only when the room
  picker is opened.
- Made room joins return a controlled 503 while Soulseek is reconnecting
  instead of surfacing an unhandled SDK state exception.
- Tightened startup hardening so public bind exposure is classified through the
  shared analyzer and unsupported hash-from-audio config fails consistently.
- Fixed downloads/uploads page spinner stalling on initial load due to queue-position API calls blocking the render; those are now fire-and-forget.
- Dramatically reduced downloads/uploads page initial load time by fetching only active transfers on the 2-second poll; completed transfers are fetched separately on a 15-second interval for header bulk operations.
- Added automatic re-queue for failed downloads: transfers ending in TimedOut, Errored, or Aborted state are automatically re-enqueued after a configurable delay (default 5 minutes). Cancelled and Rejected transfers are excluded.
- Enabled accelerated download mode (rescue mode) by default for all users.
- Fixed "Remove All Errored" and "Retry All Errored" not targeting "Completed, Aborted" transfers; aborted transfers are now included alongside timed-out, errored, and rejected.
- Fixed header bulk-operation buttons (Remove All, Retry All) silently doing nothing when "Hide Completed" was enabled, caused by the poll skipping completed transfers from the API response.

- Added full Lidarr integration: nav tab, sync page, wanted-to-wishlist sync with per-cycle cap, and auto-import on download completion with safe-candidate filtering.
- Fixed Lidarr auto-import HTTP 500 by using `filterExistingFiles=false` to avoid hitting the corrupt `MediaFiles` SQLite table.
- Added `includeCompleted=false` query param to `GET /transfers/downloads` and `GET /transfers/uploads` so the server skips serializing completed rows; the UI passes this when "Hide Completed" is on, eliminating the slow initial page load.
- Added "Hide Completed" toggle to the Transfers header (on by default) that filters completed transfers from the API response and the rendered list.
- Decoupled bulk action button disabled states so retrying errored transfers no longer locks out the Remove and Cancel buttons.
- Slowed the transfers poll interval from 1 s to 2 s to reduce server load.
- Limited the room search dropdown to the top 100 rooms by user count to avoid hanging on large lists.
- Added filter-before-render to the Join Room modal so the full table is only shown once a search term is entered.
- Surfaced room join errors as toast notifications instead of silent console errors.

- Fixed DHT mesh rendezvous behind VPN/NAT-PMP providers that rewrite requested
  ports by allowing `dht.advertised_overlay_port` to differ from the local
  `dht.overlay_port` listener.
- Fixed VPN split-routing rule order so NAT-PMP renewals for the slskd UID reach
  the provider gateway before local-CIDR bypass rules.
- Synced Snap stable package metadata to the current stable release asset.
- Aligned AUR package-owned slskd state directory modes with tmpfiles and
  systemd group-write policy to avoid pacman permission mismatch warnings.
- Fixed Soulseek search response handling across VPN ingress by accepting
  PierceFirewall handoffs that carry peer search results.
- Stabilized VPN split-routing and ingress systemd configuration so slskd
  traffic stays on the VPN/NAT-PMP path without breaking namespace setup.
- Improved browse rendering and transfer handoff performance with worker-backed
  tree processing and direct file/folder download actions.
- Completed Messaging v2 toolbar/composer cutover with regression coverage for
  quote, copy, and controlled composer state.
- Improved Messaging workspace command controls with command help, quick
  switching, unread markers, URL autolinking, and composer regression coverage.
- Synced vendored slskNet.Runtime validation coverage for diagnostic,
  outbound-message, cache-key, crypto-trust, dynamic-execution, parser-runtime,
  resource-capacity, and buffer-operation protocol boundaries.
- Added Messaging stream jump-to-latest controls and link rendering coverage.
- Completed the Messaging v2 cutover with user popovers, composer polish, and
  focused regression coverage.
- Synced vendored slskNet.Runtime protocol offset boundary coverage for
  tainted stream positioning and skip operations.
- Synced vendored slskNet.Runtime protocol enum and slice-bound validation plus
  calibrated semantic coverage for path, timeout, endpoint, enum, and slice
  trust boundaries.
- Added canonical opinion records for users, files, content hashes, artists,
  albums, tracks, pods, peers, and search terms, with Soulseek liked/hated
  interests imported as weak public signals and explicit peer opinions feeding
  pod affinity and user-card opinion badges.
- Restored the visible green `Join Room` action in the Rooms toolbar and added
  regression coverage for joining an available room such as `slskdn`.
- Preserved operator-visible usernames, paths, endpoints, hashes, and search
  text in runtime and app diagnostics while still redacting actual secrets.
- Added `npm run check:council` so slskd council cycles execute every phase in
  one command.
- Added a slskd HTTP adversarial fuzz harness for malformed JSON bodies,
  deterministic random byte bodies, and hostile query/path strings.
- Replaced the checked-in slskdN web icon and logo assets with the borg feather
  source artwork used by current frontend builds.
- Hardened System Network, Mesh, Swarm Visualization, Swarm Analytics, Jobs,
  Security, Bridge, and MediaCore polling so in-flight refreshes do not update
  state after navigation away.
- Strengthened the new slskd bug-council negative-space gate so every declared
  trust boundary asserts its validator and remediation-baseline registration,
  including mutating API, durable-state, and runtime-crossing checks.
- Fixed active package/docs branch drift so Unraid and Chocolatey fork metadata
  plus the E2E workflow guide point at `main`, with a remediation scanner
  preventing fork-owned package links from drifting back to `master`.
- Extended active branch-drift cleanup to workflow comments and fork-facing
  security/feature docs so current contributor guidance consistently targets
  `main`.
- Track and drain accepted QUIC overlay/data stream tasks during server
  shutdown instead of detaching stream handlers from lifecycle ownership.
- Hardened the Proxmox LXC installer with release checksum verification,
  stale-tree replacement, and Linux service permission convergence.
- Hardened Web lifecycle handling for Library Health scan polling, Shares
  delayed refreshes, and malformed Search hub mutation events.
- Hardened Web boundary object normalization for browser-local experience
  preferences, user-note responses, and MusicBrainz target lookup responses.
- Persist job manifests, quarantine-jury state, Spotify connection tokens,
  source-feed import history, MusicBrainz overlay/radar state, and realm subject
  indexes through the shared atomic file writer.
- Observe DHT overlay inbound/outbound background session loops so detached
  connection and message-loop faults are logged instead of becoming unobserved
  task failures.
- Persist peer profiles, reputation data, DHT node state, auto-replace state,
  and verification probe budgets with flushed sibling-temp writes before atomic
  replacement.
- Persist overlay keys, overlay certificates, and mesh certificate pins with
  atomic sibling-temp writes so interrupted saves do not corrupt security or
  trust-state files.
- Synced vendored slskNet.Runtime protocol event identifier hardening so
  malformed status flags and negative request/message ids are rejected before
  event snapshots are published.
- Hardened Collections, Share Groups, and Shared With Me web views against stale
  selection/list state and added focused Collections and Share Groups coverage.
- Hardened the Web options editor and Transfers bulk-selection state so current
  validation errors block saves, structured API errors render readable text, and
  stale transfer selections are ignored after list refreshes.
- Hardened Contacts, Library Health, and incoming share backfill workflows
  against malformed response objects and structured API error bodies.
- Hardened Collections, Share Groups, and incoming share manifests so structured
  API errors are normalized before rendering.
- Synced vendored slskNet.Runtime endpoint ownership hardening so peer
  capability, distributed-network, transfer, and connection snapshots no longer
  expose mutable endpoint state.
- Synced vendored slskNet.Runtime exact-frame parser hardening so capability
  envelopes and peer initialization handshakes reject ignored trailing bytes.
- Synced vendored slskNet.Runtime search scope validation so room and user
  search scopes reject whitespace-only subjects before protocol emission.
- Synced vendored slskNet.Runtime peer capability envelope-limit hardening so
  local descriptors, nonces, and signature material cannot exceed parser bounds.
- Hardened DHT content-safety signature metadata so signature magic bytes are
  defensively copied like the common content-safety path.
- Synced vendored slskNet.Runtime protocol string validation so outbound
  message constructors reject null strings before serialization.
- Hardened mutable-byte ownership in security and batching paths by defensively
  copying provided secret keys, signature magic bytes, and queued message payloads
  before use or storage.
- Synced vendored slskNet.Runtime validation hardening for resumed transfer
  snapshots, raw response streams, and peer capability metadata; aligned slskdN
  capability tests with the stricter non-null endpoint contract.
- Hardened council async/path findings by awaiting SearchService traffic-observer
  notifications and making streaming fallback plus Library Health recursive scans
  skip symlinks/junctions explicitly; aligned integration route smoke auth with
  admin-only security telemetry routes.
- Hardened additional async side-effect paths so app notifications, SignalR
  broadcasts, relay/share work, pod routing, FTP upload, and peer metrics are
  observed and logged instead of dropped behind raw fire-and-forget tasks.
- Hardened non-runtime council findings across release metadata, scheduled
  release-tag policy, CodeQL .NET setup, Flatpak/Snap packaging, Web list and
  route guards, security telemetry authorization, and anonymous build metadata.
- Hardened vendored slskNet.Runtime username and password confirmation
  comparisons to use ordinal protocol identity semantics.
- Hardened vendored slskNet.Runtime transfer remaining-time projections so
  malformed speed values and extreme durations cannot break transfer snapshots.
- Hardened vendored slskNet.Runtime network progress snapshots so invalid
  totals cannot emit infinite, negative, or over-complete percentages.
- Hardened persisted Messaging panels against malformed local storage entries.
- Hardened additional Web media/search rendering paths against malformed object
  and list payloads.
- Added regression coverage and ledger entries for the latest non-runtime council
  batch, including route-query refresh, malformed hub/panel payloads, and
  Web media/search list-shape scanners.
- Fixed a migration attribution header so an upstream-authored migration changed
  in slskdN preserves upstream copyright and adds the slskdN co-attribution
  block.
- Closed remaining primitive-body web helper mismatches by serializing `[FromBody]
  string` payloads with `JSON.stringify` in event and room helpers (`events,
  setTicker, addRoomMember`) and adding focused regression tests.
- Hardened additional non-runtime mutable ownership paths by deeply copying nested
  batching metadata containers and cloning DHT store signing payload arrays before
  signing and queueing, preventing caller-side mutation after enqueue/signed
  message creation.
- Hardened mesh peer endpoint ownership so discovered or updated endpoint objects
  are cloned on input and read access instead of sharing mutable `IPEndPoint`
  instances with callers.
- Fixed mesh DHT STORE signing so signed store requests use a concrete DHT store
  mesh message type and verify with the real signer payload instead of reflecting
  into an ACK message.
- Hardened computed list helpers so content-verification source groups and mesh
  transport preference orders return caller-safe snapshots instead of mutable
  backing collections.
- Removed raw relay authentication credential values from challenge-validation
  failure logs and added a remediation scanner guard for that placeholder class.
- Replaced raw exception-message API responses in Spotify, SongID, and
  Listening Party controllers with stable client-facing errors, and stopped
  reflecting Spotify OAuth callback error query text into the callback page.
- Hardened upstream-maintenance workflows so upstream sync is manual-only,
  fork writes target `main`, and automated upstream-access PR/issue creation has
  the explicit permissions and base branch it needs.
- Normalized formatter output in backend async-observation paths so repo lint
  passes after the council cleanup.

## [2026050600-slskdn.227] — 2026-05-06

This supersedes `build-main-2026050600-slskdn.226`, which built all platform
artifacts but stopped before release publication because the exact versioned
changelog section was missing.

- Fixed distributed Soulseek search request parsing so opaque signed 32-bit
  tokens from live peers, including negative values, are accepted instead of
  rejected as invalid counters.
- Fixed Gluetun VPN status polling so configured local control endpoints such
  as `http://127.0.0.1:8010` use a no-redirect local-control HTTP client
  instead of the public outbound SSRF guard.
- Fully synced the vendored slskNet.Runtime tracked-file mirror, including
  protocol token emission hardening, and normalized share-scan media attributes
  so stricter runtime validation does not reject corrupt metadata.
- Ran a broader council cycle across backend security, frontend workflows,
  release packaging, and scanner coverage: fail-closed mesh gateway auth,
  POST-only memory dumps, redacted option logs, no-redirect tunnel transports,
  encoded pod/search routes, hardened review/MediaCore list payloads, and
  corrected AUR/PPA/Snap/release-note package drift.
- Synced the vendored slskNet.Runtime council workflow with a whole-section
  sweep register so constructor candidate scans cannot close with unclassified
  hits.
- Hardened relay controller downloads so file serving uses the token-bound
  server filename instead of the caller-supplied filename header.
- Encoded additional Web helper route segments for slskdN, library-health,
  collections, identity, wishlist, and bridge APIs, and hardened more MediaCore
  result panels against malformed nested payloads.
- Fixed MediaCore pod workflow route encoding and primitive timestamp request
  bodies, restored descriptor verification result rendering, and hardened more
  admin/search/share panels against malformed nested list payloads.
- Hardened more room, files, network, Soulseek discovery, and pod Web views
  against malformed list payloads, and disabled redirects for direct NAT/GitHub
  helper HTTP clients.
- Added network-health guardrails across background discovery, native browse,
  backfill candidate lookup, rescue downloads, and content verification probe
  fan-out.
- Hardened MediaCore and Integrations System panels against malformed list
  payloads, and corrected package/build docs for current env vars and dev build
  tags.
- Hardened Browse, Chat, Rooms, System, Swarm Analytics/Visualization, and
  Transfers against malformed persisted state and nested list payloads.
- Added Soulseek safety limiter and cancellation propagation to the compatibility
  user browse endpoint.
- Switched AcoustID, MusicBrainz, release-graph, and Pushbullet integrations to
  the no-redirect outbound HTTP client, and stopped logging raw AcoustID
  fingerprints.
- Fixed build tag workflow drift for `build-dev-*` tags, corrected GitHub tag
  globs, and aligned Helm/TrueNAS charts with the published image repository and
  real runtime environment variable names.
- Fixed the first bug-council burn-down batch across Web URL/state workflows,
  Soulseek network-health guardrails, backfill probe accounting, secret-safe
  logs, no-redirect outbound callers, release packaging metadata, and default
  Docker non-root execution, plus vendored runtime room-list count validation.
- Guarded multi-source controller search helpers with the shared Soulseek safety
  limiter so wide source-discovery diagnostics cannot bypass network budgets.
- Fixed blocked-user Search helper state so malformed localStorage list shapes
  no longer crash block/unblock workflows.
- Hardened browser-local automation, experience preference, and room activity
  state so malformed object-map storage falls back to safe defaults.
- Hardened Messaging workspace, audio verification cache, and native visualizer
  preset storage against malformed browser-local object-map shapes.
- Fixed the System Events table so malformed event JSON renders as raw text
  instead of crashing the view.
- Hardened browser-local community quality, Discovery Inbox, acquisition plan,
  Discovery Shelf, album decision rule, and listening history arrays so malformed
  item entries are ignored before normalization.
- Hardened listening stats, Smart Radio, playlist intake refresh previews,
  search folding, player badges, Browse/Transfers, Chat/UserCard panels, search
  graph handoffs, filters, and admin diagnostics against malformed list fields.
- Hardened playlist intake and watchlist storage against malformed persisted
  playlists, tracks, watchlists, and expansion candidates.
- Hardened quarantine-jury and listening-party Web API list helpers so malformed
  payloads return empty arrays instead of truthy non-list values.
- Hardened Contacts, Collections, Shares, Share Groups, Soulseek Discovery, and
  Player launcher list handling so malformed API payloads cannot enter list
  render state.
- Fixed File Explorer API path encoding so Unicode file and directory names are
  UTF-8 base64 encoded and URL-safe in route segments.
- Hardened Discovery Graph saved branches and graph node/edge rendering against
  malformed browser-local storage and non-array API payloads.
- Hardened System Events, Bridge clients, and Album Completion list handling
  against malformed API payloads, including nested album track arrays.
- Hardened Collections item search and Federated Taste recommendation lists
  against malformed non-array API payloads.
- Hardened Search Detail user notes, Album Decision rule candidates, and
  Federated Taste nested reason/source actor lists against malformed list fields.
- Hardened Discography Coverage, Source Providers, and watchlist expansion
  summaries against malformed nested release, track, capability, priority, and
  expansion-candidate lists.
- Hardened Messaging workspace hydration against malformed conversations,
  joined rooms, pods, and pod channel lists.
- Hardened App navigation activity, legacy Chat, and legacy Rooms list handling
  against malformed conversation and room API payloads.
- Hardened Artist Release Radar subscriptions, notifications, and muted release
  group counts against malformed list payloads.
- Hardened Library Health, Jobs, Mesh, realm conflict review, Pods, Messaging pod
  channels, port forwarding, search provider badges/ranking, and shared stream
  URL handling against malformed lists and unsafe path segments.
- Hardened non-versioned route checks and Pushbullet notification logging.
- Synced the vendored `slskNet.Runtime` bug council ledger and remediation
  baseline checks.
- Extended Soulseek type-1 obfuscation across compatible peer-message,
  distributed-message, and file-transfer streams while preserving regular
  direct/indirect fallback.
- Added mesh/DHT private and anti-DPI transport selection without dummy
  connection attempts, plus sanitized logging for bridge searches/downloads,
  DHT store keys, and remote metadata searches.
- Added logged-in and logged-out footer build metadata with a GitHub-backed
  slskdN release check that surfaces newer packaged builds from
  `snapetech/slskdn`.
- Replaced the Web UI ASCII/logo placeholders with generated slskdN favicon,
  PWA, login, and footer logo assets.
- Refined generated logo usage with a larger login lockup and transparent
  favicon/PWA/footer icons so small app marks no longer render as dark boxes.
- Added a prominent README funnel for early testers of the independent Rust
  `slskr` rewrite targeting slskdN feature parity and Soulseek-network
  compatibility.
- Bounded release-gate commands and tag build jobs with explicit timeouts so a
  stalled test or child process fails with an actionable gate section.
- Fixed manual publish cleanup so `bin/publish --output` clears the configured
  output directory before producing a deployable payload.
- Fixed the VPN ingress migration banner so current Soulseek and mesh/DHT
  ports come from loaded configuration instead of hard-coded defaults.
- Kept live Soulseek-account mesh smoke tests out of normal release preflight
  unless `SLSKDN_RUN_LIVE_MESH_ACCOUNT_TESTS` is explicitly enabled.
- Fixed the non-bin AUR source package on Arch .NET 10 SDK builds by opting the
  framework-dependent RID publish out of missing prune package data enforcement.
- Hardened shared and release-channel publish commands with the same .NET prune
  package data opt-out used by the AUR source build.
- Fixed systemd package permissions so the `slskd` daemon owns its config/state
  paths, new files remain group accessible, and setup docs explain optional
  user membership in the `slskd` group.
- Carried the same tmpfiles-based permission metadata through tag-release AUR,
  RPM, and PPA packaging.
- Hardened remote path handling, relay transfers, streaming tickets, multi-source
  output paths, config writes/debug redaction, memory dump cleanup, login attempt
  tracking, and mutating API role checks from the security audit.
- Added follow-up security hardening for fork-specific mutating APIs, SongID and
  source-feed outbound fetches, anonymous pod verification/discovery bounds,
  release-installer checksum verification, script logging, and ListenBrainz
  token storage.
- Hardened follow-up outbound and relay security by binding anonymous API rate
  limits to authenticated principals, disabling guarded HTTP redirects with
  public-IP connect validation, validating relay-agent download paths, and
  bounding relay and pod mesh download writes.
- Fixed pending runtime, Wishlist, messaging, Browse, and room UI issues,
  including mesh rendezvous peer-message handling, capability envelopes, saved
  Wishlist search completion state, Browse action rendering, and room API
  response normalization.
- Fixed remaining shared Web API helpers for MediaCore and options endpoints,
  including response handling for JSON string bodies.
- Made Browse, Users, and Chat user-target navigation URL-addressable so user
  actions keep working when opened in a new tab, refreshed, or restored.
- Made governance, pod, and ActivityPub signature handling fail closed unless
  signer identity can be verified, and replaced unstable hash-code identifiers
  with stable SHA-256-derived values.
- Tightened release packaging metadata with passive PPA FTP settings, upstream
  tmpfiles checksums, and RPM directory ownership cleanup.
- Fixed remediation check invocation from both the repo root and `src/web`,
  restored direct `./bin/lint` execution, reconciled stale project audit
  entries, and clarified how frontend build assets are copied into backend
  publish output.
- Completed the route remediation closeout by adding versioned aliases for the
  remaining active native slskdN, VirtualSoulfind, and Audio legacy routes,
  extending route alias tests, and enforcing remediation documentation command
  references in the combined baseline.
- Closed project gap-assessment fixes: aligned PR workflows to `main`, added
  remediation baseline checks to the release gate, documented vendored runtime
  and federation diagnostics behavior, corrected incomplete packaging TODOs,
  implemented Library Health album-completion remediation jobs, fixed playback
  buffer priority classification, and split stable MediaCore workflow helpers
  into smaller modules.
- Documented the runtime capability and Soulseek mesh rendezvous additions in
  the README, API/config/native-discovery references, and a new documentation
  audit, alongside MediaCore/session surface follow-up coverage.
- Synced the vendored `slskNet.Runtime` fork with peer capability descriptors,
  signed slskdN capability handshakes, mesh rendezvous helpers, wishlist
  scheduling primitives, protocol-count hardening, and slskdN API/UI integration
  for runtime peer capability discovery.
- Shortened the logged-out browser title, description, and PWA name to
  `slskdN`, removing the old unofficial-fork tagline.
- Fixed admin restart argument forwarding, streaming limiter cleanup during
  failed setup, and passthrough no-auth docs for the required CIDR gate.
- Updated the login wordmark to show `slskdN` with a baseline-aligned block
  `N` suffix.
- Filtered release-note highlights so CI, release plumbing, documentation
  gotchas, tests, and repo-maintenance commits do not crowd out
  user-facing changes.
- Stopped generated release notes from including commit-detail sections by
  default; release pages now stay focused on product highlights unless commit
  details are explicitly requested for diagnostics.
- Added native Soulseek discovery support for interests, hated interests,
  recommendations, item recommendations, similar users, and user-interest
  lookup through the vendored runtime and slskdN API.
- Surfaced native discovery in the Web UI with Search handoffs, review-only
  Wishlist handoffs, lazy user-card interest lookup, Federated Taste opt-in,
  and Messages batch private-message sending.
- Documented Soulseek type-1 obfuscation activation, native discovery
  workflows, compatibility posture, and safety limiter buckets.
- Added shared UDP port handling so the DHT rendezvous listener can keep the
  public `50305/udp` forward while demuxing DHT rendezvous packets, UDP overlay
  control envelopes, and QUIC overlay traffic proxied to a loopback MsQuic
  listener.
- Quieted normal QUIC probe disconnects so successful handshake-only checks no
  longer log warning stack traces when the peer closes before opening a stream.
- Kept UDP overlay and QUIC active together on the shared mesh UDP port instead
  of treating QUIC as a replacement for UDP overlay.
- Routed QUIC short-header packets through the shared UDP demux so post-restart
  QUIC traffic is not mistaken for malformed UDP overlay envelopes.
- Switched slskdN to the private `slskNet.Runtime` Soulseek.NET-derived
  runtime fork for local builds, vendored the runtime source under
  `vendor/slskNet.Runtime`, activated Soulseek type-1 peer-message
  obfuscation runtime wiring, and live-validated a test host with regular and
  obfuscated listeners plus a successful Soulseek search/download smoke.
- Added Docker Hub publishing as a release image channel at `snapetech/slskdn`
  when `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` are configured, while keeping
  GHCR publishing active.
- Excluded the vendored `slskNet.Runtime` source tree from slskdN formatting
  enforcement while keeping local builds pointed at the in-repo runtime project.
- Added Launchpad SFTP upload support for PPA releases when
  `LAUNCHPAD_SFTP_KEY` is configured, pinning Launchpad SFTP to IPv4 and
  retaining the signed anonymous FTP upload path as a fallback.
- Removed the separate `sftp pwd` Launchpad PPA shell probe after it timed out
  on the upload endpoint despite account-level SSH key acceptance; the SFTP
  path now relies on noninteractive SSH config plus the bounded `dput` upload.
- Kept PPA FTP fallback active when `LAUNCHPAD_SFTP_KEY` is configured but the
  runner cannot reach Launchpad's SFTP port.
- Made the PPA upload step fall through to anonymous FTP when the selected
  SFTP `dput` upload fails after preflight.
- Kept PPA fallback on Launchpad's documented `dput` FTP path after raw `curl`
  uploads proved brittle on slow or delayed FTP greetings.
- Hardened release publishing by copying vendored runtime projects into Docker
  builds and adding slower Chocolatey timeout retries with duplicate-package
  success handling.
- Made Launchpad PPA and Chocolatey publishes best-effort in tag releases so
  external mirror outages no longer mark the entire release workflow failed.
- Fixed the AUR `slskdn` source package so clean `yay` builds enter the
  case-correct GitHub archive root (`slskdN-<tag>`) instead of failing on a
  missing lower-case `slskdn-<tag>` directory.
- Bumped the AUR `slskdn` source package release for the archive-root fix so
  AUR helpers see the corrected PKGBUILD as a package update.
- Fixed the standalone PPA retry workflow so rebuilt frontend assets are copied
  into a created `publish-linux-x64/wwwroot` directory.
- Simplified the ingress-port migration notice to show only the five old
  public forwards and two current public forwards, with no public endpoint
  detection, active/not-reported status, or obfuscation-listener row.
- Kept VPN provider forwarded ports separate from the local Soulseek listener
  so Gluetun status polling no longer tries to rebind `soulseek.listen_port` to
  the public/NAT port.
- Fixed browser audio fingerprint hashing so CI, Node/jsdom tests, and browsers
  copy file data into a native typed-array digest input before passing it to
  WebCrypto.
- Restricted tag release automation to the stable `build-main-*` path after
  retiring the obsolete dev package channel.
- Restored the stable Nix release metadata job as an explicit main release
  target while keeping dev-channel releases disabled.
- Removed the obsolete `slskdn-dev` package channel from active release
  automation, packaging validation, install docs, and dev-only manifests.
- Removed the standalone Discovery Inbox and Import Staging surfaces from the
  active Web UI path, folded the intake/review wording into current docs and
  settings, and kept the player visualizer deployment notes current after live
  test-host Playwright verification.
- Removed stale Wishlist feed-import and Discovery Inbox review entry points
  from the active Wishlist surface.
- Removed remaining player, playlist-intake, and search-test references to
  Discovery Inbox actions after the active review surface cleanup.
- Removed stale player and federated taste recommendation test references to
  Discovery Inbox promotion paths.
- Removed remaining Wishlist and Library Health inbox-promotion paths and
  aligned focused tests with direct request/review states.
- Updated Library Health focused coverage to assert quarantine packet copying
  instead of the removed inbox-promotion action.
- Removed stale dev-channel packaging, docs, release-note templates, and
  workflow references so releases use the stable `build-main-*` path only.
- Added Contacts invite QR generation and QR image scanning for slskdN invite
  links, with focused UI coverage, constructor-compatible scanner tests, and
  browser capability checks.
- Added adaptive DHT rendezvous bootstrap timeouts for warm, cold, and LAN-only
  startup paths, with bootstrap logs reporting saved node-table bytes.
- Replaced placeholder swarm analytics values with metrics derived from active
  downloads and peer throughput samples.
- Moved mesh service MessagePack record defaults into constructors to avoid
  deserialization resetting initialized `init` properties, and cleaned the
  adjacent multi-source comment formatting warning.
- Cleaned consensus policy success wording and added coverage for consensus
  required versus not-required decisions.
- Clarified FLAC analyzer heuristic wording and mesh service request/response
  capability messages, including mesh sync and bridge proxy startup wording.
- Removed stale TODO wording from mesh neighbor, signal handler, and transfer
  telemetry comments, and clarified swarm/rescue unavailable-state messages.
- Reconciled the remaining feature-expansion task logs for QR invites, native
  MilkDrop3, DHT bootstrap diagnostics, SongID research-only MIR lanes, analyzer
  cleanup, and production placeholder burn-down.
- Clarified mesh gateway, scheduler, bridge, LAN, and Mesh DHT validation
  comments to reflect current local validation and fetch-path enforcement.
- Removed more stale placeholder comments from privacy, DHT publishing, pods,
  transport selection, descriptor publishing, and mesh circuit code paths.
- Added real privacy-layer batch-created stats and clarified gated
  hash-from-audio and Tor control-port wording.
- Enforced pod mesh message size by UTF-8 byte count and marked the placeholder
  cleanup plan complete.
- Defaulted the Soulseek type-1 obfuscation option on in compatibility posture
  while reporting pending runtime support explicitly.
- Surfaced the current Soulseek type-1 obfuscation plan through native
  capabilities and System -> Network, including runtime support state and
  configured effective obfuscated port.
- Added Soulseek type-1 peer/distributed/transfer obfuscation configuration options and
  validation, including explicit mode and dedicated obfuscated listen-port
  checks, plus a runtime-support plan that reports the current unsupported
  Soulseek.NET wire-path state.
- Updated the Getting Started guide around current default ports, direct manual
  Search behavior, Acquisition Review, Multi-Source Rescue, Messages, and
  System administration surfaces.
- Added player visual-tile regression coverage for cycling back to album art
  and opening the native visualizer full-window view from spectrum mode.
- Expanded the player visual tile controls into explicit analyzer/native
  visualizer mode buttons with window and fullscreen affordances, including
  updated layout, engine remount handling, renderer disposal cleanup, and
  WebGPU-to-WebGL2 fallback status/name handling with regression coverage.
- Fixed the startup mesh transport registration build break by reading the
  separately bound `Mesh:Transport` section directly instead of assuming it is
  present on `OptionsAtStartup`.
- Fixed sharegroups streaming content resolution so non-advertisable share
  repository entries cannot fall through to allowed-root `path:` fallback
  resolution, and hardened share-token claim binding comparisons with the
  shared constant-time helper.
- Added persisted collection item display metadata for file name, title,
  artist, and album, including share-manifest output and Playlist Intake
  collection-item labels so player and playlist rows can avoid raw content ids
  when metadata is known.
- Added an explicit direct mesh transport runtime gate: `DirectQuicDialer` is
  only registered when QUIC connection/listener support is available, and
  startup warns when direct mesh transport is configured on a host that cannot
  support direct QUIC circuits.
- Added an opt-in NixOS VM smoke harness that builds a minimal NixOS system
  around the flake package, supplies required slskd module settings, boots
  headless under QEMU/KVM when available, and verifies `slskd.service` reaches
  active state through a serial success marker.
- Added a browser-local System -> Network health score with local DHT, mesh,
  HashDb, backfill, and security-signal findings plus a copyable report for
  operations review.
- Added a browser-local media-server sync review plan for Plex,
  Jellyfin/Emby, and Navidrome readiness, including base URL, token, path-map
  checks, explicit adapter review actions, and a copyable report without live
  media-server calls.
- Added browser-local Servarr compatibility review reports for wanted-pull and
  completed-import readiness without calling Lidarr or triggering imports.
- Added copyable Wishlist request review packets with quota, state, manual, and
  automatic-request summaries without starting acquisition work.
- Added Automation Center review history reports for enabled recipes and dry-run
  checkpoints without executing any automation.
- Added explicit live run actions for ready Servarr wanted sync and bounded
  enabled Wishlist searches; both are user-triggered and keep downloads behind
  normal result selection and policy.
- Added a first opt-in native MilkDrop WebGPU renderer foothold with adapter
  probing, debug adapter details, ping-pong feedback textures, and a
  preset-colored fullscreen display pass plus first waveform, shape-outline,
  motion-vector, screen-border, filled-shape, and fallback sprite-quad
  primitive draws, now including textured shape/sprite asset sampling through
  the shared native texture-alias rules and first safe-subset translated
  warp/comp WGSL execution with FFT/waveform helper uniforms and named shader
  texture samplers, plus WebGPU readiness reporting in the native
  compatibility matrix, and exposed the player overlay cycle control for
  Butterchurn, native MilkDrop3 WebGL2, and native MilkDrop3 WebGPU while
  keeping Butterchurn as the default and WebGL2 as the native baseline.
- Expanded System Info setup health into a grouped diagnostic wizard with
  readiness scoring, next steps, API/provider/queue/job checks, and group
  filters for local troubleshooting.
- Added setup-health readiness into the redacted diagnostic bundle so support
  exports include actionable local checks without exposing secrets.
- Added a browser-local mesh evidence review sandbox in System -> Mesh so
  operators can preview provenance, trust-tier, k-anonymity, confidence, and
  privacy gates before applying or publishing any mesh evidence.
- Added a one-at-a-time Discovery Inbox mobile review tray for approving,
  snoozing, rejecting, and navigating candidates without starting network or
  download work.
- Added browser-local community quality reviewer overrides and notes so local
  trust/caution/ignore decisions influence candidate ranking and action
  previews without deleting the underlying evidence.
- Added a mobile-friendly System Info setup-health check with local pass/warn/fail
  readiness cards and a copyable report for connection, identity, shares,
  downloads, restart, URL base, and remote-configuration state.
- Fixed built Web UI hosting under non-root `web.url_base` deployments by
  emitting relative Vite asset references, injecting a mounted `<base>` tag
  for subpath deep links, and adding a subpath smoke test for `/slskd/...`
  routes.
- Added Quarantine Jury route attempts over PodCore, including route attempt
  persistence and API endpoints for dispatch history.
- Added Quarantine Jury manual review and acceptance API endpoints that record
  explicit local release-candidate acceptance without mutating quarantine state.
- Added a System -> Quarantine Jury review workspace for request evidence,
  verdicts, dissent, route attempts, explicit juror routing, and modal-gated
  release-candidate acceptance without moving files.
- Added MusicBrainz overlay export review and approval API endpoints for manual
  upstream submission workflows without auto-submitting edits.
- Added opt-in MusicBrainz overlay edit route attempts over PodCore for selected
  safe peers, with dispatch history and no automatic publication.
- Added realm subject-index governance proposals and trusted review decisions
  so proposed revisions publish to resolution only after explicit acceptance.
- Added Discography Concierge priority metadata from Discovery Graph density,
  release gaps, and existing HashDb/Wishlist evidence without starting network
  discovery.
- Added versioned MusicBrainz library Bloom snapshot preview, inbound likely-gap
  comparison, and review-only Wishlist promotion without exposing filenames,
  paths, file hashes, or exact holdings.
- Added browser-local listening history and player listening stats for recent
  plays, top artists, and top tracks.
- Added listening stats time-range filters and browser-local forgotten-favorite
  derivation from older repeat plays.
- Added browser-local listening genre breakdowns from now-playing genre/tag
  metadata.
- Added explicit listening-stats recommendation seed handoffs from local top
  artists, genres, and forgotten favorites.
- Added browser-local Discovery Shelf action previews from player ratings.
- Fixed Discovery Shelf expiry-watch summary counts to use the same action key
  across shelf storage and player display.
- Added Discovery Shelf policy previews for promote, archive, expiry, review,
  and shared-library consensus gating.
- Added copyable Discovery Shelf policy reports for offline review before any
  promote/archive/expiry action exists.
- Added copyable Library Health text reports from loaded scan summaries and
  issue samples without starting remediation.
- Added copyable Library Health selected-issue action plans for safe-fix,
  replacement-search, and quarantine-review previews without applying actions.
- Added copyable Library Health replacement search seed exports from selected
  issues without opening Search, browsing peers, or downloading files.
- Added copyable Library Health quarantine review packets from selected risky
  issues without changing quarantine state or moving files.
- Added copyable Library Health safe-fix manifests from selected auto-fixable
  issues without creating remediation jobs or applying fixes.
- Added browser-local Watchlist release, country, and format filters in
  Discovery Inbox.
- Added browser-local listening history import/export for media-server CSV/JSON
  play-history files, with duplicate suppression.
- Added bounded player similar-track auto-queue from already-known recent
  session tracks.
- Added a player queue manager modal with current, upcoming, recent-session,
  remove, clear-upcoming, previous, and next controls.
- Added a review-first smart-radio seed modal in the player that creates
  explicit Search handoff queries from the current track metadata.
- Added keyboard shortcuts for player playback, seeking, mute, equalizer,
  lyrics, and visualizer controls while ignoring editable fields.
- Added browser-local now-playing ratings plus source, match-confidence, and
  verification badges in the player display.
- Added a browser-local Discovery Shelf built from player ratings, with visible
  promote/archive/expiry review previews that do not mutate files.
- Added a conservative per-artist release radar service that turns
  SongID-confirmed federated WorkRef observations into local notifications
  without polling MusicBrainz, browsing peers, searching Soulseek, or starting
  downloads.
- Added SongID queue-summary and evidence-package APIs so operators can review
  queued/running run state and export capped candidates, forensic evidence,
  plans, acquisition options, scorecards, segments, mix groups, and artifact
  references without starting searches or downloads.
- Added restart-safe persistence for artist release radar subscriptions,
  muted release groups, notifications, and duplicate observation suppression.
- Added explicit selected-peer route attempts for artist release radar
  notifications over PodCore, with signed local envelopes and persisted route
  history.
- Added graph-aware federated taste recommendations plus explicit review-only
  handoffs to Wishlist, artist release radar subscriptions, and Discovery Graph
  previews.
- Added realm subject-index conflict reports and read-only API endpoints for
  accepted indexes, recording resolutions, and provenance-preserving conflict
  inspection.
- Added backend realm subject-index authority decisions so accepted index
  authorities can be locally disabled or re-enabled; disabled authorities are
  excluded from recording resolution and conflict reports without mutating
  governance documents or publishing changes.
- Added restart-safe MusicBrainz overlay persistence for signed edits, route
  attempts, and manual upstream export approvals.
- Added Quarantine Jury audit reports for accepted, pending, stale, routed, and
  failed-route review states without changing quarantine enforcement.
- Added Quarantine Jury release evidence packages for locally accepted
  release-candidate recommendations, including request evidence, signed juror
  verdicts, route attempts, acceptance snapshots, and drift warnings without
  changing quarantine enforcement.
- Added a local source-feed import preview parser that turns artist/title rows
  into deduped suggestions with skipped-row reporting.
- Expanded source-feed imports with Spotify provider fetching for public
  playlist/album/track/artist/user playlist URLs, per-import bearer-token
  support for liked/saved/followed/current-user feeds, local CSV/text/M3U/RSS
  parsing, and a Wishlist Import Feed flow into Discovery Inbox review.
- Added Spotify account connection for source-feed imports, including OAuth
  authorization, encrypted refresh-token storage, token refresh, disconnect,
  and Wishlist Import Feed controls that use the connected account for private
  liked/saved/followed playlist feeds.
- Expanded non-Spotify source-feed URL imports with Apple Music/iTunes lookup,
  ListenBrainz public-listens import, and metadata-page fallback for YouTube,
  Bandcamp, Last.fm, and Apple URLs.
- Added optional API-key backed source-feed expansion for YouTube playlists and
  Last.fm loved/recent/top track URLs.
- Added System Integrations controls for source-feed provider settings,
  including Spotify, YouTube, and Last.fm on/off toggles, masked credential
  inputs, validation warnings, and tooltip-backed apply/reset actions.
- Added source-feed import history/audit endpoints. Preview runs now persist
  bounded app-dir history entries with provider/source metadata, source
  fingerprints, request/result counts, network-request counts, skipped-row
  samples, and suggestion samples without storing provider bearer tokens.
- Added System Integrations controls for Pushbullet, Ntfy, and Pushover
  notification settings, including on/off toggles, private-message and
  room-mention triggers, masked secret replacement, validation warnings,
  runtime apply, YAML save, reset, and tooltips.
- Added System Integrations controls for FTP completed-download uploads,
  including connection, credential, encryption, retry, overwrite, certificate,
  runtime apply, YAML save, reset, validation warnings, and tooltips.
- Added a combined Messages workspace for direct chats and rooms with
  multi-panel windows, compact sizing, collapse/restore dock behavior, and
  compatibility routes from the old Chat and Rooms pages.
- Added a Web UI surface audit covering admin/user areas that still need clearer
  signalling, toggles, settings, and status affordances.
- Added a redacted browser diagnostic bundle in System Info that can be
  inspected and copied without exposing passwords, tokens, cookies, secrets, or
  API keys.
- Added optional media-server integration readiness cards and local path
  diagnostics for Plex, Jellyfin/Emby, and Navidrome in System Integrations.
- Added a local Servarr setup readiness checklist in System Integrations for
  base URL, API key, wanted pull, completed import, and path-map sanity.
- Added a Wishlist request portal summary with enabled, automatic, review, and
  quota-style counts derived from current requests and Discovery Inbox state.
- Added bounded Automation Center dry-run reports with cooldown, max-runtime,
  impact, and approval-gate visibility while still executing no recipe work.
- Added a signed local MusicBrainz overlay-edit API that applies corrections at
  release-graph read time with original/effective graph provenance.
- Added browser-local Mesh Evidence Policy controls in the Mesh tab with
  inbound trust gates, provenance-required status, and outbound publication
  toggles that default to private/off.
- Added browser-local community quality signals for Search results, including a
  local caution report affordance and ranking context that never publishes
  global peer reputation.
- Added review-only Discovery Inbox acquisition plans for approved candidates,
  including visible provider order and manual-execution policy without starting
  searches or downloads.
- Added explained Search candidate ranking keyed to acquisition profiles, file
  evidence, availability, provider hints, and prior download history.
- Added mobile-specific Discovery Inbox and Import Staging review layouts with
  full-width touch actions and card-style staged import rows on narrow screens.
- Added browser-local failed-import denylist handling for Import Staging so
  rejected files are tracked and matching re-adds return as failed review rows.
- Added opt-in browser-local SHA-256 fingerprint verification for newly staged
  import files, storing only verification metadata in the staging queue.
- Added a local import metadata matcher with confidence, parsed identity,
  evidence, warnings, and Import Staging row/bulk matching controls.
- Added an import-staging review surface with browser-local file metadata,
  staged/ready/imported/rejected/failed states, file-picker intake, and no
  library mutation.
- Added first Wishlist/request-state unification in the Web UI, including
  shared acquisition request labels and a Wishlist-to-Discovery-Inbox review
  action that does not start downloads.
- Added the first browser-local Discovery Inbox for safe acquisition review,
  including persistent candidate state, bulk approve/reject, network-impact
  text, navigation, and Search-page save-to-inbox entry point.
- Refined browser player controls and equalizer storage behavior during the
  native MilkDrop player integration work.
- Hardened browser player visualizer storage access so blocked localStorage
  contexts fall back cleanly instead of crashing player initialization.
- Routed additional browser-local search and Discovery Graph storage reads
  through safe storage helpers for privacy-locked browser contexts.
- Routed the Network DHT exposure acknowledgement through safe browser storage
  helpers.
- Replaced several placeholder mesh/security tests with concrete helper-backed
  assertions and shared test-project imports.
- Added first Windows/macOS split-routing command scaffolding to the VPN agent
  and documented the platform support boundary.
- Made footer/player reserved height CSS variables update from measured element
  sizes so the main scroll area tracks the real chrome height.
- Added richer native `.milk2` composite controls with preset-defined transition
  durations and alpha/additive/screen/multiply secondary blend modes.
- Added native MilkDrop compatibility-matrix q-register coverage and a
  MilkDrop3-style q-register fixture in browser smoke coverage.
- Added native MilkDrop dense primitive validation with a 40-shape/20-wave
  fixture included in compatibility and browser smoke coverage.
- Added native MilkDrop transition modes for cut, fade-through-black, and
  overlay behavior in addition to the default crossfade.
- Expanded native MilkDrop shader-side audio access to 64 FFT bins plus signed
  waveform bins through `get_waveform(pos)`.
- Added native MilkDrop active `.shape` and `.wave` fragment selectors with
  selected-fragment export and remove actions that persist edited presets
  locally.
- Added persisted native MilkDrop automation settings for beat-count and timed
  preset interval selection.
- Added first native MilkDrop parameter editing for decay, zoom, rotation, and
  waveform color/alpha, plus full active-preset text export for edited presets.
- Added native MilkDrop bounded parameter randomization, pointer-fed mouse
  variables, and a compact debug snapshot overlay for active native presets.
- Added browser-local native MilkDrop playlist rename support for active preset
  playlists.
- Added browser-local native MilkDrop FPS caps with debug frame-time readout
  for visible GPU-load tuning.
- Added native MilkDrop quality presets, WebGPU capability reporting in debug
  details, and WebGL context loss/restore coverage in the native smoke test.
- Added native MilkDrop performance measurement for curated fixtures or local
  preset files/folders, plus a bounded translated-shader cache for repeated
  shader bodies.
- Added app-wide interaction affordances for hover, focus-visible, disabled,
  and clickable row states.
- Added acquisition-profile request plumbing, documentation audit notes, and
  native MilkDrop expression coverage refinements from the workspace checkpoint.
- Added a System Automation Center scaffold with visible opt-in recipe states
  and local dry-run checkpoints.
- Added missing button labels/titles in chat, rooms, users, and player browser
  entry points.
- Switched CI self-hosted/GitHub-hosted branching to GitHub's runner
  environment context instead of local runner-name checks.
- Refined dark-mode surface styling for Semantic UI shells plus chat, rooms,
  browse, and search panels.
- Added first acquisition-profile selector scaffolding to Search so users can
  persist conservative, mesh-preferred, metadata-strict, and other search
  intent profiles.
- Prepared the `2026042900-slskdn.204` stable release metadata.
- Made Winget release-version metadata validation opt-in so stable releases
  that intentionally skip Winget are not blocked by stale Winget URLs.
- Added the WebGL MilkDrop3 port design, making the external visualizer
  launcher an interim bridge while the long-term target becomes a
  browser-native MilkDrop3-compatible renderer inside slskdN.
- Introduced the browser visualizer engine adapter boundary used by the
  current Butterchurn implementation.
- Rejected incoming Soulseek upload requests from the daemon's own username so
  self-originated requests do not appear as uploads to yourself.
- Fixed auto-replace so missing/legacy state follows the opt-in
  `AutoReplaceStuck` setting instead of defaulting enabled, and so replacement
  candidates exclude the daemon's own Soulseek username.
- Added the browser-native MilkDrop visualizer implementation work, including
  preset parsing/expression tests, renderer smoke coverage, local preset import,
  player integration controls, selected-preset removal, and browser-local preset
  library cleanup.
- Added native MilkDrop shader-subset translation coverage for supported
  `warp_shader` and `comp_shader` return expressions.
- Added native MilkDrop textured-shape rendering, multi-fixture browser smoke
  coverage, and browser-local image texture asset imports with skipped-asset
  reporting plus path/basename/stem lookup for preset texture references.
- Fixed native `.milk2` import inspection so every preserved preset body is
  compatibility-checked before the file is accepted.
- Added the first native `.milk2` double-preset composite path, rendering the
  primary preset body normally and blending compatible secondary bodies over it.
- Added first native MilkDrop sprite/image primitive parsing and textured-quad
  rendering backed by imported image texture assets.
- Scoped imported native MilkDrop image assets per preset so multi-preset packs
  do not persist unrelated images with every imported preset.
- Added a native MilkDrop preset-folder import affordance for browsers that
  expose directory-relative file paths.
- Added first native MilkDrop renderer-set crossfades for preset/import changes
  and `.milk2` secondary composite-alpha controls.
- Added native MilkDrop `.shape` and `.wave` fragment import/export affordances
  with active-preset merge and browser-local persistence.
- Added native MilkDrop beat and timed automatic preset change modes with
  browser-local mode persistence.
- Added native MilkDrop local preset-bank controls for favorites, favorites-only
  filtering, previous-preset history, next-library cycling, and random jumps.
- Added native MilkDrop preset-bank search that persists locally and scopes
  imported-preset next/random navigation to the filtered result set.
- Added browser-local native MilkDrop preset playlists, including save-from-filter,
  select, clear-active, delete, and playlist-scoped next/random navigation.
- Added renderer-wide native MilkDrop q1-q64 initialization and q-register
  propagation across global, custom wave, shape, and sprite evaluation stages.
- Added first native MilkDrop shader uniform support for q1-q64 and
  bass/mid/treble audio variables in supported warp/comp shader expressions.
- Added shader-side native MilkDrop `get_fft()` and `get_fft_hz()` support for
  translated warp/comp shaders using a compact FFT uniform array.
- Added native MilkDrop primitive-field aliases so custom waves, shapes, and
  sprites honor common preset names such as `nSamples`, `bSpectrum`,
  `bUseDots`, `bDrawThick`, `bAdditive`, `bTextured`, and `texName`.
- Added first native MilkDrop screen-border rendering for classic `ob_*` and
  `ib_*` preset values.
- Added first classic native MilkDrop waveform modes with `wave_mode`,
  `wave_x`, `wave_y`, `wave_a`, `wave_scale`, and `wave_smoothing` support.
- Expanded the native MilkDrop shader translator to accept safe straight-line
  temp declarations plus common HLSL helper aliases including `frac`, `fmod`,
  `rsqrt`, and `atan2`.
- Added translated native MilkDrop shader viewport context with `resolution`,
  `pixelSize`, `aspect`, `texsize`, and generated `x/y/rad/ang` coordinates.
- Added native MilkDrop shader-body wrapper support so safe `shader_body { ... }`
  warp/comp shader blocks are translated instead of rejected.
- Added first translated native MilkDrop shader named-texture sampler support,
  binding up to four preset texture samplers with procedural fallback.
- Added first safe translated native MilkDrop shader conditional support for
  simple `if (...) ret = ...; else ret = ...;` bodies.
- Added safe translated native MilkDrop shader temp reassignment support for
  declared local variables.
- Added native MilkDrop compatibility matrix reporting for curated fixtures and
  local `.milk` / `.milk2` files or folders, including high-count wave/shape
  metrics for real preset-pack pressure.

## [2026042900-slskdn.204] — 2026-04-30

This supersedes `2026042900-slskdn.203`, whose tag failed the release gate
before build/test because optional Winget release-version metadata was treated
as mandatory.

- Added a MilkDrop visualizer (butterchurn) to the Web UI player bar with
  inline-thumbnail, full-browser-window, and native-fullscreen modes. The
  butterchurn engine and preset pack are loaded via dynamic imports so they
  ship as separate chunks and stay off the critical path until the user
  toggles the visualizer on.
- Expanded the Web UI player into a footer-safe drawer with collapse/expand,
  previous/next, rewind, fast-forward, local mute, Media Session handlers, and
  empty-state launchers for Collections plus shared/downloaded local audio.
- Added Winamp-style Web UI player controls: shared Web Audio graph plumbing,
  10-band persisted EQ presets, lightweight spectrum/oscilloscope rendering,
  LRCLIB synced lyrics, ListenBrainz now-playing/scrobble submission, optional
  crossfade, Document Picture-in-Picture spectrum output, and karaoke-style
  center-channel reduction.
- Replaced the player empty-state collection/file dropdowns with full modal
  browsers: a two-pane collection picker with playable collection items, and a
  searchable shared/downloaded local-audio table with explicit play actions.
- Documented the integrated player, modal pickers, local-root streaming,
  player extras, listening-party behavior, and PWA/mobile playback in the
  README and feature guides.
- Made the stream locator resolve `sha256:` and path-based local audio IDs from
  configured share/download roots when the file is allowed locally but has not
  yet been persisted into the indexed `content_items` table.
- Added stream ticket plumbing for browser playback flows and tightened related
  pod/listening-party controller behavior so the expanded player, local file
  browser, and radio/listen-along paths share the same integrated slskdN
  streaming boundary.
- Improved player visualizer fallback styling so hidden canvases and fallback
  surfaces render cleanly when MilkDrop cannot draw.
- Added an opt-in authenticated external visualizer launcher API and config
  surface so local deployments can start a configured helper such as MilkDrop3
  without allowing arbitrary browser-supplied commands.
- Added the WebGL MilkDrop3 port design and a visualizer engine boundary that
  keeps Butterchurn behind an adapter while the browser-native MilkDrop3 path
  is built incrementally.
- Added Discography Concierge planning and first implementation pieces,
  including MusicBrainz artist coverage services/API/UI, manual missing-track
  Wishlist promotion, and supporting docs/tasks for mesh/social music
  discovery work.
- Fixed Gold Star Club revocation handling to avoid nullable service access
  and ambiguous filesystem type resolution.
- Fixed mixed-source accelerated downloads so the Soulseek sequential-failover
  loop filters out mesh-overlay sources before calling `ISoulseekClient`.
  Mesh sources now stay on the mesh-aware path, and raw Soulseek failover only
  dials raw Soulseek peers.
- Added Layer 1 listening parties: a persistent Web UI player streams
  `ContentId` values through the existing range endpoint, updates Now Playing
  from browser playback, and publishes pod-scoped listen-along metadata over
  stored/routed pod messages plus SignalR fan-out without relaying audio bytes.
- Added local browser mute and mobile/PWA safe-area handling to the Web UI
  player so listen-along streams can keep playing while muted on one device.
- Added an opt-in slskdN radio directory for listed listening parties, with a
  mesh/DHT-backed announcement index and an integrated host radio stream
  endpoint gated by a separate mesh-streaming toggle.
- Fixed listening-party startup by keeping the live registry singleton while
  resolving scoped PodCore message storage per publish operation.
- Fixed Vite dev Web UI startup so browser API calls use the same-origin proxy
  by default instead of bypassing it with CORS-blocked absolute daemon URLs.
- Fixed Gold Star Club startup so its reserved pod id and default channel id
  conform to PodCore validation instead of crashing the host.
- Clarified Gold Star Club leaving as irrevocable in the Web UI and docs, with
  a confirmation prompt before permanent revocation.
- Reworked pod, room, chat, and contact social affordances so saved
  conversations and joined rooms rehydrate from server state, pods can be
  created or discovered from the main Pods page, and discovered pods can be
  saved locally for daemon-backed retrieval after restarts.
- Added a direct save action for discovered pods so remote discovery results
  can be promoted into the local daemon-backed pod list instead of remaining
  view-only search results in the Pods sidebar.
- Hardened security boundaries by requiring authentication for ActivityPub
  outbox publishing, adding SSRF and size guards to HTTP share backfill,
  fixing file-list path prefix authorization, removing query-string API-key
  CSRF exemptions, and avoiding a secondary service-provider build during
  SignalR API-key promotion.
- Fixed AUR upgrade hooks so an already-running `slskd.service` is restarted
  after package upgrades, while fresh installs and stopped services remain
  untouched until the operator starts them.
- Fixed stable Winget publishing so main release workflows fail loudly when
  `WINGETCREATE_GITHUB_TOKEN` is missing instead of reporting a fake-green
  skipped submission, and added a manual Winget publish workflow for retrying
  an existing release tag after credentials are configured. The stable Winget
  jobs now submit generated manifests directly so the first `snapetech.slskdn`
  PR can be opened before the package exists in `microsoft/winget-pkgs`, and
  stage manifests in the repository-shaped path expected by WingetCreate. The
  stable locale description now emits valid YAML block indentation, and the
  zip portable metadata now follows accepted winget-pkgs layout. Stable Winget
  workflow staging now uses the same numeric dotted package version emitted in
  the manifests, and the initial submission path uses the generated multi-file
  version, installer, and default locale manifests instead of a temporary
  singleton manifest rejected by Winget service validation.
- Changed stable Winget publication to an optional high-value release step:
  main release tags still regenerate checked-in Winget metadata, but public
  `microsoft/winget-pkgs` PR submission now happens only through the manual
  `Publish Winget` workflow.

## [2026042900-slskdn.202] — 2026-04-30

- Reworked the fixed Web UI header and footer chrome so primary navigation,
  utility actions, brand, speeds, network counters, and transport icons align
  as distinct rails instead of crowding together in live screenshots.
- Polished transfer rows with peer browse links, throttled queued-position
  refreshes, and batch-aware delete cleanup for completed batch downloads.
- Limited automatic queue-position checks on the Downloads page to a small,
  cached refresh batch instead of asking every queued peer every second.
- Linked transfer user headers to Browse so upload/download rows provide the
  same direct peer affordance as search results.
- Fixed delete-on-remove for successful batch downloads so files stored under
  the batch completion folder are resolved correctly.
- Documented the supported advanced search filter text syntax in the README.
- Fixed stale unit-test compile blockers in the release gate by removing an
  assertion against the retired `MusicBrainz.Enabled` option and disambiguating
  `System.IO.File` / `System.IO.Directory` in tests that import `Soulseek`.
- Fixed manual-review SongID Discovery Graph expansion so weak track candidates
  can remain visible without pulling unrelated album, artist, or segment
  context into the neighborhood.
- Updated the `UserService` disposal regression test to account for the
  fixture-owned regex username matcher options listener while still verifying
  `UserService` removes its own listener and Soulseek event handlers.

## [2026042900-slskdn.199] — 2026-04-29

- Made the Search page secondary panels collapsed by default and persisted
  each panel's open/collapsed state per browser.
- Rebuilt the README showcase UI surfaces with compact desktop navigation,
  a shorter fixed footer, a one-row mobile footer rail, cleaner search result
  cards/file lists, and clearer Discovery Graph controls/sparse-state
  messaging.
- Added a first-class Downloads `Accelerated` toggle and API state endpoint,
  persistent discovery/verification probe budgeting, and related UI/docs
  polish for the guarded multi-source rescue path.
- Redesigned the Web UI footer status dock into clearer brand, speed,
  network, transport-health, and fork-note groups with responsive spacing.
- Fixed the Web UI theme picker so browser clicks open a portal-backed menu
  reliably, with the nav trigger labeled as `Theme`.
- Added upstream-compatible configuration aliases for `transfers`,
  `integrations`, nested upload group limits, username blacklist regex
  patterns, reverse-proxy-safe Web UI icon paths, and retry-delay clamping,
  while preserving slskdN's legacy config compatibility with startup warnings.
- Changed the Network dashboard public-DHT exposure warning into a dismissable
  info notice that is remembered per browser, because public rendezvous is an
  intended slskdN feature state that only needs operator awareness.
- Fixed false Network dashboard no-peer diagnostics by treating
  `/api/v0/dht/status` node, discovered-peer, and active-mesh counters as peer
  evidence when the older mesh/discovered peer list endpoints are empty.

## [2026042900-slskdn.198] — 2026-04-29

- Fixed the stable Chocolatey publish job so repeated transient push failures
  fail the workflow instead of reporting a green release with no package
  published, and added a Chocolatey-only manual publish workflow for retrying
  an existing GitHub release with the stored Chocolatey secret. The retry
  matcher now joins PowerShell command output before checking for `504` /
  timeout responses so transient Chocolatey failures are retried correctly,
  and the nuspec is written with Chocolatey's normalized package version while
  keeping installer URLs pointed at the original GitHub release tag.
- Multi-source / swarm download safety pass:
  - Added a first-class Downloads `Accelerated` toggle and transfer API state
    endpoint. Turning it off suppresses underperformance-triggered rescue;
    turning it on lets queued-too-long, slow, or stalled downloads enter the
    conservative rescue path.
  - Added `VerificationMethod.MeshOverlay` so trusted slskdN mesh peers are no
    longer conflated with size-only Soulseek matches, and tagged rescue-mode
    overlay peers accordingly.
  - Split the download policy by source type: parallel chunked downloads now
    only run when every source is mesh-overlay; Soulseek and mixed source
    sets route through a new sequential-failover path that streams from one
    peer at a time and resumes at the current byte offset on stall, producing
    at most one mid-stream cancellation per failover instead of one per
    chunk per peer.
  - Hard-floored `SelectCanonicalSourcesAsync` so multi-source is declined
    (caller falls back to single-source) unless ≥2 sources share a verified
    content hash or every source is mesh-overlay; the explicit-API endpoints
    return a clear 400 instead of silently degrading.
  - Added a persistent per-peer-per-day verification probe budget and a
    `MeshOverlaySourceCount` request flag that skips Soulseek-side 32 KB
    SHA-256 probes entirely when mesh-overlay sources already cover the
    request, capping the visible "transfer cancelled" noise on any single
    Soulseek uploader.
  - Made discovery hash probes share that same budget so discovery cannot
    create extra probe noise beyond the verification cap.
  - Added Prometheus counters for mid-stream cancellations
    (`slskd_swarm_midstream_cancellations_total`), verification probe
    outcomes (`slskd_swarm_verification_probes_total`), hard-floor fallbacks
    (`slskd_swarm_hard_floor_fallbacks_total`), and sequential-failover
    events (`slskd_swarm_sequential_failover_total`) so the network impact
    of the multi-source path is measurable directly.
  - Rewrote `docs/multipart-downloads.md` and the README multi-source
    section to be explicit about scope (default downloads use the standard
    single-source Soulseek path; acceleration is toggle-gated or explicit)
    and document the trust split, hard floor, and probe budget.

## [2026042900-slskdn.197] — 2026-04-29

- Restored the browser tab title to the short slskdN brand name instead of
  showing the release version and fork attribution.
- Fixed the Web UI theme picker so it opens reliably and applies selected
  themes through Semantic UI's controlled dropdown path.
- Smoothed transfer bulk actions by ignoring stale transfer polls and
  optimistically hiding rows after accepted retry/remove operations.
- Made footer transfer speed totals fall back to bytes-over-elapsed-time for
  active transfers when Soulseek has not populated `AverageSpeed` yet.

## [2026042900-slskdn.196] — 2026-04-29

- Removed the top slskdN status drawer and navigation toggle, and moved its
  DHT, mesh, hash, sequence, swarm, backfill, and karma counters into the
  persistent footer.
- Fixed the slskdN theme picker contrast and dropdown surface styling so the
  selector is visible in the top navigation and the default dark palette has
  clearer separation between the page, panels, inputs, and active controls.
- Upgraded Dependabot PR dependency bumps for NuGet and npm, including patched
  OpenTelemetry `1.15.3` packages and npm `uuid` `14.0.0` to clear the open
  package advisories.
- Aligned test project Microsoft package references with the `10.0.7`
  application package line so dependency submission restores do not hit NuGet
  downgrade errors.
- Fixed the CodeQL cleartext-secret alert by deleting legacy overlay
  certificate password files without reading or logging them and regenerating
  the self-signed overlay certificate when needed.
- Clarified the System Network diagnostics when DHT rendezvous is intentionally
  isolated by `dhtRendezvous.lanOnly: true`.
- Made the two-node DHT rendezvous integration coverage wait for overlay
  readiness before failing a peer-connect attempt.

## [2026042900-slskdn.195] — 2026-04-29

- Fixed the Network dashboard public DHT exposure warning so nodes with
  backend-reported `lanOnly: true` no longer get warned as though they are
  publishing to public DHT bootstrap routers.

## [2026042900-slskdn.194] — 2026-04-29

- Fixed the AUR source package build for date-versioned slskdN releases by
  mapping public versions such as `2026042900-slskdn.193` to MSBuild-safe
  package versions while preserving the public informational version.

## [2026042900-slskdn.193] — 2026-04-29

- Added a slskdN default web theme using brown, gray, and purple tones, kept
  the upstream-style dark theme as `Classic Dark`, and preserved the light
  theme as a selectable option.
- Clarified slskdN fork attribution across docs, web metadata, package
  metadata, generated release copy, service metadata, and API surfaces while
  preserving compatibility names for existing installs.
- Normalized C# source headers against upstream `0.24.5` so unchanged upstream
  files remain upstream-attributed, modified upstream files carry slskdN
  co-attribution, and slskdN-only files use slskdN-only copyright notices.
- Clarified README comparison wording so the upstream baseline is explicitly
  framed as slskd `0.24.5` instead of current upstream `master`.

## [2026042900-slskdn.192] — 2026-04-29

- Fixed Discovery Graph neighborhood building so weak SongID manual-review
  evidence no longer promotes secondary transcript/OCR/chapter/MusicBrainz
  guesses into clickable artist, album, segment, or mix neighborhoods.

## [2026042900-slskdn.191] — 2026-04-29

This supersedes `2026042900-slskdn.190`, which created GitHub release assets and
package metadata but failed Docker publishing because the runtime image assumed
UID/GID `1000:1000` was available in the .NET runtime base image. Docker now
creates the internal `slskdn` placeholder user/group with system-allocated IDs
and still remaps it at container startup when `PUID`/`PGID` are supplied.

It carries forward the `.190` runtime and transfer changes:

- Added slskdN-native Docker runtime handling for `PUID`/`PGID`, non-root
  `--user` runs, writable app-directory validation, and packaging metadata
  checks without creating release tags.
- Added configurable direct-download retry/resume behavior and batch metadata
  for multi-file queue requests, including transfer persistence, API DTO fields,
  migration support, and regression coverage.
- Normalized IPv4-mapped IPv6 addresses before CIDR/proxy/API checks and made
  option diffs tolerate null values.

## [2026042900-slskdn.190] — 2026-04-29

This release follows the corrective `2026042900-slskdn.189` date-versioned
rollback build with runtime and transfer robustness work that landed while the
`.189` Docker publish was still finishing. It keeps the same public
`YYYYMMDDmm-slskdn.###` version shape, remains on the slskd 0.24.5
license-compliance rollback base, and does not imply upstream slskd 0.26 code.

- Added slskdN-native Docker runtime handling for `PUID`/`PGID`, non-root
  `--user` runs, writable app-directory validation, and packaging metadata
  checks without creating release tags.
- Added configurable direct-download retry/resume behavior and batch metadata
  for multi-file queue requests, including transfer persistence, API DTO fields,
  migration support, and regression coverage.
- Normalized IPv4-mapped IPv6 addresses before CIDR/proxy/API checks and made
  option diffs tolerate null values.

## [2026042900-slskdn.189] — 2026-04-29

This is a corrective slskdN-versioned release for package-manager ordering. The
previous rollback build, `0.24.5-slskdn.186`, correctly restored the
license-compliant slskd 0.24.5 codebase, but it sorted older than already
published `0.25.1-slskdn.*` packages in AUR and other downstream repositories.

Starting with this build, stable slskdN releases use the independent
`YYYYMMDDmm-slskdn.###` version shape. This release is newer than the removed
`0.25.1-slskdn.*` line for package managers, but it does not claim upstream
slskd 0.26 or newer code. The application code remains on the slskd 0.24.5
AGPLv3 rollback base with slskdN-owned backports only.

The `0.24.5-slskdn.186` release is superseded by this build for the same
license-compliance reason older releases were purged: users and packagers should
resolve to the current rollback line, not the post-0.25.0 upstream-sync line.
This also supersedes `2026042900-slskdn.187` and `2026042900-slskdn.188`,
which created GitHub releases and package metadata but failed Docker publishing
while the Dockerfile was being corrected to run Bash-only build helpers inside
the Alpine web-build stage.

Included from the rollback line:

- Soulseek.NET client minor version set to the slskdN-owned range `7700000`.
- Runtime YAML alias binding for public keys such as `dht:`.
- Controlled 503 responses for expected remote directory browse timeouts.
- Shutdown-safe download cancellation classification.
- Empty cached user groups now fall back to built-in groups.
- Release-note generation fails closed if synthetic commit lists get too large.
- Tag publishing is no longer blocked by pre-publish Nix smoke checks that need
  already-published assets.

Relevant non-documentation commits preserved in this rollback line:

- `6edafc5d3` feat(wishlist): add CSV import
- `ca51715dd` fix(transfers): require reconnect for listen endpoint changes
- `1fcfbcece` feat(transfers): add upload diagnostics
- `d8df4d15c` fix(dht): use YAML option names in exposure warning
- `00742f9cd` fix(search): publish mesh results before Soulseek timeout
- `7214f310c` fix(aur): normalize release payload permissions
- `33148d54d` test: remove dns-dependent unit flakes
- `248b81981` fix(packaging): harden aur binary zip staging
- `950a87ff3` test(mesh): validate live account mesh smoke
- `f436d48f2` test(mesh): add optional live account smoke
- `fff4367d1` chore(mesh): log mesh search peer outcomes
- `73c9ee89b` fix(mesh): advertise only routable self endpoints
- `9d60cb319` fix(search): honor API timeout seconds
- `7457f4c4d` fix(search): separate auto-replace safety budget
- `5c085b3f0` fix(mesh): quiet issue 209 live maintenance noise
- `db2119ea4` fix: Improve SongID results UX
- `b72258ba4` fix: quiet entropy and auto-replace log noise
- `a17d43868` fix: clean startup identity log polish
- `dc3898c66` fix: classify Soulseek read timeout churn
- `3f901d944` fix: quiet overlay endpoint cooldown noise
- `8a1c89643` ci: remove snap publishing from releases
- `a1f105521` fix: avoid duplicate mesh descriptor publish on startup
- `defa3ee75` fix: quiet expected shutdown and Soulseek timer noise
- `abd55416d` fix: harden package startup and release announcements
- `9c1d3f14d` fix: quiet optional user info badge misses
- `2e4cc934c` fix: discover target framework in test launchers
- `15ba2a423` fix: quiet shutdown-cancelled searches
- `56a25b31d` fix: quiet controlled user info logs
- `393e2cea4` fix: make mesh QUIC opt-in
- `3e65a5778` fix: backport rollback release fixes
- `6b6dcee6e` chore: restore main to 0.24.x rollback line
- `8f597c0f5` chore: switch stable release versioning to slskdN dates

## [0.24.5-slskdn.186] — 2026-04-29

This release is the license-compliance rollback build. It intentionally returns
slskdN to the pre-0.25.0 slskd 0.24.x AGPLv3 codebase and keeps only the
fork-owned slskdN work that can ship on that base. Releases and artifacts older
than this build are being removed from GitHub to prevent accidental installation
of builds made from the post-0.25.0 upstream-sync line.

The build also carries the release-critical backports needed to make the 0.24.x
line usable: Soulseek.NET client minor-version registration for slskdN,
release-note guardrails that fail closed on oversized synthesized commit lists,
runtime YAML alias binding for public keys such as `dht:`, controlled 503
responses for expected remote directory browse timeouts, shutdown-safe download
cancellation classification, fallback handling for empty cached user groups, and
tag publishing unblocked from pre-publish Nix smoke checks that require already
published release assets.

Relevant non-documentation commits preserved in this rollback line:

- `6edafc5d3` feat(wishlist): add CSV import
- `ca51715dd` fix(transfers): require reconnect for listen endpoint changes
- `1fcfbcece` feat(transfers): add upload diagnostics
- `d8df4d15c` fix(dht): use YAML option names in exposure warning
- `00742f9cd` fix(search): publish mesh results before Soulseek timeout
- `7214f310c` fix(aur): normalize release payload permissions
- `33148d54d` test: remove dns-dependent unit flakes
- `248b81981` fix(packaging): harden aur binary zip staging
- `950a87ff3` test(mesh): validate live account mesh smoke
- `f436d48f2` test(mesh): add optional live account smoke
- `fff4367d1` chore(mesh): log mesh search peer outcomes
- `73c9ee89b` fix(mesh): advertise only routable self endpoints
- `9d60cb319` fix(search): honor API timeout seconds
- `7457f4c4d` fix(search): separate auto-replace safety budget
- `5c085b3f0` fix(mesh): quiet issue 209 live maintenance noise
- `db2119ea4` fix: Improve SongID results UX
- `b72258ba4` fix: quiet entropy and auto-replace log noise
- `a17d43868` fix: clean startup identity log polish
- `dc3898c66` fix: classify Soulseek read timeout churn
- `3f901d944` fix: quiet overlay endpoint cooldown noise
- `8a1c89643` ci: remove snap publishing from releases
- `a1f105521` fix: avoid duplicate mesh descriptor publish on startup
- `defa3ee75` fix: quiet expected shutdown and Soulseek timer noise
- `abd55416d` fix: harden package startup and release announcements
- `9c1d3f14d` fix: quiet optional user info badge misses
- `2e4cc934c` fix: discover target framework in test launchers
- `15ba2a423` fix: quiet shutdown-cancelled searches
- `56a25b31d` fix: quiet controlled user info logs
- `393e2cea4` fix: make mesh QUIC opt-in
- `5bd0e0b88` fix: return 503 for unavailable user info
- `c875206b3` fix: preserve spacing in DHT exposure copy
- `f343ca80c` fix: classify Soulseek TCP double-disconnect race
- `c26ed38c7` fix: quiet shutdown disconnect stack noise
- `139af4e8d` fix: reduce background search log noise
- `ed5a7dd9a` fix: quiet auto-replace shutdown cancellation

## [Unreleased]

- **License rollback to slskd 0.24.x base.** slskdN no longer incorporates changes from slskd 0.25.0 or later; the project tracks the pre-0.25.0 plain-AGPLv3 codebase only, and future development is independent of upstream slskd. See `memory-bank/license-rollback-plan.md` for the full rationale and migration plan.
- Backported release-critical fixes onto the 0.24.x rollback branch: release notes now fail closed instead of synthesizing oversized commit dumps, tag builds no longer block publishing on pre-publish Nix smoke checks for unpublished stable assets, public YAML aliases such as `dht:` bind in runtime configuration, remote directory browse timeouts return controlled 503 responses, shutdown-wrapped download cancellations stay out of error logs, and empty cached user groups fall back to built-in groups.
- Changed the Soulseek client minor version from 760 to 7700000 to comply with Soulseek.NET license §5 (unique client-version requirement). The previous value conflicted with the reserved range (760-7699999) allocated to upstream slskd. slskdN claims the adjacent range 7700000-7709999, registered via PR to the Soulseek.NET README registry.
- Removed namespace claims on the upstream `slskd` package name from AUR/RPM/deb packaging metadata (`provides`, `replaces`); slskdN packages now provide their own names only and continue to declare a file-level `conflicts` with `slskd` (both binaries install to `/usr/bin/slskd`). Drop-in compatibility at the binary path is preserved.
- Removed the upstream slskd PNG referenced in the README header for trademark hygiene; an slskdN-original logo is pending.
- Added a NOTICE file at the project root with the slskdN fork attestation.
- Added CSV playlist import for issue `#216`: TuneMyMusic-style exports can now be imported from the Wishlist page into wishlist searches, with optional album terms, filters, enabled state, max results, and auto-download settings.
- Fixed a Soulseek upload reachability bug where runtime changes to `soulseek.listen_port` or `soulseek.listen_ip_address` could restart the local listener without making the Soulseek server advertise the new endpoint; these options now correctly require a reconnect so peers do not keep trying a stale port.
- Added upload diagnostics for troubleshooting remote upload failures: `/api/v0/transfers/uploads/diagnostics` now reports configured listener state, a local TCP listener probe, share/index status, upload counters, recent upload records, and actionable warnings; inbound upload enqueue requests also emit structured `[UPLOAD-DIAG]` logs.
- Fixed the DHT public-discoverability warning and sample config to use the YAML keys operators actually set (`dht.lan_only` / `dht.enabled`) instead of internal option object names.
- Published mesh/pod search results as soon as the mesh overlay responds instead of waiting behind the normal Soulseek search timeout; the search detail view now refetches when early result counts appear.
- Fixed AUR release payload permissions for `slskdn`, `slskdn-bin`, and `slskdn-dev` so `/usr/lib/slskd/releases/<version>` remains traversable by the systemd service user after zip staging.
- Fixed AUR binary package staging for `slskdn-bin` and `slskdn-dev`: the PKGBUILDs now mark the release zips as `noextract`, unpack directly from the downloaded archive during `package()`, and fail the build if the apphost, deps file, or `Microsoft.AspNetCore.Diagnostics.Abstractions.dll` are missing from the staged self-contained .NET 10 payload.
- Added and live-validated an optional live-account mesh smoke that starts two full slskdN instances with configured Soulseek test credentials, hosts a probe file on one node, mesh-searches it from the other, downloads it through the pod path, and byte-verifies the transfer.
- Added info-level mesh-search fanout diagnostics when active overlay peers are queried, including peer count, empty peer responses, failed peers, and returned file count so `meshResponses=0` no longer hides whether the mesh path was actually exercised.
- Fixed mesh self-descriptor endpoint publication so automatic detection only advertises public-routable interfaces and no longer supplements explicitly configured self endpoints with private/container/VPN addresses.
- Refactored frontend components: extracted `MediaCoreStats` and `MediaCorePods` from the monolithic `MediaCore` component (reduced from 8610 to 2969 lines), and decomposed the 3557-line `Integrations` page into 9 standalone panel components (coordinator reduced to 245 lines).
- Fixed thread-unsafe `Random` usage across 4 files by replacing `new Random()` with `Random.Shared`; seeded deterministic random providers left intact.
- Fixed `HttpClient` socket exhaustion risk by injecting `IHttpClientFactory` into `RelayClient`, `SharesController`, `NatDetectionService`, and `Application`/`GitHub` version-check path, replacing inline `new HttpClient()` with pooled named clients.
- Refactored frontend components: extracted `MediaCoreStats` and `MediaCorePods` from the monolithic `MediaCore` component (reduced from 8610 to 2969 lines), and decomposed the 3557-line `Integrations` page into 9 standalone panel components (coordinator reduced to 245 lines).
- Fixed thread-unsafe `Random` usage across 4 files by replacing `new Random()` with `Random.Shared`; seeded deterministic random providers left intact.
- Fixed `HttpClient` socket exhaustion risk by injecting `IHttpClientFactory` into `RelayClient`, `SharesController`, `NatDetectionService`, and `Application`/`GitHub` version-check path, replacing inline `new HttpClient()` with pooled named clients.
- Fixed documented seconds-to-milliseconds timeout mapping for `/api/v0/searches` and multi-source discovery searches so callers requesting a 10-second or multi-minute search no longer get an accidental 10 ms / 270 ms search window.
- Split background auto-replace searches into an `auto-replace` Soulseek safety-limiter source instead of sharing the user/API `user` bucket, and added source-aware search completion diagnostics for manual issue `#209` retesting without reintroducing routine background log noise.
- Stopped circuit maintenance from automatically running placeholder multi-hop circuit probes against live mesh peers, removing recurring `Circuit building test failed` warnings and avoiding unsolicited peer traffic during normal maintenance.
- Classified common Soulseek remote transfer rejection reasons (`Too many megabytes`, `Too many files`) as expected peer-policy outcomes so they no longer surface as fake fatal unobserved task exceptions.
- Improved the SongID results page after a headless UX audit: queue rows now show meaningful titles/status, duplicate track/action candidates collapse, the result summary promotes the best next actions, and low-level diagnostics move behind disclosure rows.
- Stabilized the release gate by isolating unit tests that inspect process-global static event subscriptions, preventing xUnit parallelism from racing `Clock.EveryMinute`/`Program.LogEmitted` subscriber-count assertions.
- Stabilized live `local test host` log noise found during the 172 package soak: entropy health checks now use a larger RNG sample to avoid routine finite-sample false warnings, and auto-replace no-result searches stay at debug-level telemetry.
- Cleaned up startup polish found during the `local test host` 172 package soak: temporary raw security config probes now log only at debug, persisted peer profiles with blank display names are migrated to a usable fallback, and LAN discovery advertises a non-empty trimmed display name.
- Classified Soulseek.NET read-loop timeout inner exception chains as expected peer-network churn so `ConnectionReadException` plus `IOException`/`SocketException` timeout stacks no longer log as fake fatal unobserved task exceptions.
- Demoted per-endpoint DHT overlay cooldown streak logs to debug so normal remote endpoint churn stays visible through aggregate DHT/overlay summaries and API stats without repeating one line per degraded endpoint at information level.
- Removed Snap publishing from release workflows so dev/stable releases no longer build or upload Snap packages, wait on Snap Store publication, or refresh Snap metadata as part of release metadata commits.
- Avoided duplicate MeshDHT self-descriptor publication at startup by letting the bootstrap service own the initial publish while the refresh service waits until its scheduled interval or an IP-change-triggered refresh.
- Quieted normal host shutdown logs so clean systemd stops/restarts no longer report `app.Run()` returning as abnormal or duplicate expected `ProcessExit` telemetry on stderr.
- Matched live Soulseek.NET timer-reset stack signatures (`Soulseek.Extensions.Reset(Timer timer)`) in the expected network-teardown classifier so known write-loop `NullReferenceException` races no longer log as fake fatal unobserved task exceptions.
- Made release announcement webhooks retry and degrade to warnings so transient Discord/Matrix gateway failures do not mark completed tag builds failed after artifacts and GitHub releases are already published.
- Added a quiet optional user-info mode for UI badges so expected offline/unavailable Soulseek users render as missing badge data without browser console 404/503 noise; the normal `/users/{username}/info` API still preserves its 404/503 semantics.
- Made mesh QUIC explicitly opt-in after recurring native `local test host` coredumps under active MsQuic listeners: UDP overlay remains enabled by default, QUIC control/data services and clients register only when configured, and the example config now documents the opt-in keys.
- Classified Soulseek.NET listener socket disposal from `Soulseek.Network.Tcp.Listener.ListenContinuouslyAsync` as expected network teardown so it no longer logs as a fake fatal unobserved task exception.
- Reduced live journal noise by demoting verbose startup `[DI]` tracepoints, SPA fallback route serving, and per-request MediaCore CSRF processing logs to debug; controlled offline user-info responses now log concise summaries instead of `UserOfflineException` stacks, and shutdown-cancelled background searches no longer emit false error logs during manual deploys.
- Fixed user info lookups so expected Soulseek peer connection failures and timeouts return a controlled `503` instead of bubbling live peer unavailability as HTTP 500s.
- Quieted expected remote-offline download failures during restart/re-enqueue: transfers still fail normally, but `UserOfflineException` peer outcomes no longer emit repeated error stack traces.
- Quieted auto-replace shutdown cancellation during manual deploys so host-stop cancellation no longer logs search error stacks or counts interrupted items as failed replacements.
- Quieted the known Soulseek disconnect race during service shutdown so handled `Sequence contains no elements` races no longer print stack traces.
- Classified Soulseek.NET TCP double-disconnect read-loop races as expected network churn so they no longer log as fatal unobserved task exceptions.
- Fixed the `/system/network` DHT exposure consent copy so the inline `dht.lan_only=true` setting no longer runs into the following word.
- Reduced background search-batch journal noise by demoting routine per-search completion, mesh-search no-peer/fanout, and passive HashDb discovery progress to debug.
- Reduced auto-replace large-batch journal noise by demoting routine per-track search and no-result progress to debug while keeping aggregate cycle and successful-candidate logs visible.
- Excluded generated `src/slskd/dist` publish output from Web SDK publish content so manual artifacts do not recursively ship stale nested build output.
- Paced auto-replace alternative searches against the configured Soulseek search safety budget and stopped the cycle after a budget rejection, so one stuck-download batch defers cleanly instead of emitting per-track rate-limit stack traces.
- Stabilized the `local test host` manual-build follow-up set: hardened certificate handling, relay TLS pin validation, QUIC connection/task cleanup, shutdown/download draining, OpenAPI response mutation, and current-process fatal-noise classification after live soak testing.
- Broadened the latest pre-existing in-flight sweep into a release-visible changelog entry: this commit ships pending security, mesh, DHT, QUIC, API, UI, and diagnostics fixes accumulated since the last published release.
- Minimized the anonymous `/api/v0/profile/{peerId}` response payload to a public-safe shape so profile lookups no longer expose internal metadata (`PublicKey`, `Signature`, timing fields) to unauthenticated callers.
- Fixed a React hook-order regression in `/system/network` after the pre-existing DHT exposure UX changes so modal visibility state updates now run in a stable hook sequence.
- Downgraded the remaining Soulseek timer-reset teardown race on `Tcp.Connection.WriteInternalAsync(...)` so that benign third-party write-loop `NullReferenceException` noise no longer logs as fake fatal unobserved-task crashes.
- Aligned `bin/publish` with the tagged release publish profile so manual/live deploys no longer use a different self-contained single-file `ReadyToRun` runtime shape than CI ships.
- Hardened mesh QUIC lifecycle management: cached/orphaned `QuicConnection` instances are now explicitly disposed, duplicate connection-creation races no longer leak live connections, and QUIC overlay/data hosted services close and drain active connection handlers during shutdown.
- Fixed the TCP mesh overlay listener failing to rebind `50305` on fast restarts with `Address already in use` even when no live listener remained by enabling socket address reuse and fully clearing stop-state after shutdown.
- Fixed another live Soulseek teardown noise path by classifying the third-party `Soulseek.Extensions.Reset(Timer)` `NullReferenceException` from `ReadContinuouslyAsync` as expected peer/read-loop churn instead of logging it as a fake fatal unobserved-task crash.
- Fixed the `/pods` admin panel by putting `PodsController` back on the explicit versioned `api/v{version:apiVersion}/pods` surface that the frontend already calls, with a contract test to keep that route aligned.
- Fixed the `/system/mediacore` admin panel crash by restoring the missing Semantic UI `Checkbox` import.
- Fixed fast authenticated admin-panel sweeps self-triggering `429 Too Many Requests` by exempting authenticated requests and non-API web-shell/static requests from the coarse global fixed-window limiter while keeping anonymous API throttling in place.
- Fixed first-run share bootstrap log noise so missing/out-of-date cache state goes straight into the recreate/scan path instead of throwing a corruption-looking exception before recovering.
- Fixed the default `/` web route to redirect directly to `/searches` without logging a router miss, and stopped the app from probing session state on every render when no token is present.
- Added DHT/overlay operator diagnostics that roll up candidate handling (`seen`, `accepted`, skip/defer/backoff counts), expose endpoint cooldown/degradation stats, and log mesh session-end summaries so repeated bad remote endpoints stand out without flooding the logs.
- Fixed release-smoke integration compilation after the download-shutdown drain change by updating the integration test `StubDownloadService` to implement the new `IDownloadService.ShutdownAsync(CancellationToken)` member.
- Fixed download shutdown cleanup so active downloads cancelled by host shutdown stop quietly without trying to fail transfers through disposed services or release already-disposed enqueue semaphores.
- Fixed service shutdown ordering so active Soulseek downloads are drained before the shared client is disposed, avoiding restart-time global download semaphore warnings during clean shutdown.
- Fixed clean-shutdown error handling to ignore a third-party Soulseek disconnect collection race that could otherwise log a false fatal `Sequence contains no elements` during service restarts.
- Fixed service signal handling so normal `systemctl stop`/`restart` SIGTERM requests stop the generic host cleanly instead of logging fatal shutdown telemetry and exiting with status 1.
- Fixed user directory browse API handling so expected remote peer connection failures return a controlled 503 response instead of escaping as unhandled request exceptions with repeated middleware stack traces.
- Fixed DHT rendezvous connection accounting so peers deferred by the overlay connector's concurrency limit are not counted as real attempts or pushed into the five-minute retry backoff. This keeps diagnostics aligned with actual socket attempts and prevents a potentially valid candidate from being delayed behind simultaneous junk DHT endpoints.
- Fixed live mesh compatibility with peers that send unframed JSON control messages after a framed handshake. The overlay reader now recognizes raw JSON starting at a frame boundary, consumes exactly one capped JSON object, and keeps the normal length-prefixed path unchanged, preventing the deterministic two-minute `Invalid message length: 2065855609` disconnect seen on `local test host`.
- Fixed QUIC runtime gating so mesh QUIC services and direct-QUIC self-descriptors are enabled only when the current runtime/native MsQuic stack reports both connection and listener support, rather than assuming every Linux/macOS/Windows host has working QUIC.
- Fixed AudioSketch ffmpeg detection so the default `FfmpegPath: ffmpeg` resolves through `PATH` instead of being rejected by `File.Exists("ffmpeg")`, removing repeated false missing-ffmpeg warnings and allowing sketch hashes on hosts with ffmpeg installed normally.
- Downgraded another expected Soulseek peer teardown shape from fake fatal telemetry: unobserved `InvalidOperationException: The underlying Tcp connection is closed` from `Soulseek.Network.MessageConnection.ReadContinuouslyAsync` now falls into the expected network-churn classifier.
- Fixed a DHT startup race where a temporary overlay-port bind probe could decide the node was not beacon-capable during a fast restart, leaving TCP overlay `50305` offline until the next service restart. The real overlay listener start now determines beacon capability directly.
- Fixed overlay keepalive reads competing with the persistent message dispatcher: inbound mesh loops now send keepalive pings without doing a direct pong read, so all overlay frames continue through the single read loop even while mesh search or service traffic is active.
- Fixed DHT rendezvous wasting overlay connection attempts on peers that advertise the configured DHT UDP port as their endpoint. Those candidates are now ignored unless the deployment intentionally uses the same port for DHT and TCP overlay traffic, so discovery does not fill the peer cache with non-overlay `:50306` contacts on the default split-port setup.
- Fixed auto-replace missing valid alternatives when search finalization races the polling window. The live symptom was `No search responses found` followed by the same search completing with responses seconds later; auto-replace now waits for the persisted completed search state before deciding whether responses are absent.
- Fixed startup directory-browse requests racing Soulseek login: `POST /api/v0/users/{username}/directory` now returns `503 Soulseek server connection is not ready` while the client is still `Connected, LoggingIn` instead of throwing through the ASP.NET/security middleware pipeline as a noisy 500.
- Fixed mesh overlay peer churn traced from live `local test host` build `157` logs: outbound connections to remote peers were disconnecting after ~2 minutes with `Protocol violation ... Invalid message length: 2065855609` (bytes `0x7B2BF939` — JSON `{` bytes being mis-read as a 4-byte length prefix). Root cause was that `SecureMessageFramer.WriteMessageAsync` had no write lock while three separate task paths could write to the same connection concurrently: the message-loop path sending `Pong`s and responses, `MeshOverlaySearchService` sending `mesh_search_req` fan-outs, and `MeshServiceClient` sending service calls. Because `SslStream.WriteAsync` is not thread-safe for concurrent writers, the 4-byte length header and JSON payload of concurrent messages interleaved on the wire and the peer's reader desynced on the first torn frame. The framer now serializes writes with a `SemaphoreSlim`, is `IDisposable` and disposed by `MeshOverlayConnection`, and `DeserializeMessage` is now a stateless static so helper classes no longer construct a dead `SecureMessageFramer(Stream.Null)` just to get JSON options.
- Fixed three live `local test host` log issues observed on build `155`: (a) demoted the benign Soulseek.NET `System.Timers.Timer` disposed-object warning that fires when a late `SearchResponse` arrives after the search has already completed — it's now at `Debug` instead of spamming `~100/hr` of `Error handling peer message` warnings; (b) made the "JWT signing key is ephemeral" warning conditional on the raw configuration tree (previously it fired even when the operator had configured a persistent key, because `JwtOptions.Key` defaults to a freshly-generated random value that's indistinguishable from a configured one at the Options layer); and (c) split the QUIC overlay server onto its own `QuicListenPort` (default `50402`) so it can run concurrently with the legacy `UdpOverlayServer` on `50400` — previously both servers raced for exclusive UDP ownership and the loser logged `QUIC overlay port 50400 is already in use`. The DirectQuic advertisement in `PeerDescriptorPublisher` now points peers at `QuicListenPort` so inbound QUIC dials reach the new port.
- Fixed overlay mesh search fanout on live operator nodes: raised `OverlayProtocol.MaxMessageSize` from 4 KiB to 64 KiB so `mesh_search_resp` frames carrying up to `MaxResults=200` file DTOs no longer overflow the framer, which was previously mis-parsing response payload bytes as a 4-byte length header and throwing `ProtocolViolationException: Invalid message length`. Healed `FileKeyStore` so overlay identity survives restarts: `WriteToFile` was serializing `Ed25519KeyPair.CreatedAt` (DateTimeOffset) while `ReadFromFile` expected `KeyFileModel.CreatedMs` (long), causing every restart to load the saved key as epoch-0 and immediately rotate, cycling the node's overlay identity and reputation. `/api/v0/mesh/peers` now returns both the hash-sync peers and the live overlay neighbor registry so operators can see the connection the overlay layer actually holds. Mesh search fanout logs are promoted from Debug to Information with a post-aggregation "returned results from N/M peer(s)" summary.
- Fixed DHT rendezvous overlay pod transfers: mesh service-fabric calls now route over established overlay connections, inbound calls dispatch through the real mesh service router, pod search results preserve content-routing metadata, and the full-instance integration suite now proves two local slskdN nodes can connect, mesh-search, download, and byte-verify content over the overlay.
- Downgraded remote Soulseek `TransferRejectedException` enqueue failures from fake `[FATAL] Unobserved task exception` noise into the expected peer-network bucket. Downloads rejected with `Enqueue failed due to internal error` are still recorded as failed/rejected transfers, but no longer look like host-side fatal crashes.
- Fixed a live `local test host` source-ranking database race where concurrent transfer history updates could trip `SQLite Error 19: UNIQUE constraint failed: DownloadHistory.Username`. Download success/failure counters now use a single atomic SQLite upsert, with regression coverage proving concurrent first writes for the same username preserve every counter update.
- Fixed DHT rendezvous diagnostics authentication so configured API keys can access `/api/v0/dht/status`, `/api/v0/dht/peers`, and `/api/v0/overlay/stats` instead of those endpoints falling through to bearer-only auth despite the rest of the operator API accepting API keys.
- Resolved the remaining Dependabot security alert without suppressions: removed the vulnerable deprecated `OpenTelemetry.Exporter.Jaeger` package, kept `exporter: jaeger` working through the supported OTLP exporter path for Jaeger collectors, bumped `AWSSDK.S3` to `4.0.21.2`, and refreshed the npm lockfiles for the active Dependabot-managed package ranges.
- Fixed the latest issue `#209` overlay-search root cause: reciprocal mesh connections now keep independent inbound and outbound lifecycles, outbound sockets run the same message loop as inbound sockets, and mesh search responses are routed through a request router instead of competing readers on the same TLS stream. This prevents two healthy peers from disposing or starving each other's live connection after DHT discovery succeeds, and the loopback integration proof now repeats real `MeshOverlaySearchService` searches over the same outbound connection to prove the path stays usable.
- Fixed the AUR binary package source cache trap: the GitHub Linux glibc zips for `slskdn-bin` and `slskdn-dev` are now saved under versioned local source filenames, so yay/makepkg cannot build a package labeled with a newer `pkgver` while silently reusing an older cached release zip.
- Fixed the issue `#209` privacy leak in DHT/overlay logs: mesh usernames, peer ids, and public endpoints now go through `OverlayLogSanitizer` before operator logs, so pasted remote logs no longer expose raw Soulseek names like the earlier `Accepted mesh connection from ...` messages.
- Fixed the newest issue `#209` mesh failure reproduced from build `151`: quiet overlay neighbors no longer disconnect after the 30-second message-read timeout, inbound handshakes advertise the peer's overlay listener so the server can start a reciprocal outbound connection for request/response mesh RPCs, old peers fall back to the configured overlay port, and stale inbound cleanup can no longer unregister a newer outbound replacement. The two-full-instance mesh smoke now waits past the read timeout and proves both nodes stay connected.
- Added a Web UI regression test proving normal searches create requests without bridge providers unless `/api/slskdn/capabilities` explicitly advertises `scene_pod_bridge`, covering the issue `#209` zero-result failure mode from the browser path.
- Fixed the latest issue `#209` search regression by making the experimental Scene ↔ Pod bridge opt-in again. Default searches now stay on the proven upstream-compatible Soulseek path, the Web UI no longer enables bridge providers from generic capabilities, and `/api/slskdn/capabilities` advertises the bridge only when the server option is explicitly enabled.
- Added a deterministic two-full-instance mesh smoke for issue `#209`: the integration harness now launches isolated `slskd` subprocesses with unique appdirs and listener ports, forces one node to dial the other through the real overlay stack, and asserts both nodes report the live neighbor plus circuit peer inventory. Added an admin-only `/api/v0/overlay/connect` diagnostic endpoint for forced local/full-instance overlay probes and a gitignored `local-mesh-accounts.env` scaffold for optional live Soulseek account tests.
- Fixed another live issue `#209` mesh regression on `local test host`: DHT-discovered overlay endpoints are no longer one-shot connection attempts. The rendezvous service now tracks retry/backoff state separately from the discovered-peer cache, so a first timeout or refusal does not suppress all future retries for that endpoint. Host validation confirmed the fix by forcing a post-backoff discovery cycle and observing `totalConnectionsAttempted` increase for the same discovered-peer set instead of staying stranded at the first-attempt count.
- Fixed issue `#209`'s remaining direct-mode circuit failure: `AnonymityMode.Direct` now registers and prioritizes a real direct transport instead of still depending on a local Tor SOCKS proxy, so DHT-ready peers no longer immediately fail circuit establishment with `No anonymity transport is available` just because Tor is absent.
### Fixed

- Hardened web and sharing authentication: session login lockouts now track both remote IP and normalized username to blunt distributed password spray, and share tokens now bind the JWT `aud` field to `collection_id` so cross-collection replay fails audience validation instead of relying on a custom claim alone.
- Hardened Chromaprint fingerprint extraction so ffmpeg PCM output is read through a bounded buffer derived from the configured sample rate, channel count, and capture duration instead of an unbounded `MemoryStream`, preventing oversized or malformed decoder output from consuming arbitrary memory during audio fingerprinting.
- Added classified outbound overlay diagnostics for issue `#209`: `/api/v0/overlay/stats` now breaks connector failures down into stable buckets (`connectTimeouts`, `noRouteFailures`, `connectionRefusedFailures`, `connectionResetFailures`, `tlsEofFailures`, `tlsHandshakeFailures`, `protocolHandshakeFailures`, `registrationFailures`, `blockedPeerFailures`, `unknownFailures`) instead of one opaque failed-connection total. Live validation on `local test host` showed the current post-fix failures are dominated by remote candidate quality (`7` timeouts, `1` no-route) rather than another local TLS or protocol regression.
- Fixed the standalone distro packaging drift that was still breaking Jammy PPA and related release jobs after the main release path moved on: `release-ppa.yml`, `release-copr.yml`, and `release-linux.yml` now use `.NET 10`, validate the staged publish output, and the DEB/RPM runtime SONAME patch now discovers `libcoreclrtraceptprovider.so` dynamically inside the staged package tree instead of assuming one flat appdir path.

- Fixed the current root cause behind the latest issue `#209` reports: DHT-discovered rendezvous peers now publish into `IMeshPeerManager` immediately instead of only triggering a one-shot overlay connect attempt, so circuit maintenance can see real onion-capable peer candidates as soon as DHT discovery succeeds. Stale antiforgery cookie recovery now also retries on any key-ring/decryption exception shape, not just `AntiforgeryValidationException`, which stops repeated stale-token decrypt spam after reinstall or key rotation.
- Fixed the remaining operator-facing stale-cookie and diagnostics fallout on issue `#209`: safe-request CSRF token minting now strips known antiforgery cookies from the incoming request before ASP.NET tries to deserialize them, which stops stale-cookie GETs from spamming decrypt stack traces in the journal, and `/api/v0/dht/status` now reports configured DHT enablement separately from live readiness so the UI no longer claims DHT is disabled during bootstrap.
- Fixed another root cause behind issue `#209`'s disappearing overlay peers: stale TOFU certificate pins no longer auto-ban reachable slskdn peers after normal cert rotation or reinstall. Inbound and outbound overlay handshakes now rotate the stored pin on mismatch instead of partitioning the mesh until an operator manually clears `cert_pins.json`.
- Cleaned up issue `#209`'s remaining peer-health diagnostics lie: DHT-discovered rendezvous endpoints are now tracked as unverified `dht-discovered` candidates until an overlay handshake succeeds, instead of being counted immediately as onion-capable peers. This keeps circuit-maintenance and security peer stats aligned with real overlay verification instead of raw DHT discovery.

- Debian/PPA source packaging now declares `patchelf` in `Build-Depends`, so Launchpad installs the tool required by `debian/rules` when patching the bundled .NET runtime during package assembly.

- Added explicit ICU runtime dependencies to the DEB and RPM packages so clean Ubuntu/Fedora installs can actually launch `slskd` instead of dying on first start with .NET globalization errors.
- Fixed the Fedora/COPR Linux package path by patching `libcoreclrtraceptprovider.so` during DEB/RPM package assembly to replace the old `liblttng-ust.so.0` SONAME with `liblttng-ust.so.1`, and by forcing the RPM bundle back onto the project's drop-in `/usr/lib/slskd` path instead of `%{_libdir}` so the shared `slskd.service` still points at a real executable on Fedora.
- Fixed the remaining false-fatal Soulseek transfer telemetry and DHT startup diagnostics: successful downloads no longer emit `[FATAL]` `Transfer failed: Transfer complete` unobserved-task noise after completion, and the DHT bootstrap grace period is now long enough for slow-but-healthy public-router bootstrap before warning operators about forwarding/firewall problems.

- Downgraded remote peer transfer rejections (`Download reported as failed by remote client`) into the expected Soulseek-network telemetry bucket so those peer-side failures no longer surface as fake `[FATAL] Unobserved task exception` host noise.

- Fixed the live download enqueue crash on Linux hosts after transfers reached `Queued, Remotely`: `DownloadService.EnqueueAsync(...)` no longer disposes its shared per-batch `SemaphoreSlim` while background enqueue tasks still release it, which removes the host-side `Cannot access a disposed object. Object name: 'System.Threading.SemaphoreSlim'.` failure and lets transfers proceed into real `InProgress` socket work again.

- Fixed the Arch/AUR packaging path so upgrades stop failing with stale `/usr/lib/slskd` file conflicts: the drop-in launcher path stays `/usr/lib/slskd/slskd`, but packaged releases now live under `/usr/lib/slskd/releases/<version>` with `/usr/lib/slskd/current`, the shared `slskd.service` still runs the packaged apphost, and the source PKGBUILD remains aligned to `.NET 10` with correct per-arch runtime IDs.

- Fixed the live Linux download failure that was aborting transfers before any bytes could be written: an unset `permissions.file.mode` now correctly falls back to the host umask in `FileService` instead of being parsed as an empty chmod string, which was throwing `The value cannot be an empty string or composed entirely of whitespace. (Parameter 'permissions')` during download file creation and move handling.

- Fixed the Transfers page bulk-action storm that was turning queue cleanup into its own failure mode: bulk retry/remove/cancel now enqueue into a background queue that drains one request at a time, duplicate submissions are ignored while the same work is already queued or running, `Remove All Completed` still uses the dedicated bulk-clear endpoint but now goes through the same deduped queue, and bulk failures surface as one summary toast instead of one popup per file.

- Fixed the newest issue `#209` mesh follow-up where DHT bootstrap/discovery succeeded but `Circuit maintenance` still stayed at `0 circuits, 0 total peers`. Live overlay neighbors are now mirrored into the circuit peer inventory through `MeshNeighborPeerSyncService`, and unit coverage reproduces the old empty-peer state without the sync service and the corrected populated-peer state with it.

- Realm-curated subject indexes now survive restarts: accepted indexes, governance proposal review state, and local authority enable/disable decisions persist to an atomic app-dir JSON state file and reload on startup, preserving resolver behavior without publishing indexes or contacting peers.

- Followed up on the newer issue `#209` feedback after DHT bootstrap started succeeding: versioned `GET /api/v0/users/notes` now resolves correctly again, and the mesh overlay connector no longer runs a guaranteed-to-fail UDP hole-punch preflight against DHT-discovered TCP overlay endpoints. Hole-punch completion logs now also label their local port as an ephemeral UDP socket so operators do not mistake it for a randomized listener port.

- Followed up on the post-bootstrap runtime fallout behind issue `#209`: `Connection reset by peer` is now treated as expected Soulseek network churn instead of `[FATAL]`, stale antiforgery cookies are cleared and reissued after reinstall/key-ring changes, and obvious non-overlay TLS garbage on the public mesh port is downgraded to debug noise instead of warning stack traces.

- Stable GitHub releases now ship the Linux service/config helper files and a supported `install-linux-release.sh` installer so raw release upgrades replace the running `slskd.service` target instead of silently leaving an older systemd-managed binary in place.

- Added runtime self-identification for release-debugging: startup now logs the running executable/base paths, and `/system/info` exposes the live executable path, base directory, app directory, config path, and process id so stale installs can be distinguished from real regressions.

- Cleaned up release asset naming: future Linux builds publish a single explicit `linux-glibc-*` asset per runtime instead of duplicating the same payload under `main`, version-specific, and alias names. Packaging and release automation now consume the explicit glibc names directly.
- Fixed the stable package metadata drift that broke `Nix Package Smoke`: stable package metadata is now aligned with the published `linux-glibc-*` release assets so flake/package smoke validates the same filenames the release workflow actually ships.

- Fixed the tagged release pipeline to match the repo's `.NET 10` target and corrected Matrix release-message redaction to use the homeserver's accepted `PUT` method.

- Fixed the real root cause behind issue `#209`: slskdn was pinned to `MonoTorrent 3.0.2`, whose DHT bootstrap path could stall forever behind a single hidden router. slskdn now uses `MonoTorrent 3.0.3-alpha.unstable.rev0049`, passes an explicit `dht.bootstrap_routers` list into DHT startup, validates that at least one bootstrap router is configured, and documents the router list in the example config.

- Bumped the remaining open dependency-update backlog on `main`: `YamlDotNet 17.0.1`, `dotNetRDF 3.5.1`, `OpenTelemetry`/console/OTLP/hosting `1.15.2`, and the web test/build toolchain updates from the open Dependabot PR set (`follow-redirects 1.16.0`, `vite 8.0.8`, `vitest 4.1.4`, `jsdom 29.0.2`, `@types/node 25.6.0`, `@vitest/coverage-v8 4.1.4`, `@microsoft/signalr 7.0.14`).
- Completed the held major-version upgrade work instead of suppressing it: the web app now runs on React 18, React Router 7, ESLint 9 flat config, and the current `@uiw/react-codemirror` line, while the backend and test projects are aligned to `.NET 10`.
- Fixed the migration fallout from that dependency work by updating router usage, restoring passing web lint/build/test runs, and tightening the integration harness so missing external `soulfind` prerequisites fail fast instead of leaving the full-solution test run hanging.
- Fixed DHT rendezvous bootstrap defaults so new installs use a stable explicit UDP port instead of a random startup port, fail validation if enabled DHT is left on port `0`, and log actionable bootstrap guidance when the DHT never reaches `Ready`.
- Rejected loopback `Soulseek.ListenIpAddress` binds for live clients so slskd fails fast instead of logging in successfully while all peer-facing operations (`info`, `browse`, transfers) silently break behind an unreachable advertised endpoint. `Flags.NoConnect = true` still permits loopback for offline/testing scenarios.
- Fixed the real root causes behind the persistent tester reports on `#200` and `#201`: the web service worker was cache-first on navigations and pre-cached the app shell, serving a stale `index.html` that pointed at asset bundle hashes no longer on disk after every rebuild (blank new tabs, 404s on `/assets/*`); it is now network-first for HTML, never caches `/assets/*`, and the shell cache name is bumped so old versions are purged on activate.
- Removed `listenIPAddress` from the startup `SoulseekClientOptionsPatch`. It is already applied via `CreateInitialSoulseekClientOptions`; re-applying it through `ReconfigureOptionsAsync` at startup tore down the `TcpListener` mid-accept and raced `Listener.ListenContinuouslyAsync`, producing the `Not listening. You must call the Start() method before calling this method.` exception and leaving the listener stopped so every inbound peer connection was refused and all transfers failed.
- Wired the existing `GET api/jobs` / `GET api/v{version}/jobs` endpoint to a real production data source. `slskd.API.Native.JobsController` depended on `IJobServiceWithList`, which had no production registration — only a test-harness one — so in production the endpoint always returned zero jobs, which is what the `System/Jobs` Web UI renders as "doesn't load." Added `HashDbJobServiceListAdapter` backed by new `ListDiscographyJobsAsync` / `ListLabelCrateJobsAsync` methods on `IHashDbService`, and registered it in DI.

- Expanded Automation Center execution beyond reports: `Wishlist Retry` can now run a bounded batch of enabled backend Wishlist searches, and `Library Health Scan` can start the real read-only scan after an operator enters an explicit library path. Both executions are recorded in automation history and keep downloads/file mutation behind the existing review flows.

- Turned more tail-side review surfaces into explicit handoffs: selected Library Health issues can now start bounded replacement searches through the real Search API, queue remediation jobs only for selected auto-fixable issue IDs, and send risky quarantine candidates to Discovery Inbox. Discovery Shelf promote previews can also be sent to Discovery Inbox individually or in a bounded batch.

- Expanded Listening Stats into live, explicit acquisition/scrobble handoffs: recommendation seeds now include top tracks, can start bounded live Search API batches, can be saved as enabled manual Wishlist requests with auto-download off, and can submit recent local plays to ListenBrainz using the saved browser token.

- Expanded the E15 player acquisition handoffs: Smart Radio plans can now start bounded live search batches, save manual Wishlist requests with auto-download disabled, and send radio seeds to Discovery Inbox. The playback queue's similar-track candidates can also start bounded searches or save manual Wishlist requests without changing the current queue.

- Expanded Discovery Inbox acquisition plans with a manual `Wishlist Ready` handoff: ready plans can create bounded Wishlist requests with auto-download disabled, record the created request on the plan, and skip duplicate Wishlist creation while keeping backend search execution as a separate explicit action.

- Added an E11 tag/organization dry run to Playlist Intake: matched rows can preview tag fields, organization templates, multi-artist behavior, cover-art policy, and ReplayGain policy with changed-field and destination-path summaries, without writing tags, moving files, running ReplayGain, contacting providers, searching, browsing peers, or downloading.

- Fixed mesh self-descriptor publication so unsupported-QUIC hosts no longer advertise fake `DirectQuic` transports or legacy Soulseek-style `2234/2235` endpoints. Auto-detected mesh endpoints now use explicit `udp://...:<overlay-port>` legacy addresses derived from the real overlay listen port, and direct QUIC transport advertisement is suppressed when the running host cannot actually accept QUIC.
## [0.24.5-slskdn.125] — 2026-04-13

- Closed the remaining tester follow-up on issues `#200` and `#201` by fixing the last versioned Web UI/API route gaps, tightening MediaCore and Jobs API versioning, removing the blanket benign `Connection refused` suppression, and covering those production `/api/v0/...` paths in release smoke.
- Removed the unnecessary download enqueue peer preflight that could fail on an auxiliary `Connection refused`, and aligned startup Soulseek option patching so `incomingConnectionOptions` is configured at startup the same way later live reconfigure already does.
- Added Matrix release announcements to the tagged dev and stable release workflow.

## [0.24.5-slskdn.124] — 2026-04-09

- Updated the frontend dependency baseline to `axios 1.15.0` and locked transitive `lodash 4.18.1`, clearing the standing Dependabot bumps and returning `npm audit` in `src/web` to `0` vulnerabilities.

## [0.24.5-slskdn.123] — 2026-04-09

- Finished the earlier issues `#200` and `#201` follow-up by restoring hard-refresh support on client-side routes, versioning the Bridge and Security Web UI/API paths consistently, preserving legacy Bridge compatibility, and moving Soulseek listener bootstrap settings into the initial client options.
- Fixed the release-gate subpath smoke harness so it mirrors the backend HTML rewrite behavior for `web.url_base` deployments instead of enforcing the obsolete relative-asset build model.
- Added Discord release announcements for tagged dev and stable releases, and blocked recurring `axios` / `lodash` Dependabot churn that was reopening the same low-value dependency PRs.
