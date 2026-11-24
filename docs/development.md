# Development

## Requirements

- .NET 11 SDK. The project targets `net11.0` and needs a C# version that supports union types. `global.json` pins the
  SDK to the 11.0.1xx band (11.0.100 or a later patch), so local and CI builds use the same compiler.

## Building and testing

```sh
dotnet build CSharpPlusPlus.slnx
dotnet test CSharpPlusPlus.slnx
```

Tests use NUnit with the constraint model (`Assert.That`). There is one fixture per class under test, and test names
follow `Member_Scenario_ExpectedResult`.

The build treats warnings as errors and runs StyleCop analyzers. Every public type and member in `src` needs an XML doc
comment (CS1591), because the package ships them for IntelliSense; tests and benchmarks are exempt. Package versions are
managed centrally in `Directory.Packages.props`.

## Pre-commit hook

A pre-commit hook in `.githooks/` runs `dotnet format` on staged `.cs` files and re-stages them, then builds the
solution and runs the tests in Release. The first local restore or build enables it by running
`git config core.hooksPath .githooks` (see the `InstallGitHooks` target in `Directory.Build.props`). If a staged file
also has unstaged changes, the hook only checks its formatting, so the unstaged changes don't end up
in the commit. Use `git commit --no-verify` to skip the hook.

## Repository layout

```
src/Cspp.Results/            Result library
src/Cspp.Results.Analyzers/  Roslyn analyzer shipped inside the Cspp.Results package
test/Cspp.Results.Tests/     NUnit tests
benchmarks/Cspp.Benchmarks/  BenchmarkDotNet comparison with other result libraries
docs/                        Documentation
```

See [Benchmarks](benchmarks.md) for running the benchmarks and [Releasing](releasing.md) for publishing a version.