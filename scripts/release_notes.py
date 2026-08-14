#!/usr/bin/env python3
"""Validate, preview, and assemble structured release-note fragments."""

from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Any


RELEASE_NOTE_DIRECTORY = "release-notes"
CATEGORIES = {
    "added": "Added",
    "changed": "Changed",
    "fixed": "Fixed",
    "security": "Security",
    "removed": "Removed",
    "deprecated": "Deprecated",
}
AUDIENCES = {"users", "operators"}
FRONTMATTER_KEYS = {"category", "audience", "area", "action", "breaking"}


def is_release_note_path(file_name: str) -> bool:
    path = Path(file_name)
    return (
        path.parent == Path(RELEASE_NOTE_DIRECTORY)
        and path.suffix == ".md"
        and path.name.lower() != "readme.md"
    )


def parse_release_note(file_name: str, content: str) -> dict[str, Any]:
    errors: list[str] = []
    normalized = content.replace("\r\n", "\n").replace("\r", "\n")
    match = re.fullmatch(r"---\n([\s\S]*?)\n---\n?([\s\S]*)", normalized)
    if not match:
        return {
            "file": file_name,
            "errors": ["must contain YAML frontmatter delimited by `---`"],
        }

    metadata: dict[str, str] = {}
    for line in match.group(1).split("\n"):
        if not line.strip():
            continue
        line_match = re.fullmatch(r"([a-z][a-z-]*):\s*(\S.*)", line)
        if not line_match:
            errors.append(f"has invalid frontmatter: {line}")
            continue
        key, value = line_match.groups()
        if key in metadata:
            errors.append(f'declares frontmatter key "{key}" more than once')
        metadata[key] = value.strip()

    for key in metadata:
        if key not in FRONTMATTER_KEYS:
            errors.append(f'frontmatter key "{key}" is not supported')

    category = metadata.get("category", "").lower()
    if category not in CATEGORIES:
        errors.append(
            "category must be one of: " + ", ".join(CATEGORIES.keys())
        )

    audience_parts = [
        part.strip().lower()
        for part in metadata.get("audience", "").split(",")
        if part.strip()
    ]
    invalid_audiences = [
        audience for audience in audience_parts if audience not in AUDIENCES
    ]
    if (
        not audience_parts
        or invalid_audiences
        or len(set(audience_parts)) != len(audience_parts)
    ):
        errors.append("audience must list users, operators, or both exactly once")

    area = metadata.get("area", "").lower()
    if not re.fullmatch(r"[a-z][a-z0-9-]{1,31}", area):
        errors.append(
            "area must be a 2-32 character lowercase slug such as `search` or `release-pipeline`"
        )

    action = re.sub(r"\s+", " ", metadata.get("action", "").strip())
    if not action:
        errors.append(
            "action is required; use `none` when no user or operator action is needed"
        )
    elif action.lower() != "none":
        if len(action) < 5 or len(action) > 200:
            errors.append("action must be 5-200 characters or exactly `none`")
        if re.search(r"<!--|-->|\b(?:todo|tbd|fill in)\b", action, re.I):
            errors.append("action contains a placeholder or HTML comment")

    breaking = metadata.get("breaking", "").lower()
    if breaking not in {"true", "false"}:
        errors.append("breaking must be either `true` or `false`")
    if breaking == "true" and action.lower() == "none":
        errors.append("breaking changes must describe an upgrade or operator action")

    body = re.sub(r"\s+", " ", match.group(2).strip())
    if len(body) < 30 or len(body) > 400:
        errors.append("body must be 30-400 characters and describe the user impact")
    if re.search(r"<!--|-->|\b(?:todo|tbd|fill in)\b", body, re.I):
        errors.append("body contains a placeholder or HTML comment")
    if body and not re.match(r"[A-Z0-9`*_]", body):
        errors.append("body must start with a capitalized sentence")
    if body and not re.search(r"[.!?)]$", body):
        errors.append("body must end with sentence punctuation")

    return {
        "file": file_name,
        "category": category,
        "category_title": CATEGORIES.get(category, ""),
        "audience": audience_parts,
        "area": area,
        "action": "none" if action.lower() == "none" else action,
        "breaking": breaking == "true",
        "body": body,
        "errors": errors,
    }


def git_output(*args: str, cwd: Path) -> str:
    return subprocess.check_output(["git", *args], cwd=cwd, text=True)


