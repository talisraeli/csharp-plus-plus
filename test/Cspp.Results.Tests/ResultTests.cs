using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Result))]
public class ResultTests
{
    [Test]
    public void ImplicitConversion_FromSuccess_ReturnsSuccessResult()
    {
        Result result = default(Success);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result is Success, Is.True);
        }
    }

    [Test]
    public void ImplicitConversion_FromFailure_ReturnsFailureResult()
    {
        Result result = new Failure(Error.Unknown);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result is Failure, Is.True);
        }
    }

    [Test]
    public void Success_WhenAccessed_IsSuccessResult()
    {
        Result result = Result.Success;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
            Assert.That(result is Success, Is.True);
        }
    }

    [Test]
    public void Failure_WhenAccessed_IsFailureWithUnknownError()
    {
        Result result = Result.Failure;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result is Failure, Is.True);
            Assert.That(((Failure)result.Value!).Error, Is.EqualTo(Error.Unknown));
        }
    }

    [Test]
    public void TryGetValue_FromSuccess_ReturnsTrue()
    {
        Result result = Result.Success;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success _), Is.True);
            Assert.That(result.TryGetValue(out Failure _), Is.False);
        }
    }

    [Test]
    public void TryGetValue_FromFailure_ReturnsFailureWithError()
    {
        var error = new Error("Test.Code", "Test message");
        Result result = new Failure(error);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success _), Is.False);
            Assert.That(result.TryGetValue(out Failure failure), Is.True);
            Assert.That(failure.Error, Is.EqualTo(error));
        }
    }

    [Test]
    public void Default_WhenCreated_IsFailureWithUnknownError()
    {
        Result result = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsFailure, Is.True);
            Assert.That(result.HasValue, Is.True);
            Assert.That(result is Failure { Error: var error } && error == Error.Unknown, Is.True);
        }
    }

    [Test]
    public void Equals_SameFailure_IsTrue()
    {
        Result first = new Failure(Error.Unknown);
        Result second = new Failure(Error.Unknown);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void Create_SuccessAndPatternMatch_DoesNotAllocate()
    {
        static int Run(int iterations)
        {
            var count = 0;
            for (var i = 0; i < iterations; i++)
            {
                Result result = Result.Success;
                if (result is Success && result.IsSuccess)
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
    public void Create_FailureAndPatternMatch_DoesNotAllocate()
    {
        static int Run(int iterations)
        {
            var count = 0;
            for (var i = 0; i < iterations; i++)
            {
                Result result = Result.Failure;
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