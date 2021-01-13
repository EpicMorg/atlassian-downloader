dotnet publish -r win7-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet publish -r win7-x86 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet publish -r win81-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet publish -r win81-x86 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet publish -r win10-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet publish -r win10-x86 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet publish -r linux-x64 --self-contained true  --framework net5.0  --configuration Release 
dotnet publish -r osx-x64   --self-contained true  --framework net5.0  --configuration Release 
dotnet publish -r linux-musl-x64 --self-contained true  --framework net5.0  --configuration Release 