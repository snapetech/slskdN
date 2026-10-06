#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
source_root = root / "src"
mutable_array = re.compile(
    r"public\s+static\s+(?:readonly\s+)?[A-Za-z0-9_.<>?, \t]+\[\][ \t]+[A-Za-z_][A-Za-z0-9_]*[ \t]*(?:=(?!>)|\{)"
)
mutable_initializer = re.compile(
    r"public\s+static\s+(?:readonly\s+)?IReadOnly(?:Collection|List|Set)<[^>]+>"
    r"[ \t]+[A-Za-z_][A-Za-z0-9_]*[ \t]*\{[ \t]*get;[ \t]*\}[ \t]*=[ \t]*\["
)

violations = []
for path in source_root.rglob("*.cs"):
    source = path.read_text()
    for pattern in (mutable_array, mutable_initializer):
        for match in pattern.finditer(source):
            line = source.count("\n", 0, match.start()) + 1
            violations.append(f"{path.relative_to(root)}:{line}: {match.group(0)}")

if violations:
    raise SystemExit("Public static mutable collection declarations found:\n" + "\n".join(violations))

agent_source = (source_root / "slskdN.VpnAgent/Program.cs").read_text()
for policy in ("LocalCidrs", "LegacyIngressNamespacePrefixes"):
    wrapped_policy = re.compile(
        rf"IReadOnlyList<string>\s+{policy}\s*\{{\s*get;\s*\}}\s*=\s*Array\.AsReadOnly\("
    )
    if not wrapped_policy.search(agent_source):
        raise SystemExit(f"VPN agent policy {policy} must be exposed as a read-only snapshot")

database_source = (source_root / "slskd/Core/Data/Databases.cs").read_text()
if not re.search(r"public static Database\[\] List => \(Database\[\]\)_databases\.Clone\(\);", database_source):
    raise SystemExit("Database.List must return an isolated array snapshot to preserve its public signature")

print("Public policy collections do not expose mutable backing arrays.")
PY
