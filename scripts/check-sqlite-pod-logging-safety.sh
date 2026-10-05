#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
service = (root / "src/slskd/PodCore/SqlitePodService.cs").read_text()
controller = (root / "src/slskd/PodCore/API/Controllers/PodContentController.cs").read_text()

for relative_path, source, expected_sanitized_exceptions in (
    ("SqlitePodService.cs", service, 14),
    ("PodContentController.cs", controller, 5),
):
    if re.search(r"(?:logger|_logger)\.Log\w+\(ex,", source):
        raise SystemExit(f"{relative_path}: raw exception objects remain attached to log events")

    sanitized = source.count("LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())")
    if sanitized != expected_sanitized_exceptions:
        raise SystemExit(
            f"{relative_path}: expected {expected_sanitized_exceptions} escaped exception values, found {sanitized}"
        )

for required in (
    "LoggingSanitizer.SanitizeExternalIdentifier(pod.PodId)",
    "LoggingSanitizer.SanitizeExternalIdentifier(podId)",
    "LoggingSanitizer.SanitizeExternalIdentifier(peerId)",
    "LoggingSanitizer.SanitizeExternalIdentifier(channel.ChannelId)",
    "LoggingSanitizer.SanitizeExternalIdentifier(channelId)",
):
    if required not in service:
        raise SystemExit(f"SqlitePodService.cs: missing safe caller identifier {required}")

cancellation_guards = len(re.findall(
    r"catch \(OperationCanceledException\) when \(ct\.IsCancellationRequested\)",
    service,
))
if cancellation_guards != 6:
    raise SystemExit(f"SqlitePodService.cs: expected six caller-cancellation guards, found {cancellation_guards}")

controller_tests = (root / "tests/slskd.Tests.Unit/PodCore/PodContentControllerTests.cs").read_text()
service_tests = (root / "tests/slskd.Tests.Unit/PodCore/SqlitePodServiceTests.cs").read_text()
for test_name, source in (
    ("CreateContentLinkedPod_WhenServiceThrowsArgumentException_ReturnsSanitizedBadRequest", controller_tests),
    ("CreateContentLinkedPod_WhenServiceFails_EscapesExceptionInLog", controller_tests),
    ("CreateAsync_WhenStorageFails_EscapesExceptionInLogAndRollsBack", service_tests),
    ("CreateAsync_WhenCallerCancelsDuringInsert_PropagatesAndRollsBack", service_tests),
    ("GetPodAsync_WhenCallerCancels_PropagatesCancellation", service_tests),
    ("JoinAsync_WhenCallerCancelsDuringRead_PropagatesCancellationAndRollsBack", service_tests),
    ("UpdateAsync_WhenCallerCancelsDuringRead_PropagatesAndRollsBack", service_tests),
    ("DeletePodAsync_WhenCallerCancelsDuringRead_PropagatesAndKeepsPod", service_tests),
    ("UpdateAsync_WhenPublishIsCancelled_PropagatesAfterCommittedUpdate", service_tests),
    ("UpdateAsync_WhenPublisherFails_EscapesExceptionAndKeepsCommittedUpdate", service_tests),
):
    if test_name not in source:
        raise SystemExit(f"missing Pod logging regression {test_name}")

if controller_tests.count("Assert.Null(entry.Exception)") < 2 or service_tests.count("Assert.Null(entry.Exception)") < 2:
    raise SystemExit("Pod logging regressions must assert that raw exception metadata is absent")

print("Pod persistence, publishing, and content-linked API diagnostics are escaped at their log boundaries.")
PY
