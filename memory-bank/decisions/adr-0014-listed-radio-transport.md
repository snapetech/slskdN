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
