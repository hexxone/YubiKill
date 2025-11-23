#!/bin/bash

# Configuration
APP_NAME="YubiKill"
PUBLISH_OUTPUT_DIR="bin/Release/net9.0/osx-arm64/publish"
APP_BUNDLE_DIR="$APP_NAME.app"
CONTENTS_DIR="$APP_BUNDLE_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"

echo "📦 Publishing AOT..."
# Publish for macOS ARM64 (Apple Silicon). Change to osx-x64 for Intel.
dotnet publish -c Release -r osx-arm64

if [ $? -ne 0 ]; then
    echo "❌ Publish failed!"
    exit 1
fi

echo "📁 Creating .app structure..."
rm -rf "$APP_BUNDLE_DIR"
mkdir -p "$MACOS_DIR"
mkdir -p "$RESOURCES_DIR"

echo "🚀 Setting up Smart Wrapper..."

# 1. Copy all binaries but rename the main one
cp -R "$PUBLISH_OUTPUT_DIR/" "$MACOS_DIR/"
mv "$MACOS_DIR/$APP_NAME" "$MACOS_DIR/$APP_NAME-app"

# 2. Create the Smart Wrapper Script
cat > "$MACOS_DIR/$APP_NAME" <<EOF
#!/bin/bash
DIR=\$(cd "\$(dirname "\$0")"; pwd)
BINARY="\$DIR/$APP_NAME-app"
# Ensure this matches the path in your C# code (UserProfile/.yubikill_config.json)
CONFIG_FILE="\$HOME/.yubikill_config.json"

if [ ! -f "\$CONFIG_FILE" ]; then
    # --- CONFIG MODE ---
    # Config missing? Launch Terminal to run the setup interactively
    open -a Terminal "\$BINARY" --args --configure
    exit 0
else
    # --- MONITOR MODE ---
    # Config exists? Run silently in background
    exec "\$BINARY" --hidden > /tmp/yubikill.log 2>&1
fi
EOF

# Make executable
chmod +x "$MACOS_DIR/$APP_NAME"
chmod +x "$MACOS_DIR/$APP_NAME-app"

echo "📝 Creating Info.plist..."
cat > "$CONTENTS_DIR/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleIdentifier</key>
    <string>one.hexx.$APP_NAME</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>$APP_NAME</string>
    <key>CFBundleIconFile</key>
    <string>AppIcon</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <!-- This HIDES the app from the Dock entirely (Background App) -->
    <key>LSUIElement</key>
    <true/>
</dict>
</plist>
EOF

echo "🧹 Cleaning up artifacts..."
rm -f "$MACOS_DIR"/*.pdb
rm -f "$MACOS_DIR"/*.xml
rm -rf "$MACOS_DIR"/*.dSYM
rm -f "$MACOS_DIR"/*.dbg

echo "✅ Done! Application packaged at: $APP_BUNDLE_DIR"