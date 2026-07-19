#!/usr/bin/env bash
# Build the three examples projects, then build BOTH Docker images tagged with the shared ProjectAssemblyVersion:
# the full playground image (Dockerfile) and the slim NativeAOT image (Dockerfile.aot).
# Any arguments are passed through to `docker build` (e.g. --pull, --no-cache). Mirrors build-docker.cmd.
set -euo pipefail
cd "$(dirname "$0")"

echo "Restoring Jumbee.Console..."
dotnet restore src/Jumbee.Console.sln

echo "Building the three examples projects (Release)..."
dotnet build examples/Jumbee.Console.Examples/Jumbee.Console.Examples.csproj -c Release
dotnet build examples/Jumbee.Console.AgentHarnessDemo/Jumbee.Console.AgentHarnessDemo.csproj -c Release
dotnet build examples/Jumbee.Console.IdeDemo/Jumbee.Console.IdeDemo.csproj -c Release

# Read the shared version (ProjectAssemblyVersion, defined in src/Directory.Build.props) from a src project — the
# examples projects live under examples/ and don't import that props file, so query Jumbee.Console for it.
version="$(dotnet msbuild src/Jumbee.Console/Jumbee.Console.csproj -getProperty:ProjectAssemblyVersion -nologo)"
version="${version//[$'\r\n ']/}"
if [[ -z "$version" ]]; then
  echo "Could not read ProjectAssemblyVersion from src/Jumbee.Console." >&2
  exit 1
fi

echo "Building Docker image jumbee-console:$version (also tagged latest)..."
docker build "$@" -t "jumbee-console:$version" -t jumbee-console:latest .

# Also build the slim NativeAOT image (examples browser only, single native binary; see Dockerfile.aot).
echo "Building NativeAOT Docker image jumbee-console-aot:$version (also tagged latest)..."
docker build "$@" -f Dockerfile.aot -t "jumbee-console-aot:$version" -t jumbee-console-aot:latest .

echo "Done: jumbee-console:$version and jumbee-console-aot:$version (both also tagged latest)."
