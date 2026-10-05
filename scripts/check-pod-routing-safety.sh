#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
controller="$repo_root/src/slskd/PodCore/API/Controllers/PodMessageRoutingController.cs"
tests="$repo_root/tests/slskd.Tests.Unit/PodCore/PodMessageRoutingControllerTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Pod routing safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(result.MessageId)' "$controller" 'router message IDs are escaped at log boundaries'
require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$controller" 'exception details are escaped at log boundaries'
require_pattern 'catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)' "$controller" 'caller cancellation bypasses generic error mapping'
require_pattern 'RouteMessage_WhenRouterReturnsExternalIdentifiers_EscapesOnlyLogValues' "$tests" 'success diagnostics are behavior tested'
require_pattern 'RouteMessage_WhenRouterThrows_EscapesExceptionWithoutAttachingIt' "$tests" 'exception diagnostics are behavior tested'
require_pattern 'AsyncAction_WhenCallerCancels_PropagatesCancellation' "$tests" 'each asynchronous action preserves cancellation'

if rg -n '_logger\.Log(Error|Warning|Information|Debug|Trace)\(ex,' "$controller"; then
  printf 'Pod routing safety check failed: raw exception objects remain attached to log events\n' >&2
  exit 1
fi

printf 'Pod routing cancellation and diagnostic boundaries are pinned.\n'
