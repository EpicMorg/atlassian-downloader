#!/./bin/bash
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

dotnet publish --runtime win7-x64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime win7-x86 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime win81-arm --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime win10-arm64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime linux-x64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime linux-musl-x64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish --runtime osx-x64   --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false

rm -rfv ./bin/Release/net6.0/linux-musl-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net6.0/linux-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net6.0/osx-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net6.0/win10-arm64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net6.0/win7-x64/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net6.0/win7-x86/publish/atlassian-downloader.pdb
rm -rfv ./bin/Release/net6.0/win81-arm/publish/atlassian-downloader.pdb

touch ./bin/Release/net6.0/linux-musl-x64/publish/createdump.exe.ignore
touch ./bin/Release/net6.0/linux-x64/publish/createdump.exe.ignore
touch ./bin/Release/net6.0/osx-x64/publish/createdump.exe.ignore
touch ./bin/Release/net6.0/win10-arm64/publish/createdump.exe.ignore
touch ./bin/Release/net6.0/win7-x64/publish/createdump.exe.ignore
touch ./bin/Release/net6.0/win7-x86/publish/createdump.exe.ignore
touch ./bin/Release/net6.0/win81-arm/publish/createdump.exe.ignore

7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net6.0-linux-musl-x64.zip ././bin/Release/net6.0/linux-musl-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net6.0-linux-x64.zip ././bin/Release/net6.0/linux-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net6.0-osx-x64.zip ././bin/Release/net6.0/osx-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net6.0-win10-arm64.zip ././bin/Release/net6.0/win10-arm64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net6.0-win7-x64.zip ././bin/Release/net6.0/win7-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net6.0-win7-x86.zip ././bin/Release/net6.0/win7-x86/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net6.0-win81-arm.zip ././bin/Release/net6.0/win81-arm/publish/*


