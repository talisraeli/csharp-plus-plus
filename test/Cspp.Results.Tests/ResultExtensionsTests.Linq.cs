namespace Cspp.Results.Tests;

/// <content>
/// Tests for <c>Select</c> and <c>SelectMany</c>, and the query syntax that uses them.
/// </content>
public partial class ResultExtensionsTests
{
    [Test]
    public void Select_OnSuccess_ReturnsSelectedValue()
    {
        Result<int> selected = from value in Parse("21") select value * 2;

        Assert.That(selected.GetValueOrDefault(), Is.EqualTo(42));
    }

    [Test]
    public void Select_OnFailure_ReturnsSameFailureWithoutCallingSelector()
    {
        bool called = false;

        Result<int> selected = Parse("abc").Select(value =>
        {
            called = true;
            return value;
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FailureOf(selected), Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void Select_WithNullSelector_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.Select((Func<int, int>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("selector"));
    }

    [Test]
    public void SelectMany_WhenBothSucceed_ReturnsCombinedValue()
    {
        Result<int> sum =
            from first in Parse("20")
            from second in Parse("22")
            select first + second;

        Assert.That(sum.GetValueOrDefault(), Is.EqualTo(42));
    }

    [Test]
    public void SelectMany_WithThreeSources_ReturnsCombinedValue()
    {
        Result<string> joined =
            from first in Parse("1")
            from second in Parse("2")
            from third in Parse("3")
            select $"{first}{second}{third}";

        Assert.That(joined.GetValueOrDefault(), Is.EqualTo("123"));
    }

    [Test]
    public void SelectMany_WithLetClause_ReturnsCombinedValue()
    {
        Result<int> total =
            from first in Parse("20")
            let doubled = first * 2
            from second in Parse("2")
            select doubled + second;

        Assert.That(total.GetValueOrDefault(), Is.EqualTo(42));
    }

    [Test]
    public void SelectMany_OnFailure_ReturnsSameFailureWithoutCallingSelectors()
    {
        bool called = false;

        Result<int> combined = Parse("abc").SelectMany(
            value =>
            {
                called = true;
                return Parse("1");
            },
            (first, second) => first + second);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FailureOf(combined), Is.SameAs(ParseFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void SelectMany_WhenSecondStepFails_ReturnsItsFailureWithoutCallingResultSelector()
    {
        bool called = false;

        Result<int> combined = Parse("-1").SelectMany(
            Positive,
            (first, second) =>
            {
                called = true;
                return first + second;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FailureOf(combined), Is.SameAs(NegativeFailure));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void SelectMany_OnDefault_ReturnsUninitializedFailure()
    {
        Result<int> result = default;

        Result<int> combined = from first in result from second in Parse("1") select first + second;

        Assert.That(FailureOf(combined), Is.SameAs(Failure.Uninitialized));
    }

    [Test]
    public void SelectMany_WithNullSelector_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.SelectMany((Func<int, Result<int>>)null!, (first, second) => first + second),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("selector"));
    }

    [Test]
    public void SelectMany_WithNullResultSelector_ThrowsArgumentNullException()
    {
        Result<int> result = 42;

        Assert.That(
            () => result.SelectMany(Positive, (Func<int, int, int>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("resultSelector"));
    }
}