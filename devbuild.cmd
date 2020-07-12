REM Restore the solution.
dotnet restore
if errorlevel 1 GOTO ERROR

REM Build the functions sdk.
dotnet build src\Microsoft.NET.Sdk.Functions.MSBuild --configuration=Release
if errorlevel 1 GOTO ERROR

REM Build the functions generator.
dotnet build src\Microsoft.NET.Sdk.Functions.Generator --configuration=Release
if errorlevel 1 GOTO ERROR

REM Pack the functions sdk.
dotnet pack src\Microsoft.NET.Sdk.Functions --configuration=Release
if errorlevel 1 GOTO ERROR

REM Run tests
dotnet test test\\Microsoft.NET.Sdk.Functions.Generator.Tests --configuration Debug
dotnet test test\\Microsoft.NET.Sdk.Functions.MSBuild.Tests --configuration Debug

REM Remove the functions sdk in the user profile so that the built sdk will be restored.
rmdir /S /Q %userprofile%\.nuget\packages\microsoft.net.sdk.functions
if errorlevel 1 GOTO ERROR

:ERROR
endlocal
exit /b 1