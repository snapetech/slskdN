#!/usr/bin/env bash

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

section() {
    echo
    echo "==> $1"
}

run_step() {
    local label="$1"
    local timeout_seconds="$2"
    shift 2

    section "$label"
    echo "Command timeout: ${timeout_seconds}s"
    echo "+ $*"

    set +e
    timeout --preserve-status --kill-after=60s "$timeout_seconds" "$@"
    local status=$?
    set -e

    if [[ "$status" -eq 124 || "$status" -eq 137 ]]; then
        echo "ERROR: ${label} exceeded ${timeout_seconds}s and was stopped." >&2
    fi

    if [[ "$status" -ne 0 ]]; then
        echo "ERROR: ${label} failed with exit code ${status}." >&2
        exit "$status"
    fi
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

run_step "Verify release branch sync" 120 \
    bash scripts/check-release-branch-sync.sh

run_step "Validate packaging metadata" 300 \
    bash packaging/scripts/validate-packaging-metadata.sh

run_step "Run remediation baseline checks" 300 \
    bash scripts/check-remediation-baseline.sh

run_step "Install frontend dependencies" 900 \
    npm --prefix src/web ci --legacy-peer-deps

run_step "Run frontend unit tests" 1200 \
    npm --prefix src/web test

run_step "Build frontend" 600 \
    npm --prefix src/web run build

run_step "Verify built frontend output" 180 \
    node src/web/scripts/verify-build-output.mjs

run_step "Smoke built frontend under a subpath" 180 \
    node src/web/scripts/smoke-subpath-build.mjs

run_step "Run backend unit tests" 1800 \
    dotnet test tests/slskd.Tests.Unit/slskd.Tests.Unit.csproj -c Release

run_step "Run backend smoke/regression tests" 1800 \
    dotnet test tests/slskd.Tests/slskd.Tests.csproj -c Release

run_step "Run backend integration smoke tests" 1800 \
    bash packaging/scripts/run-release-integration-smoke.sh

echo
echo "Release gate passed."
