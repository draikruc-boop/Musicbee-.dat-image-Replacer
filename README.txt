# DAT Image Replacer

A small Windows desktop utility for `.dat` files that contain JPEG image data (like the supplied `Red Fiction.dat`).

## What it does
- Opens one or more `.dat` image files and previews the first one.
- Lets you select a replacement image (JPG, PNG, BMP, GIF, TIFF, or WebP).
- Center-crops the replacement to each target file's original dimensions.
- Saves JPEG image data back into the original `.dat` file, keeping its filename and extension.
- Replaces the original DAT file directly. No backup is created.

## Build a standalone Windows executable
1. Install the .NET SDK. Python is not required.
2. Run `Build_DAT_Image_Replacer.bat`, or run `dotnet build DAT_Image_Replacer.csproj --configuration Release`.
3. The Windows GUI executable is created at `dist\DAT_Image_Replacer.exe`.

The first build needs an internet connection to restore the .NET Framework 4.8 reference assemblies. The app itself uses only .NET Framework libraries; the target computer must have .NET Framework 4.8 installed.

## Run
Double-click `dist\DAT_Image_Replacer.exe`, or use `Run_DAT_Image_Replacer.bat` after building.

## Important
This app is designed for `.dat` files that are actually JPEG images with a different extension. It does not modify arbitrary binary DAT formats. Replacement is permanent unless you keep your own copy of the original file.
