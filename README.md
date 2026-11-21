# C#++

Small building blocks that extend C# with patterns found in other languages.

## Cspp.Results

[![CI](https://github.com/talisraeli/csharp-plus-plus/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/talisraeli/csharp-plus-plus/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Cspp.Results?logo=nuget)](https://www.nuget.org/packages/Cspp.Results)

A `Result` type for returning success or failure without throwing exceptions. It is built on C# union types (`[Union]` /
`IUnion`).

| Type        | Description                                                                                                           |
|-------------|-----------------------------------------------------------------------------------------------------------------------|
| `Result`    | A union of `Success` or `Failure`.                                                                                    |
| `Result<T>` | A union of a `T` value or `Failure`.                                                                                  |
| `Success`   | A singleton, obtained through `Result.Success`, marking success for the non-generic `Result`.                         |
| `Failure`   | A `Code` and an `Error` message, neither null nor whitespace. Predefined: `Failure.Unknown`, `Failure.Uninitialized`. |

### Usage

```csharp
using Cspp.Results;

Result<int> Parse(string text) =>
    int.TryParse(text, out int value)
        ? value
        : new Failure("Parse.Invalid", $"'{text}' is not a number.");

string message = Parse("42") switch
{
    int value => $"Parsed {value}",
    Failure failure => $"{failure.Code}: {failure.Error}",
};
```

A value or a `Failure` converts to `Result<T>` implicitly, and union pattern matching works on the case directly, so no
wrapping or casting is needed. Because the union's cases are known, the switch is exhaustive without a discard (`_`)
arm. If you only need to check which case you have, `result.IsSuccess` and `result.IsFailure` are also available, and
`TryGetValue` gives you the case without pattern matching.

To transform a result or chain steps that can fail without a `switch` at each step, use `Map`, `Bind`, `Match` and
`GetValueOrFallback`:

```csharp
Result<Order> order = Parse(text)
    .Bind(FindCustomer)                     // Result<Customer> FindCustomer(int id)
    .Map(customer => new Order(customer));  // a failure from Parse or FindCustomer passes through

string summary = order.Match(o => $"Order for {o.Customer.Name}", failure => failure.Error);
int quantity = Parse(text).GetValueOrFallback(1);
```

`Map` is for steps that always succeed, and `Bind` is for steps that return a `Result<T>` of their own. On a failure,
neither calls its delegate. `GetValueOrFallback()` without a fallback returns `default(T)` on a failure.

For the non-generic `Result`, return `Result.Success` or a `Failure`.

A success value may be `null` when `T` allows it (for example `Result<string?>`).

A `default` result is a failure carrying `Failure.Uninitialized`, which has its own code and error so it can't be
confused with `Failure.Unknown`.

### Analyzer

`T` must not be `Failure` or a type derived from it, because then a success value and a failure would look the same. The
package includes an analyzer that reports this as a compile error:

```csharp
Result<Failure> result = default; // CSPP0001 compile error
```

The check covers types written in source, including type parameters constrained to `Failure`, and generic method calls
that return `Result<T>` with a `Failure` as `T`, such as `result.Map(_ => Failure.Unknown)`. To fail from inside a
step, use `Bind` instead. It does not see a `Result<Failure>` nested inside another return type, such as a generic
method returning `Task<Result<T>>`.

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

The build treats warnings as errors and runs StyleCop analyzers. Package versions are managed centrally in
`Directory.Packages.props`.

A pre-commit hook in `.githooks/` runs `dotnet format` on staged `.cs` files and re-stages them, then builds the
solution and runs the tests in Release. The first local restore or build enables it by running
`git config core.hooksPath .githooks` (see the `InstallGitHooks` target in `Directory.Build.props`). If a staged file
also has unstaged changes, the hook only checks its formatting, so the unstaged changes don't end up
in the commit. Use `git commit --no-verify` to skip the hook.

## Releasing

GitHub Actions builds and tests every push and pull request to `main` (`.github/workflows/ci.yml`). To publish to
NuGet, push a version tag:

```sh
git tag v0.2.0
git push origin v0.2.0
```

`.github/workflows/release.yml` takes the package version from the tag, builds, tests, packs and pushes the `.nupkg`
and `.snupkg` to nuget.org using NuGet trusted publishing.

## Repository layout

```
src/Cspp.Results/            Result library
src/Cspp.Results.Analyzers/  Roslyn analyzer shipped inside the Cspp.Results package
test/Cspp.Results.Tests/     NUnit tests
```

## License

Licensed under the [MIT License](LICENSE).