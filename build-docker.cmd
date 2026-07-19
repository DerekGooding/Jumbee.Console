@echo off
setlocal
cd /d "%~dp0"

echo Restoring Jumbee.Console...
dotnet restore src\Jumbee.Console.sln
if errorlevel 1 exit /b 1

echo Building Jumbee.Console examples projects...
dotnet build examples\Jumbee.Console.Examples\Jumbee.Console.Examples.csproj /p:Configuration=Release
if errorlevel 1 exit /b 1
dotnet build examples\Jumbee.Console.AgentHarnessDemo\Jumbee.Console.AgentHarnessDemo.csproj /p:Configuration=Release
if errorlevel 1 exit /b 1
dotnet build examples\Jumbee.Console.IdeDemo\Jumbee.Console.IdeDemo.csproj /p:Configuration=Release
if errorlevel 1 exit /b 1

rem Read the shared version (ProjectAssemblyVersion, defined in src\Directory.Build.props) from a src project — the
rem examples projects live under examples\ and don't import that props file, so query Jumbee.Console for it.
set "VERSION="
for /f "usebackq delims=" %%v in (`dotnet msbuild src\Jumbee.Console\Jumbee.Console.csproj -getProperty:ProjectAssemblyVersion -nologo`) do set "VERSION=%%v"
if not defined VERSION (
  echo Could not read ProjectAssemblyVersion from src\Jumbee.Console. 1>&2
  exit /b 1
)

echo Building Docker image jumbee-console:%VERSION% (also tagged latest)...
docker build %* -t jumbee-console:%VERSION% -t jumbee-console:latest .
if errorlevel 1 exit /b 1

rem Also build the slim NativeAOT image (examples browser only, single native binary; see Dockerfile.aot).
echo Building NativeAOT Docker image jumbee-console-aot:%VERSION% (also tagged latest)...
docker build %* -f Dockerfile.aot -t jumbee-console-aot:%VERSION% -t jumbee-console-aot:latest .
if errorlevel 1 exit /b 1

echo Done: jumbee-console:%VERSION% and jumbee-console-aot:%VERSION% (both also tagged latest).
