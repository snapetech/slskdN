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

if rg -n 'SearchScope\.Wishlist' "$repo_root/src/slskd" >&2; then
  failed=1
fi

expect_literal src/slskd/Wishlist/WishlistService.cs 'var scope = SearchScope.Network;'
expect_literal src/slskd/Wishlist/WishlistService.cs 'safetySource: "wishlist"'
expect_literal src/slskd/Backfill/BackfillSchedulerService.cs 'SemaphoreSlim backfillLock = new(2, 2)'
expect_literal src/slskd/Backfill/BackfillSchedulerService.cs 'config.MaxGlobalConnections'
expect_literal src/slskd/Backfill/BackfillSchedulerService.cs 'cancellationToken'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'TryConsumeSearchBudget("multisource-users"'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'TryConsumeSearchBudget("multisource-file-sources"'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'TryConsumeSearchBudget("multisource-download-file"'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'TryConsumeSearchBudget("multisource-swarm"'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'TryConsumeSearchBudget("multisource-search"'
expect_literal src/slskd/Transfers/MultiSource/API/MultiSourceController.cs 'TryConsumeSearchBudget("multisource-test"'
expect_literal tests/slskd.Tests.Unit/Transfers/MultiSource/API/MultiSourceControllerTests.cs 'GetTopUsers_WhenSearchSafetyBudgetExhausted_DoesNotSearchSoulseek'

if [ "$failed" -ne 0 ]; then
  cat >&2 <<'MSG'

Known Soulseek-facing automation must keep conservative network-health guardrails.
Broader automation inventories belong in the bug ledger until false positives are triaged.
MSG
  exit 1
fi

printf 'Known Soulseek-facing automation preserves network-health guardrails.\n'
