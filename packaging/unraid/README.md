# slskdn Unraid Template

This template is the Community Applications package metadata for slskdN.

## Community Applications

The repository-level profile required by the current Community Applications submission portal is at [`ca_profile.xml`](../../ca_profile.xml). Submit the public GitHub repository through [Community Applications](https://ca.unraid.net/submit/new) after reviewing the [submission help](https://ca.unraid.net/submit/help).

Until the listing is approved, use the [template XML](https://github.com/snapetech/slskdN/raw/refs/heads/main/packaging/unraid/slskdn.xml) as a local user template. Copy it to `/boot/config/plugins/dockerMan/templates-user/slskdn.xml` on the Unraid flash, then open **Docker → Add Container → User Templates** and select `slskdn`.

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
| 50305/tcp | slskdN mesh overlay |
| 50305/udp | Public DHT rendezvous and shared UDP/QUIC mesh traffic |
| 50401/udp | Optional QUIC data overlay |

Port 50300/tcp should be forwarded in your router for optimal Soulseek
connectivity. Ports 50305/tcp and 50305/udp are used when DHT/mesh services are
enabled. Port 50401/udp is only needed when the optional `overlay_data` QUIC
data plane is enabled. The loopback-only QUIC backend port 55305 must not be
published.

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
| `SLSKD_USERNAME` | Web UI username | slskd; change it |
| `SLSKD_PASSWORD` | Web UI password | slskd; change it |
| `SLSKD_SLSK_USERNAME` | Soulseek username | (set in UI) |
| `SLSKD_SLSK_PASSWORD` | Soulseek password | (set in UI) |

The template also exposes the documented optional `SLSKD_*` environment
overrides as advanced fields with blank values. Leave them blank to use the
image and YAML defaults. This includes less-common settings for downloads,
shares, search, Soulseek behavior, obfuscation, proxies, authentication,
metrics, notifications, logging, and integrations. The complete variable
reference is in [`docs/config.md`](../../docs/config.md). Runtime-only
`SLSKD_SCRIPT_DATA` and image metadata variables such as `SLSKD_DOCKER_TAG` are
not user configuration fields.

Image-managed Docker runtime settings are also visible in the advanced section
with their safe image defaults. Do not combine `PUID`/`PGID` with a Docker
`--user` override.

## Template usability feedback

Community feedback requested that Docker environment variables be exposed in
the template and left blank when optional, so users do not have to hunt through
documentation for settings that were omitted from the template. The current
template follows that approach while keeping required values and image runtime
defaults safe.

## Support

- **Issues:** https://github.com/snapetech/slskdN/issues
- **Documentation:** https://github.com/snapetech/slskdN
- **Unraid support forum:** https://forums.unraid.net/forum/71-docker-containers/
- **Unraid submission help:** https://ca.unraid.net/submit/help
- **Copy-ready GitHub Markdown support post:** [`SUPPORT_POST.md`](SUPPORT_POST.md)
- **Copy-ready Unraid forum text:** [`SUPPORT_POST_FORUM.txt`](SUPPORT_POST_FORUM.txt)
