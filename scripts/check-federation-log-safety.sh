#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
federation="$repo_root/src/slskd/SocialFederation"
tests="$repo_root/tests/slskd.Tests.Unit/SocialFederation"

if rg -n 'Log(Debug|Information|Warning|Error|Critical)\(ex,' "$federation" -g '*.cs'; then
  printf 'Federation logging safety check failed: a raw exception object is attached to a log entry\n' >&2
  exit 1
else
  result=$?
  if [[ $result -ne 1 ]]; then
    printf 'Federation logging safety check failed: ripgrep returned status %s\n' "$result" >&2
    exit "$result"
  fi
fi

for source in \
  "$federation/ActivityDeliveryService.cs" \
  "$federation/HttpSignatureKeyFetcher.cs" \
  "$federation/API/ActivityPubController.cs" \
  "$federation/API/WebFingerController.cs" \
  "$federation/TasteRecommendationService.cs" \
  "$federation/FederationService.cs" \
  "$federation/VirtualSoulfindFederationIntegration.cs" \
  "$federation/ActivityPubKeyStore.cs"; do
  if ! rg -q '(LoggingSanitizer|LogSanitizer)\.' "$source"; then
    printf 'Federation logging safety check failed: %s has no logging sanitizer boundary\n' "${source#"$repo_root/"}" >&2
    exit 1
  fi
done

for test_name in \
  'LogDeliveryFailure_RedactsRemoteUrlAndDoesNotAttachException' \
  'FetchPublicKeyPkixAsync_FailureRedactsRemoteUrlAndDoesNotAttachException' \
  'LogInboundActivity_EscapesRemoteTextAndRedactsActorUrlDetails' \
  'WebFingerResourceLogs_RedactUrlSecretsAndEscapeMalformedResources' \
  'GetRecommendationsAsync_MalformedRemoteWorkRefEscapesActorAndDoesNotAttachException'; do
  if ! rg -q --fixed-strings "$test_name" "$tests"; then
    printf 'Federation logging safety check failed: captured-log regression %s is missing\n' "$test_name" >&2
    exit 1
  fi
done

printf 'Federation remote-input diagnostics are pinned.\n'
