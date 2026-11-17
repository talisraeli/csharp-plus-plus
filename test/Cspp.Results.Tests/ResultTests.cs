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
        Result result = Success.Instance;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.IsFailure, Is.False);
        }
    }

    [Test]
    public void ImplicitConversion_FromError_ReturnsFailureResult()
    {
        Result result = Error.Unknown;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.IsFailure, Is.True);
        }
    }

    [Test]
    public void TryGetValue_OnSuccess_ReturnsSuccessAndNoError()
    {
        Result result = Result.Success;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success? success), Is.True);
            Assert.That(success, Is.SameAs(Success.Instance));
            Assert.That(result.TryGetValue(out Error? error), Is.False);
            Assert.That(error, Is.Null);
        }
    }

    [Test]
    public void TryGetValue_OnFailure_ReturnsErrorAndNoSuccess()
    {
        Error inError = new("Test.Code", "Test message");
        Result result = inError;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TryGetValue(out Success? success), Is.False);
            Assert.That(success, Is.Null);
            Assert.That(result.TryGetValue(out Error? outError), Is.True);
            Assert.That(outError, Is.EqualTo(inError));
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
            Assert.That(result.Value, Is.EqualTo(Error.Unknown));
            Assert.That(result.TryGetValue(out Error? error), Is.True);
            Assert.That(error, Is.EqualTo(Error.Unknown));
        }
    }

    [Test]
    public void Switch_OnDefault_MatchesUnknownError()
    {
        Result result = default;

        string code = result switch
        {
            Success => "success",
            Error error => error.Code,
        };

        Assert.That(code, Is.EqualTo(Error.Unknown.Code));
    }

    [Test]
    public void Equals_SameError_ReturnsTrue()
    {
        Result first = Error.Unknown;
        Result second = Error.Unknown;

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
                Result result = Error.Unknown;
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
}