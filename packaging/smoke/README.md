# Package Smoke Validation

`packaging/smoke/package-smoke` validates post-release package channels by installing from the public channel and writing evidence:

```bash
packaging/smoke/package-smoke slskdn container-ghcr 2026042900-slskdn.202
```

The driver emits `evidence.json`, `junit.xml`, and logs under `artifacts/package-smoke/`. It is intended for internal GitLab post-release validation and for disabled GitHub workflow scaffolding.

Channels currently wired: `github-archive`, `deb`, `rpm`, `container-ghcr`, `container-dockerhub`, `aur`, `aur-bin`, `copr`, `ppa`, `snap`, `chocolatey`, `winget`, `homebrew`, `nix`, `flatpak`, `helm`, `unraid`, `proxmox-lxc`, and `synology-spk`.
