# Contributing to Jumbee.Console

## Clone with submodules

The library contains forks of several libraries (ConsoleGUI, Spectre.Console, and a handful of others) that live under `ext/` as git submodules. The build does not work without them.

    git clone --recurse-submodules https://github.com/allisterb/Jumbee.Console

Or from an existing clone:
    git submodule update --init --recursive

## Build

    .\build.cmd     # Windows
    ./build         # Linux/macOS

Either script restores and builds the library and the example apps. To build the whole solution directly:

    dotnet build src/Jumbee.Console.sln

You need the .NET 10 SDK.

## Test

The first-party test suite is an xUnit project:

    dotnet test tests/Jumbee.Console.Tests/Jumbee.Console.Tests.csproj

Or run everything in the solution with `dotnet test src/Jumbee.Console.sln`.

## Where your changes belong

- **Work in `src/`.** That is the library, and it's where nearly every change should land.
- **Leave `ext/` alone.** Those are vendored forks, pinned as submodules. Patching them locally breaks the clone-and-build contract and is hard to carry forward. If a fork genuinely needs a change, raise an issue first.
- Match the surrounding style: 4-space indentation, `#region` grouping, and the public → internal → protected → private member ordering the files already use. Target .NET 10 / C# 14.

The architecture (how the ConsoleGUI layout engine and the Spectre.Console rendering pipeline are bridged) is written up under [docs/internal](docs/internal/). Read that before attempting a large change.

## Pull requests

- Keep a PR focused on one thing.
- Say what you changed, why, and how you tested it.
- New public API should carry XML-doc comments and, where it makes sense, a snapshot test (see the `Jumbee.Console.Snapshot` package and the existing tests).
