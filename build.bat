@echo off
setlocal enabledelayedexpansion

echo.
echo  ============================================================
echo   Code-Cli Build Script
echo  ============================================================
echo.

:: Check .NET SDK
where dotnet >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo  [ERROR] .NET 8 SDK not found.
    echo  Download from: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version') do set DOTNET_VER=%%i
echo  .NET SDK: %DOTNET_VER%
echo.

echo  [1/3] Restoring...
dotnet restore Code-Cli.csproj --nologo -q
if %ERRORLEVEL% neq 0 (
    echo  [ERROR] Restore failed.
    pause
    exit /b 1
)

echo  [2/3] Building...
dotnet build Code-Cli.csproj -c Release --nologo -q
if %ERRORLEVEL% neq 0 (
    echo  [ERROR] Build failed.
    pause
    exit /b 1
)

echo  [3/3] Publishing single-file exe (win-x64)...
dotnet publish Code-Cli.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -p:EnableCompressionInSingleFile=true ^
    -o ./publish ^
    --nologo -q

if %ERRORLEVEL% neq 0 (
    echo  [ERROR] Publish failed.
    pause
    exit /b 1
)

echo.
echo  ============================================================
echo   BUILD SUCCESSFUL!
echo  ============================================================
echo.
echo   Output  :  %CD%\publish\code-cli.exe
echo.
echo   Next steps:
echo   1. Run install.bat  (adds to PATH automatically)
echo   2. Local Ollama:    https://ollama.ai
echo   3. Docker Ollama:   install Docker Desktop
echo   4. Run local:       code-cli chat
echo   5. Run Docker:      code-cli chat --runtime docker
echo.
explorer publish 2>nul
pause
