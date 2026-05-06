#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
test_file="$repo_root/tests/slskd.Tests/WebInputAdversarialFuzzTests.cs"
failed=0

require_literal() {
  local literal="$1"
  if ! rg -q --fixed-strings "$literal" "$test_file"; then
    printf 'Web input fuzz harness is missing required marker: %s\n' "$literal" >&2
    failed=1
  fi
}

if [ ! -f "$test_file" ]; then
  printf 'Missing slskd Web input adversarial fuzz harness: %s\n' "${test_file#$repo_root/}" >&2
  exit 1
fi

require_literal "MalformedJsonLoginBodies_ReturnClientErrorsWithoutUnhandledExceptions"
require_literal "RandomByteLoginBodies_ReturnClientErrorsWithoutUnhandledExceptions"
require_literal "HostileQueryAndPathInputs_ReturnDocumentedHttpResponses"
require_literal "ByteArrayContent"
require_literal "Uri.EscapeDataString"
require_literal "/api/v0/session"

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Web input adversarial fuzz harness is present.\n'
