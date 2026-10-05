#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
transport="$repo_root/src/slskd/Common/Security/I2PTransport.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Mesh/Transport/I2PTransportTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'I2P probe deadline check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'ConnectAsync(samHost, samPort, linkedCts.Token)' "$transport" 'TCP connect uses the linked deadline'
require_pattern 'WriteLineAsync("HELLO VERSION MIN=3.1 MAX=3.1".AsMemory(), linkedCts.Token)' "$transport" 'SAM HELLO write uses the linked deadline'
require_pattern 'FlushAsync(linkedCts.Token)' "$transport" 'SAM HELLO flush uses the linked deadline'
require_pattern 'ReadLineAsync(linkedCts.Token)' "$transport" 'SAM HELLO response read uses the linked deadline'
require_pattern 'IsAvailableAsync_SamPeerAcceptsButDoesNotRespond_UsesProbeTimeout' "$tests" 'a stalled SAM peer is behavior tested'

printf 'I2P availability probe deadline is pinned.\n'
