<#
.SYNOPSIS
    Code-Cli one-click installer for Windows.
    Downloads the latest release, installs to PATH, and launches automatically.

.DESCRIPTION
    Run this from PowerShell (no admin required):
        iwr -useb https://raw.githubusercontent.com/shatru123/Code-Cli/feature/local-ai-agent/setup.ps1 | iex

    Or double-click setup.ps1 after cloning the repo.

    What this script does — in order:
      1. Checks / installs Docker Desktop (prompts if missing)
      2. Downloads the latest code-cli.exe from GitHub Releases
         (or builds from source if no release exists yet)
      3. Installs to %USERPROFILE%\.code-cli\bin and adds to PATH
      4. Launches: code-cli chat --runtime docker
         which then automatically:
           a. Pulls the ollama/ollama Docker image
           b. Creates and starts the container
           c. Pulls qwen2.5-coder:7b inside the container
           d. Opens the chat session — ready to use
#>

#Requires -Version 5.1
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ── Colours ───────────────────────────────────────────────────────────────────

function Write-Header  { param($t) Write-Host "`n  $t" -ForegroundColor Cyan }
function Write-Step    { param($t) Write-Host "  --> $t" -ForegroundColor Yellow }
function Write-OK      { param($t) Write-Host "  [OK] $t" -ForegroundColor Green }
function Write-Warn    { param($t) Write-Host "  [!]  $t" -ForegroundColor Yellow }
function Write-Err     { param($t) Write-Host "  [X]  $t" -ForegroundColor Red }
function Write-Sep     { Write-Host ("  " + "-" * 64) -ForegroundColor DarkGray }

# ── Banner ────────────────────────────────────────────────────────────────────

Clear-Host
Write-Host ""
Write-Host "  ██████╗ ██████╗ ██████╗ ███████╗███╗   ███╗ █████╗ ████████╗███████╗" -ForegroundColor Cyan
Write-Host " ██╔════╝██╔═══██╗██╔══██╗██╔════╝████╗ ████║██╔══██╗╚══██╔══╝██╔════╝" -ForegroundColor Cyan
Write-Host " ██║     ██║   ██║██║  ██║█████╗  ██╔████╔██║███████║   ██║   █████╗  " -ForegroundColor Cyan
Write-Host " ██║     ██║   ██║██║  ██║██╔══╝  ██║╚██╔╝██║██╔══██║   ██║   ██╔══╝  " -ForegroundColor Cyan
Write-Host " ╚██████╗╚██████╔╝██████╔╝███████╗██║ ╚═╝ ██║██║  ██║   ██║   ███████╗" -ForegroundColor Cyan
Write-Host "  ╚═════╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝     ╚═╝╚═╝  ╚═╝   ╚═╝   ╚══════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "  One-Click Installer  v2.0.0" -ForegroundColor DarkGray
Write-Host "  AI Coding Assistant · Claude + Ollama · No subscription" -ForegroundColor DarkGray
Write-Host ""
Write-Sep

# ── Config ────────────────────────────────────────────────────────────────────

$InstallDir      = Join-Path $env:USERPROFILE '.code-cli\bin'
$ExeName         = 'code-cli.exe'
$ExePath         = Join-Path $InstallDir $ExeName
$RepoOwner       = 'shatru123'
$RepoName        = 'Code-Cli'
$DefaultModel    = 'qwen2.5-coder:7b'
$DockerImage     = 'ollama/ollama:latest'
$ContainerName   = 'code-cli-ollama'

# ── Step 1: Check Docker ──────────────────────────────────────────────────────

Write-Header "STEP 1 — Docker"
Write-Sep

$dockerCmd = Get-Command docker -ErrorAction SilentlyContinue

if (-not $dockerCmd) {
    Write-Warn "Docker Desktop not found."
    Write-Host ""
    Write-Host "  Docker Desktop is required to run Ollama locally." -ForegroundColor Gray
    Write-Host "  Download from: https://www.docker.com/products/docker-desktop/" -ForegroundColor Cyan
    Write-Host ""
    $choice = Read-Host "  Open Docker Desktop download page now? [Y/n]"
    if ($choice -ne 'n' -and $choice -ne 'N') {
        Start-Process "https://www.docker.com/products/docker-desktop/"
        Write-Host ""
        Write-Warn "Install Docker Desktop, start it, then re-run this script."
        Write-Host ""
        Read-Host "  Press Enter to exit"
        exit 1
    }
    exit 1
}

Write-OK "Docker CLI found: $(docker --version 2>&1)"

# Check daemon
$daemonCheck = docker info 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Err "Docker daemon is not running."
    Write-Host "  Start Docker Desktop from the Start menu or system tray, then re-run this script." -ForegroundColor Gray
    Write-Host ""
    Read-Host "  Press Enter to exit"
    exit 1
}

Write-OK "Docker daemon is running."

# ── Step 2: Download or build code-cli.exe ────────────────────────────────────

