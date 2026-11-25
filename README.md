# C#++

[![CI](https://github.com/talisraeli/csharp-plus-plus/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/talisraeli/csharp-plus-plus/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Cspp.Results?logo=nuget)](https://www.nuget.org/packages/Cspp.Results)

Small building blocks that extend C# with patterns found in other languages.

## Cspp.Results

A `Result` type for returning success or failure without throwing exceptions, built on C# union types. It needs
.NET 11 (`net11.0`).

```sh
dotnet add package Cspp.Results
```

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

Result<Order> order = Parse(text)
    .Bind(FindCustomer)                     // Result<Customer> FindCustomer(int id)
    .Map(customer => new Order(customer));  // a failure from Parse or FindCustomer passes through
```

A built-in analyzer turns misuse, such as `Result<Failure>`, into compile errors.

## Documentation

- [Cspp.Results](https://github.com/talisraeli/csharp-plus-plus/blob/main/docs/results.md): the types, matching, and
  chaining with `Map`, `Bind`, `Match` and `GetValueOrDefault`.
- [Analyzers](https://github.com/talisraeli/csharp-plus-plus/blob/main/docs/analyzers.md): CSPP0001, CSPP0002 and CSPP0003.
- [Benchmarks](https://github.com/talisraeli/csharp-plus-plus/blob/main/docs/benchmarks.md): performance compared with
  FluentResults, ErrorOr, CSharpFunctionalExtensions, OneOf, Ardalis.Result and exceptions.
- [Development](https://github.com/talisraeli/csharp-plus-plus/blob/main/docs/development.md): building, testing and
  the repository layout.
- [Releasing](https://github.com/talisraeli/csharp-plus-plus/blob/main/docs/releasing.md): CI and publishing to NuGet.

## License

Licensed under the [MIT License](https://github.com/talisraeli/csharp-plus-plus/blob/main/LICENSE).