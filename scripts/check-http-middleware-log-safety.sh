#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
path = root / "src/slskd/Bootstrap/WebApplicationPipelineExtensions.cs"
source = path.read_text()

raw_exception = re.compile(r"Serilog\.Log\.(?:Debug|Warning|Error)\s*\(\s*ex\s*,")
match = raw_exception.search(source)
if match:
    line = source[:match.start()].count("\n") + 1
    raise SystemExit(f"{path.relative_to(root)}:{line}: middleware must not attach raw exception metadata")

if "ex.Message" in source:
    raise SystemExit(f"{path.relative_to(root)}: middleware must not log raw exception messages")

escaped_exception = "LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())"
if source.count(escaped_exception) != 3:
    raise SystemExit(
        f"{path.relative_to(root)}: expected escaped full exception text at the HTTP error and two CSRF log sites"
    )

if source.count("LoggingSanitizer.SanitizeExternalIdentifier(context.Request.Method)") < 3:
    raise SystemExit(f"{path.relative_to(root)}: request methods must be escaped at all exception log sites")

test_path = root / "tests/slskd.Tests.Unit/Common/Security/LoggingSanitizerTests.cs"
test_source = test_path.read_text()
if "SanitizeExternalIdentifier_WithExceptionText_EscapesRequestControlledLineBreaks" not in test_source:
    raise SystemExit(f"{test_path.relative_to(root)}: missing exception-text escaping regression")

print("HTTP exception and CSRF middleware diagnostics escape request and exception text.")
PY
