#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
backfill="$repo_root/src/slskd/PodCore/PodMessageBackfill.cs"
tests="$repo_root/tests/slskd.Tests.Unit/PodCore/PodMessageBackfillTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Pod backfill safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$backfill" 'peer exception details are escaped'
require_pattern 'catch (OperationCanceledException) when (ct.IsCancellationRequested)' "$backfill" 'caller cancellation escapes generic failure mapping'
require_pattern 'ProcessBackfillResponse_WhenStorageThrows_EscapesPeerExceptionWithoutAttachingIt' "$tests" 'peer exception logging is behavior tested'
require_pattern 'SyncOnRejoin_WhenCallerCancelsStorageRead_PropagatesCancellation' "$tests" 'sync cancellation is behavior tested'
require_pattern 'HandleBackfillRequest_WhenCallerCancelsStorageRead_PropagatesCancellation' "$tests" 'request cancellation is behavior tested'
require_pattern 'ProcessBackfillResponse_WhenCallerCancelsStorageWrite_PropagatesCancellation' "$tests" 'response cancellation is behavior tested'

if rg -n '_logger\.Log(Error|Warning|Information|Debug|Trace)\(ex,' "$backfill"; then
  printf 'Pod backfill safety check failed: raw exception objects remain attached to log events\n' >&2
  exit 1
fi

printf 'Pod backfill cancellation and diagnostic boundaries are pinned.\n'
