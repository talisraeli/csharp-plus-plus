using JetBrains.Annotations;

namespace Cspp.Results.Tests;

[TestFixture]
[TestSubject(typeof(Result))]
public class ResultTests
{
    [Test]
    public void Success_WhenAccessedTwice_ReturnsSameInstance()
    {
        Success first = Result.Success;
        Success second = Result.Success;

        Assert.That(first, Is.SameAs(second));
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
    public void Constructor_WithNullFailure_ThrowsArgumentNullException()
    {
        Assert.That(
            () => new Result((Failure)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failure"));
    }

    [Test]
    public void ImplicitConversion_FromNullFailure_ThrowsArgumentNullException()
    {
        static Result Convert(Failure failure) => failure;

        Assert.That(
            () => Convert(null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("failure"));
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
    public void Switch_OnSuccess_MatchesSuccess()
    {
        Assert.That(Describe(Result.Success), Is.EqualTo("success"));
    }

    [Test]
    public void Switch_OnDefault_MatchesUninitializedFailure()
    {
        Assert.That(Describe(default), Is.EqualTo(Failure.Uninitialized.Code));
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
            int count = 0;
            for (int i = 0; i < iterations; i++)
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
        long before = GC.GetAllocatedBytesForCurrentThread();
        Run(10_000);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void PatternMatch_OnFailure_DoesNotAllocate()
    {
        static int Run(int iterations)
        {
            int count = 0;
            for (int i = 0; i < iterations; i++)
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
        long before = GC.GetAllocatedBytesForCurrentThread();
        Run(10_000);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void Value_OnSuccess_ReturnsSuccess()
    {
        Result result = Result.Success;

        Assert.That(result.Value, Is.SameAs(Result.Success));
    }

    [Test]
    public void Value_OnFailure_ReturnsFailure()
    {
        Result result = Failure.Unknown;

        Assert.That(result.Value, Is.SameAs(Failure.Unknown));
    }

    // One exhaustive switch shared by the switch tests, so each of its arms is reached by one of them.
    private static string Describe(Result result)
    {
        return result switch
        {
            Success => "success",
            Failure failure => failure.Code
        };
    }
}