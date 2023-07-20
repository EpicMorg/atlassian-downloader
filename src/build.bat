SET DOTNET_CLI_TELEMETRY_OPTOUT=true
SET DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

dotnet.exe publish --runtime win7-x64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet.exe publish --runtime win7-x86 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet.exe publish --runtime win81-arm --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet.exe publish --runtime win10-arm64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=false -p:PublishReadyToRun=true
dotnet.exe publish --runtime linux-x64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime linux-musl-x64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false
dotnet.exe publish --runtime osx-x64 --force --self-contained true  --framework net6.0  --configuration Release -p:PublishTrimmed=false -p:PublishSingleFile=false -p:PublishReadyToRun=false