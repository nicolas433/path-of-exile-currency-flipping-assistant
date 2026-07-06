@echo off
REM Double-click to rebuild and launch the overlay (dev convenience).
cd /d "%~dp0"

taskkill /IM PoE2FlipOverlay.exe /F >nul 2>&1

dotnet build src\PoE2FlipOverlay.App -c Debug
if errorlevel 1 (
    echo.
    echo *** FALHA NA COMPILACAO *** veja os erros acima.
    pause
    exit /b 1
)

start "" "src\PoE2FlipOverlay.App\bin\Debug\net8.0-windows10.0.19041.0\PoE2FlipOverlay.exe"
