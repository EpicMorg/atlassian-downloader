SET DOTNET_CLI_TELEMETRY_OPTOUT=true
SET DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

dotnet.exe publish --runtime win-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet.exe publish --runtime win-x86 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet.exe publish --runtime win-arm64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet.exe publish --runtime osx-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime osx-arm64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime linux-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime linux-musl-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime linux-arm --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime linux-arm64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime linux-bionic-x64 --force --self-contained true  --framework net8.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false

del /F bin\\Release\\net8.0\\win-x64\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\win-x86\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\win-arm64\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\osx-x64\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\osx-arm64\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\linux-x64\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\linux-musl-x64\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\linux-arm\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\linux-arm64\\publish\\atlassian-downloader.pdb
del /F bin\\Release\\net8.0\\linux-bionic-x64\\publish\\atlassian-downloader.pdb

type nul > bin/Release/net8.0/win-x64/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/win-x86/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/win-arm64/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/osx-x64/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/osx-arm64/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/linux-x64/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/linux-musl-x64/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/linux-arm/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/linux-arm64/publish/createdump.exe.ignore
type nul > bin/Release/net8.0/linux-bionic-x64/publish/createdump.exe.ignore

7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-win-x64.zip ./bin/Release/net8.0/win-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-win-x86.zip ./bin/Release/net8.0/win-x86/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-win-arm64.zip ./bin/Release/net8.0/win-arm64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-osx-x64.zip ./bin/Release/net8.0/osx-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-osx-arm64.zip ./bin/Release/net8.0/osx-arm64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-x64.zip ./bin/Release/net8.0/linux-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-musl-x64.zip ./bin/Release/net8.0/linux-musl-x64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-arm.zip ./bin/Release/net8.0/linux-arm/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-arm64.zip ./bin/Release/net8.0/linux-arm64/publish/*
7z a -tzip -mx5 -r0 ./bin/atlassian-downloader-net8.0-linux-bionic-x64.zip ./bin/Release/net8.0/linux-bionic-x64/publish/*

