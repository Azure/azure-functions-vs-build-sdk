dotnet restore
dotnet build src\Microsoft.NET.Sdk.Functions --configuration=Release
dotnet pack src\Microsoft.NET.Sdk.Functions --configuration=Release

rmdir /S /Q %userprofile%\.nuget\packages\microsoft.net.sdk.functions
dotnet restore sample\SampleFunctionApp\SampleFunctionApp.csproj
msbuild sample\SampleFunctionApp\SampleFunctionApp.csproj /p:DeployOnBuild=true /p:configuration=Release