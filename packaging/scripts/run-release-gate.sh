#!/usr/bin/env bash

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

section() {
    echo
    echo "==> $1"
}

ensure_tool() {
    local command_name="$1"
    local package_name="$2"

    if command -v "$command_name" >/dev/null 2>&1; then
        return
    fi

    if command -v apt-get >/dev/null 2>&1 && command -v sudo >/dev/null 2>&1; then
        section "Install ${package_name}"
        sudo apt-get update
        sudo apt-get install -y "$package_name"
        return
    fi

    echo "ERROR: required command '${command_name}' is not installed; install package '${package_name}' and rerun." >&2
    exit 127
}

ensure_tool rg ripgrep

section "Validate packaging metadata"
bash packaging/scripts/validate-packaging-metadata.sh

section "Run remediation baseline checks"
bash scripts/check-remediation-baseline.sh

section "Install frontend dependencies"
npm --prefix src/web ci --legacy-peer-deps

section "Run frontend unit tests"
npm --prefix src/web test

section "Build frontend"
npm --prefix src/web run build

section "Verify built frontend output"
node src/web/scripts/verify-build-output.mjs

section "Smoke built frontend under a subpath"
node src/web/scripts/smoke-subpath-build.mjs

section "Run backend unit tests"
dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj -c Release

section "Run backend smoke/regression tests"
dotnet test tests/slskd.Tests/slskd.Tests.csproj -c Release

section "Run backend integration smoke tests"
bash packaging/scripts/run-release-integration-smoke.sh

echo
echo "Release gate passed."
