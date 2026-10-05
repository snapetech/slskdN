#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

python3 - "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

root = Path(sys.argv[1])
controllers = {
    "src/slskd/API/Native/PodsController.cs": ("ct", 13),
    "src/slskd/PodCore/API/Controllers/PodContentController.cs": ("cancellationToken", 4),
    "src/slskd/API/VirtualSoulfind/ShadowIndexController.cs": ("ct", 1),
}

for relative_path, (token, expected) in controllers.items():
    source = (root / relative_path).read_text()
    pattern = re.compile(
        rf"catch \(OperationCanceledException\) when \({token}\.IsCancellationRequested\)"
        rf"\s*\{{\s*throw;\s*\}}\s*catch \(Exception ex\)",
        re.MULTILINE,
    )
    guarded = len(pattern.findall(source))
    broad = source.count("catch (Exception ex)")
    if guarded != expected or broad != expected:
        raise SystemExit(
            f"{relative_path}: expected {expected} guarded broad catches; "
            f"found {guarded} guarded and {broad} broad"
        )

tests = {
    "tests/slskd.Tests.Unit/PodCore/PodsControllerTests.cs": "ListPods_WhenRequestIsCancelled_PropagatesCancellation",
    "tests/slskd.Tests.Unit/PodCore/PodContentControllerTests.cs": "SearchContent_WhenRequestIsCancelled_PropagatesCancellation",
    "tests/slskd.Tests.Unit/API/VirtualSoulfind/ShadowIndexControllerTests.cs": "GetShadowIndex_WhenRequestIsCancelled_PropagatesCancellation",
}
for relative_path, test_name in tests.items():
    if test_name not in (root / relative_path).read_text():
        raise SystemExit(f"missing cancellation regression {test_name} in {relative_path}")

print("Pod, content-linking, and shadow-index actions preserve request cancellation.")
PY
