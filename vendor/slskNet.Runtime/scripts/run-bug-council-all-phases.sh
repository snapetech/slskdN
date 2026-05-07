#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out_dir="${COUNCIL_OUT_DIR:-.council}"
mkdir -p "$out_dir"
scan_out="$out_dir/latest-candidate-counts.md"

printf '==> Fresh candidate inventory\n'
bash scripts/scan-bug-council-candidates.sh | tee "$scan_out"

printf '\n==> Active bug discovery probes\n'
bash scripts/run-council-active-bughunt.sh

printf '\n==> Regression and process gates\n'
bash scripts/check-remediation-baseline.sh
bash scripts/check-council-active-backlog.sh
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

printf '\nCouncil verdict: all registered slskNet.Runtime phases passed, and no registered drift/finding gate fired.\n'
printf 'Council verdict boundary: this is not proof of no bugs. It means the current calibrated lenses, active backlog, closed sweep counts, fuzz corpus, build, and vulnerability scan passed. Candidate counts were saved to %s and active-discovery candidates were saved under %s.\n' "$scan_out" "$out_dir"
