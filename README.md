# C#++

Small building blocks that extend C# with patterns found in other languages.

## Cspp.Results

[![NuGet](https://img.shields.io/nuget/v/Cspp.Results?logo=nuget)](https://www.nuget.org/packages/Cspp.Results)

A `Result` type for returning success or failure without throwing exceptions. It is built on C# union types (`[Union]` /
`IUnion`).

| Type        | Description                                                                                                    |
|-------------|----------------------------------------------------------------------------------------------------------------|
| `Result`    | A union of `Success` or `Error`.                                                                               |
| `Result<T>` | A union of a `T` value or `Error`.                                                                             |
| `Success`   | A singleton (`Success.Instance`) marking success for the non-generic `Result`.                                 |
| `Error`     | A `Code` and a `Message`, neither of which can be null or whitespace. Predefined: `Error.Unknown`, `Error.Uninitialized`. |

### Usage

```csharp
using Cspp.Results;

Result<int> Parse(string text) =>
    int.TryParse(text, out int value)
        ? value
        : new Error("Parse.Invalid", $"'{text}' is not a number.");

string message = Parse("42") switch
{
    int value => $"Parsed {value}",
    Error error => $"{error.Code}: {error.Message}",
};
```

A value or an `Error` converts to `Result<T>` implicitly, and union pattern matching works on the case directly, so no
wrapping or casting is needed. Because the union's cases are known, the switch is exhaustive without a discard (`_`)
arm. If you only need to check which case you have, `result.IsSuccess` and `result.IsFailure` are also available, and
`TryGetValue` gives you the case without pattern matching.

For the non-generic `Result`, return `Result.Success` or an `Error`.

A success value may be `null` when `T` allows it (for example `Result<string?>`).

A `default` result is a failure carrying `Error.Uninitialized`, which has its own code and message so it can't be
confused with `Error.Unknown`.

### Analyzer

`T` must not be `Error` or a type derived from it, because then a success value and a failure would look the same. The
package includes an analyzer that reports this as a compile error:

```csharp
Result<Error> result = default; // error CSPP0001
```

The check covers types written in source, including type parameters constrained to `Error`. It does not see a
`Result<Error>` that only exists through generic inference, such as a generic method called with an `Error` argument.

## Requirements

- .NET 11 SDK. The project targets `net11.0` and needs a C# version that supports union types.

## Building and testing

```sh
dotnet build CSharpPlusPlus.slnx
dotnet test CSharpPlusPlus.slnx
```

Tests use NUnit with the constraint model (`Assert.That`). There is one fixture per class under test, and test names
follow `Member_Scenario_ExpectedResult`.

The build treats warnings as errors and runs StyleCop analyzers. Package versions are managed centrally in
`Directory.Packages.props`.

## Repository layout

```
src/Cspp.Results/            Result library
src/Cspp.Results.Analyzers/  Roslyn analyzer shipped inside the Cspp.Results package
test/Cspp.Results.Tests/     NUnit tests
```

## License

Licensed under the [MIT License](LICENSE).