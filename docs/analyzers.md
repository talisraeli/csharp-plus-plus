# Analyzers

The `Cspp.Results` package includes a Roslyn analyzer, so these checks run in every project that references it without
a separate package. Both report compile errors.

| ID                                              | Reports                                                    |
|-------------------------------------------------|------------------------------------------------------------|
| [CSPP0001](#cspp0001-result-of-failure)         | A `Result<T>` whose `T` is `Failure` or derives from it.   |
| [CSPP0002](#cspp0002-map-returns-a-failure)     | A `Map` delegate that returns a `Failure`.                 |

## CSPP0001: Result of Failure

`T` must not be `Failure` or a type derived from it, because then a success value and a failure would look the same:

```csharp
Result<Failure> result = default; // CSPP0001 compile error
```

The check covers types written in source, including type parameters constrained to `Failure`, and method calls whose
type arguments make them return `Result<T>` with a `Failure` as `T`, such as `result.Map(_ => Failure.Unknown)`. That
includes members of generic types and of extension blocks. To fail from inside a step, use `Bind` instead.

It does not see a `Result<Failure>` nested inside another return type, such as a generic method returning
`Task<Result<T>>`, or one reached through a property or a delegate.

## CSPP0002: Map returns a Failure

`Map` stores whatever its delegate returns as the success value, so a `Failure` returned under a broader type such as
`object` would become a success holding a failure:

```csharp
Result<object> mapped = result.Map<int, object>(_ => Failure.Unknown); // CSPP0002 compile error
```

To fail from inside a step, use `Bind` and return the failure as a `Result`.

The check looks at lambdas and method groups passed to `Map` directly, by the static type of what they return. It does
not see a delegate stored in a variable first, or a `Failure` returned as `object`.