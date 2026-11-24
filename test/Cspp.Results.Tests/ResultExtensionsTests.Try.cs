namespace Cspp.Results.Tests;

/// <content>
/// Tests for <c>Result.Try</c> and <c>Result.TryAsync</c>.
/// </content>
public partial class ResultExtensionsTests
{
    private static readonly Failure SaveFailure = new("Save.Failed", "Could not save.");

    [Test]
    public void Try_WhenActionSucceeds_ReturnsSuccessWithoutCallingOnException()
    {
        bool ran = false;
        bool called = false;

        Result result = Result.Try(
            () => { ran = true; },
            _ =>
            {
                called = true;
                return SaveFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ran, Is.True);
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void Try_WhenActionThrowsWithoutOnException_ReturnsExceptionFailure()
    {
        Result result = Result.Try(() => throw new InvalidOperationException());

        Assert.That(result, Is.EqualTo((Result)Failure.Exception));
    }

    [Test]
    public void Try_WhenActionThrows_ReturnsFailureFromOnException()
    {
        InvalidOperationException thrown = new("Boom.");
        Exception? caught = null;

        Result result = Result.Try(
            () => throw thrown,
            exception =>
            {
                caught = exception;
                return SaveFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo((Result)SaveFailure));
            Assert.That(caught, Is.SameAs(thrown));
        }
    }

    [Test]
    public void Try_WhenActionThrowsOperationCanceledException_ReturnsExceptionFailure()
    {
        Result result = Result.Try(() => throw new OperationCanceledException());

        Assert.That(result, Is.EqualTo((Result)Failure.Exception));
    }

    [Test]
    public void Try_WhenOnExceptionReturnsNull_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.Try(() => throw new InvalidOperationException(), _ => null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("onException"));
    }

    [Test]
    public void Try_WithNullAction_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.Try((Action)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("action"));
    }

    [Test]
    public void TryWithValue_WhenFuncReturns_ReturnsValueWithoutCallingOnException()
    {
        bool called = false;

        Result<int> result = Result.Try(
            () => int.Parse("42"),
            _ =>
            {
                called = true;
                return ParseFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetValueOrDefault(), Is.EqualTo(42));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public void TryWithValue_WhenFuncThrowsWithoutOnException_ReturnsExceptionFailure()
    {
        Result<int> result = Result.Try(() => int.Parse("abc"));

        Assert.That(result, Is.EqualTo((Result<int>)Failure.Exception));
    }

    [Test]
    public void TryWithValue_WhenFuncThrows_ReturnsFailureFromOnException()
    {
        Exception? caught = null;

        Result<int> result = Result.Try(
            () => int.Parse("abc"),
            exception =>
            {
                caught = exception;
                return ParseFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo((Result<int>)ParseFailure));
            Assert.That(caught, Is.TypeOf<FormatException>());
        }
    }

    [Test]
    public void TryWithValue_WhenFuncThrowsOperationCanceledException_ReturnsExceptionFailure()
    {
        Result<int> result = Result.Try<int>(() => throw new OperationCanceledException());

        Assert.That(result, Is.EqualTo((Result<int>)Failure.Exception));
    }

    [Test]
    public void TryWithValue_WithNullFunc_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.Try((Func<int>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("func"));
    }

    [Test]
    public async Task TryAsync_WhenFuncSucceeds_ReturnsSuccessWithoutCallingOnException()
    {
        bool called = false;

        Result result = await Result.TryAsync(
            async () => await Task.Yield(),
            _ =>
            {
                called = true;
                return SaveFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public async Task TryAsync_WhenFuncThrowsWithoutOnException_ReturnsExceptionFailure()
    {
        Result result = await Result.TryAsync(ThrowAfterYieldAsync);

        Assert.That(result, Is.EqualTo((Result)Failure.Exception));
    }

    [Test]
    public async Task TryAsync_WhenFuncThrows_ReturnsFailureFromOnException()
    {
        Exception? caught = null;

        Result result = await Result.TryAsync(
            ThrowAfterYieldAsync,
            exception =>
            {
                caught = exception;
                return SaveFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo((Result)SaveFailure));
            Assert.That(caught, Is.TypeOf<InvalidOperationException>());
        }
    }

    [Test]
    public async Task TryAsync_WhenFuncThrowsBeforeReturningTask_ReturnsExceptionFailure()
    {
        Result result = await Result.TryAsync(() => throw new InvalidOperationException());

        Assert.That(result, Is.EqualTo((Result)Failure.Exception));
    }

    [Test]
    public void TryAsync_WhenFuncThrowsOperationCanceledException_Propagates()
    {
        bool called = false;

        Assert.That(
            async () => await Result.TryAsync(
                CancelAfterYieldAsync,
                _ =>
                {
                    called = true;
                    return SaveFailure;
                }),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(called, Is.False);
    }

    [Test]
    public void TryAsync_WithNullFunc_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.TryAsync((Func<Task>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("func"));
    }

    [Test]
    public async Task TryAsyncWithValue_WhenFuncReturns_ReturnsValueWithoutCallingOnException()
    {
        bool called = false;

        Result<int> result = await Result.TryAsync(
            () => DoubleAsync(21),
            _ =>
            {
                called = true;
                return ParseFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.GetValueOrDefault(), Is.EqualTo(42));
            Assert.That(called, Is.False);
        }
    }

    [Test]
    public async Task TryAsyncWithValue_WhenFuncThrowsWithoutOnException_ReturnsExceptionFailure()
    {
        Result<int> result = await Result.TryAsync(ParseAfterYieldAsync);

        Assert.That(result, Is.EqualTo((Result<int>)Failure.Exception));
    }

    [Test]
    public async Task TryAsyncWithValue_WhenFuncThrows_ReturnsFailureFromOnException()
    {
        Exception? caught = null;

        Result<int> result = await Result.TryAsync(
            ParseAfterYieldAsync,
            exception =>
            {
                caught = exception;
                return ParseFailure;
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo((Result<int>)ParseFailure));
            Assert.That(caught, Is.TypeOf<FormatException>());
        }
    }

    [Test]
    public void TryAsyncWithValue_WhenFuncThrowsOperationCanceledException_Propagates()
    {
        Assert.That(
            async () => await Result.TryAsync(CancelWithValueAfterYieldAsync),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void TryAsyncWithValue_WithNullFunc_ThrowsArgumentNullException()
    {
        Assert.That(
            () => Result.TryAsync((Func<Task<int>>)null!),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("func"));
    }

    private static async Task ThrowAfterYieldAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("Boom.");
    }

    private static async Task CancelAfterYieldAsync()
    {
        await Task.Yield();
        throw new OperationCanceledException();
    }

    private static async Task<int> ParseAfterYieldAsync()
    {
        await Task.Yield();
        return int.Parse("abc");
    }

    private static async Task<int> CancelWithValueAfterYieldAsync()
    {
        await Task.Yield();
        throw new OperationCanceledException();
    }
}