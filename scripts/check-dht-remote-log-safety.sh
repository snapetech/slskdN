#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
directories = (
    root / "src/slskd/DhtRendezvous",
    root / "src/slskd/Mesh/Dht",
)
sources = [path for directory in directories for path in directory.rglob("*.cs")]
raw_exception = re.compile(r"\b(?:_logger|logger)\.Log\w+\s*\(\s*(?:ex|exception)\s*,")
exception_text = re.compile(r"\b(?:ex|exception)\.ToString\(\)")

for path in sources:
    source = path.read_text()
    raw = raw_exception.search(source)
    if raw:
        line = source[:raw.start()].count("\n") + 1
        raise SystemExit(f"{path.relative_to(root)}:{line}: raw exception object is attached to a log event")
    for line_number, line in enumerate(source.splitlines(), start=1):
        if "Log" in line and exception_text.search(line) and "SanitizeExternalIdentifier(" not in line:
            raise SystemExit(f"{path.relative_to(root)}:{line_number}: exception text is not escaped at its log boundary")

required_source = {
    "src/slskd/DhtRendezvous/DhtRendezvousService.cs": (
        "LoggingSanitizer.SanitizeExternalIdentifier(uri.ToString())",
        "LoggingSanitizer.SanitizeExternalIdentifier(peerInfo.ToString())",
    ),
    "src/slskd/DhtRendezvous/SharedMeshUdpListener.cs": (
        "LoggingSanitizer.SanitizeExternalIdentifier(exception.ToString())",
    ),
    "src/slskd/Mesh/Dht/MeshDhtClient.cs": (
        "LoggingSanitizer.SanitizeExternalIdentifier(key)",
    ),
    "src/slskd/Mesh/Dht/MeshDirectory.cs": (
        "LoggingSanitizer.SanitizeExternalIdentifier(peerId)",
        "LoggingSanitizer.SanitizeExternalIdentifier(reason)",
    ),
    "src/slskd/Mesh/Dht/PeerDescriptorRefreshService.cs": (
        "LoggingSanitizer.SanitizeExternalIdentifier(reason)",
    ),
}
for relative_path, values in required_source.items():
    source = (root / relative_path).read_text()
    for value in values:
        if value not in source:
            raise SystemExit(f"{relative_path}: expected escaped DHT diagnostic value {value}")

tests = (
    root / "tests/slskd.Tests.Unit/DhtRendezvous/SharedMeshUdpListenerTests.cs",
    root / "tests/slskd.Tests.Unit/Mesh/Phase8MeshTests.cs",
)
for test_path in tests:
    source = test_path.read_text()
    if "Assert.Null(debug.Exception)" not in source and "Assert.Null(entry.Exception)" not in source:
        raise SystemExit(f"{test_path.relative_to(root)}: captured log test must assert no raw exception metadata")

for test_name in (
    "MalformedOverlayDatagram_LogsRateLimitedInformationWithoutException",
    "MeshDirectory_WhenRemoteDescriptorIsMalformed_EscapesPeerAndExceptionInLog",
):
    if not any(test_name in path.read_text() for path in tests):
        raise SystemExit(f"missing DHT log-boundary regression {test_name}")

print("DHT and mesh-directory remote diagnostics escape peer fields and exception details.")
PY
