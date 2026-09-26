# Co-op Cloud recipe draft

This directory contains a review-ready Co-op Cloud recipe draft for slskdN.
It uses the public Docker Hub image, Traefik for the web UI, and host-published
TCP/UDP port `50300` for Soulseek traffic.

The recipe is not yet in the canonical `coop-cloud/apps` collection. Submit a
wishlist issue first, then copy this directory into the recipe repository after
the maintainers accept the app. The usual deployment flow is:

```sh
abra app new slskdn
abra app config slskdn.example.com
abra app deploy slskdn.example.com
```

The external `proxy` network and a reachable Docker Swarm host are required.