Write-Host ""
Write-Header "STEP 2 — Installing code-cli"
Write-Sep

$null = New-Item -ItemType Directory -Force -Path $InstallDir

# Try GitHub Releases first
$releaseUrl = "https://api.github.com/repos/$RepoOwner/$RepoName/releases/latest"
$downloaded  = $false

try {
    Write-Step "Checking for latest GitHub release…"
    $release     = Invoke-RestMethod -Uri $releaseUrl -UseBasicParsing -TimeoutSec 10
    $asset       = $release.assets | Where-Object { $_.name -eq $ExeName } | Select-Object -First 1

    if ($asset) {
        Write-Step "Downloading $ExeName from release $($release.tag_name)…"
        Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $ExePath -UseBasicParsing
        Write-OK "Downloaded $ExeName ($([math]::Round((Get-Item $ExePath).Length / 1MB, 1)) MB)"
        $downloaded = $true
    }
} catch {
    Write-Warn "Could not reach GitHub Releases — will build from source."
}

# Build from source if no release binary
if (-not $downloaded) {
    Write-Step "Building from source (requires .NET 8 SDK)…"

    $dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Err ".NET 8 SDK not found."
        Write-Host "  Install from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Cyan
        Read-Host "  Press Enter to exit"
        exit 1
    }

    # Determine script location (repo root when run after clone)
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $csprojPath = Join-Path $scriptDir 'Code-Cli.csproj'

    if (-not (Test-Path $csprojPath)) {
        Write-Err "Code-Cli.csproj not found at $scriptDir"
        Write-Host "  Clone the repo first: git clone https://github.com/$RepoOwner/$RepoName" -ForegroundColor Gray
        Read-Host "  Press Enter to exit"
        exit 1
    }

    Write-Step "Running dotnet publish…"
    $publishDir = Join-Path $scriptDir 'publish'
    & dotnet publish $csprojPath `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $publishDir `
        --nologo -q

    if ($LASTEXITCODE -ne 0) {
        Write-Err "Build failed. See output above."
        Read-Host "  Press Enter to exit"
        exit 1
    }

    $builtExe = Join-Path $publishDir $ExeName
    Copy-Item -Path $builtExe -Destination $ExePath -Force
    Write-OK "Built and installed $ExeName"
}

# ── Step 3: Add to PATH ───────────────────────────────────────────────────────

Write-Host ""
Write-Header "STEP 3 — PATH"
Write-Sep

$currentPath = [System.Environment]::GetEnvironmentVariable('PATH', 'User')
if ($currentPath -notlike "*$InstallDir*") {
    [System.Environment]::SetEnvironmentVariable(
        'PATH',
        "$currentPath;$InstallDir",
        'User')
    $env:PATH = "$env:PATH;$InstallDir"
    Write-OK "Added $InstallDir to your user PATH."
    Write-Warn "Open a new terminal after setup for PATH to take effect globally."
} else {
    Write-OK "PATH already contains $InstallDir"
}

# ── Step 4: Configure runtime ─────────────────────────────────────────────────

Write-Host ""
Write-Header "STEP 4 — First-run configuration"
Write-Sep

Write-Host ""
Write-Host "  Choose your AI provider:" -ForegroundColor White
Write-Host ""
Write-Host "    [1] Ollama (local, free, 100%% offline, uses Docker)" -ForegroundColor Green
Write-Host "    [2] Claude (Anthropic API, best quality, needs API key)" -ForegroundColor Magenta
Write-Host ""
$providerChoice = Read-Host "  Enter 1 or 2 [default: 1]"

if ($providerChoice -eq '2') {
    Write-Host ""
    $apiKey = Read-Host "  Paste your Anthropic API key (sk-ant-...)"
    & $ExePath config --set-key $apiKey
    Write-OK "Claude configured. You can switch back any time with: code-cli config --set-provider ollama"
} else {
    & $ExePath config --set-provider ollama
    Write-OK "Provider set to Ollama (Docker runtime)."
}

# ── Step 5: Launch ────────────────────────────────────────────────────────────

Write-Host ""
Write-Header "STEP 5 — Launching Code-Cli"
Write-Sep
Write-Host ""

if ($providerChoice -eq '2') {
    Write-Host "  Connecting to Claude and opening chat…" -ForegroundColor Gray
    Write-Host ""
    & $ExePath chat --provider claude
} else {
    Write-Host "  Code-Cli will now:" -ForegroundColor White
    Write-Host "    · Pull the ollama/ollama Docker image (if not cached)" -ForegroundColor Gray
    Write-Host "    · Create and start the container automatically" -ForegroundColor Gray
    Write-Host "    · Pull $DefaultModel inside the container" -ForegroundColor Gray
    Write-Host "    · Open the chat session — ready to code" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  This may take a few minutes on first run." -ForegroundColor DarkGray
    Write-Host "  Subsequent launches are instant." -ForegroundColor DarkGray
    Write-Host ""
    & $ExePath chat --runtime docker
}
