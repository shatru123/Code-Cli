#!/usr/bin/env bash
# Code-Cli one-click installer for macOS and Linux.
#
# Usage (run from anywhere — no clone needed):
#   curl -sSL https://raw.githubusercontent.com/shatru123/Code-Cli/feature/local-ai-agent/setup.sh | bash
#
# Or after cloning:
#   chmod +x setup.sh && ./setup.sh
#
# What this does — in order:
#   1. Checks Docker is installed and running
#   2. Downloads the latest code-cli binary from GitHub Releases
#      (or builds from source if no release exists yet)
#   3. Installs to ~/.code-cli/bin and adds to PATH
#   4. Asks which provider you want (Ollama or Claude)
#   5. Launches: code-cli chat --runtime docker
#      which then automatically:
#        a. Pulls the ollama/ollama Docker image
#        b. Creates and starts the container
#        c. Pulls qwen2.5-coder:7b inside the container
#        d. Opens the chat session — ready to use

set -e

# ── Colours ───────────────────────────────────────────────────────────────────

CYAN='\033[0;96m'; GREEN='\033[0;92m'; YELLOW='\033[0;93m'
RED='\033[0;91m';  GRAY='\033[0;90m';  BOLD='\033[1m'; RESET='\033[0m'

header() { echo -e "\n${BOLD}${CYAN}  $1${RESET}"; echo -e "${GRAY}  ────────────────────────────────────────────────────────────────${RESET}"; }
ok()     { echo -e "${GREEN}  [✔] $1${RESET}"; }
step()   { echo -e "${YELLOW}  --> $1${RESET}"; }
warn()   { echo -e "${YELLOW}  [!] $1${RESET}"; }
err()    { echo -e "${RED}  [✖] $1${RESET}"; }
info()   { echo -e "${GRAY}  $1${RESET}"; }

# ── Banner ────────────────────────────────────────────────────────────────────

clear
echo ""
echo -e "${BOLD}${CYAN}  ██████╗ ██████╗ ██████╗ ███████╗███╗   ███╗ █████╗ ████████╗███████╗${RESET}"
echo -e "${BOLD}${CYAN} ██╔════╝██╔═══██╗██╔══██╗██╔════╝████╗ ████║██╔══██╗╚══██╔══╝██╔════╝${RESET}"
echo -e "${BOLD}${CYAN} ██║     ██║   ██║██║  ██║█████╗  ██╔████╔██║███████║   ██║   █████╗  ${RESET}"
echo -e "${BOLD}${CYAN} ██║     ██║   ██║██║  ██║██╔══╝  ██║╚██╔╝██║██╔══██║   ██║   ██╔══╝  ${RESET}"
echo -e "${BOLD}${CYAN} ╚██████╗╚██████╔╝██████╔╝███████╗██║ ╚═╝ ██║██║  ██║   ██║   ███████╗${RESET}"
echo -e "${BOLD}${CYAN}  ╚═════╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝     ╚═╝╚═╝  ╚═╝   ╚═╝   ╚══════╝${RESET}"
echo ""
echo -e "${GRAY}  One-Click Installer  v2.0.0  ·  AI Coding Assistant (Claude + Ollama)${RESET}"
echo ""

# ── Config ────────────────────────────────────────────────────────────────────

INSTALL_DIR="$HOME/.code-cli/bin"
EXE_NAME="code-cli"
EXE_PATH="$INSTALL_DIR/$EXE_NAME"
REPO_OWNER="shatru123"
REPO_NAME="Code-Cli"
DEFAULT_MODEL="qwen2.5-coder:7b"

# Detect OS + arch for release binary selection
OS="$(uname -s)"
ARCH="$(uname -m)"

case "$OS" in
  Darwin)
    [[ "$ARCH" == "arm64" ]] && RID="osx-arm64" || RID="osx-x64"
    ASSET_NAME="code-cli-$RID"
    ;;
  Linux)
    [[ "$ARCH" == "aarch64" ]] && RID="linux-arm64" || RID="linux-x64"
    ASSET_NAME="code-cli-$RID"
    ;;
  *)
    err "Unsupported OS: $OS. Use setup.ps1 on Windows."
    exit 1
    ;;
esac

# ── Step 1: Docker ────────────────────────────────────────────────────────────

header "STEP 1 — Docker"

if ! command -v docker &>/dev/null; then
  err "Docker not found."
  echo ""
  if [[ "$OS" == "Darwin" ]]; then
    info "Install Docker Desktop for Mac from:"
    info "  https://www.docker.com/products/docker-desktop/"
    info ""
    info "Or install via Homebrew:"
    info "  brew install --cask docker"
    read -rp "  Open download page in browser? [Y/n] " choice
    [[ "$choice" != "n" && "$choice" != "N" ]] && open "https://www.docker.com/products/docker-desktop/"
  else
    info "Install Docker Engine:"
    info "  https://docs.docker.com/engine/install/"
    info "  Or: curl -fsSL https://get.docker.com | sh"
  fi
  echo ""
  warn "Install Docker, start it, then re-run this script."
  exit 1
fi

ok "Docker CLI found: $(docker --version)"

if ! docker info &>/dev/null; then
  err "Docker daemon is not running."
  if [[ "$OS" == "Darwin" ]]; then
    info "Start Docker Desktop from your Applications folder."
  else
    info "Run: sudo systemctl start docker"
  fi
  exit 1
fi

ok "Docker daemon is running."

