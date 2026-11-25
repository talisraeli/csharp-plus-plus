# Analyzers

The `Cspp.Results` package includes a Roslyn analyzer, so these checks run in every project that references it without
a separate package. All of them report compile errors.

| ID                                                          | Reports                                                        |
|-------------------------------------------------------------|----------------------------------------------------------------|
| [CSPP0001](#cspp0001-result-of-failure)                     | A `Result<T>` whose `T` is `Failure` or derives from it.       |
| [CSPP0002](#cspp0002-map-returns-a-failure)                 | A `Map` delegate that returns a `Failure`.                     |
| [CSPP0003](#cspp0003-a-failure-becomes-a-success-value)     | A `Failure` cast to a broader type, becoming a success value.  |

## CSPP0001: Result of Failure

`T` must not be `Failure` or a type derived from it, because then a success value and a failure would look the same:

```csharp
Result<Failure> result = default; // CSPP0001 compile error
```

The check covers types written in source, including type parameters constrained to `Failure`, and method calls whose
type arguments make them return `Result<T>` with a `Failure` as `T`, such as `result.Map(_ => Failure.Unknown)`. That
includes members of generic types and of extension blocks, and calls that return a `Task` or `ValueTask` of such a
result, such as `result.MapAsync(_ => GetFailureAsync())`. To fail from inside a step, use `Bind` instead.

It does not see a `Result<Failure>` nested inside other return types, such as a generic method returning
`List<Result<T>>`, or one reached through a property or a delegate.

## CSPP0002: Map returns a Failure

`Map` stores whatever its delegate returns as the success value, so a `Failure` returned under a broader type such as
`object` would become a success holding a failure:

```csharp
Result<object> mapped = result.Map<int, object>(_ => Failure.Unknown); // CSPP0002 compile error
```

To fail from inside a step, use `Bind` and return the failure as a `Result`.

The check covers `Map` on `Result<T>`, on the non-generic `Result` and on a `Task<Result<T>>`, `MapAsync`, where it
looks at what an `async` lambda returns, and query syntax: `Select`, which a `select` clause calls, and the result
selector of `SelectMany`. `SelectMany`'s first delegate returns a `Result`, so returning a `Failure` there is a failed
step and isn't reported. It looks at lambdas and method groups passed to these members directly, by
the static type of what they return. It does not see a delegate stored in a variable first, or a `Failure` returned as
`object`.

## CSPP0003: A Failure becomes a success value

A `Failure` converts to the failure case of `Result<T>`. Cast to a broader type such as `object` first, it converts to
the success case instead, so the result is a success holding a failure:

```csharp
Result<object> result = (object)Failure.Unknown;              // CSPP0003 compile error
Result<object> created = new Result<object>((object)failure); // CSPP0003 compile error
Result<object> failed = Failure.Unknown;                      // fine: a failure
```

Convert the `Failure` itself, without the cast. The check covers implicit conversions and the `Result<T>` constructor,
wherever they are written: assignments, return values, lambdas such as a `Bind` step, and the branches of `?:`, `??` and
`switch` expressions. It looks through casts that keep the same object.

It only sees a value that is a `Failure` where the conversion is written. A value that reaches it typed as `object` or
as a type parameter, such as one stored in an `object` variable first or passed through a generic helper like
`Wrap<object>(Failure.Unknown)`, is out of its reach. When `T` is itself a `Failure`, CSPP0001 reports the type instead.