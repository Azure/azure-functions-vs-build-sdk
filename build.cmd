dotnet restore
dotnet build src\Microsoft.NET.Sdk.Functions --configuration=Release
dotnet pack src\Microsoft.NET.Sdk.Functions --configuration=Release