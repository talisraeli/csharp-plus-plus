using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(ResultExtensions))]
public partial class ResultExtensionsTests
{
    private static readonly Failure ParseFailure = new("Parse.Invalid", "Not a number.");

    private static readonly Failure NegativeFailure = new("Validate.Negative", "The number is negative.");

    [Test]
    public void GetValueOrDefault_OnSuccess_ReturnsValue()
    {
        Result<int> result = 42;

        Assert.That(result.GetValueOrDefault(-1), Is.EqualTo(42));
    }

    [Test]
    public void GetValueOrDefault_OnFailure_ReturnsDefaultValue()
    {
        Result<int> result = ParseFailure;

        Assert.That(result.GetValueOrDefault(-1), Is.EqualTo(-1));
    }

    [Test]
    public void GetValueOrDefault_OnDefault_ReturnsDefaultValue()
    {
        Result<int> result = default;

        Assert.That(result.GetValueOrDefault(-1), Is.EqualTo(-1));
    }

    [Test]
    public void GetValueOrDefault_OnFailureWithoutDefaultValue_ReturnsDefault()
    {
        Result<int> result = ParseFailure;

        Assert.That(result.GetValueOrDefault(), Is.Zero);
    }

    [Test]
    public void GetValueOrDefault_OnReferenceTypeFailureWithoutDefaultValue_ReturnsNull()
    {
        Result<string> result = ParseFailure;

        Assert.That(result.GetValueOrDefault(), Is.Null);
    }

    [Test]
    public void GetValueOrDefault_OnSuccessWithoutDefaultValue_ReturnsValue()
    {
        Result<string> result = "value";

        Assert.That(result.GetValueOrDefault(), Is.EqualTo("value"));
    }

    [Test]
    public void GetValueOrDefault_OnSuccessWithNullValue_ReturnsNull()
    {
        Result<string?> result = (string?)null;

        Assert.That(result.GetValueOrDefault("default"), Is.Null);
    }

    [Test]
    public void Match_OnSuccess_CallsOnSuccessWithValue()
    {
        Result<int> result = 42;

        string message = result.Match(value => $"value {value}", failure => $"failure {failure.Code}");

        Assert.That(message, Is.EqualTo("value 42"));
    }

    [Test]
    public void Match_OnSuccessWithNullValue_CallsOnSuccessWithNull()
    {
        Result<string?> result = (string?)null;

        string message = result.Match(value => value ?? "null", failure => $"failure {failure.Code}");

        Assert.That(message, Is.EqualTo("null"));
    }

    [Test]
    public void Map_OnSuccessWithNullValue_CallsMapWithNull()
    {
        Result<string?> result = (string?)null;

        Result<bool> mapped = result.Map(value => value is null);

        Assert.That(mapped.TryGetValue(out bool isNull) && isNull, Is.True);
    }

    [Test]
    public void Bind_OnSuccessWithNullValue_CallsBindWithNull()
    {
        Result<string?> result = (string?)null;

        Result<bool> bound = result.Bind(value => (Result<bool>)(value is null));

        Assert.That(bound.TryGetValue(out bool isNull) && isNull, Is.True);
    }

    [Test]
    public void Match_OnFailure_CallsOnFailureWithFailure()
    {
        Result<int> result = ParseFailure;

        Failure? received = result.Match(_ => (Failure?)null, failure => failure);

        Assert.That(received, Is.SameAs(ParseFailure));
    }

    [Test]
    public void Match_OnDefault_CallsOnFailureWithUninitialized()
    {
        Result<int> result = default;

        Failure? received = result.Match(_ => (Failure?)null, failure => failure);

        Assert.That(received, Is.SameAs(Failure.Uninitialized));
    }

