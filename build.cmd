REM Restor the solution
dotnet restore
if errorlevel 1 GOTO ERROR

REM build the functions sdk.
dotnet build src\Microsoft.NET.Sdk.Functions.MSBuild --configuration=Release
if errorlevel 1 GOTO ERROR

REM Pack the functions sdk
dotnet pack pack\Microsoft.NET.Sdk.Functions --configuration=Release
if errorlevel 1 GOTO ERROR

REM Remove the functions sdk in the user profile so that the built sdk will be restored.
rmdir /S /Q %userprofile%\.nuget\packages\microsoft.net.sdk.functions
if errorlevel 1 GOTO ERROR

REM Restore the NuGet.exe
dotnet restore sample\NuGet\NuGet.csproj
if errorlevel 1 GOTO ERROR

REM Restore the sample project.
%userprofile%\.nuget\packages\nuget.commandline\4.1.0\tools\nuget.exe restore sample\FunctionApp\FunctionApp.sln
if errorlevel 1 GOTO ERROR

REM Build the sample solution.
msbuild sample\FunctionApp\FunctionApp.sln /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample functon app using the publish target.
msbuild sample\FunctionApp\FunctionApp\FunctionApp.csproj /t:Publish /p:PublishDir="bin\Release\dotnetpublishoutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample functon app using DeployOnBuild.
msbuild sample\FunctionApp\FunctionApp\FunctionApp.csproj /p:DeployOnBuild=true /p:PublishUrl="bin\Release\deployOnBuildOutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM run tests on unit test projects that references the functions project.
mstest /testcontainer:sample\FunctionApp\UnitTestProject1\bin\Release\UnitTestProject1.dll 
if errorlevel 1 GOTO ERROR

dotnet test sample\FunctionApp\UnitTestProject2\UnitTestProject2.csproj
if errorlevel 1 GOTO ERROR

dotnet test sample\FunctionApp\XUnitTestProject1\XUnitTestProject1.csproj
if errorlevel 1 GOTO ERROR

:ERROR
endlocal
exit /b 1