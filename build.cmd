@echo off
echo Building Jumbee.Console...
dotnet restore src\Jumbee.Console.sln
echo Building Jumbee.Console examples projects...
dotnet build examples\Jumbee.Console.Examples\Jumbee.Console.Examples.csproj /p:Configuration=Release
dotnet build examples\Jumbee.Console.AgentHarnessDemo\Jumbee.Console.AgentHarnessDemo.csproj /p:Configuration=Release
dotnet build examples\Jumbee.Console.IdeDemo\Jumbee.Console.IdeDemo.csproj /p:Configuration=Release
echo Jumbee.Console build complete.