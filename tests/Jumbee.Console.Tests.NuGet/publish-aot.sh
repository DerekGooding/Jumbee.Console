#!/usr/bin/env bash
# NativeAOT variant of the Jumbee.Console NuGet package smoke test.
#
# Publishes Program.cs as a NativeAOT native single-file binary, restoring Jumbee.Console and its bundled
# ext/ fork assemblies purely from nuget.org, then runs it headlessly and asserts exit code 0. Proves the
# *published* package is AOT/trim-clean when consumed from the feed.
#
# Requires a native toolchain (clang + zlib on Linux). Opt-in; not part of the default `dotnet run` test.
#
# Usage:
#   ./publish-aot.sh                       # test the LATEST stable release, linux-x64
#   ./publish-aot.sh 0.1.2                  # pin a specific release
#   ./publish-aot.sh 0.1.2 linux-arm64      # ...for a specific RID
set -euo pipefail

version="${1:-}"
rid="${2:-linux-x64}"
project_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
project="$project_dir/Jumbee.Console.Tests.NuGet.csproj"
publish_dir="$project_dir/bin/aot/$rid"

echo "Jumbee.Console NuGet package AOT smoke test"
echo "==========================================="
echo "  RID:     $rid"
echo "  Version: ${version:-* (latest stable on nuget.org)}"
echo "  Output:  $publish_dir"
echo

publish_args=(publish "$project" -c Release -r "$rid" -p:PublishAot=true -o "$publish_dir")
[ -n "$version" ] && publish_args+=("-p:JumbeeVersion=$version")

echo "> dotnet ${publish_args[*]}"
dotnet "${publish_args[@]}"

exe="$publish_dir/Jumbee.Console.Tests.NuGet"
if [ ! -f "$exe" ]; then
    echo "Published binary not found at $exe" >&2
    exit 1
fi

echo
echo "Running AOT binary: $exe"
run_args=()
[ -n "$version" ] && run_args+=(--version "$version")
set +e
"$exe" "${run_args[@]}"
code=$?
set -e

echo
if [ "$code" -eq 0 ]; then
    echo "AOT SMOKE TEST PASSED — the published package publishes and runs under NativeAOT."
else
    echo "AOT SMOKE TEST FAILED — $code check(s) failed in the native binary."
fi
exit "$code"
