@echo off
:: Code-Cli one-click installer for Windows (no PowerShell required).
:: Double-click this file — it does everything automatically.
::
:: What this does:
::   1. Checks Docker is installed and running
::   2. Builds code-cli.exe from source (or uses pre-built if available)
::   3. Installs to %USERPROFILE%\.code-cli\bin and adds to PATH
::   4. Asks which provider you want (Ollama or Claude)
::   5. Launches the chat — Docker + model pull happen automatically

setlocal enabledelayedexpansion
chcp 65001 >nul 2>&1

echo.
echo   ======================================================================
echo    Code-Cli  One-Click Setup  v2.0.0
echo    AI Coding Assistant  ^|  Claude + Ollama  ^|  No subscription
echo   ======================================================================
echo.

set INSTALL_DIR=%USERPROFILE%\.code-cli\bin
set EXE_NAME=code-cli.exe
set EXE_PATH=%INSTALL_DIR%\%EXE_NAME%

:: ── STEP 1: Docker ────────────────────────────────────────────────────────────

echo   STEP 1 ^|^| Docker
echo   ----------------------------------------------------------------------
echo.

where docker >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo   [X] Docker CLI not found.
    echo.
    echo       Docker Desktop is required to run Ollama locally.
    echo       Download from:  https://www.docker.com/products/docker-desktop/
    echo.
    set /p OPEN_BROWSER=   Open the download page now? [Y/n]: 
    if /i "!OPEN_BROWSER!" neq "n" (
        start https://www.docker.com/products/docker-desktop/
    )
    echo.
    echo   Install Docker Desktop, start it, then double-click setup.bat again.
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('docker --version 2^>^&1') do echo   [OK] %%v

docker info >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo.
    echo   [X] Docker daemon is not running.
    echo       Start Docker Desktop from the taskbar or Start menu, then re-run.
    echo.
    pause
    exit /b 1
)

echo   [OK] Docker daemon is running.
echo.

:: ── STEP 2: Build or locate code-cli.exe ─────────────────────────────────────

echo   STEP 2 ^|^| Installing code-cli
echo   ----------------------------------------------------------------------
echo.

if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Try pre-built publish\code-cli.exe first
if exist "%~dp0publish\%EXE_NAME%" (
    echo   [OK] Found pre-built exe in publish\
    copy /Y "%~dp0publish\%EXE_NAME%" "%EXE_PATH%" >nul
    goto :installed
)

:: Otherwise build from source
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo   [X] .NET 8 SDK not found.
    echo       Install from: https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

if not exist "%~dp0Code-Cli.csproj" (
    echo   [X] Code-Cli.csproj not found.
    echo       Run setup.bat from inside the cloned repository folder.
    pause
    exit /b 1
)

echo   --> Building from source (win-x64)...
dotnet publish "%~dp0Code-Cli.csproj" ^
    -c Release -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o "%~dp0publish" ^
    --nologo -q

if %ERRORLEVEL% neq 0 (
    echo   [X] Build failed. See output above.
    pause
    exit /b 1
)

copy /Y "%~dp0publish\%EXE_NAME%" "%EXE_PATH%" >nul

:installed
echo   [OK] code-cli.exe installed to: %INSTALL_DIR%

:: ── STEP 3: PATH ──────────────────────────────────────────────────────────────

echo.
echo   STEP 3 ^|^| PATH
echo   ----------------------------------------------------------------------
echo.

set "CURRENT_PATH="
for /f "tokens=2*" %%A in ('reg query "HKCU\Environment" /v PATH 2^>nul') do set "CURRENT_PATH=%%B"

echo "%CURRENT_PATH%" | findstr /i "%INSTALL_DIR%" >nul
if %ERRORLEVEL% neq 0 (
    if defined CURRENT_PATH (
        setx PATH "%CURRENT_PATH%;%INSTALL_DIR%" >nul
    ) else (
        setx PATH "%INSTALL_DIR%" >nul
    )
    set "PATH=%PATH%;%INSTALL_DIR%"
    echo   [OK] Added to PATH. Open a new terminal for it to apply globally.
) else (
    echo   [OK] PATH already contains the install directory.
)

:: ── STEP 4: Provider choice ───────────────────────────────────────────────────

echo.
echo   STEP 4 ^|^| Choose Provider
echo   ----------------------------------------------------------------------
echo.
echo     [1]  Ollama  —  local, free, 100%% offline, uses Docker
echo     [2]  Claude  —  Anthropic API, best quality, needs API key
echo.
set /p PROVIDER=   Enter 1 or 2 [default: 1]: 

if "%PROVIDER%"=="2" (
    echo.
    set /p API_KEY=   Paste your Anthropic API key (sk-ant-...): 
    "%EXE_PATH%" config --set-key !API_KEY!
    echo.
    echo   [OK] Claude configured.
) else (
    "%EXE_PATH%" config --set-provider ollama
    echo   [OK] Provider set to Ollama (Docker runtime).
)

:: ── STEP 5: Launch ────────────────────────────────────────────────────────────

echo.
echo   STEP 5 ^|^| Launch
echo   ======================================================================
echo.

if "%PROVIDER%"=="2" (
    echo   Connecting to Claude...
    echo.
    "%EXE_PATH%" chat --provider claude
) else (
    echo   Code-Cli will now automatically:
    echo     · Pull the ollama/ollama Docker image  (if not already cached)
    echo     · Create and start the container       (no manual steps)
    echo     · Pull qwen2.5-coder:7b inside it      (no manual steps)
    echo     · Open the chat session                (ready to code)
    echo.
    echo   First run may take a few minutes. All future launches are instant.
    echo.
    "%EXE_PATH%" chat --runtime docker
)