def changed_release_note_files(
    base: str, head: str, cwd: Path | str = "."
) -> list[dict[str, str]]:
    root = Path(cwd)
    output = git_output(
        "diff",
        "--name-status",
        "--find-renames",
        base,
        head,
        "--",
        RELEASE_NOTE_DIRECTORY,
        cwd=root,
    )
    entries: list[dict[str, str]] = []
    for line in output.splitlines():
        columns = line.split("\t")
        if len(columns) < 2:
            continue
        status_code = columns[0]
        file_name = columns[2] if status_code.startswith("R") and len(columns) > 2 else columns[1]
        if is_release_note_path(file_name):
            entries.append({"status": status_code[0], "file": file_name})
    return entries


def read_at_ref(ref: str, file_name: str, cwd: Path) -> str:
    if ref == "WORKTREE":
        return (cwd / file_name).read_text(encoding="utf-8")
    return git_output("show", f"{ref}:{file_name}", cwd=cwd)


def read_release_notes(
    entries: list[dict[str, str]], ref: str, cwd: Path | str = "."
) -> tuple[list[dict[str, Any]], list[str]]:
    root = Path(cwd)
    notes: list[dict[str, Any]] = []
    errors: list[str] = []
    for entry in entries:
        try:
            content = read_at_ref(ref, entry["file"], root)
        except (OSError, subprocess.CalledProcessError) as error:
            errors.append(f"{entry['file']}: unable to read file ({error})")
            continue
        note = parse_release_note(entry["file"], content)
        if note["errors"]:
            errors.extend(
                f"{entry['file']}: {error}" for error in note["errors"]
            )
        else:
            notes.append(note)
    return notes, errors


def area_title(area: str) -> str:
    return " ".join(part.capitalize() for part in area.split("-"))


def format_curated_notes(notes: list[dict[str, Any]]) -> str:
    if not notes:
        return ""
    lines = ["### User-facing changes", ""]
    for category, title in CATEGORIES.items():
        category_notes = [note for note in notes if note["category"] == category]
        if not category_notes:
            continue
        lines.extend([f"#### {title}", ""])
        for note in category_notes:
            breaking_prefix = "**Breaking:** " if note["breaking"] else ""
            lines.append(
                f"- **{area_title(note['area'])}:** {breaking_prefix}{note['body']}"
            )
            if note["action"] != "none":
                lines.append(f"  - **Action required:** {note['action']}")
        lines.append("")
    return "\n".join(lines).strip()


def format_capture_metadata(notes: list[dict[str, Any]]) -> str:
    if not notes:
        return ""

    def escape_cell(value: object) -> str:
        return str(value).replace("|", r"\|")

    lines = [
        "### Capture metadata",
        "",
        "| Fragment | Audience | Area | Action | Breaking |",
        "| --- | --- | --- | --- | --- |",
    ]
    for note in notes:
        lines.append(
            "| "
            + " | ".join(
                [
                    escape_cell(note["file"]),
                    escape_cell(", ".join(note["audience"])),
                    escape_cell(note["area"]),
                    escape_cell(note["action"]),
                    "yes" if note["breaking"] else "no",
                ]
            )
            + " |"
        )
    return "\n".join(lines)


def inject_curated_notes(changelog: str, curated_notes: str) -> str:
    if not curated_notes:
        return changelog
    heading = re.search(r"^## .+$", changelog, re.M)
    if heading is None:
        return f"{curated_notes}\n\n{changelog.strip()}\n"
    tail = "\n" + changelog[heading.end() :].lstrip("\n")
    return (
        changelog[: heading.end()]
        + "\n\n"
        + curated_notes
        + "\n"
        + tail
    )


def has_explicit_no_release_note(body: str) -> bool:
    return bool(
        re.search(r"release-note\s*:\s*none", body, re.I)
        or re.search(
            r"-\s*\[x\][^\n]*(?:internal-only|no user-facing release note)",
            body,
            re.I,
        )
    )


def issue_exit(issues: list[str]) -> int:
    if not issues:
        return 0
    print("Release-note validation failed:", file=sys.stderr)
    for issue in issues:
        print(f"- {issue}", file=sys.stderr)
    return 1


def common_entries(base: str, head: str, cwd: Path) -> tuple[list[dict[str, str]], list[dict[str, Any]], list[str]]:
    entries = changed_release_note_files(base, head, cwd)
    added = [entry for entry in entries if entry["status"] == "A"]
    notes, errors = read_release_notes(added, head, cwd)
    return entries, notes, errors


