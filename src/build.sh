#!/./bin/bash
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

dotnet publish --runtime win-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet publish --runtime win-x86 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet publish --runtime win-arm64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet publish --runtime osx-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime osx-arm64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime linux-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=fal
dotnet publish --runtime linux-musl-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=falsese
dotnet publish --runtime linux-arm --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime linux-arm64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime linux-bionic-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false

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
