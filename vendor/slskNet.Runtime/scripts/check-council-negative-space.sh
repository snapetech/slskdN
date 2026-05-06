#!/usr/bin/env bash
#
# Negative-space gate for the bug council. Asserts that every declared trust
# boundary in docs/dev/bug-council-negative-space.md still has its required
# validator symbol present in the expected sink file. This catches the
# "I added a new boundary and forgot to wire the validator" failure mode that
# the candidate scanner cannot see.
#
# Wired into scripts/check-remediation-baseline.sh; see
# docs/dev/bug-council-negative-space.md for the policy.

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

failures=0

pass() {
  printf 'PASS %s\n' "$1"
}

fail() {
  printf 'FAIL %s\n' "$1" >&2
  failures=$((failures + 1))
}

assert_validator_present() {
  local boundary="$1"
  local sink="$2"
  local symbol="$3"

  if [[ ! -e "$sink" ]]; then
    fail "negative-space: sink missing for boundary [$boundary]: $sink"
    return
  fi

  if rg -n --fixed-strings -- "$symbol" "$sink" >/dev/null; then
    pass "negative-space: [$boundary] $symbol present in $sink"
  else
    fail "negative-space: [$boundary] $symbol missing from $sink"
  fi
}

# Server message frames (TCP from server)
assert_validator_present \
  "server-message-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "ValidateMessageLength"
assert_validator_present \
  "server-message-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "MaxMessageLength"

# Init frames (TCP from peer)
assert_validator_present \
  "init-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "ValidateInitMessageLength"
assert_validator_present \
  "init-frames" \
  "src/Network/MessageFrameValidator.cs" \
  "MaxInitMessageLength"

# Buffered network reads
assert_validator_present \
  "buffered-network-reads" \
  "src/Network/Tcp/Connection.cs" \
  "MaximumBufferedReadLength"

# Server protocol counts
assert_validator_present \
  "server-protocol-counts" \
  "src/Messaging/Messages/Server/ProtocolCountReader.cs" \
  "ReadValidatedCount"

# Server endpoint ports
assert_validator_present \
  "server-endpoint-ports" \
  "src/Messaging/Messages/ProtocolValueValidator.cs" \
  "ValidatePort"

# Peer transfer file sizes
assert_validator_present \
  "peer-transfer-file-sizes" \
  "src/Messaging/Messages/Peer/TransferRequest.cs" \
  "Invalid transfer file size"
assert_validator_present \
  "peer-transfer-file-sizes" \
  "src/Messaging/Messages/Peer/TransferResponse.cs" \
  "Invalid transfer file size"

# Distributed branch level
assert_validator_present \
  "distributed-branch-level" \
  "src/Messaging/Messages/Distributed/DistributedBranchLevel.cs" \
  "branch level"

# Distributed child depth
assert_validator_present \
  "distributed-child-depth" \
  "src/Messaging/Messages/Distributed/DistributedChildDepth.cs" \
  "child depth"

# Resolver outputs (raw response handler is the negative-space anchor for
# application-supplied data crossing the peer serialization boundary).
assert_validator_present \
  "resolver-outputs" \
  "src/Messaging/Handlers/PeerMessageHandler.cs" \
  "WriteRaw"

if [[ "$failures" -gt 0 ]]; then
  printf '\n%d negative-space gate check(s) failed.\n' "$failures" >&2
  exit 1
fi

printf '\nAll negative-space gate checks passed.\n'
