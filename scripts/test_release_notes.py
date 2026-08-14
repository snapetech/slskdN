#!/usr/bin/env python3

from __future__ import annotations

import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import release_notes


VALID_NOTE = """---
category: fixed
audience: users, operators
area: release-pipeline
action: none
breaking: false
---
Release validation now publishes the same curated summary to GitHub and Discord so operators and users see consistent release details.
"""


class ReleaseNotesTests(unittest.TestCase):
    def test_schema_and_formats(self) -> None:
        note = release_notes.parse_release_note("release-notes/pipeline.md", VALID_NOTE)
        self.assertEqual(note["errors"], [])
        self.assertIn("Release validation", note["body"])
        curated = release_notes.format_curated_notes([note])
        self.assertIn("### User-facing changes", curated)
        self.assertIn("#### Fixed", curated)
        self.assertIn("**Release Pipeline:**", curated)
        metadata = release_notes.format_capture_metadata([note])
        self.assertIn("### Capture metadata", metadata)
        self.assertIn("users, operators", metadata)

    def test_invalid_schema_is_rejected(self) -> None:
        note = release_notes.parse_release_note(
            "release-notes/incomplete.md",
            "---\ncategory: changed\n---\nTODO",
        )
        self.assertTrue(note["errors"])
        self.assertIn("placeholder", "\n".join(note["errors"]))

    def test_range_discovery_assembly_and_append_only_guard(self) -> None:
        repository = Path(tempfile.mkdtemp(prefix="slskdn-release-notes-"))
        try:
            self.run_git(repository, "init", "--initial-branch=main", "--quiet")
            self.run_git(repository, "config", "user.name", "Release Notes Test")
            self.run_git(repository, "config", "user.email", "release-notes@example.invalid")
            (repository / "release-notes").mkdir()
            (repository / "release-notes" / "README.md").write_text("# Release notes\n")
            self.run_git(repository, "add", ".")
            self.run_git(repository, "commit", "--quiet", "-m", "chore: initialize")
            base = self.run_git(repository, "rev-parse", "HEAD")
            script = Path(__file__).with_name("release_notes.py")
            opt_out_body = repository / "opt-out.md"
            opt_out_body.write_text("release-note: none\n")
            subprocess.check_call(
                [
                    sys.executable,
                    str(script),
                    "--cwd",
                    str(repository),
                    "check",
                    "--base",
                    base,
                    "--head",
                    base,
                    "--pr-body-file",
                    str(opt_out_body),
                ]
            )
            with self.assertRaises(subprocess.CalledProcessError):
                empty_body = repository / "empty-body.md"
                empty_body.write_text("")
                subprocess.check_call(
                    [
                        sys.executable,
                        str(script),
                        "--cwd",
                        str(repository),
                        "check",
                        "--base",
                        base,
                        "--head",
                        base,
                        "--pr-body-file",
                        str(empty_body),
                    ]
                )

            (repository / "release-notes" / "pipeline.md").write_text(VALID_NOTE)
            self.run_git(repository, "add", ".")
            self.run_git(repository, "commit", "--quiet", "-m", "chore: add release note")
            head = self.run_git(repository, "rev-parse", "HEAD")

            entries = release_notes.changed_release_note_files(base, head, repository)
            self.assertEqual(entries, [{"status": "A", "file": "release-notes/pipeline.md"}])
            notes, errors = release_notes.read_release_notes(entries, head, repository)
            self.assertEqual(errors, [])
            self.assertEqual(len(notes), 1)

            preview = subprocess.check_output(
                [
                    sys.executable,
                    str(script),
                    "--cwd",
                    str(repository),
                    "preview",
                    "--base",
                    base,
                    "--head",
                    head,
                ],
                text=True,
            )
            self.assertIn("### User-facing changes", preview)

            body = "# slskdN 1.0.0\n\n## Highlights\n\n- Existing release detail.\n"
            body_path = repository / "release.md"
            body_path.write_text(body)
            summary_path = repository / "summary.md"
            subprocess.check_call(
                [
                    sys.executable,
                    str(script),
                    "--cwd",
                    str(repository),
                    "check",
                    "--base",
                    base,
                    "--head",
                    head,
                    "--pr-body-file",
                    str(body_path),
                    "--summary-file",
                    str(summary_path),
                ]
            )
            subprocess.check_call(
                [
                    sys.executable,
                    str(script),
                    "--cwd",
                    str(repository),
                    "assemble",
                    "--base",
                    base,
                    "--head",
                    head,
                    "--input",
                    str(body_path),
                    "--output",
                    str(body_path),
                ]
            )
            assembled = body_path.read_text()
            self.assertIn("### User-facing changes", assembled)
            self.assertIn("## Highlights\n\n### User-facing changes", assembled)

            (repository / "release-notes" / "pipeline.md").write_text(
                VALID_NOTE.replace("Release validation", "Changed release validation")
            )
            self.run_git(repository, "add", ".")
            self.run_git(repository, "commit", "--quiet", "-m", "chore: edit release note")
            edited_head = self.run_git(repository, "rev-parse", "HEAD")
            self.assertEqual(
                release_notes.changed_release_note_files(head, edited_head, repository),
                [{"status": "M", "file": "release-notes/pipeline.md"}],
            )
            with self.assertRaises(subprocess.CalledProcessError):
                subprocess.check_call(
                    [
                        sys.executable,
                        str(script),
                        "--cwd",
                        str(repository),
                        "check",
                        "--base",
                        head,
                        "--head",
                        edited_head,
                        "--pr-body-file",
                        str(body_path),
                    ]
                )
        finally:
            shutil.rmtree(repository)

    @staticmethod
    def run_git(repository: Path, *args: str) -> str:
        return subprocess.check_output(["git", *args], cwd=repository, text=True).strip()


if __name__ == "__main__":
    unittest.main()
