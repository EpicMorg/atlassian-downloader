dotnet.exe publish -r win7-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet.exe publish -r win7-x86 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet.exe publish -r win81-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet.exe publish -r win81-x86 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet.exe publish -r win10-x64 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet.exe publish -r win10-x86 --self-contained true  --framework net5.0  --configuration Release -p:PublishTrimmed=true -p:PublishSingleFile=true -p:PublishReadyToRun=true
dotnet.exe publish -r linux-x64 --self-contained true  --framework net5.0  --configuration Release 
dotnet.exe publish -r linux-musl-x64 --self-contained true  --framework net5.0  --configuration Release 