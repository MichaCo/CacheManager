@echo off
cd %~dp0

SETLOCAL
SET NUGET_VERSION=latest
SET CACHED_NUGET=%LocalAppData%\NuGet\nuget.%NUGET_VERSION%.exe

@powershell -NoProfile -ExecutionPolicy unrestricted -Command "&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}"
call dnvm install latest

IF EXIST %CACHED_NUGET% goto copynuget

echo Downloading latest version of NuGet.exe...
IF NOT EXIST %LocalAppData%\NuGet md %LocalAppData%\NuGet
@powershell -NoProfile -ExecutionPolicy unrestricted -Command "$ProgressPreference = 'SilentlyContinue'; Invoke-WebRequest 'https://dist.nuget.org/win-x86-commandline/%NUGET_VERSION%/nuget.exe' -OutFile '%CACHED_NUGET%'"

:copynuget
IF EXIST .nuget\nuget.exe goto restore
md .nuget
copy %CACHED_NUGET% .nuget\nuget.exe > nul

:restore
IF EXIST msdn.4.5.2 goto build
.nuget\nuget.exe install msdn.4.5.2 -pre -ExcludeVersion
.nuget\nuget.exe install docfx -ExcludeVersion -pre -Out packages

:build

call dnu restore packages\docfx\app
del "*.log"
call packages\docfx\app\docfx.cmd metadata -l meta.log --logLevel info -f
call packages\docfx\app\docfx.cmd build -l build.log --logLevel info -f