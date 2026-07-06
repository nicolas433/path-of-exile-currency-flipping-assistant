@echo off
REM Builds a single self-contained .exe that runs on any 64-bit Windows PC,
REM even without the .NET runtime installed. Double-click to publish.
cd /d "%~dp0"

dotnet publish src\PoE2FlipOverlay.App -c Release -r win-x64 --self-contained true ^
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

if errorlevel 1 (
    echo.
    echo *** FALHA NA PUBLICACAO *** veja os erros acima.
    pause
    exit /b 1
)

set "OUT=src\PoE2FlipOverlay.App\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
echo.
echo ====================================================================
echo  Executavel gerado em:
echo    %OUT%\PoE2FlipOverlay.exe
echo  E so dar duplo-clique (nao precisa de terminal nem .NET instalado).
echo ====================================================================
explorer "%OUT%"
