#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
pod_core = root / "src/slskd/PodCore"
sources = list(pod_core.rglob("*.cs"))
raw_exception = re.compile(r"\b(?:_logger|logger)\.Log\w+\s*\(\s*ex\s*,")
escaped_exception = "LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())"
exception_field = "; exception: {Exception}"

raw_calls = [
    f"{path.relative_to(root)}:{source[:match.start()].count(chr(10)) + 1}"
    for path in sources
    for source in [path.read_text()]
    for match in raw_exception.finditer(source)
]
if raw_calls:
    raise SystemExit("PodCore log events must not attach raw exceptions: " + ", ".join(raw_calls))

exception_logs = [
    line
    for path in sources
    for line in path.read_text().splitlines()
    if exception_field in line
]
if len(exception_logs) != 94:
    raise SystemExit(f"expected 94 escaped PodCore exception log events, found {len(exception_logs)}")

for line in exception_logs:
    if escaped_exception not in line:
        raise SystemExit(f"PodCore exception log does not escape exception text: {line.strip()}")
    open_paren = line.find("(")
    close_paren = line.rfind(")")
    arguments = line[open_paren + 1:close_paren]
    quoted = False
    escaped = False
    depth = 0
    current = []
    parts = []
    for char in arguments:
        if quoted:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                quoted = False
        elif char == '"':
            quoted = True
        elif char == "(":
            depth += 1
        elif char == ")":
            depth -= 1
        elif char == "," and depth == 0:
            parts.append("".join(current).strip())
            current = []
            continue
        current.append(char)
    parts.append("".join(current).strip())
    if len(parts) < 2 or not parts[0].startswith('"'):
        raise SystemExit(f"PodCore exception log lost its static message template: {line.strip()}")
    if any(not value.startswith("LoggingSanitizer.SanitizeExternalIdentifier(") for value in parts[1:]):
        raise SystemExit(f"PodCore exception log has an unescaped dynamic field: {line.strip()}")

tests = root / "tests/slskd.Tests.Unit/PodCore/PodChannelControllerTests.cs"
test_source = tests.read_text()
for required in (
    "CreateChannel_WhenServiceThrowsArgumentException_EscapesRequestAndExceptionInLog",
    "Assert.DoesNotContain('\\r', entry.Message)",
    "Assert.DoesNotContain('\\n', entry.Message)",
    "Assert.Null(entry.Exception)",
):
    if required not in test_source:
        raise SystemExit(f"missing PodCore log-safety regression assertion: {required}")

print("PodCore exception and request-value diagnostics are escaped at their log boundaries.")
PY
