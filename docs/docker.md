# Running in Docker

You'll need to [install Docker](https://docs.docker.com/get-docker/) first.

Next, you'll need to make a few choices:

* The HTTP and/or HTTPS ports for the slskd web UI (defaults 5030 and 5031)
* The port for incoming connections from the Soulseek network (default 50300)
* The directory for the slskd application data

For most users, a quick start will be all that is needed:

```shell
docker run -d \
  -p <HTTP port>:5030 \
  -p <HTTPS port>:5031 \
  -p <listen port>:50300 \
  -v <path/to/application/data>:/app \
  --name slskd \
  ghcr.io/snapetech/slskdn:latest
```

The Docker image includes the core runtime tools used by the built-in media and
SongID paths: `ffmpeg`/`ffprobe`, `yt-dlp`, Chromaprint `fpcalc`, and
Microsoft `libmsquic` for .NET QUIC mesh transports on Linux. Larger
experimental recognizers such as Whisper, Demucs, SongRec, Panako, Audfprint,
C2PA tooling, and OCR engines are not bundled by default. They can be installed
after the container is running, or baked into a derived image when you
intentionally enable those workflows.

To populate optional heavyweight media tools in an existing container, run the
installer as root inside the container:

```shell
docker exec -u root slskd install-optional-media-tools distro ai-python
```

The installer supports explicit profiles:

| Profile | Installs |
| --- | --- |
| `distro` | Java 17/21, Python venv support, Tesseract OCR, build tools, Git, Rust/Cargo, curl, and native build libraries used by Cargo-installed recognizers, including SongRec's Linux GUI/audio headers |
| `ai-python` | `openai-whisper` and `demucs` in `/opt/slskdn-media-tools/python`, with `whisper` and `demucs` on `PATH` |
| `c2pa` | `c2patool` via Cargo |
| `songrec` | SongRec via a rustup-managed Cargo toolchain, exposed on `PATH` |
| `audfprint` | `audfprint.py` cloned under `/opt/slskdn-media-tools/audfprint` and exposed on `PATH` |
| `panako` | Installs `panako.jar` to `/usr/local/share/java/panako.jar`; downloads `PANAKO_JAR_URL` when set, otherwise builds JorenSix/Panako from source |
| `all` | Runs every profile |

Examples:

```shell
docker exec -u root slskd install-optional-media-tools distro ai-python c2pa songrec audfprint

docker exec -u root \
  -e PANAKO_JAR_URL=https://example.invalid/panako.jar \
  slskd install-optional-media-tools panako
```

For reproducible deployments, bake the tools into a derived image instead of
mutating a running container:

```dockerfile
FROM ghcr.io/snapetech/slskdn:latest

RUN install-optional-media-tools all
```

For repeated local validation builds, use the bundled all-tools Dockerfile. It
uses BuildKit cache mounts for apt packages, Python wheels, Rust crates/toolchain
downloads, and Gradle artifacts, so the first build is large but later rebuilds
reuse the downloaded optional-tool dependencies:

```shell
DOCKER_BUILDKIT=1 docker build -t slskdn:all-tools \
  -f packaging/docker/Dockerfile.all-tools \
  --build-arg BASE_IMAGE=ghcr.io/snapetech/slskdn:latest \
  .
```

The all-tools image is intentionally large because it includes the complete
optional recognizer stack, including the Python AI packages and their transitive
runtime wheels. Keep substantial free Docker storage available for the final
image, temporary build layers, and cache mounts.

Use `SLSKDN_PIP_PACKAGES` to pin or replace the Python tool set in a derived
image, for example:

```dockerfile
FROM ghcr.io/snapetech/slskdn:latest

ENV SLSKDN_PIP_PACKAGES="openai-whisper==20250625 demucs==4.0.1"
RUN install-optional-media-tools distro ai-python
```

Use `SLSKDN_RUST_TOOLCHAIN` and `SLSKDN_SONGREC_CRATE_VERSION` to pin the
SongRec toolchain and crate version when baking reproducible local-validation
images.

The app does not install these tools automatically when a UI feature is enabled.
That is intentional: installing Python, Java, Rust-built, or externally hosted
recognizers changes the container supply chain and often requires elevated
container permissions. The SongID capabilities API reports which specific tool
is missing and points Docker users at the matching installer profile.

For opt-in experimental media work, build an experimental media image from the
released runtime image:

```shell
docker build -t slskdn:experimental-media \
  -f packaging/docker/Dockerfile.experimental-media \
  --build-arg BASE_IMAGE=ghcr.io/snapetech/slskdn:latest \
  .
```

This variant adds the conservative distro-level prerequisites for OCR and local
recognizer experiments: `tesseract-ocr`, Java, Python, build tools, Git, and
Rust/Cargo. Tools without a stable distro package in the base image, such as
Whisper, Demucs, SongRec, Panako, Audfprint, and C2PA tooling, should still be
installed with `install-optional-media-tools`, installed in a pinned derived
image, or mounted into the container. For local validation where image size is
less important than feature coverage, build
`packaging/docker/Dockerfile.all-tools`. The SongID capabilities API reports
which specific command or file is missing and points Docker users at the
installer.

For an internet-facing or always-on host, prefer a hardened container launch.
This keeps the web UI on loopback for a reverse proxy or SSH tunnel, drops
Linux capabilities, prevents privilege escalation, and makes the container
filesystem read-only except for explicit state and download mounts:

