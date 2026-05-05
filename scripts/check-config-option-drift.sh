#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
failed=0

expect_example() {
  local key="$1"

  if ! rg -q "^[#[:space:]]*${key}:" "$repo_root/config/slskd.example.yml"; then
    printf 'config/slskd.example.yml is missing option example for %s\n' "$key" >&2
    failed=1
  fi
}

expect_example relay
expect_example permissions
expect_example shares
expect_example transfers
expect_example player
expect_example dht
expect_example destinations
expect_example web
expect_example integrations
expect_example mesh
expect_example overlay
expect_example security
expect_example logging

if ! rg -q 'max_request_body_size' "$repo_root/config/slskd.example.yml"; then
  printf 'config/slskd.example.yml is missing web.max_request_body_size example\n' >&2
  failed=1
fi

if ! rg -q 'AllowMissingPrunePackageData' "$repo_root/packaging/scripts/validate-packaging-metadata.sh"; then
  printf 'packaging metadata validation must guard release publish option drift\n' >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

printf 'Representative config options are present in the example config and packaging drift gates.\n'
