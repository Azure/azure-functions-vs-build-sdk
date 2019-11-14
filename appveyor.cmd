powershell Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -UseBasicParsing -OutFile '%TEMP%\dotnet-install.ps1'
powershell %TEMP%\dotnet-install.ps1 -Architecture x64 -Version '3.0.100' -InstallDir '%ProgramFiles%\dotnet'
.paket\paket.exe install
packages\FAKE\tools\fake .\build.fsx