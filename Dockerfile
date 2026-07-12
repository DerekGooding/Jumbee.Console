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

WORKDIR /src

# Copy the repo (see .dockerignore) and build the examples browser in Release. This
# restores every project reference (ext/*, src/*) and NuGet package (Mermaider,
# AdocNet, Sugiyama, …) from nuget.org and bakes the build into the image, so the
# container starts straight into the app. NOTE: the ext/* git submodules must be
# initialised on the host first (`git submodule update --init --recursive`).
COPY . .
RUN dotnet build examples/Jumbee.Console.Examples/Jumbee.Console.Examples.csproj -c Release

# The examples are an INTERACTIVE full-screen TUI (mouse, alternate screen, raw key
# input), so the container MUST be given a TTY:
#     docker build -t jumbee-console .
#     docker run --rm -it jumbee-console
# Quit the app with Ctrl+Q; it restores your terminal on exit.
ENTRYPOINT ["dotnet", "/src/examples/Jumbee.Console.Examples/bin/Release/net10.0/Jumbee.Console.Examples.dll"]