    [Test]
    public void Match_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Match(null!, _ => 0),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onSuccess"));
    }

    [Test]
    public void Match_WithNullOnFailure_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Match(value => value, null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onFailure"));
    }

    [Test]
    public void Map_OnSuccess_ReturnsSuccessWithMappedValue()
    {
        Result<int> result = 42;

        Result<string> mapped = result.Map(value => value.ToString());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.TryGetValue(out string? value), Is.True);
            Assert.That(value, Is.EqualTo("42"));
        }
    }

    [Test]
    public void Map_OnFailure_ReturnsSameFailureWithoutCallingMap()
    {
        Result<int> result = ParseFailure;
        bool called = false;

        Result<string> mapped = result.Map(value =>
        {
            called = true;
            return value.ToString();
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void Map_OnDefault_ReturnsUninitializedFailure()
    {
        Result<int> result = default;

        Result<string> mapped = result.Map(value => value.ToString());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(Failure.Uninitialized));
        }
    }

    [Test]
    public void Map_WithNullMap_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Map((Func<int, string>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public void Bind_OnSuccessWhenBindSucceeds_ReturnsBoundSuccess()
    {
        Result<string> result = "42";

        Result<int> bound = result.Bind(Parse);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }
    }

    [Test]
    public void Bind_OnSuccessWhenBindFails_ReturnsBindFailure()
    {
        Result<string> result = "abc";

        Result<int> bound = result.Bind(Parse);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(ParseFailure));
        }
    }

    [Test]
    public void Bind_OnFailure_ReturnsSameFailureWithoutCallingBind()
    {
        Failure original = new("Read.Failed", "Could not read the input.");
        Result<string> result = original;
        bool called = false;

        Result<int> bound = result.Bind(text =>
        {
            called = true;
            return Parse(text);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(original));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void Bind_OnDefault_ReturnsUninitializedFailure()
    {
        Result<string> result = default;

        Result<int> bound = result.Bind(Parse);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(Failure.Uninitialized));
        }
    }

    [Test]
    public void Bind_WithNullBind_ThrowsArgumentNullException()
    {
        Result<string> result = "42";

        Assert.That(
            () => result.Bind((Func<string, Result<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bind"));
    }

    [Test]
    public void BindThenMap_WhenChained_StopsAtFirstFailure()
    {
        Result<string> success = "21";
        Result<string> failure = "abc";

        int doubled = success.Bind(Parse).Map(value => value * 2).GetValueOrDefault(-1);
        int fallback = failure.Bind(Parse).Map(value => value * 2).GetValueOrDefault(-1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(doubled, Is.EqualTo(42));
            Assert.That(fallback, Is.EqualTo(-1));
        }
    }

    [Test]
    public void BindToResult_OnSuccess_ReturnsBindResult()
    {
        Result<int> result = -1;

        Result bound = result.Bind(Validate);

        Assert.That(bound, Is.EqualTo((Result)NegativeFailure));
    }

    [Test]
    public void BindToResult_OnFailure_ReturnsSameFailureWithoutCallingBind()
    {
        Result<int> result = ParseFailure;
        bool called = false;

        Result bound = result.Bind(value =>
        {
            called = true;
            return Validate(value);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void BindToResult_WithNullBind_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Bind((Func<int, Result>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bind"));
    }

    [Test]
    public void ToResult_OnSuccess_ReturnsSuccess()
    {
        Result<int> result = 42;

        Assert.That(result.ToResult().IsSuccess, Is.True);
    }

    [Test]
    public void ToResult_OnFailure_ReturnsSameFailure()
    {
        Result<int> result = ParseFailure;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ToResult().TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(ParseFailure));
        }
    }

    [Test]
    public void ToResult_OnDefault_ReturnsUninitializedFailure()
    {
        Result<int> result = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.ToResult().TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(Failure.Uninitialized));
        }
    }

    [Test]
    public void MatchNonGeneric_OnSuccess_CallsOnSuccess()
    {
        Result result = Result.Success;

        string message = result.Match(() => "success", failure => $"failure {failure.Code}");

        Assert.That(message, Is.EqualTo("success"));
    }

    [Test]
    public void MatchNonGeneric_OnFailure_CallsOnFailureWithFailure()
    {
        Result result = ParseFailure;

        Failure? received = result.Match(() => (Failure?)null, failure => failure);

        Assert.That(received, Is.SameAs(ParseFailure));
    }

    [Test]
    public void MatchNonGeneric_OnDefault_CallsOnFailureWithUninitialized()
    {
        Result result = default;

        Failure? received = result.Match(() => (Failure?)null, failure => failure);

        Assert.That(received, Is.SameAs(Failure.Uninitialized));
    }

    [Test]
    public void MatchNonGeneric_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        Result result = Result.Success;

        Assert.That(
            () => result.Match(null!, _ => 0),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onSuccess"));
    }

    [Test]
    public void MatchNonGeneric_WithNullOnFailure_ThrowsArgumentNullException()
    {
        Result result = Result.Success;

        Assert.That(
            () => result.Match(() => 0, null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onFailure"));
    }

    [Test]
    public void MapNonGeneric_OnSuccess_ReturnsMappedValue()
    {
        Result result = Result.Success;

        Result<int> mapped = result.Map(() => 42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }
    }

    [Test]
    public void MapNonGeneric_OnFailure_ReturnsSameFailureWithoutCallingMap()
    {
        Result result = ParseFailure;
        bool called = false;

        Result<int> mapped = result.Map(() =>
        {
            called = true;
            return 42;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mapped.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void MapNonGeneric_WithNullMap_ThrowsArgumentNullException()
    {
        Result result = Result.Success;

        Assert.That(
            () => result.Map((Func<int>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public void BindNonGeneric_OnSuccess_ReturnsBindResult()
    {
        Result result = Result.Success;

        Result bound = result.Bind(() => Validate(-1));

        Assert.That(bound, Is.EqualTo((Result)NegativeFailure));
    }

    [Test]
    public void BindNonGeneric_OnFailure_ReturnsSameFailureWithoutCallingBind()
    {
        Result result = ParseFailure;
        bool called = false;

        Result bound = result.Bind(() =>
        {
            called = true;
            return Validate(1);
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound, Is.EqualTo((Result)ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void BindNonGeneric_WithNullBind_ThrowsArgumentNullException()
    {
        Result result = Result.Success;

        Assert.That(
            () => result.Bind((Func<Result>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bind"));
    }

    [Test]
    public void BindNonGenericToValue_OnSuccess_ReturnsBindResult()
    {
        Result result = Result.Success;

        Result<int> bound = result.Bind(() => Parse("42"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
        }
    }

    [Test]
    public void BindNonGenericToValue_OnFailure_ReturnsSameFailureWithoutCallingBind()
    {
        Result result = ParseFailure;
        bool called = false;

        Result<int> bound = result.Bind(() =>
        {
            called = true;
            return Parse("42");
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(bound.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void BindNonGenericToValue_WithNullBind_ThrowsArgumentNullException()
    {
        Result result = Result.Success;

        Assert.That(
            () => result.Bind((Func<Result<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("bind"));
    }

    [Test]
    public void BindAndMap_WhenMixingResultAndResultOfT_StopsAtFirstFailure()
    {
        static Result<int> Run(string text)
        {
            return Parse(text)
                .Bind(value => Validate(value).Map(() => value))
                .Map(value => value * 2);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Run("21").GetValueOrDefault(), Is.EqualTo(42));
            Assert.That(Run("-1").Match(_ => string.Empty, failure => failure.Code), Is.EqualTo(NegativeFailure.Code));
            Assert.That(Run("abc").Match(_ => string.Empty, failure => failure.Code), Is.EqualTo(ParseFailure.Code));
        }
    }

    [Test]
    public void Tap_OnSuccess_CallsOnSuccessAndReturnsSameResult()
    {
        Result<int> result = 42;
        int received = 0;

        Result<int> tapped = result.Tap(value => received = value);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(received, Is.EqualTo(42));
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void Tap_OnFailure_ReturnsSameResultWithoutCallingOnSuccess()
    {
        Result<int> result = ParseFailure;
        bool called = false;

        Result<int> tapped = result.Tap(_ => called = true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(called, Is.False);
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void Tap_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Tap(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onSuccess"));
    }

    [Test]
    public void TapFailure_OnFailure_CallsOnFailureAndReturnsSameResult()
    {
        Result<int> result = ParseFailure;
        Failure? received = null;

        Result<int> tapped = result.TapFailure(failure => received = failure);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(received, Is.SameAs(ParseFailure));
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void TapFailure_OnDefault_CallsOnFailureWithUninitialized()
    {
        Result<int> result = default;
        Failure? received = null;

        result.TapFailure(failure => received = failure);

        Assert.That(received, Is.SameAs(Failure.Uninitialized));
    }

    [Test]
    public void TapFailure_OnSuccess_ReturnsSameResultWithoutCallingOnFailure()
    {
        Result<int> result = 42;
        bool called = false;

        Result<int> tapped = result.TapFailure(_ => called = true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(called, Is.False);
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void TapFailure_WithNullOnFailure_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.TapFailure(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onFailure"));
    }

    [Test]
    public void MapFailure_OnFailure_ReturnsMappedFailure()
    {
        Result<int> result = ParseFailure;

        Result<int> mapped = result.MapFailure(_ => NegativeFailure);

        Assert.That(mapped, Is.EqualTo((Result<int>)NegativeFailure));
    }

    [Test]
    public void MapFailure_OnDefault_CallsMapWithUninitialized()
    {
        Result<int> result = default;
        Failure? received = null;

        result.MapFailure(failure => received = failure);

        Assert.That(received, Is.SameAs(Failure.Uninitialized));
    }

    [Test]
    public void MapFailure_OnSuccess_ReturnsSameResultWithoutCallingMap()
    {
        Result<int> result = 42;
        bool called = false;

        Result<int> mapped = result.MapFailure(failure =>
        {
            called = true;
            return failure;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(called, Is.False);
            Assert.That(mapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void MapFailure_WithNullMap_ThrowsArgumentNullException()
    {
        Result<int> result = ParseFailure;

        Assert.That(
            () => result.MapFailure(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public void MapFailure_WhenMapReturnsNull_ThrowsArgumentNullException()
    {
        Result<int> result = ParseFailure;

        Assert.That(
            () => result.MapFailure(_ => null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public void TapNonGeneric_OnSuccess_CallsOnSuccessAndReturnsSameResult()
    {
        Result result = Result.Success;
        bool called = false;

        Result tapped = result.Tap(() => called = true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(called, Is.True);
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void TapNonGeneric_OnFailure_ReturnsSameResultWithoutCallingOnSuccess()
    {
        Result result = ParseFailure;
        bool called = false;

        Result tapped = result.Tap(() => called = true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(called, Is.False);
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void TapNonGeneric_WithNullOnSuccess_ThrowsArgumentNullException()
    {
        Result result = Result.Success;

        Assert.That(
            () => result.Tap(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onSuccess"));
    }

    [Test]
    public void TapFailureNonGeneric_OnFailure_CallsOnFailureAndReturnsSameResult()
    {
        Result result = ParseFailure;
        Failure? received = null;

        Result tapped = result.TapFailure(failure => received = failure);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(received, Is.SameAs(ParseFailure));
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void TapFailureNonGeneric_OnSuccess_ReturnsSameResultWithoutCallingOnFailure()
    {
        Result result = Result.Success;
        bool called = false;

        Result tapped = result.TapFailure(_ => called = true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(called, Is.False);
            Assert.That(tapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void TapFailureNonGeneric_WithNullOnFailure_ThrowsArgumentNullException()
    {
        Result result = Result.Success;

        Assert.That(
            () => result.TapFailure(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onFailure"));
    }

    [Test]
    public void MapFailureNonGeneric_OnFailure_ReturnsMappedFailure()
    {
        Result result = ParseFailure;

        Result mapped = result.MapFailure(_ => NegativeFailure);

        Assert.That(mapped, Is.EqualTo((Result)NegativeFailure));
    }

    [Test]
    public void MapFailureNonGeneric_OnSuccess_ReturnsSameResultWithoutCallingMap()
    {
        Result result = Result.Success;
        bool called = false;

        Result mapped = result.MapFailure(failure =>
        {
            called = true;
            return failure;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(called, Is.False);
            Assert.That(mapped, Is.EqualTo(result));
        }
    }

    [Test]
    public void MapFailureNonGeneric_WithNullMap_ThrowsArgumentNullException()
    {
        Result result = ParseFailure;

        Assert.That(
            () => result.MapFailure(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [Test]
    public void MapFailureNonGeneric_WhenMapReturnsNull_ThrowsArgumentNullException()
    {
        Result result = ParseFailure;

        Assert.That(
            () => result.MapFailure(_ => null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("map"));
    }

    [TestCase("21", "", "value 42")]
    [TestCase("abc", "Parse.Invalid", "failure User.Invalid")]
    public void TapAndMapFailure_WhenChained_LogAndTranslateFailure(string text, string logged, string expected)
    {
        string log = string.Empty;

        string message = Parse(text)
            .TapFailure(failure => log = failure.Code)
            .MapFailure(failure => new Failure("User.Invalid", failure.Error))
            .Map(value => value * 2)
            .Match(value => $"value {value}", failure => $"failure {failure.Code}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(message, Is.EqualTo(expected));
            Assert.That(log, Is.EqualTo(logged));
        }
    }

    [Test]
    public void Ensure_OnSuccessThatHolds_ReturnsSameResult()
    {
        Result<int> result = 42;

        Result<int> ensured = result.Ensure(value => value > 0, NegativeFailure);

        Assert.That(ensured, Is.EqualTo(result));
    }

    [Test]
    public void Ensure_OnSuccessThatDoesNotHold_ReturnsFailure()
    {
        Result<int> result = -1;

        Result<int> ensured = result.Ensure(value => value > 0, NegativeFailure);

        Assert.That(FailureOf(ensured), Is.SameAs(NegativeFailure));
    }

    [Test]
    public void Ensure_OnFailure_ReturnsSameFailureWithoutCallingPredicate()
    {
        Result<int> result = ParseFailure;
        bool called = false;

        Result<int> ensured = result.Ensure(
            _ =>
            {
                called = true;
                return false;
            },
            NegativeFailure);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FailureOf(ensured), Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void Ensure_WithNullPredicate_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Ensure(null!, NegativeFailure),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("predicate"));
    }

    [Test]
    public void Ensure_WithNullFailure_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Ensure(value => value > 0, (Failure)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failure"));
    }

    [Test]
    public void EnsureWithFailureForValue_OnSuccessThatDoesNotHold_ReturnsFailureForValue()
    {
        Result<int> result = -1;

        Result<int> ensured = result.Ensure(
            value => value > 0,
            value => new Failure("Validate.Negative", $"{value} is negative."));

        Assert.That(FailureOf(ensured)?.Error, Is.EqualTo("-1 is negative."));
    }

    [Test]
    public void EnsureWithFailureForValue_OnSuccessThatHolds_ReturnsSameResultWithoutBuildingFailure()
    {
        Result<int> result = 42;
        bool called = false;

        Result<int> ensured = result.Ensure(
            value => value > 0,
            _ =>
            {
                called = true;
                return NegativeFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ensured, Is.EqualTo(result));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void EnsureWithFailureForValue_OnFailure_ReturnsSameFailureWithoutCallingPredicate()
    {
        Result<int> result = ParseFailure;
        bool called = false;

        Result<int> ensured = result.Ensure(
            _ =>
            {
                called = true;
                return false;
            },
            _ => NegativeFailure);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FailureOf(ensured), Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void EnsureWithFailureForValue_WithNullPredicate_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Ensure(null!, _ => NegativeFailure),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("predicate"));
    }

    [Test]
    public void EnsureWithFailureForValue_WithNullFailure_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Ensure(value => value > 0, (Func<int, Failure>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failure"));
    }

    [Test]
    public void EnsureWithFailureForValue_WhenFailureReturnsNull_ThrowsArgumentNullException()
    {
        Result<int> result = -1;

        Assert.That(
            () => result.Ensure(value => value > 0, _ => null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failure"));
    }

    private static Result<int> Parse(string text)
    {
        return int.TryParse(text, out int value) ? value : ParseFailure;
    }

    private static Result Validate(int value)
    {
        if (value < 0)
        {
            return NegativeFailure;
        }

        return Result.Success;
    }
}