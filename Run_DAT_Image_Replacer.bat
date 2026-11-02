@echo off
cd /d "%~dp0"
if exist "dist\DAT_Image_Replacer.exe" (
    start "" "dist\DAT_Image_Replacer.exe"
    exit /b 0
)
echo DAT_Image_Replacer.exe was not found. Run Build_DAT_Image_Replacer.bat first.
pause
exit /b 1
