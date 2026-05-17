# On-demand Windows runner

The `Windows Smoke` workflow runs on the private `snapetech/packer` disposable
Windows VM runner:

```yaml
runs-on: [self-hosted, Windows, X64, packer-windows]
```

The existing Windows release/publishing workflows can stay on their current
hosted or protected runner paths. This smoke workflow is for PR-time Windows
restore/build/test coverage and shuts the VM down after one ephemeral job.
