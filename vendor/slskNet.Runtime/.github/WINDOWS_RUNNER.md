# On-demand Windows runner

The `Windows Smoke` workflow runs .NET restore/build/test on the private
`snapetech/packer` Windows runner:

```yaml
runs-on: [self-hosted, Windows, X64, packer-windows]
```

The dispatcher boots a disposable Windows VM only while a matching job is
queued, then the ephemeral runner powers down after the job completes.
