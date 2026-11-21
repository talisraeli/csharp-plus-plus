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
`GetValueOrDefault`:

```csharp
Result<Order> order = Parse(text)
    .Bind(FindCustomer)                     // Result<Customer> FindCustomer(int id)
    .Map(customer => new Order(customer));  // a failure from Parse or FindCustomer passes through

string summary = order.Match(o => $"Order for {o.Customer.Name}", failure => failure.Error);
int quantity = Parse(text).GetValueOrDefault(1);
```

`Map` is for steps that always succeed, and `Bind` is for steps that return a `Result<T>` of their own. On a failure,
neither calls its delegate. `GetValueOrDefault()` without a default value returns `default(T)` on a failure.

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

The check covers types written in source, including type parameters constrained to `Failure`, and method calls whose
type arguments make them return `Result<T>` with a `Failure` as `T`, such as `result.Map(_ => Failure.Unknown)`. That
includes members of generic types and of extension blocks. To fail from inside a step, use `Bind` instead. It does not
see a `Result<Failure>` nested inside another return type, such as a generic method returning `Task<Result<T>>`, or
one reached through a property or a delegate.

`Map` stores whatever its delegate returns as the success value, so a `Failure` returned under a broader type such as
`object` would become a success holding a failure. The analyzer reports that as a compile error too:

```csharp
Result<object> mapped = result.Map<int, object>(_ => Failure.Unknown); // CSPP0002 compile error
```

To fail from inside a step, use `Bind` and return the failure as a `Result`. The check looks at lambdas and method
groups passed to `Map` directly, by the static type of what they return. It does not see a delegate stored in a
variable first, or a `Failure` returned as `object`.

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

## Benchmarks

`benchmarks/Cspp.Results.Benchmarks` uses [BenchmarkDotNet](https://benchmarkdotnet.org) to compare `Cspp.Results`
with other result libraries: FluentResults, ErrorOr, CSharpFunctionalExtensions, OneOf and Ardalis.Result. Each library
writes the same scenarios in its own idiomatic style, in `Libraries/`, and every failure carries a code and a message:

| Benchmark            | What it measures                                                                                     |
|----------------------|------------------------------------------------------------------------------------------------------|
| `CreateBenchmarks`   | Creating a success or a failure, including the failure's error object.                               |
| `MatchBenchmarks`    | Reading an existing success or failure into a value.                                                 |
| `PipelineBenchmarks` | Parse, `Bind` a validation step, `Map` and read the outcome, ending in success, a parse failure or a validation failure. Also runs the same steps with exceptions as a reference point. |

`Cspp` is the baseline, so the `Ratio` column shows each library relative to it. `MatchBenchmarks` and
`PipelineBenchmarks` fail the run if the libraries don't all return the same value.

```sh
dotnet run -c Release --project benchmarks/Cspp.Results.Benchmarks                         # pick interactively
dotnet run -c Release --project benchmarks/Cspp.Results.Benchmarks -- --filter '*'         # run everything
dotnet run -c Release --project benchmarks/Cspp.Results.Benchmarks -- --filter '*Pipeline*'
```

Results are written to `BenchmarkDotNet.Artifacts/`. BenchmarkDotNet 0.15 doesn't recognize the `net11.0` runtime, so
the project uses the 0.16 preview until a stable release supports it.

### Results

Mean time and allocated memory per operation, from one run on an AMD Ryzen AI 7 350 laptop, Windows 11, .NET 11.0.0,
BenchmarkDotNet 0.16.0-preview.2. Differences under about 1 ns are within noise, and BenchmarkDotNet reported bimodal
distributions for a few cases, so run the benchmarks on your own hardware before relying on small gaps.

**Pipeline** (parse, `Bind` validate, `Map`, read the outcome):

| Library                    | `42` (success)   | `abc` (parse fails) | `-5` (validation fails) |
|----------------------------|------------------|---------------------|-------------------------|
| Cspp.Results               | 10.6 ns, 0 B     | 9.7 ns, 32 B        | 12.0 ns, 32 B           |
| FluentResults              | 111 ns, 784 B    | 177 ns, 1048 B      | 223 ns, 1216 B          |
| ErrorOr                    | 15.3 ns, 0 B     | 14.9 ns, 88 B       | 23.2 ns, 88 B           |
| CSharpFunctionalExtensions | 16.1 ns, 0 B     | 10.7 ns, 32 B       | 16.5 ns, 32 B           |
| OneOf                      | 16.1 ns, 0 B     | 11.5 ns, 32 B       | 18.7 ns, 32 B           |
| Ardalis.Result             | 20.2 ns, 128 B   | 30.8 ns, 264 B      | 34.7 ns, 264 B          |
| Exceptions                 | 7.6 ns, 0 B      | 854 ns, 200 B       | 1234 ns, 200 B          |

**Create** (a success, or a failure including its error object):

| Library                    | Success         | Failure         |
|----------------------------|-----------------|-----------------|
| Cspp.Results               | 1.39 ns, 0 B    | 5.08 ns, 32 B   |
| FluentResults              | 9.35 ns, 112 B  | 30.5 ns, 408 B  |
| ErrorOr                    | 0.92 ns, 0 B    | 6.99 ns, 88 B   |
| CSharpFunctionalExtensions | 1.10 ns, 0 B    | 3.33 ns, 32 B   |
| OneOf                      | 1.08 ns, 0 B    | 3.34 ns, 32 B   |
| Ardalis.Result             | 4.66 ns, 64 B   | 12.6 ns, 136 B  |

**Match** (read an existing result into a value):

| Library                    | Success         | Failure         |
|----------------------------|-----------------|-----------------|
| Cspp.Results               | 1.82 ns, 0 B    | 1.38 ns, 0 B    |
| FluentResults              | 11.1 ns, 96 B   | 9.62 ns, 88 B   |
| ErrorOr                    | 1.65 ns, 0 B    | 1.16 ns, 0 B    |
| CSharpFunctionalExtensions | 1.63 ns, 0 B    | 2.13 ns, 0 B    |
| OneOf                      | 1.18 ns, 0 B    | 1.90 ns, 0 B    |
| Ardalis.Result             | 0.86 ns, 0 B    | 0.87 ns, 0 B    |

In short:

- In the pipeline, Cspp.Results was the fastest result library on all three paths (CSharpFunctionalExtensions is within
  noise when parsing fails), and it allocates only the `Failure` on a failure path.
- Creating a failure takes about 5 ns, against about 3.3 ns for CSharpFunctionalExtensions and OneOf with the same
  32 B; the gap is likely the `Failure` constructor's validation of its code and error.
- Creating a success takes about 1 ns in every library except FluentResults and Ardalis.Result, which allocate.
  Matching takes about 2 ns or less everywhere except FluentResults, which allocates on every read.
- Exceptions are cheapest when nothing fails, because there is no wrapper, but about 90 to 105 times slower than
  Cspp.Results when something does.

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
src/Cspp.Results/                    Result library
src/Cspp.Results.Analyzers/          Roslyn analyzer shipped inside the Cspp.Results package
test/Cspp.Results.Tests/             NUnit tests
benchmarks/Cspp.Results.Benchmarks/  BenchmarkDotNet comparison with other result libraries
```

## License

Licensed under the [MIT License](LICENSE).