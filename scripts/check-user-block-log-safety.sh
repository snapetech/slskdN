#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
service="$repo_root/src/slskd/Users/Notes/UserBlockService.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Users/UserBlockServiceTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'User block logging safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(username)' "$service" 'blocked usernames are escaped only at log calls'
require_pattern 'BlockAsync_EscapesUsernameOnlyInLogs' "$tests" 'stored username and diagnostic output are behavior tested'

printf 'User block username log escaping is pinned.\n'
