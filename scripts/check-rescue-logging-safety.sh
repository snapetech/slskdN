#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
service="$repo_root/src/slskd/Transfers/Rescue/RescueService.cs"
guardrails="$repo_root/src/slskd/Transfers/Rescue/RescueGuardrailService.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Transfers/Rescue/RescueServiceLoggingTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Rescue logging safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeFilePath(filename)' "$service" 'peer filenames are escaped at service log boundaries'
require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$service" 'exception details are escaped at service log boundaries'
require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(transferId)' "$guardrails" 'transfer IDs are escaped at guardrail log boundaries'
require_pattern 'LoggingSanitizer.SanitizeFilePath(filename)' "$guardrails" 'peer filenames are escaped at guardrail log boundaries'
require_pattern 'ActivateRescueModeAsync_EscapesCallerAndPeerDiagnosticsInLogs' "$tests" 'caller and peer diagnostics are behavior tested'

if rg -n 'log\.(Verbose|Debug|Information|Warning|Error|Fatal)\(ex,' "$service"; then
  printf 'Rescue logging safety check failed: raw exception objects remain attached to log events\n' >&2
  exit 1
fi

printf 'Rescue transfer and peer logging boundaries are pinned.\n'