def command_check(args: argparse.Namespace) -> int:
    root = Path(args.cwd).resolve()
    entries, notes, errors = common_entries(args.base, args.head, root)
    body = (
        Path(args.pr_body_file).read_text(encoding="utf-8")
        if args.pr_body_file
        else os.environ.get(args.pr_body_env or "", "")
    )
    explicit_no_release_note = has_explicit_no_release_note(body)
    issues = list(errors)
    issues.extend(
        f"{entry['file']}: release-note fragments are append-only; add a new fragment instead of modifying an old one"
        for entry in entries
        if entry["status"] != "A"
    )
    if not notes and not explicit_no_release_note:
        issues.append(
            "add a validated file under release-notes/ or explicitly mark the PR `release-note: none` for internal-only work"
        )
    if notes and explicit_no_release_note:
        issues.append(
            "choose either a release-note fragment or `release-note: none`; do not select both"
        )

    if args.summary_file:
        summary = (
            "## Release-note preview\n\n"
            + format_curated_notes(notes)
            + "\n\n"
            + format_capture_metadata(notes)
            if notes
            else "## Release-note preview\n\nInternal-only change; no release note will be published."
        )
        with Path(args.summary_file).open("a", encoding="utf-8") as output:
            output.write(summary + "\n")

    result = issue_exit(issues)
    if result:
        return result
    if notes:
        print(f"Validated {len(notes)} user-facing release-note fragment(s).")
    else:
        print("Explicitly marked as internal-only; no release note required.")
    return 0


def command_preview(args: argparse.Namespace) -> int:
    root = Path(args.cwd).resolve()
    entries, notes, errors = common_entries(args.base, args.head, root)
    issues = list(errors)
    issues.extend(
        f"{entry['file']}: release-note fragments are append-only; add a new fragment instead"
        for entry in entries
        if entry["status"] != "A"
    )
    result = issue_exit(issues)
    if result:
        return result
    if not notes:
        print("No new release-note fragments were found in this range.")
        return 0
    print(format_curated_notes(notes))
    return 0


def command_assemble(args: argparse.Namespace) -> int:
    root = Path(args.cwd).resolve()
    entries, notes, errors = common_entries(args.base, args.head, root)
    issues = list(errors)
    issues.extend(
        f"{entry['file']}: release-note fragments are append-only; add a new fragment instead of modifying an old one"
        for entry in entries
        if entry["status"] != "A"
    )
    result = issue_exit(issues)
    if result:
        return result
    input_path = Path(args.input)
    output_path = Path(args.output)
    content = input_path.read_text(encoding="utf-8")
    if notes:
        content = inject_curated_notes(content, format_curated_notes(notes))
        print(f"Added {len(notes)} curated release-note fragment(s) to {output_path}.")
    else:
        print(f"No new curated release-note fragments found; retained {output_path}.")
    output_path.write_text(content, encoding="utf-8")
    return 0


def parser() -> argparse.ArgumentParser:
    command_parser = argparse.ArgumentParser(
        description="Validate, preview, and assemble release-note fragments."
    )
    command_parser.add_argument("--cwd", default=".")
    subparsers = command_parser.add_subparsers(dest="command", required=True)

    def add_range_arguments(subparser: argparse.ArgumentParser) -> None:
        subparser.add_argument("--base", required=True)
        subparser.add_argument("--head", required=True)

    check_parser = subparsers.add_parser("check")
    add_range_arguments(check_parser)
    body_group = check_parser.add_mutually_exclusive_group(required=True)
    body_group.add_argument("--pr-body-file")
    body_group.add_argument("--pr-body-env")
    check_parser.add_argument("--summary-file")

    preview_parser = subparsers.add_parser("preview")
    add_range_arguments(preview_parser)

    assemble_parser = subparsers.add_parser("assemble")
    add_range_arguments(assemble_parser)
    assemble_parser.add_argument("--input", required=True)
    assemble_parser.add_argument("--output", required=True)

    return command_parser


def main() -> int:
    args = parser().parse_args()
    if args.command == "check":
        return command_check(args)
    if args.command == "preview":
        return command_preview(args)
    if args.command == "assemble":
        return command_assemble(args)
    raise AssertionError(f"unknown command: {args.command}")


if __name__ == "__main__":
    raise SystemExit(main())
