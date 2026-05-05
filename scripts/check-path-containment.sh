#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

expect_literal() {
  local file="$1"
  local literal="$2"

  if ! grep -Fq -- "$literal" "$repo_root/$file"; then
    printf '%s is missing literal: %s\n' "$file" "$literal" >&2
    failed=1
  fi
}

expect_literal src/slskd/Relay/RelayClient.cs 'PathGuard.NormalizeAndValidate'
expect_literal src/slskd/Relay/RelayClient.cs 'CopyWithLimitAsync'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'IOPath.GetFileName'
expect_literal src/slskd/LibraryHealth/LibraryHealthService.cs 'NormalizeAbsolutePathWithinRoots'
expect_literal src/slskd/Common/Security/PathGuard.cs 'NormalizeAndValidate'
expect_literal src/slskd/Common/Security/PathGuard.cs 'NormalizeAbsolutePathWithinRoots'

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Known remote/user-derived file write paths must keep PathGuard containment.
Broader file-operation inventories belong in the bug ledger until false positives are triaged.
MSG
  exit 1
fi

printf 'Known remote/user-derived file paths are paired with containment helpers.\n'
