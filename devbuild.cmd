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

:ERROR
endlocal
exit /b 1