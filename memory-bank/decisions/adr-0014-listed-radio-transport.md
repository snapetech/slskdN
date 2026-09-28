# ADR-0014: Listed Radio Transport

## Context

Directory entries contain relative local stream paths. The browsing node cannot
validate a remote host's opaque ticket or find its in-process party state.
Service calls use connected overlay Soulseek usernames, separately from web
account display identity. General shared-content previews do not enforce radio
permission.

## Decision

Advertise transport username and party capability separately. A manual,
authenticated listener request obtains host metadata and creates a local
opaque ticket pinned to that host, party, content and capability. A dedicated
ListedRadio mesh service validates current listing, streaming permission,
content and capability on each read. It accepts no filesystem paths.

Read at most 44 KiB per call, leaving room for base64 and JSON in the 64 KiB
overlay frame. Pace reads at no more than five per second and give this service
a 500-call budget within the existing 500-call global peer limit. Keep the
existing owner limiter, fairness gate, traffic accounting and bounded pipe.
Do not discover alternate peers or fall back to general shared-content reads.

## Consequences

Revocation and track changes stop further reads. Legacy announcements lacking
transport metadata cannot supply remote playback; the UI must report this.
The transport requires an existing mesh connection and does not
automatically contact additional peers. Its paced throughput is bounded;
high bitrate media may buffer. Physical and sustained playback checks remain
necessary to establish performance beyond controlled transport tests.

## Connected-neighbor discovery — 2026-09-28

A real two-backend browser workflow found that a connected overlay did not seed
the mesh DHT. Empty routing tables now bootstrap on demand through at most three
existing connected neighbors. FIND_NODE responses optionally include the real
responder node ID, preserving older response compatibility; known seed contacts
remain in closest-node results. Local values bypass remote bootstrap. This adds
no periodic discovery sweep and creates no new peer connections.

Native Chromium range replacement can keep the preceding response open. A new
range cancels the preceding producer for the same ticket, then waits up to two
seconds for owner/host leases to release. No concurrent stream allowance or
aggregate read pacing is increased. Different tickets cannot preempt each other.

Fairness is playback admission, rather than a decision repeated for each HTTP
range. An admitted radio ticket retains its admission only until ticket expiry;
the cache is bounded by the ticket service's 1,000-ticket limit and expired
entries are removed on subsequent valid radio opens. New tickets still check
fairness. Host permission, traffic accounting, concurrency and pacing remain
active on range reads.

## Radio accounting and recovery — 2026-09-28

Successful ListedRadio Read payloads are credited at the completed TLS write
boundary in both server and connector handlers. Counts exclude framing and do
not claim peer consumption. A radio HTTP response waits for its first audio
bytes; initial host failure returns 503 and releases owner/host reservations.
Both player layouts offer explicit retry after native buffering or failure.

Manual directory refresh bypasses the normal cache, shares in-flight work and
has a two-second cooldown. A valid index removes withdrawn remote entries while
preserving current local publications. The bounded in-memory DHT returns newest
locally received values first and promotes duplicate refreshes; this is local
receipt ordering, not distributed version agreement. Read pacing, concurrency
and fairness checks remain enforced.

## Transport identity and inbound reuse — 2026-09-28

Service calls and empty-table bootstrap may reuse handshaken, connected inbound
links advertising mesh service support. Outbound links retain preference when
both directions exist. No new connection or periodic discovery is added. Radio
host reservations normalize transport username casing, and per-peer call
counters use the same case-insensitive identity semantics as link selection;
owner keys and opaque capabilities retain their original identity semantics.

A signed Store keeps its cryptographically verified requester node ID and
publisher quota, but records the actual remote transport username as its
routing address. Cryptographic publisher IDs do not name overlay connections.
