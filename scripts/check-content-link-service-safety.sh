#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
service = root / "src/slskd/PodCore/ContentLinkService.cs"
source = service.read_text()
pattern = re.compile(
    r"catch \(OperationCanceledException\) when \(ct\.IsCancellationRequested\)"
    r"\s*\{\s*throw;\s*\}\s*catch \(Exception ex\)",
    re.MULTILINE,
)
guarded = len(pattern.findall(source))
broad = source.count("catch (Exception ex)")
if guarded != 3 or broad != 3:
    raise SystemExit(f"expected three caller-cancellation guards, found {guarded}/{broad}")

for required in (
    "LoggingSanitizer.SanitizeExternalIdentifier(normalizedContentId)",
    "LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())",
    "LoggingSanitizer.SanitizeExternalIdentifier(normalizedDomain)",
    "LoggingSanitizer.SanitizeQueryText(query)",
):
    if required not in source:
        raise SystemExit(f"missing safe content-link diagnostic: {required}")

if re.search(r"_logger\.Log\w+\(ex,", source):
    raise SystemExit("raw exception objects remain attached to content-link log events")

test_file = root / "tests/slskd.Tests.Unit/PodCore/ContentLinkServiceTests.cs"
tests = test_file.read_text()
for test_name in (
    "ValidateContentIdAsync_WhenCallerCancels_PropagatesCancellation",
    "GetContentMetadataAsync_WhenProviderFails_EscapesExceptionTextInLog",
    "SearchContentAsync_WhenCallerCancels_PropagatesCancellation",
    "SearchContentAsync_UnsupportedDomainEscapesDomainInLog",
):
    if test_name not in tests:
        raise SystemExit(f"missing content-link safety regression {test_name}")

print("Content-link cancellation and remote diagnostic boundaries are pinned.")
PY
