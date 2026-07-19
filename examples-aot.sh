#!/usr/bin/env bash
# Entry point for the SLIM NativeAOT image (Dockerfile.aot). Unlike the full image's examples.sh — which runs each
# demo via `dotnet <dll>` — this execs the pre-compiled NATIVE binaries. Only the two AOT-eligible apps are bundled:
# the examples browser and the agent-harness demo. The IDE demo is intentionally NOT here: it needs the in-container
# .NET SDK (you edit a project and run `dotnet build`/`run` in its terminal pane), which the slim runtime image does
# not ship — use the full `jumbee-console` image for it.
#
#   docker run --rm -it jumbee-console-aot                 # examples browser (default)
#   docker run --rm -it jumbee-console-aot agent-harness   # agent harness demo
#
# The first argument picks the app; any remaining arguments pass through. Quit any app with Ctrl+Q.
set -euo pipefail

examples=/app/examples/Jumbee.Console.Examples
agent=/app/agent/Jumbee.Console.AgentHarnessDemo

case "${1:-}" in
  agent-harness|agent|ah)  shift; exec "$agent" "$@" ;;
  examples|browser)        shift; exec "$examples" "$@" ;;
  ide)
    echo "The IDE demo needs the .NET SDK and is not in the slim AOT image." >&2
    echo "Use the full image:  docker run --rm -it jumbee-console ide" >&2
    exit 2 ;;
  -h|--help|help)
    echo "Slim AOT image apps:  examples (default) | agent-harness"
    echo "(The IDE demo needs the SDK — use the full 'jumbee-console' image.)"
    exit 0 ;;
  '')                      exec "$examples" ;;
  *)                       exec "$examples" "$@" ;;   # unrecognized → default browser (mirrors examples.sh)
esac
