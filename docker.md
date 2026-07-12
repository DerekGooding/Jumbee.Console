# Running the Jumbee.Console examples with Docker

Run the interactive examples browser with nothing installed on your machine but Docker.

## Quick start — run the published image

No clone, no build. Docker pulls the image ([`allisterb/jumbee-console`](https://hub.docker.com/r/allisterb/jumbee-console) on Docker Hub) on first run:

```sh
docker run --rm -it allisterb/jumbee-console
```

The examples are a full-screen TUI (mouse, alternate screen, raw key input), so the container **must** be given an
interactive terminal — the `-i` (keep stdin open) and `-t` (allocate a TTY) flags are required.

- Navigate with the arrow keys / mouse; **Ctrl+Q** quits.
- On exit the app restores your terminal (it renders on the alternate screen buffer).
- `--rm` removes the container when you quit.
- If colours or box-drawing look off, forward your terminal type: `docker run --rm -it -e TERM=$TERM allisterb/jumbee-console`.

> The published image is `linux/amd64` (native on Windows/WSL2 and Intel Linux/macOS; Apple Silicon runs it under emulation).

## Build it yourself

The [`Dockerfile`](Dockerfile) uses the full **.NET 10 SDK** image (Debian-slim) — not just the runtime — so the
`dotnet` build tools stay available inside the container. It copies the repo and builds the examples project in
Release, baking the build into the image.

```sh
# Make sure the vendored submodules are present first:
git submodule update --init --recursive

# --pull refreshes the SDK base image and re-applies the latest OS security patches (see below).
docker build --pull -t jumbee-console .
docker run --rm -it jumbee-console
```

The build patches the base OS packages (`apt-get upgrade`) to trim the CVEs Docker Scout reports in the SDK image's
build tools. Those are mostly medium-severity issues in tools (`git`, `wget`, `tar`, …) the running TUI never uses, so
the practical risk is low — but rebuilding periodically with **`--pull`** keeps both the base image and the patch layer
current.

> For a smaller image, change the base tag in the `Dockerfile` to `mcr.microsoft.com/dotnet/sdk:10.0-alpine`.
> The examples set `InvariantGlobalization=true`, so no ICU package is required on either base.

## Publishing (maintainers)

The published `linux/amd64` + `linux/arm64` image is built and pushed by GitHub Actions
([`.github/workflows/docker-publish.yml`](.github/workflows/docker-publish.yml)) — on a version tag `vX.Y.Z`
(tags the image `X.Y.Z`, `X.Y`, `latest`) or a manual **Run workflow** (tags `latest`). Each arch builds on its own
**native** runner (`ubuntu-24.04` + `ubuntu-24.04-arm`, no QEMU emulation) and the digests are merged into one
multi-arch manifest. The native arm64 runner is free only for **public** repositories.

One-time setup — add two repository secrets (Settings → Secrets and variables → Actions):

| Secret | Value |
| --- | --- |
| `DOCKERHUB_USERNAME` | your Docker Hub username (`allisterb`) |
| `DOCKERHUB_TOKEN` | a Docker Hub **access token** (Account Settings → Security → New Access Token, Read/Write) |

Then `git tag v0.1.0 && git push --tags`, or trigger the workflow by hand from the Actions tab.
