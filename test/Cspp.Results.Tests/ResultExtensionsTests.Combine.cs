namespace Cspp.Results.Tests;

/// <content>
/// Tests for <c>Result.Combine</c> and <c>Result.CombineAll</c>.
/// </content>
public partial class ResultExtensionsTests
{
    [Test]
    public void Combine_AllSuccesses_ReturnsSuccess()
    {
        Result combined = Result.Combine(Result.Success, Result.Success);

        Assert.That(combined.IsSuccess, Is.True);
    }

    [Test]
    public void Combine_NoResults_ReturnsSuccess()
    {
        Result combined = Result.Combine(Array.Empty<Result>());

        Assert.That(combined.IsSuccess, Is.True);
    }

    [Test]
    public void Combine_SeveralFailures_ReturnsFirstFailure()
    {
        Result combined = Result.Combine(Result.Success, ParseFailure, NegativeFailure);

        Assert.That(combined, Is.EqualTo((Result)ParseFailure));
    }

    [Test]
    public void Combine_DefaultResult_ReturnsUninitializedFailure()
    {
        Result combined = Result.Combine(Result.Success, default(Result));

        Assert.That(combined, Is.EqualTo((Result)Failure.Uninitialized));
    }

    [Test]
    public void Combine_FailureFound_StopsReading()
    {
        int read = 0;

        IEnumerable<Result> Results()
        {
            read++;
            yield return ParseFailure;
            read++;
            yield return Result.Success;
        }

        Result.Combine(Results());

        Assert.That(read, Is.EqualTo(1));
    }

    [Test]
    public void Combine_WithNullResults_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.Combine((IEnumerable<Result>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("results"));
    }

    [Test]
    public void CombineWithValues_AllSuccesses_ReturnsValuesInOrder()
    {
        Result<IReadOnlyList<int>> combined = Result.Combine<int>(1, 2, 3);

        Assert.That(combined.GetValueOrDefault(), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void CombineWithValues_NoResults_ReturnsEmptyList()
    {
        Result<IReadOnlyList<int>> combined = Result.Combine(Array.Empty<Result<int>>());

        Assert.That(combined.GetValueOrDefault(), Is.Empty);
    }

    [Test]
    public void CombineWithValues_NullSuccessValue_KeepsIt()
    {
        Result<IReadOnlyList<string?>> combined = Result.Combine<string?>("a", (string?)null);

        Assert.That(combined.GetValueOrDefault(), Is.EqualTo(new[] { "a", null }));
    }

    [Test]
    public void CombineWithValues_SeveralFailures_ReturnsFirstFailure()
    {
        Result<IReadOnlyList<int>> combined = Result.Combine(Parse("1"), Parse("abc"), Parse("-1").Bind(Positive));

        Assert.That(FailureOf(combined), Is.SameAs(ParseFailure));
    }

    [Test]
    public void CombineWithValues_FailureFound_StopsReading()
    {
        int read = 0;

        IEnumerable<Result<int>> Results()
        {
            read++;
            yield return ParseFailure;
            read++;
            yield return 1;
        }

        Result.Combine(Results());

        Assert.That(read, Is.EqualTo(1));
    }

    [Test]
    public void CombineWithValues_WithNullResults_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.Combine((IEnumerable<Result<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("results"));
    }

    [Test]
    public void CombineAll_AllSuccesses_ReturnsSuccess()
    {
        Result combined = Result.CombineAll(Result.Success, Result.Success);

        Assert.That(combined.IsSuccess, Is.True);
    }

    [Test]
    public void CombineAll_OneFailure_ReturnsThatFailure()
    {
        Result combined = Result.CombineAll(Result.Success, ParseFailure);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(combined.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.SameAs(ParseFailure));
        }
    }

    [Test]
    public void CombineAll_SeveralFailures_ReturnsAggregateFailureInOrder()
    {
        Result combined = Result.CombineAll(ParseFailure, Result.Success, NegativeFailure);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(combined.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.TypeOf<AggregateFailure>());
            Assert.That(((AggregateFailure)failure!).Failures, Is.EqualTo(new[] { ParseFailure, NegativeFailure }));
        }
    }

    [Test]
    public void CombineAll_WithNullResults_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.CombineAll((IEnumerable<Result>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("results"));
    }

    [Test]
    public void CombineAllWithValues_AllSuccesses_ReturnsValuesInOrder()
    {
        Result<IReadOnlyList<int>> combined = Result.CombineAll(Parse("1"), Parse("2"));

        Assert.That(combined.GetValueOrDefault(), Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void CombineAllWithValues_OneFailure_ReturnsThatFailure()
    {
        Result<IReadOnlyList<int>> combined = Result.CombineAll(Parse("1"), Parse("abc"));

        Assert.That(FailureOf(combined), Is.SameAs(ParseFailure));
    }

    [Test]
    public void CombineAllWithValues_SeveralFailures_ReturnsAggregateFailureInOrder()
    {
        Result<IReadOnlyList<int>> combined =
            Result.CombineAll(Parse("abc"), Parse("1"), Parse("-1").Bind(Positive));

        Assert.That(
            FailureOf(combined),
            Is.EqualTo(new AggregateFailure([ParseFailure, NegativeFailure])));
    }

    [Test]
    public void CombineAllWithValues_WithNullResults_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.CombineAll((IEnumerable<Result<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("results"));
    }
}