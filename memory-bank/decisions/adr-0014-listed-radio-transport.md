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
The transport requires an existing outbound mesh connection and does not
automatically contact additional peers. Its paced throughput is bounded;
high bitrate media may buffer. Physical and sustained playback checks remain
necessary to establish performance beyond controlled transport tests.

## Connected-neighbor discovery — 2026-09-28

A real two-backend browser workflow found that a connected overlay did not seed
the mesh DHT. Empty routing tables now bootstrap on demand through at most three
existing outbound neighbors. FIND_NODE responses optionally include the real
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
