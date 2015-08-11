#!/bin/bash
mkdir -p .nuget

url=https://www.nuget.org/nuget.exe

if test ! -f .nuget/nuget.exe; then
    wget -O .nuget/nuget.exe $url 2>/dev/null || curl -o .nuget/nuget.exe --location $url /dev/null
fi

if test ! -d packages/KoreBuild; then
    mono .nuget/nuget.exe install KoreBuild -ExcludeVersion -o packages -nocache -pre
    mono .nuget/nuget.exe install Sake -version 0.2 -o packages -ExcludeVersion
fi

if ! type dnvm > /dev/null 2>&1; then
    source packages/KoreBuild/build/dnvm.sh
fi

if ! type dnx > /dev/null 2>&1; then
    dnvm upgrade
fi

mono packages/Sake/tools/Sake.exe -I packages/KoreBuild/build -f makefile.shade "$@"