```shell
docker run -d \
  --name slskd \
  --network host \
  --user <uid>:<gid> \
  --read-only \
  --tmpfs /tmp:rw,noexec,nosuid,nodev,size=256m \
  --tmpfs /run:rw,noexec,nosuid,nodev,size=32m,mode=1777 \
  --cap-drop ALL \
  --security-opt no-new-privileges:true \
  --security-opt apparmor=slskdn-docker \
  --pids-limit 512 \
  --memory 4g \
  --memory-swap 4g \
  -e SLSKD_HTTP_ADDRESS=127.0.0.1 \
  -e SLSKD_NO_HTTPS=true \
  -e SLSKD_UMASK=0007 \
  -v <path/to/slskd.yml>:/etc/slskd/slskd.yml:ro \
  -v <path/to/application/data>:/app \
  -v <path/to/downloads>:/downloads \
  -v <path/to/shares>:/shares:ro \
  ghcr.io/snapetech/slskdn:latest \
  ./slskd --config /etc/slskd/slskd.yml --app-dir /app
```

The optional AppArmor profile lives at
`packaging/docker/apparmor/slskdn-docker`. Load it on hosts where AppArmor is
enabled before using `--security-opt apparmor=slskdn-docker`:

```shell
sudo apparmor_parser -r packaging/docker/apparmor/slskdn-docker
```

If AppArmor is disabled on the Docker host, omit the AppArmor security option
and keep the other hardening flags. Docker will reject an unknown or unloaded
profile.

To prove the profile on an AppArmor-enabled Docker host, build or pull the
image you want to test and run:

```shell
SLSKDN_DOCKER_IMAGE=ghcr.io/snapetech/slskdn:latest \
  bash packaging/scripts/run-docker-apparmor-smoke.sh
```

The smoke test loads the profile, starts a local container with the hardened
Docker flags, checks the Web UI and API, verifies capability/seccomp/no-new-
privileges state, verifies writable app/download mounts, and verifies the share
mount rejects writes.

Keep writable mounts as narrow as possible. Shared libraries that slskd only
serves to Soulseek peers should be mounted read-only; only application state,
incomplete downloads, and final download destinations need write access.
Avoid world-writable host media directories; use a dedicated media group and
group-writable permissions instead. The container warns when the app directory
is world-writable. Set `SLSKD_STRICT_APP_DIR_PERMISSIONS=true` to make the
entrypoint set only the app directory itself to `0770`; it does not recursively
change mounted media trees.

This configuration, however, doesn't include any shared directories.

First, you need to map each share to the container as a volume. Then each local directory within the container needs to be added to the configuration. The image starts as root only long enough to prepare the application directory and then drops to the built-in `slskdn` user. You may specify the user and group ID that should run the container and own files created by slskd. Docker accepts numeric values in the `UID:GID` format, such as `1000:1000` in this example.

In the following example, assume that the slskd application directory will be `/var/slskd` on the docker host. Assume that the directories `/home/JohnDoe/Music` and `/home/JohnDoe/eBooks` will be shared. 


For this scenario, the `docker run` command would be:

```shell
docker run -d \
  -p 5030:5030 \
  -p 5031:5031 \
  -p 50300:50300 \
  -e SLSKD_REMOTE_CONFIGURATION=true \
  -v /var/slskd:/app \
  -v /home/JohnDoe/Music:/music \
  -v /home/JohnDoe/eBooks:/ebooks \
  --name slskd \
  --user 1000:1000 \
  ghcr.io/snapetech/slskdn:latest
```

Or, for `docker-compose`:

```yaml
version: "3"
services:
  slskd:
    environment:
      - SLSKD_REMOTE_CONFIGURATION=true
    ports:
      - 5030:5030/tcp
      - 5031:5031/tcp
      - 50300:50300/tcp
    volumes:
      - /var/slskd:/app:rw
      - /home/JohnDoe/Music:/music:rw
      - /home/JohnDoe/eBooks:/ebooks:rw
    user: 1000:1000
    image: ghcr.io/snapetech/slskdn:latest
```
The YAML configuration file would contain:

```yaml
shares:
  directories:
    - /music
    - /ebooks
```

You can achieve the same configuration by setting the `SLSKD_SHARED_DIR` environment variable in the `docker run` command:

```shell
docker run -d \
  -p 5030:5030 \
  -p 5031:5031 \
  -p 50300:50300 \
  -e SLSKD_REMOTE_CONFIGURATION=true \
  -v /var/slskd:/app \
  -v /home/JohnDoe/Music:/music \
  -v /home/JohnDoe/eBooks:/ebooks \
  -e "SLSKD_SHARED_DIR=/music;/ebooks" \
  --name slskd \
  --user 1000:1000 \
  ghcr.io/snapetech/slskdn:latest
```

Or, for `docker-compose`:

```yaml
version: "3"
services:
  slskd:
    environment:
      - SLSKD_REMOTE_CONFIGURATION=true
      - "SLSKD_SHARED_DIR=/music;/ebooks"
    ports:
      - 5030:5030/tcp
      - 5031:5031/tcp
      - 50300:50300/tcp
    volumes:
      - /var/slskd:/app:rw
      - /home/JohnDoe/Music:/music:rw
      - /home/JohnDoe/eBooks:/ebooks:rw
    user: 1000:1000
    image: ghcr.io/snapetech/slskdn:latest
```