# ── Step 2: Download / build ──────────────────────────────────────────────────

header "STEP 2 — Installing code-cli"

mkdir -p "$INSTALL_DIR"
DOWNLOADED=false

# Try GitHub Releases first
step "Checking for latest GitHub release…"
RELEASE_JSON=$(curl -fsSL \
  "https://api.github.com/repos/$REPO_OWNER/$REPO_NAME/releases/latest" 2>/dev/null || echo "{}")

ASSET_URL=$(echo "$RELEASE_JSON" | \
  python3 -c "import sys,json; r=json.load(sys.stdin); \
    assets=[a for a in r.get('assets',[]) if a['name']=='$ASSET_NAME']; \
    print(assets[0]['browser_download_url'] if assets else '')" 2>/dev/null || echo "")

if [[ -n "$ASSET_URL" ]]; then
  step "Downloading $ASSET_NAME…"
  curl -fsSL -o "$EXE_PATH" "$ASSET_URL"
  chmod +x "$EXE_PATH"
  ok "Downloaded code-cli ($(du -sh "$EXE_PATH" | cut -f1))"
  DOWNLOADED=true
fi

# Build from source if no release binary found
if [[ "$DOWNLOADED" == "false" ]]; then
  warn "No release binary found — building from source."

  if ! command -v dotnet &>/dev/null; then
    err ".NET 8 SDK not found."
    info "Install from: https://dotnet.microsoft.com/download/dotnet/8.0"
    if [[ "$OS" == "Darwin" ]]; then
      info "Or via Homebrew: brew install --cask dotnet-sdk"
    fi
    exit 1
  fi

  ok ".NET SDK: $(dotnet --version)"

  # Determine repo root (where this script lives)
  SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  CSPROJ="$SCRIPT_DIR/Code-Cli.csproj"

  if [[ ! -f "$CSPROJ" ]]; then
    err "Code-Cli.csproj not found at $SCRIPT_DIR"
    info "Clone the repo first:"
    info "  git clone https://github.com/$REPO_OWNER/$REPO_NAME.git && cd $REPO_NAME"
    exit 1
  fi

  step "Building ($RID)…"
  PUBLISH_DIR="$SCRIPT_DIR/publish"
  dotnet publish "$CSPROJ" \
    -c Release -r "$RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o "$PUBLISH_DIR" --nologo -q

  cp "$PUBLISH_DIR/code-cli" "$EXE_PATH"
  chmod +x "$EXE_PATH"
  ok "Built and installed code-cli"
fi

# ── Step 3: PATH ──────────────────────────────────────────────────────────────

header "STEP 3 — PATH"

# Detect active shell config file
SHELL_RC=""
case "$SHELL" in
  */zsh)  SHELL_RC="$HOME/.zshrc" ;;
  */bash) SHELL_RC="${HOME}/.bashrc" ;;
  *)      SHELL_RC="$HOME/.profile" ;;
esac

EXPORT_LINE="export PATH=\"\$PATH:$INSTALL_DIR\""

if [[ ":$PATH:" != *":$INSTALL_DIR:"* ]]; then
  if ! grep -qF "$INSTALL_DIR" "$SHELL_RC" 2>/dev/null; then
    echo "" >> "$SHELL_RC"
    echo "# Code-Cli" >> "$SHELL_RC"
    echo "$EXPORT_LINE" >> "$SHELL_RC"
    ok "Added $INSTALL_DIR to PATH in $SHELL_RC"
  fi
  export PATH="$PATH:$INSTALL_DIR"
  ok "PATH updated for this session."
  warn "Open a new terminal after setup for the PATH to take effect globally."
else
  ok "PATH already contains $INSTALL_DIR"
fi

# ── Step 4: Provider choice ───────────────────────────────────────────────────

header "STEP 4 — Provider"

echo ""
echo -e "  Choose your AI provider:\n"
echo -e "  ${GREEN}[1] Ollama${RESET}  — local, free, 100% offline, runs in Docker"
echo -e "  ${CYAN}[2] Claude${RESET}  — Anthropic API, best quality, requires API key"
echo ""
read -rp "  Enter 1 or 2 [default: 1]: " PROVIDER_CHOICE

if [[ "$PROVIDER_CHOICE" == "2" ]]; then
  echo ""
  read -rp "  Paste your Anthropic API key (sk-ant-...): " API_KEY
  "$EXE_PATH" config --set-key "$API_KEY"
  ok "Claude configured."
  info "Switch back any time with: code-cli config --set-provider ollama"
else
  "$EXE_PATH" config --set-provider ollama
  ok "Provider set to Ollama (Docker runtime)."
fi

# ── Step 5: Launch ────────────────────────────────────────────────────────────

header "STEP 5 — Launch"

echo ""
if [[ "$PROVIDER_CHOICE" == "2" ]]; then
  info "Connecting to Claude and opening chat…"
  echo ""
  "$EXE_PATH" chat --provider claude
else
  echo -e "  ${BOLD}Code-Cli will now:${RESET}"
  info "  · Pull the ollama/ollama Docker image  (if not already cached)"
  info "  · Create and start the container       (automatic)"
  info "  · Pull $DEFAULT_MODEL inside the container (automatic)"
  info "  · Open the chat session               — ready to code"
  echo ""
  warn "First run may take a few minutes. Subsequent launches are instant."
  echo ""
  "$EXE_PATH" chat --runtime docker
fi
