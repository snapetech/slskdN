#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
service="$repo_root/src/slskd/Identity/LanDiscoveryService.cs"
tests="$repo_root/tests/slskd.Tests.Unit/Identity/LanDiscoveryServiceTests.cs"

if ! rg -q --fixed-strings 'LogDiscoveredPeerParseFailure(host.DisplayName, ex)' "$service"; then
  printf 'LAN discovery logging safety check failed: peer parse errors bypass the sanitized log boundary\n' >&2
  exit 1
fi

if ! rg -q --fixed-strings 'LoggingSanitizer.SanitizeExternalIdentifier(displayName)' "$service" ||
   ! rg -q --fixed-strings 'LoggingSanitizer.SanitizeExternalIdentifier(exception.ToString())' "$service"; then
  printf 'LAN discovery logging safety check failed: peer names or exceptions are not escaped\n' >&2
  exit 1
fi

if ! rg -q --fixed-strings 'LogDiscoveredPeerParseFailure_EscapesRemoteNameAndExceptionWithoutAttachingIt' "$tests"; then
  printf 'LAN discovery logging safety check failed: captured-log regression is missing\n' >&2
  exit 1
fi

printf 'LAN discovery peer parse diagnostics are pinned.\n'
