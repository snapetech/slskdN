# Self-hosting catalog deployment

This directory is the source of truth for the Docker-based self-hosting
catalog submissions for slskdN. The catalog adapters intentionally use the
public Docker Hub image because it is pullable by new installations without a
registry login.

## Current stable image

```text
docker.io/snapetech/slskdn:2026092517-slskdn.325
```

The image supports `linux/amd64` and `linux/arm64`. The web UI is available on
TCP port `5030`. TCP port `50300` carries the Soulseek listener and mesh TCP
traffic; UDP port `50300` is optional and is only needed when DHT, mesh
rendezvous, or QUIC UDP features are enabled.

For Compose-based catalogs, persist `/app`, mount the download destination at
`/downloads`, and mount the shared music library at `/music` (read-only is
recommended). The Cloudron adapter instead keeps all three paths under its
`/app/data` persistent mount. Set
`SLSKD_SLSK_USERNAME` and `SLSKD_SLSK_PASSWORD` only when credentials should
be supplied through the platform instead of the web UI.

The platform-specific submissions prepared from this contract are CasaOS /
ZimaOS, Umbrel, TrueNAS, Cosmos, CapRover, Portainer, Co-op Cloud, Cloudron,
and StartOS. Unraid and YunoHost are maintained separately.
