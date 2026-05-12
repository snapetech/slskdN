#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
workflow="$repo_root/.github/workflows/codeql.yml"
global_json="$repo_root/global.json"
project="$repo_root/src/slskd/slskd.csproj"
failed=0

if [ ! -f "$workflow" ]; then
  printf '.github/workflows/codeql.yml is missing\n' >&2
  exit 1
fi

target_major="$(sed -n 's/.*<TargetFramework>net\([0-9][0-9]*\)\..*/\1/p' "$project" | head -n1)"
sdk_major=""
sdk_version=""
if [ -f "$global_json" ]; then
  sdk_version="$(sed -n 's/.*"version": "\([^"]*\)".*/\1/p' "$global_json" | head -n1)"
  sdk_major="$(sed -n 's/.*"version": "\([0-9][0-9]*\)\..*/\1/p' "$global_json" | head -n1)"
fi

if [ -z "$target_major" ]; then
  printf 'unable to read target framework major version\n' >&2
  exit 1
fi

if [ -n "$sdk_major" ] && [ "$target_major" != "$sdk_major" ]; then
  printf 'global.json SDK major %s does not match slskd target framework net%s.0\n' "$sdk_major" "$target_major" >&2
  failed=1
fi

expected_version="${sdk_version:-$target_major}"
if ! rg -q "DOTNET_VERSION: '$expected_version'" "$workflow"; then
  printf 'CodeQL workflow must set DOTNET_VERSION to %s\n' "$expected_version" >&2
  failed=1
fi

if ! rg -q 'dotnet-version: \$\{\{ env\.DOTNET_VERSION \}\}' "$workflow"; then
  printf 'CodeQL workflow must use env.DOTNET_VERSION for setup-dotnet\n' >&2
  failed=1
fi

if rg -n "dotnet-version: '[0-9]+'" "$workflow" >&2; then
  printf 'CodeQL workflow must not pin a divergent hard-coded dotnet-version\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'CodeQL .NET setup matches the application target framework.\n'
