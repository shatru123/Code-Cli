#!/usr/bin/env bash
set -e

echo ""
echo "============================================================"
echo " Code-Cli Build Script (Linux / macOS)"
echo "============================================================"
echo ""

# Detect OS and set runtime identifier
OS="$(uname -s)"
ARCH="$(uname -m)"

if [[ "$OS" == "Darwin" ]]; then
    if [[ "$ARCH" == "arm64" ]]; then
        RID="osx-arm64"
    else
        RID="osx-x64"
    fi
elif [[ "$OS" == "Linux" ]]; then
    if [[ "$ARCH" == "aarch64" ]]; then
        RID="linux-arm64"
    else
        RID="linux-x64"
    fi
else
    echo "[ERROR] Unsupported OS: $OS"
    exit 1
fi

echo " Detected: $OS $ARCH → Runtime: $RID"
echo ""

# Verify dotnet
if ! command -v dotnet &>/dev/null; then
    echo "[ERROR] .NET 8 SDK not found."
    echo "Install from: https://dotnet.microsoft.com/download"
    exit 1
fi

DOTNET_VER=$(dotnet --version)
echo " .NET SDK: $DOTNET_VER"
echo ""

# Restore
echo "[1/3] Restoring packages..."
dotnet restore Code-Cli.csproj --nologo -q
echo "      Done."

# Build
echo "[2/3] Building..."
dotnet build Code-Cli.csproj -c Release --nologo -q
echo "      Done."

# Publish
echo "[3/3] Publishing..."
dotnet publish Code-Cli.csproj \
    -c Release \
    -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o ./publish \
    --nologo -q
echo "      Done."

# Make executable
chmod +x ./publish/code-cli

echo ""
echo "============================================================"
echo " BUILD SUCCESSFUL!"
echo "============================================================"
echo ""
echo " Binary: $(pwd)/publish/code-cli"
echo ""
echo " To install globally:"
echo "   sudo cp ./publish/code-cli /usr/local/bin/code-cli"
echo ""
echo " Or add to PATH in ~/.bashrc / ~/.zshrc:"
echo "   export PATH=\"\$PATH:$(pwd)/publish\""
echo ""
echo " Next:"
echo "   1. Local Ollama:  install from https://ollama.ai"
echo "   2. Docker Ollama: install Docker Desktop / Docker Engine"
echo "   3. Run local:     code-cli chat"
echo "   4. Run Docker:    code-cli chat --runtime docker"
echo ""
