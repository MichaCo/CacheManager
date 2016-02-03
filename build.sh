#!/usr/bin/env bash

cachedir=.nuget			
mkdir -p $cachedir				
nugetVersion=latest
cachePath=$cachedir/nuget.exe
BUILDCMD_DNX_VERSION=1.0.0-rc1-update1
DNX_FEED=https://www.nuget.org/api/v2/
DNX_UNSTABLE_FEED=https://www.myget.org/F/aspnetvnext/api/v2

url=https://dist.nuget.org/win-x86-commandline/$nugetVersion/nuget.exe

if test ! -f $cachePath; then
    wget -O $cachePath $url 2>/dev/null || curl -o $cachePath --location $url /dev/null
fi

echo "testing sake"
if test ! -d packages/Sake; then
    echo "installing kore build"
    mono .nuget/nuget.exe install KoreBuild -ExcludeVersion -Source https://www.myget.org/F/aspnetvnext/api/v2 -o packages -nocache -pre
    echo "installing sake"
    mono .nuget/nuget.exe install Sake -ExcludeVersion -Source https://www.nuget.org/api/v2/ -Out packages
fi

if ! type dnvm > /dev/null 2>&1; then
    source packages/KoreBuild/build/dnvm.sh
fi

if ! type dnx > /dev/null 2>&1 || [ -z "$SKIP_DNX_INSTALL" ]; then
    dnvm install latest -runtime coreclr -alias default
    dnvm install default -runtime mono -alias default
else
    dnvm use default -runtime mono
fi

mono packages/Sake/tools/Sake.exe -I packages/KoreBuild/build -f makefile.shade "$@"
cd samples/CacheManager.Examples
dnx run
dnvm install -r coreclr latest
dnx run