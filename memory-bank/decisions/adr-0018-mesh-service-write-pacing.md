# ADR-0018: Bound and pace mesh service writes

Status: Accepted  
Date: 2026-09-28

## Context

The accepted mesh transport permits ten received frames per rolling second.
A pending-call limit does not limit sequential request throughput: ordinary
listed-radio metadata bursts exhausted the quota on real TLS connections.
Delaying replies inside the receive loop would also delay quota checks and let
an abusive raw burst spread over enough time to escape rejection.

## Decision

Each connection owns a bounded FIFO service writer, started lazily on its first
service frame. Calls and replies share a 140-millisecond minimum interval,
measured with a monotonic clock after the previous write and accounting callback.
The queue holds at most 32 waiting entries plus one active entry. Overflow closes
the connection. Ping, Pong and other controls retain direct writes; pacing and
post-send accounting do not hold the framer lock.

Receive loops enqueue replies without awaiting pacing. Their existing ingress
quota remains unchanged and counts actual received frames. Successful radio
upload accounting runs after its payload has actually been written. Caller
cancellation completes waiting callers promptly and skips unsent entries. Once
committed, a frame finishes with the connection token and existing write timeout,
so caller cancellation cannot split its length header from its payload. Active
write timeouts remain transport failures even if the caller has also canceled.
Writer failures are observed, close the transport and fail pending callers;
connection disposal cancels and awaits the owned worker before freeing resources.
An idle worker waits on the channel without polling or extra connections.

## Consequences

Normal sequential RPC bursts no longer disconnect solely because callers are
fast. Real TLS tests cover bursts in both directions, raw ten/eleven-frame
boundaries, cancellation, control progress, bounded overflow and writer failure.
Shared pacing preserves room for ordinary control traffic but does not guarantee
arbitrary control bursts fit the ingress quota. Queueing adds latency under load.
High-rate sustained radio formats still require explicit throughput validation;
this decision does not establish their support. No public discovery, peer scan,
new dependency or configuration setting is introduced.
