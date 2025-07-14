#!/./bin/bash
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

dotnet publish -p:PublishProfile=win-x64 --net9.0
dotnet publish -p:PublishProfile=win-x86 --net9.0
dotnet publish -p:PublishProfile=win-arm64 --net9.0
dotnet publish -p:PublishProfile=osx-x64 --net9.0
dotnet publish -p:PublishProfile=osx-arm64 --net9.0
dotnet publish -p:PublishProfile=linux-x64 --net9.0
dotnet publish -p:PublishProfile=linux-musl-x64 --net9.0
dotnet publish -p:PublishProfile=linux-arm --net9.0
dotnet publish -p:PublishProfile=linux-arm64 --net9.0
dotnet publish -p:PublishProfile=linux-bionic-x64 --net9.0

rm -rfv ./bin/Release/net8.0/win-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/win-x86/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/win-arm64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/osx-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/osx-arm64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/linux-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/linux-musl-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/linux-arm/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/linux-arm64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net8.0/linux-bionic-x64/publish/atlassian-downloader.pdb

touch ./bin/Release/net8.0/win-x64/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/win-x86/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/win-arm64/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/osx-x64/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/osx-arm64/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/linux-x64/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/linux-musl-x64/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/linux-arm/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/linux-arm64/publish/createdump.exe.ignore
touch ./bin/Release/net8.0/linux-bionic-x64/publish/createdump.exe.ignore

7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-win-x64.zip ././bin/Release/net8.0/win-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-win-x86.zip ././bin/Release/net8.0/win-x86/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-win-arm64.zip ././bin/Release/net8.0/win-arm64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-osx-x64.zip ././bin/Release/net8.0/osx-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-osx-arm64.zip ././bin/Release/net8.0/osx-arm64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-x64.zip ././bin/Release/net8.0/linux-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-musl-x64.zip ././bin/Release/net8.0/linu-musl-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-arm.zip ././bin/Release/net8.0/linux-arm/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-arm64.zip ././bin/Release/net8.0/linux-arm64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-bionic-x64.zip ././bin/Release/net8.0/linu-bionic-x64/publish/*
