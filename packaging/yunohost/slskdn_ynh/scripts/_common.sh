patch_lttng_soname() {
    local tracept_provider
    tracept_provider="$(find "$install_dir" -name libcoreclrtraceptprovider.so -print -quit)"

    if [ -n "$tracept_provider" ] &&
        patchelf --print-needed "$tracept_provider" | grep -Fqx "liblttng-ust.so.0"; then
        patchelf --replace-needed liblttng-ust.so.0 liblttng-ust.so.1 "$tracept_provider"
    fi
}
