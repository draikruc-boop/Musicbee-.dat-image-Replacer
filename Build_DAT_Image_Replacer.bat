@echo off
@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo The .NET SDK was not found. Install the .NET SDK to build this app.
    pause
    exit /b 1
)
dotnet build DAT_Image_Replacer.csproj --configuration Release
if errorlevel 1 (
    echo The .NET Framework 4.8 build failed. Check your internet connection for the first NuGet restore.
    pause
    exit /b 1
)

echo Build complete: dist\DAT_Image_Replacer.exe
echo The target computer must have .NET Framework 4.8 installed.
pause