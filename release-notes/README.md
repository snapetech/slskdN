# Release-note fragments

Every pull request adds one new fragment under this directory for user-facing
behavior, security, operational, or user-facing documentation changes. The
fragments are append-only: never edit a fragment after it has shipped.

Internal-only work must explicitly select the internal-only option in the pull
request template. The release-note workflow rejects a pull request that has
neither a fragment nor that explicit opt-out, and it rejects selecting both.

Each fragment contains YAML-style frontmatter with exactly these fields:

```yaml
---
category: added|changed|fixed|security|removed|deprecated
audience: users, operators
area: lowercase-area-slug
action: none
breaking: false
---
Describe the user or operator impact in 30-400 characters.
```

`audience` may contain `users`, `operators`, or both exactly once. `area` is a
2-32 character lowercase slug. Use `action: none` when no action is required;
breaking changes must describe the upgrade or operator action.

Preview a range locally with:

```sh
python3 scripts/release_notes.py preview --base <base> --head <head>
```

The preview is grouped by category for the GitHub release and Discord
announcement. Pull-request checks also append capture metadata showing the
fragment, audience, area, action, and breaking status.
