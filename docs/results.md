# Cspp.Results

A `Result` type for returning success or failure without throwing exceptions. It is built on C# union types (`[Union]` /
`IUnion`).

| Type        | Description                                                                                                           |
|-------------|-----------------------------------------------------------------------------------------------------------------------|
| `Result`    | A union of `Success` or `Failure`.                                                                                    |
| `Result<T>` | A union of a `T` value or `Failure`.                                                                                  |
| `Success`   | A singleton, obtained through `Result.Success`, marking success for the non-generic `Result`.                         |
| `Failure`   | A `Code` and an `Error` message, neither null nor whitespace. Predefined: `Failure.Unknown`, `Failure.Uninitialized`. |

## Creating and matching results

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

## Chaining steps

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

To fail from inside a step, use `Bind` and return the failure. Returning a `Failure` from `Map` is a compile error; see
[Analyzers](analyzers.md).

## The non-generic `Result`

For the non-generic `Result`, return `Result.Success` or a `Failure`. It has `Match`, `Map` and `Bind` too, with
delegates that take no value, so steps returning `Result` and `Result<T>` chain together:

```csharp
Result Validate(Order order);
Result<Receipt> Charge(Order order);

Result<Receipt> receipt = FindOrder(id)
    .Bind(order => Validate(order).Map(() => order))  // keep the order once validation passes
    .Bind(Charge);

Result saved = Save(order).Bind(() => Notify(order)); // Result Save(Order), Result Notify(Order)
string status = saved.Match(() => "Saved", failure => failure.Error);
```

`Result<T>.Bind` also takes a step that returns a `Result`, and `ToResult()` drops a `Result<T>`'s value, keeping only
whether it succeeded.

## Null and default values

A success value may be `null` when `T` allows it (for example `Result<string?>`). It is still a success: `IsSuccess` is
`true`, and `Match`, `Map`, `Bind` and `GetValueOrDefault` pass the `null` to the success delegate or return it.

In a `switch`, a `null` value matches a `null` arm, not the type arm, as with any other `null`:

```csharp
string text = result switch
{
    null => "no value",
    string value => value,
    Failure failure => failure.Error,
};
```

Without the `null` arm the compiler warns (CS8655), and a `null` value throws `SwitchExpressionException`. C# doesn't
allow a nullable type pattern such as `string? value`, so to handle `null` and non-null values in one place, use
`Match`. For the same reason, `HasValue` and `TryGetValue(out T)` return `false` for a `null` value; check `IsSuccess`
instead.

A `default` result is a failure carrying `Failure.Uninitialized`, which has its own code and error so it can't be
confused with `Failure.Unknown`. It is equal to, and has the same hash code as, a result created from
`Failure.Uninitialized`.