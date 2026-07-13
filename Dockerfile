# syntax=docker/dockerfile:1

# ─────────────────────────────────────────────────────────────────────────────
# Jumbee.Console examples — run the interactive TUI browser with nothing installed
# but Docker. Uses the FULL .NET 10 SDK (not just the runtime) so the build tools
# stay available inside the image (some examples shell out to `dotnet`). Base is
# the Debian-slim SDK image; swap the tag for `10.0-alpine` for a smaller image
# (the examples set InvariantGlobalization=true, so no ICU is required either way).
# ─────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0

# UTF-8 everywhere (box-drawing glyphs) and a colour-capable TERM for the TUI.
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    LANG=C.UTF-8 \
    LC_ALL=C.UTF-8 \
    TERM=xterm-256color

# Patch the base OS packages to the latest available at build time — trims the (mostly medium, build-tool) CVEs
# Docker Scout flags in the SDK image. Rebuild with `docker build --pull` to also refresh the base image itself and
# re-fetch fresh patches (a plain rebuild reuses this cached layer). Cleaned up in the same layer to stay small.
RUN apt-get update \
    && apt-get -y upgrade \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Copy the repo (see .dockerignore) and build the examples browser in Release. This
# restores every project reference (ext/*, src/*) and NuGet package (Mermaider,
# AdocNet, Sugiyama, …) from nuget.org and bakes the build into the image, so the
# container starts straight into the app. NOTE: the ext/* git submodules must be
# initialised on the host first (`git submodule update --init --recursive`).
COPY . .
RUN dotnet build examples/Jumbee.Console.Examples/Jumbee.Console.Examples.csproj -c Release

# Also build the VS Code–style IDE demo (a separate standalone app). It is NOT the default entrypoint; it opens a
# bundled sample C# project you can edit and `dotnet build`/`dotnet run` right in its terminal pane (the SDK above
# makes that work in-container). Run it by overriding the entrypoint:
#     docker run --rm -it --entrypoint dotnet jumbee-console \
#         /src/examples/Jumbee.Console.IdeDemo/bin/Release/net10.0/Jumbee.Console.IdeDemo.dll
RUN dotnet build examples/Jumbee.Console.IdeDemo/Jumbee.Console.IdeDemo.csproj -c Release

# The examples are an INTERACTIVE full-screen TUI (mouse, alternate screen, raw key
# input), so the container MUST be given a TTY:
#     docker build -t jumbee-console .
#     docker run --rm -it jumbee-console
# Quit the app with Ctrl+Q; it restores your terminal on exit.
ENTRYPOINT ["dotnet", "/src/examples/Jumbee.Console.Examples/bin/Release/net10.0/Jumbee.Console.Examples.dll"]
