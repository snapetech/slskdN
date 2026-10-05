#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
signer="$repo_root/src/slskd/Mesh/MeshMessageSigner.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Mesh/MeshMessageSignerTests.cs"

require_pattern() {
  local pattern="$1"
  local file="$2"
  local description="$3"

  if ! rg -q --fixed-strings "$pattern" "$file"; then
    printf 'Mesh signature safety check failed: %s\n' "$description" >&2
    exit 1
  fi
}

require_pattern 'LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())' "$signer" 'signing and verification exceptions are escaped'
require_pattern 'VerifyMessage_WhenPeerPublicKeyIsMalformed_EscapesFailureWithoutAttachingException' "$tests" 'peer verification failure logging is tested'
require_pattern 'SignMessage_WhenKeyStoreThrows_EscapesFailureAndRethrows' "$tests" 'signing failure logging preserves rethrow behavior'

if rg -n 'logger\.Log(Error|Warning|Information|Debug|Trace)\(ex,' "$signer"; then
  printf 'Mesh signature safety check failed: raw exception objects remain attached to log events\n' >&2
  exit 1
fi

printf 'Mesh signature exception diagnostics are pinned.\n'
