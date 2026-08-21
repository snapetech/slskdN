# Docker Permissions for Download Destinations

This guide explains how to make multiple download destinations writable by
slskdN when the application runs in Docker or Docker Compose.

## The short version

This Compose mount is necessary:

```yaml
- /srv/media/music:/downloads/music:rw
```

It is not sufficient by itself. `:rw` only says that Docker should not mount
the target read-only. The host directory still has to be writable by the
numeric user and group that run slskdN inside the container.

The path in `destinations.folders[].path` is always the **container path**.
The host path belongs only on the left side of the volume mapping:

```yaml
destinations:
  folders:
    - name: Music
      path: /downloads/music
      default: true
    - name: Audiobooks
      path: /downloads/audiobooks
```

```yaml
services:
  slskdn:
    image: ghcr.io/snapetech/slskdn:latest
    environment:
      PUID: "1000"
      PGID: "1000"
    volumes:
      - type: bind
        source: /srv/slskdn/app
        target: /app
        read_only: false
      - type: bind
        source: /srv/media/music
        target: /downloads/music
        read_only: false
      - type: bind
        source: /srv/media/audiobooks
        target: /downloads/audiobooks
        read_only: false
    restart: unless-stopped
```

The `source` paths are on the Docker host. The `target` paths are the paths
that must appear in the slskdN YAML configuration. Short syntax such as
`/srv/media/music:/downloads/music:rw` is equivalent to the long syntax above.

## Choose one container identity

The image supports two ways to select the runtime identity. Do not combine
them.

### Recommended: `PUID` and `PGID`

Set `PUID` and `PGID` to the host UID and GID that should own files created in
the mounted directories:

```yaml
environment:
  PUID: "1000"
  PGID: "1000"
```

The entrypoint uses those values for the `slskdn` process and prepares the
`/app` directory. It does **not** recursively change ownership of arbitrary
download bind mounts, so those mounts must already have suitable host-side
ownership or ACLs.

Find the owner of an existing host directory with:

```bash
stat -c '%u:%g %A %n' \
  /srv/media/music \
  /srv/media/audiobooks
```

If the directories should be owned by UID `1000` and GID `1000`, for example:

```bash
sudo chown -R 1000:1000 \
  /srv/media/music \
  /srv/media/audiobooks
sudo chmod -R u+rwX \
  /srv/media/music \
  /srv/media/audiobooks
```

Replace `1000:1000` with the actual `PUID:PGID` values. If the directories are
shared by several host accounts, use a dedicated group and grant that group
`rwX` access instead of making the directories world-writable.

### Alternative: Docker `user:` or `--user`

You can run the container directly as a numeric identity:

```yaml
user: "1000:1000"
```

When using this mode, remove both `PUID` and `PGID`. The host directories,
including `/app`, must already be readable and writable by that UID/GID because
the entrypoint cannot remap users or repair ownership after Docker starts the
container as a non-root user.

Do not use this combination:

```yaml
environment:
  PUID: "1000"
  PGID: "1000"
user: "1000:1000"
```

The image rejects that configuration. Pick either `PUID`/`PGID` or `user:`.

### No explicit identity

If neither option is set, the image uses its built-in `slskdn` user. That user
has image-specific numeric IDs, so external bind mounts are harder to prepare
reliably. `PUID` and `PGID` are preferable for host directories.

## Configure every writable path

Every path used for downloads must be writable by the runtime identity. This
includes:

- each `destinations.folders[].path`;
- `directories.downloads`, when it is used as the default or fallback; and
- `directories.incomplete`, if it is configured outside the writable `/app`
  volume.

For example, if incomplete files are kept under `/downloads/incomplete`, mount
that host directory too:

```yaml
volumes:
  - /srv/media/incomplete:/downloads/incomplete:rw
```

If the configured path is `/app/incomplete`, it is covered by the writable
`/app` mapping instead. A host path in the YAML does not grant access to a
directory that was mounted at a different container path.

Keep library/share mounts read-only when slskdN only needs to serve them:

```yaml
- /srv/media/library:/music:ro
```

Only application state, incomplete downloads, and final download destinations
need write access.

## Verify the actual container permissions

Changing a Compose file's volume mapping requires container recreation:

```bash
docker compose up -d --force-recreate slskdn
```

Replace `slskdn` with the service name in the Compose file. Then check the
identity and every destination from inside the running container:

```bash
docker compose exec slskdn sh -c '
  id
  for d in /downloads/music /downloads/audiobooks; do
    if test -d "$d" && test -r "$d" && test -w "$d" && test -x "$d"; then
      echo "$d: writable"
    else
      echo "$d: NOT writable"
    fi
  done
'
```

The output from `id` must match the UID/GID used when preparing the host
directories. To test an actual file creation and deletion, use a temporary
marker in the destination:

```bash
docker compose exec slskdn sh -c \
  'touch /downloads/music/.slskdn-write-test && rm /downloads/music/.slskdn-write-test'
```

## Troubleshooting

### `Permission denied`

Compare the `id` output inside the container with the owner from `stat` on the
host. Correct the host owner, group, ACL, or Compose `PUID`/`PGID` values. A
directory also needs execute (`x`) permission so the process can traverse it;
files being overwritten need write permission themselves.

### `Read-only file system` or a failed write test

Check both sides of the mount for read-only settings:

- remove `:ro` and use `:rw` for a destination;
- set `read_only: false` in long Compose syntax; and
- check whether the host filesystem or NAS export itself is mounted read-only.

### `Operation not permitted` while running `chown`

For NFS, `root_squash` and server-side ownership rules can prevent a container
from changing ownership. For CIFS/SMB, the UID/GID and mode are commonly set
when the host mounts the share, for example:

```text
uid=1000,gid=1000,dir_mode=0770,file_mode=0660
```

Fix the ownership or ACL at the NAS/export or host mount layer, then make the
container's `PUID`/`PGID` match it. Do not rely on a recursive `chown` inside
the container for network storage.

### The destination does not exist

Confirm that the YAML path exactly matches the Compose `target` path. After
adding or changing a bind mount, run `docker compose up -d --force-recreate`
so Docker creates the new container with the updated mounts.

Do not solve permission errors with `chmod 777`. Use a matching owner, a
dedicated group, or an explicit ACL so that the writable surface stays limited
to the directories that need it.
