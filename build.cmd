REM Restor the solution
dotnet restore
if errorlevel 1 GOTO ERROR

REM build the functions sdk.
dotnet build src\Microsoft.NET.Sdk.Functions.MSBuild --configuration=Release
if errorlevel 1 GOTO ERROR

REM build the functions generator
dotnet build src\Microsoft.NET.Sdk.Functions.Generator --configuration=Release
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


REM  ***************************NETFramework************************************

REM Restore the sample NETFramework project.
%userprofile%\.nuget\packages\nuget.commandline\4.1.0\tools\nuget.exe restore sample\FunctionAppNETFramework\FunctionAppNETFramework.sln
if errorlevel 1 GOTO ERROR

REM Build the sample NETFramework solution.
msbuild sample\FunctionAppNETFramework\FunctionAppNETFramework.sln /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample .NETFramework functon app using the publish target (full framework msbuild).
msbuild sample\FunctionAppNETFramework\FunctionAppNETFramework\FunctionAppNETFramework.csproj /t:Publish /p:PublishDir="bin\Release\dotnetpublishoutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample .NETFramework functon app using DeployOnBuild (full framework msbuild).
msbuild sample\FunctionAppNETFramework\FunctionAppNETFramework\FunctionAppNETFramework.csproj /p:DeployOnBuild=true /p:PublishUrl="bin\Release\deployOnBuildOutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Run tests on .NETFramework projects

dotnet test sample\FunctionAppNETFramework\UnitTestProject2\UnitTestProject2.csproj --configuration=Release --no-build
if errorlevel 1 GOTO ERROR

dotnet test sample\FunctionAppNETFramework\XUnitTestProject1\XUnitTestProject1.csproj --configuration=Release --no-build
if errorlevel 1 GOTO ERROR

REM run tests on unit test projects that references the functions project.
dotnet test sample\FunctionAppNETFramework\UnitTestProject1\UnitTestProject1.csproj --configuration=Release --no-build
if errorlevel 1 GOTO ERROR


REM  ***************************NETStandard************************************


REM Restore the sample NETStandard project.
dotnet restore sample\FunctionAppNETStandard\FunctionAppNETStandard.sln
if errorlevel 1 GOTO ERROR

REM Build the sample NETStandard solution.
msbuild sample\FunctionAppNETStandard\FunctionAppNETStandard.sln /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample .NETStandard functon app using the publish target (core msbuild).
dotnet build sample\FunctionAppNETStandard\FunctionAppNETStandard\FunctionAppNETStandard.csproj /t:Publish /p:PublishDir="bin\Release\core\dotnetpublishoutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample .NETStandard functon app using DeployOnBuild (core msbuild).
dotnet build sample\FunctionAppNETStandard\FunctionAppNETStandard\FunctionAppNETStandard.csproj /p:DeployOnBuild=true /p:PublishUrl="bin\Release\core\deployOnBuildOutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample .NETStandard functon app using the publish target (full framework msbuild).
msbuild sample\FunctionAppNETStandard\FunctionAppNETStandard\FunctionAppNETStandard.csproj /t:Publish /p:PublishDir="bin\Release\full\dotnetpublishoutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR

REM Publish the sample .NETStandard functon app using DeployOnBuild (full framework msbuild).
msbuild sample\FunctionAppNETStandard\FunctionAppNETStandard\FunctionAppNETStandard.csproj /p:DeployOnBuild=true /p:PublishUrl="bin\Release\full\deployOnBuildOutput" /p:configuration=Release
if errorlevel 1 GOTO ERROR


REM Run tests on .NETStandard projects
dotnet test sample\FunctionAppNETStandard\UnitTestNETFramework\UnitTestNETFramework.csproj --configuration=Release --no-build
if errorlevel 1 GOTO ERROR

dotnet test sample\FunctionAppNETStandard\UnitTestProject2\UnitTestProject2.csproj --configuration=Release --no-build
if errorlevel 1 GOTO ERROR

dotnet test sample\FunctionAppNETStandard\XUnitTestProject1\XUnitTestProject1.csproj --configuration=Release --no-build
if errorlevel 1 GOTO ERROR


:ERROR
endlocal
exit /b 1