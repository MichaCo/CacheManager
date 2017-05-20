@echo off
cd %~dp0

call GetMsdn.cmd

SETLOCAL
SET DOCFX_VERSION=2.16.7
SET CACHED_ZIP=%LocalAppData%\DocFx\docfx.%DOCFX_VERSION%.zip

IF EXIST %CACHED_ZIP% goto extract
echo Downloading latest version of docfx...
IF NOT EXIST %LocalAppData%\DocFx md %LocalAppData%\DocFx
SET DWL_FILE=https://github.com/dotnet/docfx/releases/download/v%DOCFX_VERSION%/docfx.zip
echo Downloading %DWL_FILE%
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest '%DWL_FILE%' -OutFile '%CACHED_ZIP%'"

:extract

copy %CACHED_ZIP% docfx.zip > nul

RMDIR /S /Q  "DocFxBin"
IF NOT EXIST \DocFxBin md \DocFxBin
@powershell -nologo -noprofile -command "& { Add-Type -A 'System.IO.Compression.FileSystem'; [IO.Compression.ZipFile]::ExtractToDirectory('docfx.zip', 'DocFxBin'); }"

:restore

:run

rd /S /Q ..\..\cachemanager.net\website\docs
rd /S /Q obj
DocFxBin\docfx.exe