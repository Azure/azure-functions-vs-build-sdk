dotnet restore
dotnet build src\Microsoft.NET.Sdk.Functions --configuration=Release
dotnet pack src\Microsoft.NET.Sdk.Functions --configuration=Release

rmdir /S /Q %userprofile%\.nuget\packages\microsoft.net.sdk.functions
dotnet restore sample\FunctionApp\FunctionApp\FunctionApp.csproj
msbuild sample\FunctionApp\FunctionApp\FunctionApp.csproj /t:Publish /p:PublishDir="bin\Release\dotnetpublishoutput" /p:configuration=Release
msbuild sample\FunctionApp\FunctionApp\FunctionApp.csproj /p:DeployOnBuild=true /p:PublishUrl="bin\Release\deployOnBuildOutput" /p:configuration=Release