using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Result))]
public class ResultTests
{
    [Test]
    public void Success_WhenAccessed_ReturnsSuccessResult()
    {
        Result result = Result.Success;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
        }
    }

    [Test]
    public void ImplicitConversion_FromSuccess_ReturnsSuccessResult()
    {
        Result result = Result.Success;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
        }
    }

    [Test]
    public void ImplicitConversion_FromFailure_ReturnsFailureResult()
    {
        Result result = Failure.Unknown;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
        }
    }

    [Test]
    public void TryGetValue_OnSuccess_ReturnsSuccessAndNoFailure()
    {
        Result result = Result.Success;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success? success), Is.True);
            Assert.That(success, Is.SameAs(Result.Success));
            Assert.That(result.TryGetValue(out Failure? failure), Is.False);
            Assert.That(failure, Is.Null);
        }
    }

    [Test]
    public void TryGetValue_OnFailure_ReturnsFailureAndNoSuccess()
    {
        Failure inFailure = new("Test.Code", "Test error");
        Result result = inFailure;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success? success), Is.False);
            Assert.That(success, Is.Null);
            Assert.That(result.TryGetValue(out Failure? outFailure), Is.True);
            Assert.That(outFailure, Is.EqualTo(inFailure));
        }
    }

    [Test]
    public void Default_WhenCreated_IsUninitializedFailure()
    {
        Result result = default;

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
        Result result = default;

        string code = result switch
        {
            Success => "success",
            Failure failure => failure.Code,
        };

        Assert.That(code, Is.EqualTo(Failure.Uninitialized.Code));
    }

    [Test]
    public void Equals_SameFailure_ReturnsTrue()
    {
        Result first = Failure.Unknown;
        Result second = Failure.Unknown;

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void PatternMatch_OnSuccess_DoesNotAllocate()
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
    public void PatternMatch_OnFailure_DoesNotAllocate()
    {
        static int Run(int iterations)
        {
            var count = 0;
            for (var i = 0; i < iterations; i++)
            {
                Result result = Failure.Unknown;
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
}