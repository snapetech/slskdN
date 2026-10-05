#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
client="$repo_root/src/slskd/Mesh/Dht/KademliaRpcClient.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Mesh/KademliaRpcClientTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Kademlia RPC safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(reply.ErrorMessage)' "$client" 'remote RPC errors are escaped'
require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$client" 'peer exception details are escaped'
require_pattern 'PeerRpcFailure_EscapesAddressAndDiagnosticText' "$tests" 'all four RPC failure paths have behavior coverage'

if rg -n '_logger\.Log(Debug|Information|Warning|Error)\(ex,' "$client"; then
  printf 'Kademlia RPC safety check failed: raw exception objects remain attached to log events\n' >&2
  exit 1
fi

printf 'Kademlia peer RPC diagnostics are pinned.\n'
