#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out_dir="${COUNCIL_OUT_DIR:-.council}"
mkdir -p "$out_dir"
scan_out="$out_dir/latest-candidate-counts.md"

printf '==> Fresh candidate inventory\n'
bash scripts/scan-bug-council-candidates.sh | tee "$scan_out"

printf '\n==> Regression and process gates\n'
bash scripts/check-remediation-baseline.sh
bash scripts/check-council-sweep-counts.sh
bash scripts/check-council-negative-space.sh

printf '\n==> Runtime restore\n'
dotnet restore slskNet.Runtime.sln

printf '\n==> Analyzer lenses\n'
dotnet test tests/Soulseek.CouncilAnalyzers.Tests/Soulseek.CouncilAnalyzers.Tests.csproj --no-restore
dotnet test tests/Soulseek.CouncilAnalyzers.Calibration/Soulseek.CouncilAnalyzers.Calibration.csproj --no-restore

printf '\n==> Protocol fuzz corpus\n'
dotnet test tests/Soulseek.Tests.Unit/Soulseek.Tests.Unit.csproj --no-restore --filter Category=Fuzz

printf '\n==> Runtime build and package vulnerability scan\n'
dotnet build slskNet.Runtime.sln --no-restore
dotnet list slskNet.Runtime.sln package --vulnerable --include-transitive

printf '\nAll slskNet.Runtime bug council phases passed. Candidate counts saved to %s.\n' "$scan_out"
