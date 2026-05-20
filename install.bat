@echo off
setlocal enabledelayedexpansion

echo.
echo  ============================================================
echo   Code-Cli Installer
echo  ============================================================
echo.

set INSTALL_DIR=%USERPROFILE%\.code-cli\bin
set EXE_NAME=code-cli.exe
set SOURCE_EXE=publish\%EXE_NAME%

:: Check if exe was built
if not exist "%SOURCE_EXE%" (
    echo  [ERROR] code-cli.exe not found. Run build.bat first.
    pause
    exit /b 1
)

:: Create install dir
echo  Installing to: %INSTALL_DIR%
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

:: Copy exe
copy /Y "%SOURCE_EXE%" "%INSTALL_DIR%\%EXE_NAME%" >nul
if %ERRORLEVEL% neq 0 (
    echo  [ERROR] Failed to copy exe.
    pause
    exit /b 1
)

:: Add to user PATH if not already there
echo  Updating PATH...
set "CURRENT_PATH="
for /f "tokens=2*" %%A in ('reg query "HKCU\Environment" /v PATH 2^>nul') do set "CURRENT_PATH=%%B"

echo %CURRENT_PATH% | findstr /i "%INSTALL_DIR%" >nul
if %ERRORLEVEL% neq 0 (
    if defined CURRENT_PATH (
        setx PATH "%CURRENT_PATH%;%INSTALL_DIR%" >nul
    ) else (
        setx PATH "%INSTALL_DIR%" >nul
    )
    echo  PATH updated. Restart your terminal for changes to take effect.
) else (
    echo  PATH already contains install directory.
)

echo.
echo  ============================================================
echo   INSTALLATION COMPLETE!
echo  ============================================================
echo.
echo   code-cli.exe installed to: %INSTALL_DIR%
echo.
echo   Restart your terminal, then run:
echo     code-cli --help
echo     code-cli chat
echo.
pause
