# slskdn Unraid Template

This template is the Community Applications package metadata for slskdN.

## Community Applications

The repository-level profile required by the current Community Applications submission portal is at [`ca_profile.xml`](../../ca_profile.xml). Submit the public GitHub repository through [Community Applications](https://ca.unraid.net/submit/new) after reviewing the [submission help](https://ca.unraid.net/submit/help).

Until the listing is approved, use the [raw template XML](https://raw.githubusercontent.com/snapetech/slskdn/main/packaging/unraid/slskdn.xml) as a local user template. Copy it to `/boot/config/plugins/dockerMan/templates-user/slskdn.xml` on the Unraid flash, then open **Docker → Add Container → User Templates** and select `slskdn`.

The old **Settings → Docker → Template Repositories** instructions are obsolete on current Unraid releases.

## Default Paths

| Setting | Container Path | Default / Recommendation |
|---------|---------------|-------------------|
| App Data | `/app` | `/mnt/user/appdata/slskdn` |
| Downloads | `/downloads` | Choose a share and directory |
| Music Library | `/music` | Optional; choose a read-only share and directory |

## Default Ports

| Port | Purpose |
|------|---------|
| 5030 | Web UI (HTTP) |
| 5031 | Web UI (HTTPS) |
| 50300 | Soulseek incoming connections |

**Important:** Port 50300 must be forwarded in your router for optimal connectivity.

## First Run

1. Access the web UI at `http://YOUR_UNRAID_IP:5030`
2. Default login: `slskd` / `slskd`
3. Go to **System** → Configure your Soulseek username/password
4. Configure your shared folders under **System** → **Shares**

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `PUID` | User ID for file permissions | 1000 |
| `PGID` | Group ID for file permissions | 1000 |
| `TZ` | Timezone | America/Chicago |
| `SLSKD_SLSK_USERNAME` | Soulseek username | (set in UI) |
| `SLSKD_SLSK_PASSWORD` | Soulseek password | (set in UI) |

## Support

- **Issues:** https://github.com/snapetech/slskdn/issues
- **Documentation:** https://github.com/snapetech/slskdn
- **Unraid submission help:** https://ca.unraid.net/submit/help



