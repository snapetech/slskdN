# StartOS submission packet

The compiled StartOS wrapper is published at
https://github.com/snapetech/slskdn-startos.

- Package ID: `slskdn`
- Image: `docker.io/snapetech/slskdn:2026092517-slskdn.325`
- Web UI: TCP `5030`
- Soulseek/mesh: TCP and UDP `50300`
- Persistent data: `/app`, `/downloads`, `/music`

The wrapper passes the current Start9 SDK typecheck and `ncc` build. Start9's
marketplace submission is a human-gated review: send the wrapper repository URL
and this source repository to `submissions@start9labs.com`, including the
license and runtime-port details above.
