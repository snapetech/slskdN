# YunoHost packaging

The package tree is maintained in slskdn_ynh and mirrored in the standalone
Git repository https://github.com/snapetech/slskdn_ynh. YunoHost's catalog
expects an app package to have its own repository. The catalog tracks the
`testing` branch while maintainers review and validate the package; do not
change its catalog entry to `main` until the package is ready for regular use.

From the repository root, publish a package update after committing it here:

    git subtree push --prefix=packaging/yunohost/slskdn_ynh https://github.com/snapetech/slskdn_ynh.git testing

The app package downloads the checksum-pinned, self-contained Linux release
bundles. Its manifest tracks future stable GitHub releases. Review package
changes and run the YunoHost package linter before updating the standalone
repository. Full install, upgrade, backup, restore, and URL-change checks
should run through YunoHost package_check or the YunoHost app CI.
