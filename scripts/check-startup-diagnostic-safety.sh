#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import sys

root = Path(sys.argv[1])
required = {
    "src/slskd/Bootstrap/StartupCommandMode.cs": (
        "LoggingSanitizer.SanitizeFilePath(filename)",
    ),
    "src/slskd/Bootstrap/StartupApplicationDirectories.cs": (
        "LoggingSanitizer.SanitizeExternalIdentifier(appName)",
        "LoggingSanitizer.SanitizeFilePath(directories.AppDirectory)",
        "LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())",
    ),
    "src/slskd/Bootstrap/StartupDiagnostics.cs": (
        "LoggingSanitizer.SanitizeFilePath(context.ExecutablePath)",
        "LoggingSanitizer.SanitizeFilePath(context.BaseDirectory)",
        "LoggingSanitizer.SanitizeExternalIdentifier(optionsAtStartup.InstanceName)",
        "LoggingSanitizer.SanitizeFilePath(context.AppDirectory)",
        "LoggingSanitizer.SanitizeFilePath(context.ConfigurationFile)",
        "LoggingSanitizer.SanitizeFilePath(context.DataDirectory)",
        "LoggingSanitizer.SanitizeFilePath(context.LogDirectory)",
        "LoggingSanitizer.SanitizeExternalIdentifier(warning)",
        "LoggingSanitizer.SanitizeExternalIdentifierOrUrl(optionsAtStartup.Logger.Loki)",
    ),
    "src/slskd/Bootstrap/StartupFileSystem.cs": (
        "LoggingSanitizer.SanitizeFilePath(configurationFile)",
        "LoggingSanitizer.SanitizeExternalIdentifier(ex.ToString())",
    ),
}

for relative_path, snippets in required.items():
    source = (root / relative_path).read_text()
    for snippet in snippets:
        if snippet not in source:
            raise SystemExit(f"{relative_path}: missing startup diagnostic sanitizer {snippet}")

test_path = root / "tests/slskd.Tests.Unit/Bootstrap/StartupLoggingTests.cs"
tests = test_path.read_text()
for test_name in (
    "LogStartupDiagnostics_EscapesConfiguredPathsAndRedactsLokiCredentials",
    "RecreateConfigurationFileIfMissing_EscapesPathAndExceptionInLogs",
):
    if test_name not in tests:
        raise SystemExit(f"{test_path.relative_to(root)}: missing startup diagnostic regression {test_name}")

print("Startup diagnostic paths and configured Loki credentials are safe at log boundaries.")
PY
