#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
runner="$repo_root/scripts/run-bug-council-all-phases.sh"
failed=0

require_literal() {
  local literal="$1"
  local file="$2"

  if ! rg -q --fixed-strings "$literal" "$file"; then
    printf '%s is missing required council all-phases marker: %s\n' "${file#$repo_root/}" "$literal" >&2
    failed=1
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
    failed=1
  fi
}

if [ ! -x "$runner" ]; then
  printf 'Council all-phases runner is missing or not executable: %s\n' "${runner#$repo_root/}" >&2
  exit 1
fi

require_literal "scan-bug-council-candidates.sh" "$runner"
require_literal "run-council-active-bughunt.sh" "$runner"
require_literal "check-remediation-baseline.sh" "$runner"
require_literal "check-council-active-backlog.sh" "$runner"
require_literal "check-council-sweep-counts.sh" "$runner"
require_literal "check-council-negative-space.sh" "$runner"
require_literal "Soulseek.CouncilAnalyzers.Tests.csproj" "$runner"
require_literal "Soulseek.CouncilAnalyzers.Calibration.csproj" "$runner"
require_literal "Category=Fuzz" "$runner"
require_literal "dotnet restore slskNet.Runtime.sln" "$runner"
require_literal "dotnet build slskNet.Runtime.sln" "$runner"
require_literal "dotnet list slskNet.Runtime.sln package --vulnerable --include-transitive" "$runner"
require_literal "this is not proof of no bugs" "$runner"

require_literal 'scripts/check-bug-council-all-phases.sh' "$repo_root/scripts/check-remediation-baseline.sh"
require_literal "bug-council-active-backlog.md" "$repo_root/scripts/check-remediation-baseline.sh"
require_literal "not proof of no bugs" "$repo_root/scripts/run-council-active-bughunt.sh"

assert_phase_done "Council process upgrades"
assert_phase_done "Roslyn"
assert_phase_done "Protocol fuzz harness"
assert_phase_done "Generic"
assert_phase_done "Mirror to"
assert_phase_done "Broaden CSL0001"
assert_phase_done "Add CSL0002"
assert_phase_done "Mutation/calibration"
assert_phase_done "Multi-seed"
assert_phase_done "All-phases council runner"
assert_phase_done "Non-proof verdict"
assert_phase_done "Active backlog"
assert_phase_done "Add CSL0003"
assert_phase_done "Add CSL0004"
assert_phase_done "Add batched semantic"
assert_phase_done "Add full runtime semantic"

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Bug council all-phases runner is registered.\n'
