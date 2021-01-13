dotnet.exe publish -r linux-x64 --self-contained true  --framework net5.0  --configuration Release
dotnet.exe publish -r linux-musl-x64 --self-contained true  --framework net5.0  --configuration Release
dotnet.exe publish -r rhel.6-x64 --self-contained true  --framework net5.0  --configuration Release
dotnet.exe publish -r tizen --self-contained true  --framework net5.0  --configuration Release
