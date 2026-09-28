FROM docker.io/snapetech/slskdn:2026092820-slskdn.328

ENV SLSKD_APP_DIR=/app/data \
    SLSKD_DOWNLOADS_DIR=/app/data/downloads \
    SLSKD_SHARED_DIR=/app/data/music

CMD ["/bin/sh", "-ec", "mkdir -p \"$SLSKD_DOWNLOADS_DIR\" \"$SLSKD_SHARED_DIR\"; exec ./slskd"]
