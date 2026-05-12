#!/usr/bin/env bash

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

usage() {
    cat >&2 <<'USAGE'
Usage:
  scripts/create-release-tag.sh <build-main-*|build-dev-*>

Runs the full local release gate against the exact pushed branch head, then
creates and pushes the release tag. This is the only supported local path for
starting a tag-triggered release.
USAGE
}

fail() {
    echo "ERROR: $1" >&2
    exit 1
}

tag="${1:-}"
if [[ -z "$tag" || "${tag:-}" == "-h" || "${tag:-}" == "--help" ]]; then
    usage
    exit 1
fi

if [[ "$tag" =~ [[:space:]] ]]; then
    fail "Release tag must not contain whitespace."
fi

if [[ ! "$tag" =~ ^build-(main|dev)-[A-Za-z0-9][A-Za-z0-9._-]*$ ]]; then
    fail "Release tag must start with build-main- or build-dev- and contain only letters, numbers, dots, underscores, and hyphens."
fi

if [[ "$tag" =~ ^build-main- ]]; then
    version="${tag#build-main-}"
    if [[ ! "$version" =~ ^([0-9]+\.[0-9]+\.[0-9]+|[0-9]{10}-slskdn\.[0-9]+)$ ]]; then
        fail "Main release tags must use MAJOR.MINOR.PATCH or YYYYMMDDHH-slskdn.N after build-main-."
    fi
fi

if [[ "$tag" =~ ^build-dev- ]]; then
    version="${tag#build-dev-}"
    if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.dev\.[0-9]{8}\.[0-9]{6}$ ]]; then
        fail "Dev release tags must use MAJOR.MINOR.PATCH.dev.YYYYMMDD.HHMMSS after build-dev-."
    fi
fi

git rev-parse --is-inside-work-tree >/dev/null 2>&1 || fail "Not inside a git worktree."

branch="$(git symbolic-ref --quiet --short HEAD || true)"
if [[ -z "$branch" ]]; then
    fail "Detached HEAD releases are not supported by this helper."
fi

if [[ -n "$(git status --porcelain)" ]]; then
    git status --short >&2
    fail "Working tree must be clean before creating a release tag."
fi

if git rev-parse -q --verify "refs/tags/$tag" >/dev/null; then
    fail "Local tag already exists: $tag"
fi

if git ls-remote --exit-code --tags origin "refs/tags/$tag" >/dev/null 2>&1; then
    fail "Remote tag already exists on origin: $tag"
fi

echo "==> Verify GitHub target"
./scripts/verify-github-target.sh

echo
echo "==> Verify release branch sync"
bash scripts/check-release-branch-sync.sh

echo
echo "==> Run release gate"
bash packaging/scripts/run-release-gate.sh

echo
echo "==> Create and push release tag"
git tag "$tag"
git push origin "$tag"

cat <<MSG

Release tag pushed: $tag

Watch the tag build:
  gh run list --repo snapetech/slskdN --workflow build-on-tag.yml --limit 5

After the GitHub release is published, verify artifacts:
  scripts/verify-release-artifacts.sh "$tag"
MSG
