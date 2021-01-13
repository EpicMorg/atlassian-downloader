dotnet publish -r win7-x64 --self-contained true  --framework net5.0  --configuration Release
dotnet publish -r win7-x86 --self-contained true  --framework net5.0  --configuration Release
dotnet publish -r linux-x64 --self-contained true  --framework net5.0  --configuration Release
dotnet publish -r linux-musl-x64 --self-contained true  --framework net5.0  --configuration Release
