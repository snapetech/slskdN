#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
service="$repo_root/src/slskd/DhtRendezvous/NatDetectionService.cs"
tests="$repo_root/tests/slskd.Tests.Unit/DhtRendezvous/NatDetectionServiceTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'NAT probe safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$service" 'network exception details are escaped'
require_pattern 'cancellationToken.ThrowIfCancellationRequested();' "$service" 'STUN and HTTP probes check cancellation'
require_pattern 'InitializeAsync_WhenPublicIpServicesThrow_EscapesNetworkExceptions' "$tests" 'network exception logging is behavior tested offline'
require_pattern 'InitializeAsync_WhenCallerCancelsPublicIpProbe_PropagatesCancellation' "$tests" 'caller cancellation is behavior tested'

if rg -n '_logger\.Log(Debug|Information|Warning|Error)\(ex,' "$service"; then
  printf 'NAT probe safety check failed: raw exception objects remain attached to log events\n' >&2
  exit 1
fi

printf 'NAT probe cancellation and diagnostics are pinned.\n'
