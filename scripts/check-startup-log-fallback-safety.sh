#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
startup_logging="$repo_root/src/slskd/Bootstrap/StartupLogging.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Bootstrap/StartupLoggingTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Startup log fallback safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.Message)' "$startup_logging" 'callback exception text is escaped before stderr output'
require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(message)' "$startup_logging" 'rendered log text is escaped before stderr output'
require_pattern 'Configure_WhenLogRecordCallbackThrows_EscapesFallbackText' "$tests" 'the callback failure boundary is behavior tested'

printf 'Startup logger fallback escaping is pinned.\n'
