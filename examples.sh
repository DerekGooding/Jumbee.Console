#!/usr/bin/env bash
# Run the TestDemo's active demo (TerminalDemo) using the already-built binary (run ./build first).
# Pass a shell to launch; defaults to $SHELL (or /bin/bash). Examples:
#   ./testdemo                # uses $SHELL
#   ./testdemo /bin/bash
#   ./testdemo /usr/bin/zsh
set -euo pipefail
cd "$(dirname "$0")"

dll="examples/Jumbee.Console.Examples/bin/Release/net10.0/Jumbee.Console.Examples.dll"
if [[ ! -f "$dll" ]]; then
  echo "Not built yet — run ./build first." >&2
  exit 1
fi

shell="${1:-${SHELL:-/bin/bash}}"
exec dotnet "$dll" "$shell"
