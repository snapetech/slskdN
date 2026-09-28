# ADR-0013: Register Server-Discovered Player Files

## Context

The player can search configured download/share directories before indexing.
Content IDs for these files use path and size identity. Resolving an arbitrary
unknown ID uses a bounded directory scan with a cooldown; rapid selection of
different known downloads must not depend on that scan becoming available.

## Decision

The library picker registers only its returned local page with the existing
content locator. Registration verifies allowed roots, existence, nonzero size
and any existing non-advertisable mapping, then primes the bounded path cache.
Streaming still checks repository restrictions and current roots/size on reuse.
Unknown-ID scan limits remain unchanged. No client path input is introduced.

## Consequences

Ticket, metadata and media requests use paths already found by explicit search
without additional recursive enumeration or audio hashing. The cache remains
bounded and process-local; restart still uses allowed-root resolution until
an explicit picker search supplies the known path again. Changing or removing
a root or file invalidates cached playback access.
