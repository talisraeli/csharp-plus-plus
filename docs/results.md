# Cspp.Results

A `Result` type for returning success or failure without throwing exceptions. It is built on C# union types (`[Union]` /
`IUnion`).

| Type               | Description                                                                                                                                |
|--------------------|--------------------------------------------------------------------------------------------------------------------------------------------|
| `Result`           | A union of `Success` or `Failure`.                                                                                                         |
| `Result<T>`        | A union of a `T` value or `Failure`.                                                                                                       |
| `Success`          | A singleton, obtained through `Result.Success`, marking success for the non-generic `Result`.                                              |
| `Failure`          | A `Code` and an `Error` message, neither null nor whitespace. Predefined: `Failure.Unknown`, `Failure.Uninitialized`, `Failure.Exception`. |
| `AggregateFailure` | A `Failure` holding several failures in `Failures`, with the code `Failure.Multiple`. `Result.CombineAll` returns it.                      |

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

## Query syntax

When a later step needs values from several earlier ones, nested `Bind` calls get deep. C# query syntax flattens them:

```csharp
Result<Order> order =
    from id in Parse(text)
    from customer in FindCustomer(id)  // Result<Customer> FindCustomer(int id)
    from product in FindProduct(sku)   // Result<Product> FindProduct(string sku)
    select new Order(customer, product);
```

Each `from` is a step that can fail, and the first failure ends the query. `let` works too. There is no `where`: a
condition that doesn't hold has no failure to report, so use `Ensure` instead.

## Validating and combining results

To check a condition on the success value, use `Ensure`. A success that fails the check becomes the failure you pass, or
the one a delegate builds from the value; a failure passes through without checking:

```csharp
Result<Order> order = Parse(text)
    .Ensure(o => o.Quantity > 0, new Failure("Order.Quantity", "Quantity must be positive."))
    .Ensure(o => o.Total <= limit, o => new Failure("Order.Limit", $"{o.Total} is over the limit."));
```

To turn several results into one, use `Result.Combine`, which stops at the first failure, or `Result.CombineAll`, which
reports every failure. Both take any number of results, or a collection:

```csharp
Result valid = Result.Combine(ValidateName(name), ValidateEmail(email));        // success, or the first failure
Result<IReadOnlyList<int>> ids = Result.Combine(texts.Select(Parse));           // every value, or the first failure

Result checkedAll = Result.CombineAll(ValidateName(name), ValidateEmail(email));
string message = checkedAll switch
{
    Success => "Valid.",
    AggregateFailure all => string.Join(" ", all.Failures.Select(f => f.Error)), // several failures
    Failure one => one.Error,                                                    // exactly one failure
};
```

`CombineAll` returns a single failure unchanged, and an `AggregateFailure` only when more than one result fails. Two
aggregate failures are equal when they hold equal failures in the same order.

## Side effects and changing a failure

To log or record metrics without breaking the chain, use `Tap` for a success and `TapFailure` for a failure; both return
the result unchanged. To replace a failure, for example to add context or turn a low-level code into a domain one, use
`MapFailure`; a success passes through unchanged:

```csharp
Result<User> user = FindUser(id)
    .TapFailure(failure => logger.LogWarning("Lookup failed: {Code}", failure.Code))
    .MapFailure(failure => new Failure("User.NotFound", failure.Error))
    .Tap(found => metrics.Increment("users.found"));
```

The non-generic `Result` has the same three members; its `Tap` takes a delegate with no value. `MapFailure` throws
`ArgumentNullException` if its delegate returns `null`.

## Turning exceptions into failures

To call code that throws, such as `int.Parse` or a file API, use `Result.Try`. It returns the value, or a failure if the
code throws. The failure is `Failure.Exception`, unless you pass `onException`, which receives the exception and returns
the failure to use, for example after logging it:

```csharp
Result<int> parsed = Result.Try(() => int.Parse(text));
Result<int> quantity = Result.Try(() => int.Parse(text), _ => new Failure("Quantity.Invalid", "Not a number."));
Result saved = Result.Try(
    () => File.WriteAllText(path, content),
    exception =>
    {
        logger.LogError(exception, "Save failed");
        return new Failure("Save.Failed", exception.Message);
    });
```

`Result.TryAsync` does the same for asynchronous code:

```csharp
Result<string> content = await Result.TryAsync(() => File.ReadAllTextAsync(path, cancellationToken));
```

`Try` catches every exception, including `OperationCanceledException`. `TryAsync` catches every exception except
`OperationCanceledException`, so a cancelled operation still ends as cancelled. A lambda that assigns a value, such as
`() => done = true`, returns that value and so creates a `Result<T>`; write `() => { done = true; }` for a `Result`.
`onException` must not return `null`.

## Asynchronous steps

For a step that runs asynchronously, such as a database or HTTP call, use `MapAsync`, `BindAsync` and `MatchAsync`.
`Map`, `Bind` and `Match` (and the `Async` versions) also work directly on a `Task<Result<T>>`, so an async chain needs
only one `await`:

```csharp
Result<Receipt> receipt = await FindOrderAsync(id)          // Task<Result<Order>>
    .Bind(Validate)                                         // Result<Order> Validate(Order order)
    .BindAsync(order => ChargeAsync(order, cancellationToken)); // Task<Result<Receipt>> ChargeAsync(...)
```

The delegates don't take a `CancellationToken`; to cancel a step, pass the token in from the enclosing method, as
above. A `null` delegate or task throws `ArgumentNullException` when the member is called, not when its task is awaited.

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