The web interface is available at https://__DOMAIN____PATH__.

The app has its own login in addition to YunoHost's access permission. The
username is slskd. Retrieve the generated password with:

    sudo yunohost app setting slskdn web_password

If you installed an additional instance, replace slskdn with that instance's
app ID. After logging in, configure your Soulseek account and choose any
directories you want to share.

The incoming port is available with:

    sudo yunohost app setting slskdn port_soulseek

Forward that port for both TCP and UDP in your router if you are behind NAT.
