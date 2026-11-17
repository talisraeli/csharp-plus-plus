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
    public void ImplicitConversion_FromError_ReturnsFailureResult()
    {
        Result<int> result = Error.Unknown;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
        }
    }

    [Test]
    public void TryGetValue_OnSuccess_ReturnsValueAndNoError()
    {
        Result<int> result = 42;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
            Assert.That(result.TryGetValue(out Error? error), Is.False);
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public void TryGetValue_OnFailure_ReturnsErrorAndNoValue()
    {
        Error inError = new("Test.Code", "Test message");
        Result<string> result = inError;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out string? value), Is.False);
            Assert.That(value, Is.Null);
            Assert.That(result.TryGetValue(out Error? outError), Is.True);
            Assert.That(outError, Is.EqualTo(inError));
        }
    }

    [Test]
    public void Default_WhenCreated_IsFailureWithUninitializedError()
    {
        Result<int> result = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(Error.Uninitialized));
            Assert.That(result.TryGetValue(out Error? error), Is.True);
            Assert.That(error, Is.EqualTo(Error.Uninitialized));
        }
    }

    [Test]
    public void Switch_OnDefault_MatchesUninitializedError()
    {
        Result<int> result = default;

        string code = result switch
        {
            int value => $"success {value}",
            Error error => error.Code,
        };

        Assert.That(code, Is.EqualTo(Error.Uninitialized.Code));
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
                Result<int> result = Error.Unknown;
                if (result is Error && result.IsFailure)
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
            Assert.That(result.TryGetValue(out Error? error), Is.False);
            Assert.That(error, Is.Null);
        }
    }
}