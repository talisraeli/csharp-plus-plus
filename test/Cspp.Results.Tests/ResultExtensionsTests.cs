using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(ResultExtensions))]
public class ResultExtensionsTests
{
    private static readonly Failure ParseFailure = new("Parse.Invalid", "Not a number.");

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

    private static Result<int> Parse(string text)
    {
        return int.TryParse(text, out int value) ? value : ParseFailure;
    }
}