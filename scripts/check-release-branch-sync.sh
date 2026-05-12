#!/usr/bin/env bash

set -euo pipefail

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    echo "Not inside a git worktree; skipping release branch sync check."
    exit 0
fi

branch="$(git symbolic-ref --quiet --short HEAD || true)"
if [[ -z "$branch" ]]; then
    echo "Detached HEAD; skipping release branch sync check."
    exit 0
fi

upstream="$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null || true)"
if [[ -z "$upstream" ]]; then
    echo "Branch ${branch} has no upstream; skipping release branch sync check."
    exit 0
fi

git fetch --quiet --prune "${upstream%%/*}"

local_head="$(git rev-parse HEAD)"
upstream_head="$(git rev-parse '@{u}')"
merge_base="$(git merge-base HEAD '@{u}')"

if [[ "$local_head" == "$upstream_head" ]]; then
    echo "Release branch sync check passed: ${branch} matches ${upstream}."
    exit 0
fi

if [[ "$merge_base" == "$upstream_head" ]]; then
    echo "Release branch sync check failed: ${branch} is ahead of ${upstream}." >&2
    echo "Push the branch before cutting a release tag, or intentionally tag the exact pushed commit." >&2
    exit 1
fi

if [[ "$merge_base" == "$local_head" ]]; then
    echo "Release branch sync check failed: ${branch} is behind ${upstream}." >&2
    echo "Pull/rebase before cutting a release tag." >&2
    exit 1
fi

echo "Release branch sync check failed: ${branch} and ${upstream} have diverged." >&2
echo "Resolve the divergence before cutting a release tag." >&2
exit 1
