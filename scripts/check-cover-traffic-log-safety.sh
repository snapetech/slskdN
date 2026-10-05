#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
generator="$repo_root/src/slskd/Common/Security/CoverTrafficGenerator.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Common/Security/CoverTrafficGeneratorTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Cover traffic logging safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$generator" 'send failures are escaped at the worker log boundary'
require_pattern 'GenerateCoverTraffic_WhenSendThrows_EscapesExceptionWithoutAttachingIt' "$tests" 'worker failure diagnostics are behavior tested'
require_pattern 'Dispose_WhenGenerationFailed_EscapesExceptionWithoutAttachingIt' "$tests" 'disposal failure diagnostics are behavior tested'
require_pattern 'Dispose_WhenGenerationFaultsAfterStopTimeout_EscapesExceptionWithoutAttachingIt' "$tests" 'timed-out cleanup diagnostics are behavior tested'

if raw_exception_logs="$(rg -n -- '_logger\.Log[A-Za-z]+\((ex|exception),' "$generator")"; then
  printf '%s\n' "$raw_exception_logs"
  printf 'Cover traffic logging safety check failed: raw exception objects remain attached to log events\n' >&2
  exit 1
else
  scan_status=$?
  if [[ "$scan_status" -ne 1 ]]; then
    printf 'Cover traffic logging safety check could not scan exception log calls (rg exit %s)\n' "$scan_status" >&2
    exit "$scan_status"
  fi
fi

printf 'Cover traffic send exception logging is pinned.\n'
