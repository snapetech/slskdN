slskdN connects to the third-party Soulseek peer-to-peer network. Installation
opens one app-selected port for both TCP and UDP in the YunoHost firewall.
Forward the same port through your router if your server is behind NAT and you
want other peers to connect directly.

No local folders are configured for sharing by default. Choose explicitly
which folders to share after installation.

While the package is under catalog review, install the testing branch with:

    sudo yunohost app install https://github.com/snapetech/slskdn_ynh/tree/testing
