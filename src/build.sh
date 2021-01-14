#!/bin/bash
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

dotnet publish -r win7-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish -r win7-x86 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish -r win81-arm --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish -r win10-arm64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish -r linux-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish -r linux-musl-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet publish -r osx-x64   --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false