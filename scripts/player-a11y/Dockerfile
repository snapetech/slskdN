FROM debian:trixie-slim

RUN apt-get update \
  && DEBIAN_FRONTEND=noninteractive apt-get install --yes --no-install-recommends \
    at-spi2-core \
    dbus-x11 \
    espeak-ng \
    orca \
    pulseaudio-utils \
    speech-dispatcher \
    speech-dispatcher-espeak-ng \
    x11-xkb-utils \
    && rm -rf /var/lib/apt/lists/*

CMD ["sleep", "infinity"]
