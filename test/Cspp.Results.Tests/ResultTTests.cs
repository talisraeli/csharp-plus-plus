using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Result<>))]
public class ResultTTests
{
    [Test]
    public void ImplicitConversion_FromValue_ReturnsSuccessResult()
    {
        Result<int> result = 42;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
        }
    }

    [Test]
    public void ImplicitConversion_FromFailure_ReturnsFailureResult()
    {
        Result<int> result = Failure.Unknown;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
        }
    }

    [Test]
    public void TryGetValue_OnSuccess_ReturnsValueAndNoFailure()
    {
        Result<int> result = 42;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
            Assert.That(result.TryGetValue(out Failure? failure), Is.False);
            Assert.That(failure, Is.Null);
        }
    }

    [Test]
    public void TryGetValue_OnFailure_ReturnsFailureAndNoValue()
    {
        Failure inFailure = new("Test.Code", "Test error");
        Result<string> result = inFailure;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out string? value), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(result.TryGetValue(out Failure? outFailure), Is.True);
            Assert.That(outFailure, Is.EqualTo(inFailure));
        }
    }

    [Test]
    public void Constructor_WithNullFailure_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new Result<int>(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failure"));
    }

    [Test]
    public void ImplicitConversion_FromNullFailure_ThrowsArgumentNullException()
    {
        Assert.That(
            () =>
            {
                Result<int> result = (Failure)null!;
                return result;
            },
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failure"));
    }

    [Test]
    public void Default_WhenCreated_IsUninitializedFailure()
    {
        Result<int> result = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(Failure.Uninitialized));
            Assert.That(result.TryGetValue(out Failure? failure), Is.True);
            Assert.That(failure, Is.EqualTo(Failure.Uninitialized));
        }
    }

    [Test]
    public void Switch_OnDefault_MatchesUninitializedFailure()
    {
        Result<int> result = default;

        string code = result switch
        {
            int value => $"success {value}",
            Failure failure => failure.Code
        };

        Assert.That(code, Is.EqualTo(Failure.Uninitialized.Code));
    }

    [Test]
    public void Equals_SameValue_ReturnsTrue()
    {
        Result<int> first = 42;
        Result<int> second = 42;

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void PatternMatch_OnSuccess_DoesNotAllocate()
    {
        static long Run(int iterations)
        {
            long sum = 0;
            for (var i = 0; i < iterations; i++)
            {
                Result<int> result = i;
                if (result is int value && result.IsSuccess)
                {
                    sum += value;
                }
            }

            return sum;
        }

        Run(10);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Run(10_000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void PatternMatch_OnFailure_DoesNotAllocate()
    {
        static int Run(int iterations)
        {
            var count = 0;
            for (var i = 0; i < iterations; i++)
            {
                Result<int> result = Failure.Unknown;
                if (result is Failure && result.IsFailure)
                {
                    count++;
                }
            }

            return count;
        }

        Run(10);
        var before = GC.GetAllocatedBytesForCurrentThread();
        Run(10_000);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void ImplicitConversion_FromNullValue_ReturnsSuccessWithNull()
    {
        Result<string?> result = (string?)null;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.TryGetValue(out string? value), Is.True);
            Assert.That(value, Is.Null);
            Assert.That(result.TryGetValue(out Failure? failure), Is.False);
            Assert.That(failure, Is.Null);
        }
    }
}