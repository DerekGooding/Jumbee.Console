#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

dll="examples/Jumbee.Console.AgentHarnessDemo/bin/Release/net10.0/Jumbee.Console.AgentHarnessDemo.dll"
if [[ ! -f "$dll" ]]; then
  echo "Not built yet — run ./build first." >&2
  exit 1
fi

shell="${1:-${SHELL:-/bin/bash}}"
exec dotnet "$dll" "$shell"
