#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

assert_mirror() {
  local name="$1"
  local local_file="$repo_root/docs/dev/$name"
  local runtime_file="$repo_root/vendor/slskNet.Runtime/docs/dev/$name"

  if ! cmp -s "$local_file" "$runtime_file"; then
    printf 'Council mirror drift: docs/dev/%s differs from vendored canonical copy\n' "$name" >&2
    exit 1
  fi
}

assert_phase_done() {
  local phase_name="$1"

  if ! awk -F'|' -v phase="$phase_name" '
    $2 ~ /^[[:space:]]*[0-9]+[[:space:]]*$/ && $3 ~ phase {
      status = $4
      gsub(/^[[:space:]]+|[[:space:]]+$/, "", status)
      found = 1
      if (status != "Done") {
        exit 2
      }
    }
    END {
      if (!found) {
        exit 3
      }
    }
  ' "$repo_root/docs/dev/bug-council-phases.md"; then
    printf 'Council phase is not Done or is missing: %s\n' "$phase_name" >&2
    exit 1
  fi
}

assert_mirror "bug-council-severity-schema.md"
assert_mirror "bug-council-sibling-search.md"
assert_mirror "bug-council-behavior-pinning.md"

assert_phase_done "Mirror council process docs"
assert_phase_done "Wire negative-space gate into the meta-runner"
assert_phase_done "Web-input adversarial fuzz"

npm run check:remediation
dotnet test "$repo_root/tests/slskd.Tests/slskd.Tests.csproj" --no-restore --filter FullyQualifiedName~WebInputAdversarialFuzzTests

printf 'All slskd bug council phases passed.\n'
