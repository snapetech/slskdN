#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
handler="$repo_root/src/slskd/DhtRendezvous/Search/MeshSearchRpcHandler.cs"
tests="$repo_root/tests/slskd.Tests.Unit/DhtRendezvous/Search/MeshSearchRpcHandlerTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Mesh search safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'cancellationToken.ThrowIfCancellationRequested();' "$handler" 'caller cancellation is checked before work'
require_pattern '.WaitAsync(operationToken)' "$handler" 'the search wait observes the linked cancellation budget'
require_pattern 'operationToken.ThrowIfCancellationRequested();' "$handler" 'result processing observes cancellation'
require_pattern 'catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)' "$handler" 'caller cancellation escapes ordinary error mapping'
require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(request.RequestId)' "$handler" 'request IDs are escaped in diagnostics'
require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$handler" 'exception details are escaped in diagnostics'
require_pattern 'HandleAsync_CallerCancellationBeforeSearch_Propagates' "$tests" 'pre-search cancellation is behavior tested'
require_pattern 'HandleAsync_CallerCancellationDuringSearch_StopsWaitingAndPropagates' "$tests" 'search wait cancellation is behavior tested'
require_pattern 'HandleAsync_SearchFailureEscapesRequestIdAndExceptionInLogs' "$tests" 'request and exception log escaping is behavior tested'
require_pattern 'HandleAsync_ContentLookupFailureEscapesFilenameAndExceptionInLogs' "$tests" 'filename and content lookup log escaping is behavior tested'

printf 'Mesh search cancellation and diagnostic boundaries are pinned.\n'
