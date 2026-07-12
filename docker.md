# Running the Jumbee.Console examples with Docker

Run the interactive examples browser with nothing installed on your machine but Docker.

## Build

The [`Dockerfile`](Dockerfile) uses the full **.NET 10 SDK** image (Debian-slim) — not just the runtime —
so the `dotnet` build tools stay available inside the container. It copies the repo and builds the examples
project in Release, baking the build into the image.

```sh
# Make sure the vendored submodules are present first:
git submodule update --init --recursive

docker build -t jumbee-console .
```

> For a smaller image, change the base tag in the `Dockerfile` to `mcr.microsoft.com/dotnet/sdk:10.0-alpine`.
> The examples set `InvariantGlobalization=true`, so no ICU package is required on either base.

## Run

The examples are a full-screen TUI (mouse, alternate screen, raw key input), so the container **must** be given
an interactive terminal — the `-i` (keep stdin open) and `-t` (allocate a TTY) flags are required:

```sh
docker run --rm -it jumbee-console
```

- Navigate with the arrow keys / mouse; **Ctrl+Q** quits.
- On exit the app restores your terminal (it renders on the alternate screen buffer).
- `--rm` removes the container when you quit.

If colours or box-drawing look off, forward your terminal type: `docker run --rm -it -e TERM=$TERM jumbee-console`.
