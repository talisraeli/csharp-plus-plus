using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Result<>))]
public class ResultTTests
{
    [Test]
    public void ImplicitConversion_FromSuccess_ReturnsSuccessWithValue()
    {
        Result<int> result = new Success<int>(42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result is Success<int>, Is.True);
            Assert.That(((Success<int>)result.Value).Value, Is.EqualTo(42));
        }
    }

    [Test]
    public void ImplicitConversion_FromFailure_ReturnsFailureWithError()
    {
        Result<int> result = new Failure(Error.Unknown);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result is Failure, Is.True);
            Assert.That(((Failure)result.Value!).Error, Is.EqualTo(Error.Unknown));
        }
    }

    [Test]
    public void TryGetValue_FromSuccess_ReturnsValue()
    {
        Result<int> result = new Success<int>(42);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success<int> success), Is.True);
            Assert.That(success.Value, Is.EqualTo(42));
            Assert.That(result.TryGetValue(out Failure _), Is.False);
        }
    }

    [Test]
    public void TryGetValue_FromFailure_ReturnsFailureWithError()
    {
        var error = new Error("Test.Code", "Test message");
        Result<int> result = new Failure(error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success<int> _), Is.False);
            Assert.That(result.TryGetValue(out Failure failure), Is.True);
            Assert.That(failure.Error, Is.EqualTo(error));
        }
    }

    [Test]
    public void Default_WhenCreated_IsFailureWithUnknownError()
    {
        Result<int> result = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.HasValue, Is.True);
            Assert.That(result is Failure { Error: var error } && error == Error.Unknown, Is.True);
        }
    }

    [Test]
    public void Equals_SameSuccessValue_IsTrue()
    {
        Result<int> first = new Success<int>(42);
        Result<int> second = new Success<int>(42);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void Create_FromValueAndPatternMatch_DoesNotAllocate()
    {
        static long Run(int iterations)
        {
            long sum = 0;
            for (var i = 0; i < iterations; i++)
            {
                Result<int> result = Result.FromValue(i);
                if (result is Success<int> success && result.IsSuccess)
                {
                    sum += success.Value;
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
    public void Create_FromErrorAndPatternMatch_DoesNotAllocate()
    {
        static int Run(int iterations)
        {
            var count = 0;
            for (var i = 0; i < iterations; i++)
            {
                Result<int> result = Result.FromError(Error.Unknown);
                if (result is Failure { Error: not null } && result.IsFailure)
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
}