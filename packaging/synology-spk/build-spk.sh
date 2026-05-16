#!/bin/bash
# Build script for slskdN Synology SPK package

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
BUILD_DIR="$SCRIPT_DIR/build"
PACKAGE_DIR="$BUILD_DIR/package"
SPK_FILE="$BUILD_DIR/slskdn.spk"
RUNTIME="${SLSKDN_SPK_RUNTIME:-linux-x64}"
ARCH="${SLSKDN_SPK_ARCH:-x86_64}"
VERSION="${SLSKDN_VERSION:-$(git -C "$REPO_ROOT" describe --tags --abbrev=0 --match '[0-9]*' 2>/dev/null || git -C "$REPO_ROOT" rev-parse --short HEAD)}"

echo "🔨 Building slskdN SPK package..."
echo "Runtime: $RUNTIME"
echo "Synology arch: $ARCH"
echo "Version: $VERSION"

# Clean previous build
if [[ -d "$BUILD_DIR" ]]; then
    echo "🧹 Cleaning previous build..."
    rm -rf "$BUILD_DIR"
fi

# Create build directories
mkdir -p "$PACKAGE_DIR"

if [[ -n "${SLSKDN_SPK_PUBLISH_DIR:-}" ]]; then
    echo "📦 Copying existing publish output from $SLSKDN_SPK_PUBLISH_DIR..."
    if [[ ! -x "$SLSKDN_SPK_PUBLISH_DIR/slskd" ]]; then
        echo "Existing publish directory must contain an executable slskd binary." >&2
        exit 1
    fi

    cp -a "$SLSKDN_SPK_PUBLISH_DIR"/. "$PACKAGE_DIR"/
else
    echo "📦 Publishing slskdN for $RUNTIME..."
    dotnet publish "$REPO_ROOT/src/slskd/slskd.csproj" \
        -c Release \
        -r "$RUNTIME" \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:PublishTrimmed=false \
        -p:Version="$VERSION" \
        -o "$PACKAGE_DIR"
fi

if [[ ! -x "$PACKAGE_DIR/slskd" ]]; then
    echo "SPK package payload is missing executable slskd binary." >&2
    exit 1
fi

# Copy package metadata and scripts
echo "📋 Copying package metadata..."
sed \
    -e "s/^version=.*/version=\"${VERSION}\"/" \
    -e "s/^arch=.*/arch=\"${ARCH}\"/" \
    "$SCRIPT_DIR/INFO" > "$BUILD_DIR/INFO"
cp -r "$SCRIPT_DIR/scripts" "$BUILD_DIR/"
cp -r "$SCRIPT_DIR/conf" "$BUILD_DIR/"
cp -r "$SCRIPT_DIR/ui" "$BUILD_DIR/" 2>/dev/null || true

# Create package.tgz
echo "📦 Creating package archive..."
cd "$PACKAGE_DIR"
tar czf "$BUILD_DIR/package.tgz" *

# Create SPK file
echo "📦 Creating SPK file..."
cd "$BUILD_DIR"
tar cf "$SPK_FILE" INFO package.tgz scripts conf ui 2>/dev/null || tar cf "$SPK_FILE" INFO package.tgz scripts conf

echo "✅ Build complete!"
echo ""
echo "SPK file created: $SPK_FILE"
echo ""
echo "To install on Synology:"
echo "1. Copy $SPK_FILE to your Synology"
echo "2. Package Center → Manual Install → Upload SPK"
echo "3. Follow installation wizard"
echo ""
echo "For production builds:"
echo "1. Set SLSKDN_VERSION to the release version if not building from a release tag"
echo "2. Set SLSKDN_SPK_RUNTIME/SLSKDN_SPK_ARCH for non-x64 NAS targets"
echo "3. Or set SLSKDN_SPK_PUBLISH_DIR to reuse existing dotnet publish output"

